using UnityEngine;

public sealed class PassengerInspection : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PassengerInspectUI inspectUI;
    [SerializeField] private PassengerQuestionUI questionUI;
    [SerializeField] private StopGate stopGate;
    [SerializeField] private ScoreManager scoreManager;

    [Header("Scoring")]
    [SerializeField] private int correctHumanAccept = 5;
    [SerializeField] private int correctAnomalyReject = 15;
    [SerializeField] private int wrongHumanReject = -10;
    [SerializeField] private int wrongAnomalyAccept = -20;
    [SerializeField] private int correctFareBonus = 2;
    [SerializeField] private int wrongFarePenalty = -2;

    [Header("Behaviour")]
    [SerializeField] private bool autoRegisterPassengerOnInspect = true;

    private Passenger current;

    public Passenger Current => current;

    private void Awake()
    {
        if (inspectUI == null) inspectUI = FindFirstObjectByType<PassengerInspectUI>();
        if (questionUI == null) questionUI = FindFirstObjectByType<PassengerQuestionUI>();
        if (stopGate == null) stopGate = FindFirstObjectByType<StopGate>();
        if (scoreManager == null) scoreManager = FindFirstObjectByType<ScoreManager>();
    }

    public void Inspect(Passenger passenger)
    {
        if (passenger == null)
            return;

        current = passenger;

        if (autoRegisterPassengerOnInspect)
            stopGate?.Register(passenger);

        inspectUI?.Show(passenger);
        questionUI?.Clear();
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
            PassengerQuestionType.DestinationStop => "How many stops left?",
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

        ResolveCurrent(keepPassenger: true);
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

        if (SeatManager.Instance != null)
            SeatManager.Instance.NotifyPassengerRemoved(target);

        ResolveCurrent(keepPassenger: false);

        Destroy(target.gameObject);
    }

    private void ResolveCurrent(bool keepPassenger)
    {
        if (current != null)
            stopGate?.Resolve(current);

        if (!keepPassenger)
            current = null;

        inspectUI?.Hide();
        questionUI?.Clear();

        if (keepPassenger)
            current = null;
    }
}