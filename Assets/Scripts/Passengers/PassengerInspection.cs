using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PassengerInspection : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PassengerInspectUI inspectUI;
    [SerializeField] private PassengerQuestionUI questionUI;
    [SerializeField] private StopGate stopGate;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private CabinPeek cabinPeek;
    [SerializeField] private QueueManagerNodes queueManager;

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

    private Passenger current;

    public Passenger Current => current;
    public bool IsInspecting => current != null;

    private void Awake()
    {
        if (inspectUI == null) inspectUI = FindFirstObjectByType<PassengerInspectUI>();
        if (questionUI == null) questionUI = FindFirstObjectByType<PassengerQuestionUI>();
        if (stopGate == null) stopGate = FindFirstObjectByType<StopGate>();
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
        if (cabinPeek == null) cabinPeek = FindFirstObjectByType<CabinPeek>();
        if (queueManager == null) queueManager = FindFirstObjectByType<QueueManagerNodes>();
    }

    private void Update()
    {
        if (!IsInspecting)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ClearInspectionAndResumeLook();
    }

    public void Inspect(Passenger passenger)
    {
        if (passenger == null)
            return;

        current = passenger;
        FreezePassengerMovement(passenger);

        if (autoRegisterPassengerOnInspect && !passenger.HasBeenProcessed)
            stopGate?.Register(passenger);

        inspectUI?.Show(passenger);
        questionUI?.Show("Passenger", passenger.GetOpeningStatement());

        EnterInspectionMode();
    }

    public void ClearInspection()
    {
        current = null;
        inspectUI?.Hide();
        questionUI?.Clear();
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

        string prompt = questionType switch
        {
            PassengerQuestionType.BoardingStop => "Where did you get on?",
            PassengerQuestionType.DestinationStop => "Where are you getting off?",
            PassengerQuestionType.Seat => "Which seat is yours?",
            PassengerQuestionType.Fare => "How much did you pay?",
            _ => "Question"
        };

        string answer = current.GetAnswer(questionType);
        questionUI?.Show(prompt, answer);
    }

    public void AcceptCurrent()
    {
        if (current == null)
            return;

        bool fareCorrect = current.PaidAmount == current.ExpectedFare;

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

        ResolveAndClose();
    }

    public void RejectCurrent()
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

        ResolveAndClose();
        Destroy(target.gameObject);
    }

    private void ResolveAndClose()
    {
        if (current != null)
            stopGate?.Resolve(current);

        current = null;
        inspectUI?.Hide();
        questionUI?.Clear();

        ExitInspectionMode();
    }

    private void ClearInspectionAndResumeLook()
    {
        current = null;
        inspectUI?.Hide();
        questionUI?.Clear();
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
            {
                Debug.Log($"Passenger seated: {passenger.PassengerName} -> {seat.name}");
                return true;
            }
        }

        Debug.LogWarning($"No free seat found for {passenger.PassengerName}");
        return false;
    }
}