using UnityEngine;

public enum PassengerIdVisual
{
    Real,
    ObviousFake,
    FakeAlt1,
    FakeAlt2,
    FakeAlt3
}

public enum PassengerQuestionType
{
    BoardingStop,
    DestinationStop,
    Seat,
    Fare
}

public class Passenger : MonoBehaviour
{
    public enum StopInfoAccuracy
    {
        Correct,
        Incorrect,
        Unknown,
        AccidentalMistake,
        IntentionalLie
    }

    [Header("Identity")]
    [SerializeField] private string passengerName;

    [Header("Anomaly Flag (hidden from player)")]
    [SerializeField] private bool isAnomaly;

    [Header("Stops")]
    [SerializeField] private int dropOffStopIndex = -1;

    // Spawner-provided stop info
    [SerializeField] private int stopsInfoA; // usually "stops remaining"
    [SerializeField] private int stopsInfoB; // usually "boarded X stops ago" or related second stop value
    [SerializeField] private StopInfoAccuracy stopsInfoAccuracy = StopInfoAccuracy.Unknown;

    [Header("Observation (for gaze/anomaly system)")]
    public Transform Head;
    public bool IsObserved { get; internal set; }

    [Header("Inspection")]
    [SerializeField] private PassengerIdVisual idVisual = PassengerIdVisual.Real;
    [SerializeField] private bool debugObservation;

    private bool lastObserved;

    public bool IsAnomaly => isAnomaly;
    public int DropOffStopIndex => dropOffStopIndex;
    public string PassengerName => passengerName;

    public int StopsInfoA => stopsInfoA;
    public int StopsInfoB => stopsInfoB;
    public StopInfoAccuracy StopsAccuracy => stopsInfoAccuracy;
    public int ClaimedStopsRemaining => stopsInfoA;

    public PassengerIdVisual IdVisual => idVisual;

    public int ExpectedFare { get; private set; }
    public int PaidAmount { get; private set; }

    private void OnEnable()
    {
        PassengerRegistry.Register(this);
    }

    private void OnDisable()
    {
        PassengerRegistry.Unregister(this);
    }

    private void Awake()
    {
        if (Head == null)
            Head = transform;
    }

    private void Update()
    {
        if (!debugObservation)
            return;

        if (IsObserved != lastObserved)
        {
            Debug.Log($"{PassengerName} observed = {IsObserved}");
            lastObserved = IsObserved;
        }
    }

    public void SetPassengerName(string newName) => passengerName = newName;
    public void SetDropOffStopIndex(int index) => dropOffStopIndex = index;

    // Matches PassengerSpawner.cs call shape: (int, int, StopInfoAccuracy)
    public void SetStopsInfo(int valueA, int valueB, StopInfoAccuracy accuracy)
    {
        stopsInfoA = valueA;
        stopsInfoB = valueB;
        stopsInfoAccuracy = accuracy;
    }

    public void SetIsAnomaly(bool value) => isAnomaly = value;

    public void SetFare(int expected, int paid)
    {
        ExpectedFare = Mathf.Max(0, expected);
        PaidAmount = Mathf.Max(0, paid);
    }

    public void SetIdVisual(PassengerIdVisual visual)
    {
        idVisual = visual;
    }

    public string GetAnswer(PassengerQuestionType questionType)
    {
        AnomalyController anomalyController = GetComponent<AnomalyController>();
        AnomalySkill anomalySkill = anomalyController != null ? anomalyController.Skill : AnomalySkill.Mid;

        if (!IsAnomaly)
            return GetHumanAnswer(questionType);

        return GetAnomalyAnswer(questionType, anomalySkill);
    }

    private string GetHumanAnswer(PassengerQuestionType questionType)
    {
        switch (questionType)
        {
            case PassengerQuestionType.BoardingStop:
                return FormatBoardingAnswer(GetDisplayedBoardingStopsAgo());

            case PassengerQuestionType.DestinationStop:
                return FormatDestinationAnswer(GetDisplayedStopsRemaining());

            case PassengerQuestionType.Seat:
                return FormatSeatAnswer(GetActualSeatName());

            case PassengerQuestionType.Fare:
                return $"I paid {PaidAmount}.";

            default:
                return "...";
        }
    }

    private string GetAnomalyAnswer(PassengerQuestionType questionType, AnomalySkill skill)
    {
        switch (skill)
        {
            case AnomalySkill.Low:
                return GetLowSkillAnomalyAnswer(questionType);

            case AnomalySkill.Mid:
                return GetMidSkillAnomalyAnswer(questionType);

            case AnomalySkill.High:
                return GetHighSkillAnomalyAnswer(questionType);

            default:
                return GetMidSkillAnomalyAnswer(questionType);
        }
    }

