using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;

public sealed class PassengerInspection : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PassengerInspectUI inspectUI;
    [SerializeField] private PassengerQuestionUI questionUI;
    [SerializeField] private BusFareUI fareUI;
    [SerializeField] private InspectionPanelSwitcher panelSwitcher;
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
    [SerializeField] private bool enableNumberHotkeys = true;

    private readonly List<int> selectedChangeDenominations = new List<int>();
    private Passenger current;
    private string openingLine = string.Empty;
    private bool questionTabOpenedOnce;

    public Passenger Current => current;
    public bool IsInspecting => current != null;

    private void Awake()
    {
        if (inspectUI == null) inspectUI = FindSceneObject<PassengerInspectUI>();
        if (questionUI == null) questionUI = FindSceneObject<PassengerQuestionUI>();
        if (fareUI == null) fareUI = FindSceneObject<BusFareUI>();
        if (panelSwitcher == null) panelSwitcher = FindSceneObject<InspectionPanelSwitcher>();
        if (stopGate == null) stopGate = FindFirstObjectByType<StopGate>();
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
        if (cabinPeek == null) cabinPeek = FindFirstObjectByType<CabinPeek>();
        if (queueManager == null) queueManager = FindFirstObjectByType<QueueManagerNodes>();
        if (driverWallet == null) driverWallet = FindFirstObjectByType<DriverWallet>();
        if (fareTable == null) fareTable = FindFirstObjectByType<FareTable>();
    }

    private void Update()
    {
        if (!IsInspecting)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClearInspectionAndResumeLook();
            return;
        }

        if (!enableNumberHotkeys || Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            OpenIdTab();

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            OpenQuestionsTab();

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            OpenFareTab();
    }

    public void Inspect(Passenger passenger)
    {
        if (passenger == null)
            return;

        current = passenger;
        openingLine = passenger.GetOpeningStatement();
        questionTabOpenedOnce = false;
        selectedChangeDenominations.Clear();

        FreezePassengerMovement(passenger);

        if (autoRegisterPassengerOnInspect && !passenger.HasBeenProcessed)
            stopGate?.Register(passenger);

        inspectUI?.Show(passenger);
        questionUI?.Clear();
        fareUI?.Hide();
        panelSwitcher?.Show(InspectionPanelType.Id);

        EnterInspectionMode();
    }

    public void ClearInspection()
    {
        current = null;
        openingLine = string.Empty;
        questionTabOpenedOnce = false;
        selectedChangeDenominations.Clear();
        inspectUI?.Hide();
        questionUI?.Clear();
        fareUI?.Hide();
        panelSwitcher?.HideAll();
    }

    public void RegisterPendingPassenger(Passenger passenger)
    {
        if (passenger == null)
            return;

        stopGate?.Register(passenger);
    }

    public void OpenIdTab()
    {
        if (current == null)
            return;

        panelSwitcher?.Show(InspectionPanelType.Id);
        inspectUI?.Show(current);
    }

    public void OpenQuestionsTab()
    {
        if (current == null)
            return;

        panelSwitcher?.Show(InspectionPanelType.Questions);

        if (!questionTabOpenedOnce)
        {
            questionUI?.Show("Passenger", openingLine);
            questionTabOpenedOnce = true;
        }
        else
        {
            questionUI?.ActivateAndReplayLast();
        }
    }

    public void OpenFareTab()
    {
        if (current == null)
            return;

        panelSwitcher?.Show(InspectionPanelType.Fare);
        fareUI?.Show(current, driverWallet, fareTable);
        fareUI?.Refresh(selectedChangeDenominations);
    }

    public void AskCurrentQuestion(PassengerQuestionType questionType)
    {
        if (current == null)
            return;

        panelSwitcher?.Show(InspectionPanelType.Questions);
        questionTabOpenedOnce = true;

        string prompt = questionType switch
        {
            PassengerQuestionType.CurrentStop => "What stop is this?",
            PassengerQuestionType.DestinationStop => "Where are you getting off?",
            PassengerQuestionType.Seat => "Which seat is yours?",
            PassengerQuestionType.Fare => "How are you paying?",
            _ => "Question"
        };

        string answer = current.GetAnswer(questionType);
        questionUI?.Show(prompt, answer);
    }

    public void AddChangeDenomination(int denominationPence)
    {
        if (current == null || denominationPence <= 0)
            return;

        selectedChangeDenominations.Add(denominationPence);
        fareUI?.Refresh(selectedChangeDenominations);
    }

    public void RemoveLastChangeDenomination()
    {
        if (selectedChangeDenominations.Count <= 0)
            return;

        selectedChangeDenominations.RemoveAt(selectedChangeDenominations.Count - 1);
        fareUI?.Refresh(selectedChangeDenominations);
    }

    public void ClearSelectedChange()
    {
        selectedChangeDenominations.Clear();
        fareUI?.Refresh(selectedChangeDenominations);
    }

    public void AutoSelectCorrectChange()
    {
        if (current == null)
            return;

        selectedChangeDenominations.Clear();

        if (!current.UsesCash || current.ChangeDuePence <= 0)
        {
            fareUI?.Refresh(selectedChangeDenominations);
            return;
        }

        if (driverWallet == null)
        {
            AskPaymentMessage("No driver wallet found in the scene.");
            fareUI?.Refresh(selectedChangeDenominations);
            return;
        }

        if (driverWallet.TryPreviewExactChangeAfterTender(current.GetTenderedDenominationsCopy(), current.ChangeDuePence, out List<int> plan))
        {
            selectedChangeDenominations.AddRange(plan);
        }
        else
        {
            AskPaymentMessage("You can't make the exact change from the current float.");
        }

        fareUI?.Refresh(selectedChangeDenominations);
    }

    public void AcceptCurrent()
    {
        if (current == null)
            return;

        bool fareCorrect;
        if (!TryResolveFare(out fareCorrect, out string paymentFailure))
        {
            AskPaymentMessage(paymentFailure);
            fareUI?.Refresh(selectedChangeDenominations);
            return;
        }

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

    private bool TryResolveFare(out bool fareCorrect, out string paymentFailure)
    {
        fareCorrect = false;
        paymentFailure = string.Empty;

        if (current == null)
        {
            paymentFailure = "No passenger selected.";
            return false;
        }

        if (current.UsesDayRider)
        {
            fareCorrect = current.IsDayRiderValid;
            return true;
        }

        if (driverWallet == null)
        {
            paymentFailure = "No DriverWallet assigned. Add one to the scene first.";
            return false;
        }

        if (!driverWallet.TryApplyCashTransaction(current.GetTenderedDenominationsCopy(), selectedChangeDenominations, out paymentFailure))
            return false;

        int selectedChange = GetSelectedChangeTotal();
        fareCorrect = current.CashTenderedPence >= current.ExpectedFare && selectedChange == current.ChangeDuePence;
        return true;
    }

    private int GetSelectedChangeTotal()
    {
        int total = 0;
        for (int i = 0; i < selectedChangeDenominations.Count; i++)
            total += Mathf.Max(0, selectedChangeDenominations[i]);
        return total;
    }

    private void ResolveAndClose()
    {
        if (current != null)
            stopGate?.Resolve(current);

        current = null;
        openingLine = string.Empty;
        questionTabOpenedOnce = false;
        selectedChangeDenominations.Clear();
        inspectUI?.Hide();
        questionUI?.Clear();
        fareUI?.Hide();
        panelSwitcher?.HideAll();

        ExitInspectionMode();
    }

    private void ClearInspectionAndResumeLook()
    {
        current = null;
        openingLine = string.Empty;
        questionTabOpenedOnce = false;
        selectedChangeDenominations.Clear();
        inspectUI?.Hide();
        questionUI?.Clear();
        fareUI?.Hide();
        panelSwitcher?.HideAll();
        ExitInspectionMode();
    }

    private void AskPaymentMessage(string message)
    {
        panelSwitcher?.Show(InspectionPanelType.Questions);
        questionTabOpenedOnce = true;
        questionUI?.Show("Payment", message);
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

    private static T FindSceneObject<T>() where T : Component
    {
        T active = FindFirstObjectByType<T>();
        if (active != null)
            return active;

        T[] all = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < all.Length; i++)
        {
            T item = all[i];
            if (item == null || item.gameObject == null)
                continue;

            if (!item.gameObject.scene.IsValid())
                continue;

            return item;
        }

        return null;
    }
}
