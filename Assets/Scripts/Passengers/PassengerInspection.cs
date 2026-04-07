using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PassengerInspection : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InspectionDeskUI deskUI;
    [SerializeField] private SeatedRequestionUI seatedRequestionUI;
    [SerializeField] private StopGate stopGate;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private CabinPeek cabinPeek;
    [SerializeField] private QueueManagerNodes queueManager;
    [SerializeField] private DriverWallet driverWallet;
    [SerializeField] private FareTable fareTable;

    [Header("Scoring")]
    [SerializeField] private int correctHumanAccept = 5;
    [SerializeField] private int correctAnomalyReject = 15;
    [SerializeField] private int wrongHumanReject = -10;
    [SerializeField] private int wrongAnomalyAccept = -20;
    [SerializeField] private int correctFareBonus = 2;
    [SerializeField] private int wrongFarePenalty = -2;

    [Header("Behaviour")]
    [SerializeField] private bool autoRegisterPassengerOnInspect = true;
    [SerializeField] private bool seatPassengerOnAccept = true;
    [SerializeField, Range(0f, 1f)] private float rareReturnReminderChance = 0.15f;

    private Passenger current;
    private bool viewOnlyPaused;
    private bool inSeatedMode;

    public Passenger Current => current;
    public bool HasOpenSession => current != null;

    private void Awake()
    {
        if (deskUI == null) deskUI = FindFirstObjectByType<InspectionDeskUI>(FindObjectsInactive.Include);
        if (seatedRequestionUI == null) seatedRequestionUI = FindFirstObjectByType<SeatedRequestionUI>(FindObjectsInactive.Include);
        if (stopGate == null) stopGate = FindFirstObjectByType<StopGate>();
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
        if (cabinPeek == null) cabinPeek = FindFirstObjectByType<CabinPeek>();
        if (queueManager == null) queueManager = FindFirstObjectByType<QueueManagerNodes>();
        if (driverWallet == null) driverWallet = FindFirstObjectByType<DriverWallet>();
        if (fareTable == null) fareTable = FindFirstObjectByType<FareTable>();

        if (deskUI != null)
        {
            deskUI.SeatRequested += AcceptCurrent;
            deskUI.DenyRequested += DenyCurrent;
        }

        if (seatedRequestionUI != null)
        {
            seatedRequestionUI.PassengerKickedOff += HandlePassengerKickedOff;
            seatedRequestionUI.Closed += HandleSeatedUIClosed;
        }
    }

    private void OnDestroy()
    {
        if (deskUI != null)
        {
            deskUI.SeatRequested -= AcceptCurrent;
            deskUI.DenyRequested -= DenyCurrent;
        }

        if (seatedRequestionUI != null)
        {
            seatedRequestionUI.PassengerKickedOff -= HandlePassengerKickedOff;
            seatedRequestionUI.Closed -= HandleSeatedUIClosed;
        }
    }

    private void Update()
    {
        if (current == null || viewOnlyPaused)
            return;

        // SeatedRequestionUI handles its own ESC close.
        if (inSeatedMode)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            PauseInspectionAndResumeLook();
    }

    public void Inspect(Passenger passenger)
    {
        if (passenger == null)
            return;

        // Clicking the same passenger again should just reopen the right UI.
        if (current != null && passenger == current)
        {
            viewOnlyPaused = false;

            if (inSeatedMode)
            {
                seatedRequestionUI?.Show(passenger);
                EnterInspectionMode();
            }
            else
            {
                deskUI?.ShowExistingSession();
                EnterInspectionMode();
            }

            return;
        }

        // Do not allow switching targets mid session.
        if (current != null && passenger != current)
            return;

        current = passenger;
        viewOnlyPaused = false;

        // Seated passengers should never open the desk UI.
        if (passenger.IsSeatedPassenger)
        {
            inSeatedMode = true;
            deskUI?.HideViewOnly();
            seatedRequestionUI?.Show(passenger);
            EnterInspectionMode();
            return;
        }

        inSeatedMode = false;

        FreezePassengerMovement(passenger);

        if (autoRegisterPassengerOnInspect && !passenger.HasBeenProcessed)
            stopGate?.Register(passenger);

        deskUI?.Open(passenger, driverWallet, fareTable);
        EnterInspectionMode();
    }

    public void ClearInspection()
    {
        current = null;
        viewOnlyPaused = false;
        inSeatedMode = false;

        deskUI?.CloseAndForgetCurrent(false);
        seatedRequestionUI?.Hide();
    }

    public void RegisterPendingPassenger(Passenger passenger)
    {
        if (passenger == null)
            return;

        stopGate?.Register(passenger);
    }

    public void AskCurrentQuestion(PassengerQuestionType questionType)
    {
        if (current == null)
            return;

        if (inSeatedMode)
            seatedRequestionUI?.AskLegacyQuestion(questionType);
        else
            deskUI?.AskLegacyQuestion(questionType);
    }

    public void AcceptCurrent()
    {
        if (current == null)
            return;

        // Seated passengers are not accepted through the boarding desk flow.
        if (inSeatedMode)
            return;

        int notReturnedPassengerItems = deskUI != null ? deskUI.GetImportantPassengerItemsOutsideSharedTrayCount() : 0;
        int notReturnedIssuedTickets = deskUI != null ? deskUI.GetImportantIssuedTicketsOutsideSharedTrayCount() : 0;

        if (notReturnedPassengerItems > 0 && Random.value < rareReturnReminderChance)
        {
            deskUI?.Say("Hang on - you still have my stuff.");
            return;
        }

        if (notReturnedIssuedTickets > 0)
        {
            deskUI?.Say("You need to hand me the ticket first.");
            return;
        }

        bool fareCorrect = deskUI == null || deskUI.IsFareCorrect();

        if (current.IsAnomaly)
            scoreManager?.Add(wrongAnomalyAccept);
        else
            scoreManager?.Add(correctHumanAccept);

        scoreManager?.Add(fareCorrect ? correctFareBonus : wrongFarePenalty);

        bool seated = false;
        if (seatPassengerOnAccept)
        {
            RemovePassengerFromQueue(current);
            seated = TrySeatPassenger(current);
        }

        current.MarkProcessed(seated);
        ResolveAndClose(true);
    }

    public void DenyCurrent()
    {
        if (current == null)
            return;

        Passenger target = current;

        if (target.IsAnomaly)
            scoreManager?.Add(correctAnomalyReject);
        else
            scoreManager?.Add(wrongHumanReject);

        RemovePassengerFromQueue(target);
        target.MarkProcessed(false);

        if (SeatManager.Instance != null)
            SeatManager.Instance.NotifyPassengerRemoved(target);

        ResolveAndClose(false);
        Destroy(target.gameObject);
    }

    private void HandlePassengerKickedOff(Passenger passenger)
    {
        if (passenger == null)
            return;

        if (current == passenger)
        {
            stopGate?.Resolve(passenger);
            current = null;
            viewOnlyPaused = false;
            inSeatedMode = false;
            ExitInspectionMode();
        }
    }

    private void HandleSeatedUIClosed()
    {
        if (!inSeatedMode)
            return;

        current = null;
        viewOnlyPaused = false;
        inSeatedMode = false;
        ExitInspectionMode();
    }

    private void ResolveAndClose(bool accepted)
    {
        if (current != null)
            stopGate?.Resolve(current);

        deskUI?.CloseAndForgetCurrent(accepted);
        seatedRequestionUI?.Hide();

        current = null;
        viewOnlyPaused = false;
        inSeatedMode = false;
        ExitInspectionMode();
    }

    private void PauseInspectionAndResumeLook()
    {
        viewOnlyPaused = true;

        if (inSeatedMode)
            seatedRequestionUI?.Hide();
        else
            deskUI?.HideViewOnly();

        current = null;
        inSeatedMode = false;
        ExitInspectionMode();
    }

    private void EnterInspectionMode()
    {
        if (cabinPeek == null)
            return;

        cabinPeek.SetLookEnabled(false);
        cabinPeek.SetCursorLocked(false);
    }

    private void ExitInspectionMode()
    {
        if (cabinPeek == null)
            return;

        cabinPeek.SetCursorLocked(true);
        cabinPeek.SetLookEnabled(true);
    }

    private void FreezePassengerMovement(Passenger passenger)
    {
        if (passenger == null)
            return;

        PassengerJoinQueue joinQueue = passenger.GetComponent<PassengerJoinQueue>();
        if (joinQueue != null)
            joinQueue.enabled = false;

        NodeQueueWalker walker = passenger.GetComponent<NodeQueueWalker>();
        if (walker != null)
            walker.StopMoving();
    }

    private void RemovePassengerFromQueue(Passenger passenger)
    {
        if (passenger == null)
            return;

        if (queueManager != null)
            queueManager.Remove(passenger);

        passenger.RemoveFromQueueAndStopMovement();
    }

    private bool TrySeatPassenger(Passenger passenger)
    {
        if (passenger == null || SeatManager.Instance == null)
            return false;

        if (SeatManager.Instance.GetSeatForPassenger(passenger) != null)
            return true;

        for (int i = 0; i < SeatManager.Instance.SeatCount; i++)
        {
            SeatAnchor seat = SeatManager.Instance.GetSeat(i);
            if (seat == null)
                continue;

            if (!SeatManager.Instance.IsSeatFree(seat))
                continue;

            bool moved = SeatManager.Instance.TryTeleportToSeat(passenger, seat, false);
            if (moved)
                return true;
        }

        return false;
    }
}