    private string GetLowSkillAnomalyAnswer(PassengerQuestionType questionType)
    {
        switch (questionType)
        {
            case PassengerQuestionType.BoardingStop:
                return $"I got on... here. Or maybe {Mathf.Max(0, StopsInfoB + 2)} stops ago.";

            case PassengerQuestionType.DestinationStop:
                return $"I'm getting off in {Mathf.Max(0, ClaimedStopsRemaining + 2)} stops.";

            case PassengerQuestionType.Seat:
                return "I was just about to sit down.";

            case PassengerQuestionType.Fare:
                return $"I paid {Mathf.Max(0, PaidAmount + 2)}.";

            default:
                return "...";
        }
    }

    private string GetMidSkillAnomalyAnswer(PassengerQuestionType questionType)
    {
        bool subtleSlip = StableBool(17);

        switch (questionType)
        {
            case PassengerQuestionType.BoardingStop:
                return subtleSlip
                    ? FormatBoardingAnswer(Mathf.Max(0, StopsInfoB + 1))
                    : $"Uh... {FormatBoardingAnswer(StopsInfoB)}";

            case PassengerQuestionType.DestinationStop:
                return subtleSlip
                    ? FormatDestinationAnswer(Mathf.Max(0, ClaimedStopsRemaining + 1))
                    : $"I think {ClaimedStopsRemaining} stop(s).";

            case PassengerQuestionType.Seat:
                {
                    string actualSeat = GetActualSeatName();
                    if (string.IsNullOrEmpty(actualSeat))
                        return "I don't have a seat yet.";

                    return subtleSlip ? "Somewhere near the middle." : $"Seat {actualSeat}.";
                }

            case PassengerQuestionType.Fare:
                return subtleSlip
                    ? $"I paid {Mathf.Max(0, PaidAmount + 1)}."
                    : $"I paid {PaidAmount}.";

            default:
                return "...";
        }
    }

    private string GetHighSkillAnomalyAnswer(PassengerQuestionType questionType)
    {
        bool verySmallTell = StableBool(31);

        switch (questionType)
        {
            case PassengerQuestionType.BoardingStop:
                return verySmallTell
                    ? $"I got on {StopsInfoB} stop(s) ago, I think."
                    : FormatBoardingAnswer(StopsInfoB);

            case PassengerQuestionType.DestinationStop:
                return verySmallTell
                    ? $"I should be getting off in {ClaimedStopsRemaining} stop(s)."
                    : FormatDestinationAnswer(ClaimedStopsRemaining);

            case PassengerQuestionType.Seat:
                {
                    string actualSeat = GetActualSeatName();
                    if (string.IsNullOrEmpty(actualSeat))
                        return "I don't have a seat yet.";

                    return verySmallTell ? $"I think it's seat {actualSeat}." : $"Seat {actualSeat}.";
                }

            case PassengerQuestionType.Fare:
                return verySmallTell
                    ? $"Pretty sure I paid {PaidAmount}."
                    : $"I paid {PaidAmount}.";

            default:
                return "...";
        }
    }

    private int GetDisplayedBoardingStopsAgo()
    {
        return stopsInfoAccuracy switch
        {
            StopInfoAccuracy.Correct => StopsInfoB,
            StopInfoAccuracy.Incorrect => Mathf.Max(0, StopsInfoB + 1),
            StopInfoAccuracy.Unknown => StopsInfoB,
            StopInfoAccuracy.AccidentalMistake => Mathf.Max(0, StopsInfoB + (StableBool(5) ? 1 : 0)),
            StopInfoAccuracy.IntentionalLie => Mathf.Max(0, StopsInfoB + 1),
            _ => StopsInfoB
        };
    }

    private int GetDisplayedStopsRemaining()
    {
        return stopsInfoAccuracy switch
        {
            StopInfoAccuracy.Correct => ClaimedStopsRemaining,
            StopInfoAccuracy.Incorrect => Mathf.Max(0, ClaimedStopsRemaining + 1),
            StopInfoAccuracy.Unknown => ClaimedStopsRemaining,
            StopInfoAccuracy.AccidentalMistake => Mathf.Max(0, ClaimedStopsRemaining + (StableBool(9) ? 1 : 0)),
            StopInfoAccuracy.IntentionalLie => Mathf.Max(0, ClaimedStopsRemaining + 1),
            _ => ClaimedStopsRemaining
        };
    }

    private string GetActualSeatName()
    {
        if (SeatManager.Instance == null)
            return null;

        SeatAnchor seat = SeatManager.Instance.GetSeatForPassenger(this);
        return seat != null ? seat.name : null;
    }

    private static string FormatBoardingAnswer(int stopsAgo)
    {
        return $"I got on {stopsAgo} stop(s) ago.";
    }

    private static string FormatDestinationAnswer(int stopsLeft)
    {
        return $"I've got {stopsLeft} stop(s) left.";
    }

    private static string FormatSeatAnswer(string seatName)
    {
        if (string.IsNullOrEmpty(seatName))
            return "I don't have a seat yet.";

        return $"I'm in seat {seatName}.";
    }

    private bool StableBool(int salt)
    {
        int seed = Mathf.Abs((GetInstanceID() * 73856093) ^ (salt * 19349663));
        return (seed % 100) < 50;
    }
}