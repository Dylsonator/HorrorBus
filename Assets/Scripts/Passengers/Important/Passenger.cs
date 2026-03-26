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
    CurrentStop,
    DestinationStop,
    Seat,
    Fare
}

public enum PassengerPaymentMethod
{
    Cash,
    DayRider
}

public enum PassengerTicketState
{
    None,
    Valid,
    Old,
    Fake
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
    [SerializeField] private string dateOfBirth;
    [SerializeField] private string idNumber;
    [SerializeField] private string expiryDate;

    [Header("Anomaly Flag (hidden from player)")]
    [SerializeField] private bool isAnomaly;

    [Header("Stops")]
    [SerializeField] private int dropOffStopIndex = -1;
    [SerializeField] private int stopsInfoA;
    [SerializeField] private int stopsInfoB;
    [SerializeField] private StopInfoAccuracy stopsInfoAccuracy = StopInfoAccuracy.Unknown;

    [Header("Observation")]
    public Transform Head;
    public bool IsObserved { get; internal set; }

    [Header("Inspection")]
    [SerializeField] private PassengerIdVisual idVisual = PassengerIdVisual.Real;

    [Header("State")]
    [SerializeField] private bool isSeatedPassenger;
    [SerializeField] private bool hasBeenProcessed;
    [SerializeField] private bool hasBeenInspectedBefore;

    [Header("Payment")]
    [SerializeField] private PassengerPaymentMethod paymentMethod = PassengerPaymentMethod.Cash;
    [SerializeField] private PassengerTicketState ticketState = PassengerTicketState.None;
    [SerializeField] private string ticketLabel = "";
    [SerializeField] private string ticketDateLabel = "";
    [SerializeField] private int[] tenderedDenominations = System.Array.Empty<int>();

    [Header("Disable These When Seated")]
    [SerializeField] private MonoBehaviour[] disableWhenSeated;

    private bool lastObserved;

    public bool IsAnomaly => isAnomaly;
    public int DropOffStopIndex => dropOffStopIndex;
    public string PassengerName => passengerName;
    public string DateOfBirth => dateOfBirth;
    public string IdNumber => idNumber;
    public string ExpiryDate => expiryDate;
    public int StopsInfoA => stopsInfoA;
    public int StopsInfoB => stopsInfoB;
    public StopInfoAccuracy StopsAccuracy => stopsInfoAccuracy;
    public int ClaimedStopsRemaining => stopsInfoA;
    public PassengerIdVisual IdVisual => idVisual;
    public bool IsSeatedPassenger => isSeatedPassenger;
    public bool HasBeenProcessed => hasBeenProcessed;
    public bool HasBeenInspectedBefore => hasBeenInspectedBefore;
    public PassengerPaymentMethod PaymentMethod => paymentMethod;
    public PassengerTicketState TicketState => ticketState;
    public int ExpectedFare { get; private set; }
    public int PaidAmount { get; private set; }
    public bool UsesCash => paymentMethod == PassengerPaymentMethod.Cash;
    public bool UsesDayRider => paymentMethod == PassengerPaymentMethod.DayRider;
    public bool IsDayRiderValid => ticketState == PassengerTicketState.Valid;
    public int CashTenderedPence => SumTendered();
    public int ChangeDuePence => UsesCash ? Mathf.Max(0, CashTenderedPence - ExpectedFare) : 0;

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

        EnsureIdData();
    }

    private void Update()
    {
        if (IsObserved != lastObserved)
        {
            Debug.Log($"{PassengerName} observed = {IsObserved}");
            lastObserved = IsObserved;
        }
    }

    public void SetPassengerName(string newName) => passengerName = newName;
    public void SetDropOffStopIndex(int index) => dropOffStopIndex = index;

    public void SetStopsInfo(int valueA, int valueB, StopInfoAccuracy accuracy)
    {
        stopsInfoA = valueA;
        stopsInfoB = valueB;
        stopsInfoAccuracy = accuracy;
    }

    public void SetIsAnomaly(bool value) => isAnomaly = value;
    public void SetIdVisual(PassengerIdVisual visual) => idVisual = visual;
    public void SetDateOfBirth(string value) => dateOfBirth = value;
    public void SetIdNumber(string value) => idNumber = value;
    public void SetExpiryDate(string value) => expiryDate = value;
    public void SetExpectedFare(int pence) => ExpectedFare = Mathf.Max(0, pence);
    public void SetPaidAmount(int pence) => PaidAmount = Mathf.Max(0, pence);

    public void SetVisibleIdentity(string visibleName, string dob, string number, string expiry)
    {
        passengerName = visibleName;
        dateOfBirth = dob;
        idNumber = number;
        expiryDate = expiry;
    }

    public void CopyVisibleIdentityFrom(Passenger other)
    {
        if (other == null || other == this)
            return;

        SetVisibleIdentity(other.PassengerName, other.DateOfBirth, other.IdNumber, other.ExpiryDate);
    }

    public void SetFare(int expected, int paid)
    {
        int cleanExpected = Mathf.Max(0, expected);
        int cleanPaid = Mathf.Max(0, paid);

        if (cleanPaid <= 0)
            cleanPaid = cleanExpected;

        SetCashPayment(cleanExpected, new[] { cleanPaid });
    }

    public void SetCashPayment(int expectedFare, int[] denominations)
    {
        paymentMethod = PassengerPaymentMethod.Cash;
        ticketState = PassengerTicketState.None;
        ticketLabel = string.Empty;
        ticketDateLabel = string.Empty;
        ExpectedFare = Mathf.Max(0, expectedFare);
        tenderedDenominations = CopyArray(denominations);
        PaidAmount = CashTenderedPence;
    }

    public void SetDayRiderPayment(int expectedFare, bool valid, bool oldTicket, bool fakeTicket, string dateText)
    {
        paymentMethod = PassengerPaymentMethod.DayRider;
        ticketState = fakeTicket ? PassengerTicketState.Fake : (oldTicket ? PassengerTicketState.Old : (valid ? PassengerTicketState.Valid : PassengerTicketState.None));
        ticketLabel = "DayRider";
        ticketDateLabel = dateText;
        ExpectedFare = Mathf.Max(0, expectedFare);
        tenderedDenominations = System.Array.Empty<int>();
        PaidAmount = 0;
    }

    public int[] GetTenderedDenominationsCopy() => CopyArray(tenderedDenominations);

    public void MarkProcessed(bool seated)
    {
        hasBeenProcessed = true;
        isSeatedPassenger = seated;

        if (!seated || disableWhenSeated == null)
            return;

        for (int i = 0; i < disableWhenSeated.Length; i++)
        {
            if (disableWhenSeated[i] != null)
                disableWhenSeated[i].enabled = false;
        }
    }

    public void RemoveFromQueueAndStopMovement()
    {
        QueueManagerNodes queue = FindFirstObjectByType<QueueManagerNodes>();
        if (queue != null)
            queue.Remove(this);

        PassengerJoinQueue joinQueue = GetComponent<PassengerJoinQueue>();
        if (joinQueue != null)
            Destroy(joinQueue);

        NodeQueueWalker walker = GetComponent<NodeQueueWalker>();
        if (walker != null)
            walker.StopMoving();
    }

    public string GetOpeningStatement()
    {
        bool firstInspect = !hasBeenInspectedBefore;
        hasBeenInspectedBefore = true;

        if (isSeatedPassenger)
            return GetSeatedRepeatLine();

        if (!firstInspect)
            return GetStandingRepeatLine();

        string destination = GetPublicDestinationName();

        if (UsesDayRider)
        {
            if (!IsAnomaly)
            {
                if (IsDayRiderValid)
                    return Say(
                        $"I'm headed to {destination}. I've got a DayRider here.",
                        $"{destination}. DayRider.",
                        $"I'm getting off at {destination}. Here's my DayRider."
                    );

                if (ticketState == PassengerTicketState.Old)
                    return Say(
                        $"I'm headed to {destination}. I've only got yesterday's DayRider.",
                        $"{destination}. I've got an old DayRider here.",
                        $"I've only got this DayRider from yesterday for {destination}."
                    );
            }

            return ticketState switch
            {
                PassengerTicketState.Valid => Say(
                    $"I'm getting off at {destination}. DayRider.",
                    $"{destination}. I've got a DayRider."
                ),
                PassengerTicketState.Old => Say(
                    $"I'm getting off at {destination}. This DayRider should still work.",
                    $"{destination}. It's still valid enough."
                ),
                PassengerTicketState.Fake => Say(
                    $"{destination}. DayRider. That's all you need.",
                    $"I'm getting off at {destination}. Here's the DayRider."
                ),
                _ => $"I'm going to {destination}. I've got a pass."
            };
        }

        int tendered = CashTenderedPence;
        if (!IsAnomaly)
        {
            return tendered >= ExpectedFare
                ? Say(
                    $"I'm heading to {destination}. I've got {FareTable.FormatMoney(tendered)} here.",
                    $"{destination}. Here's {FareTable.FormatMoney(tendered)}.",
                    $"I'm getting off at {destination}. I've got {FareTable.FormatMoney(tendered)} on me."
                )
                : Say(
                    $"I'm heading to {destination}. I've only got {FareTable.FormatMoney(tendered)}.",
                    $"{destination}. I've only got {FareTable.FormatMoney(tendered)} on me.",
                    $"I'm getting off at {destination}. That's all I've got - {FareTable.FormatMoney(tendered)}."
                );
        }

        return tendered >= ExpectedFare
            ? Say(
                $"I'm going to {destination}. Here - {FareTable.FormatMoney(tendered)}.",
                $"{destination}. I've got {FareTable.FormatMoney(tendered)}.",
                $"Heading to {destination}. Here's {FareTable.FormatMoney(tendered)}."
            )
            : Say(
                $"I'm going to {destination}. That should be enough.",
                $"{destination}. I've got {FareTable.FormatMoney(tendered)}.",
                $"Heading to {destination}. That's all I've got."
            );
    }

    private string GetStandingRepeatLine()
    {
        string destination = GetPublicDestinationName();

        if (!IsAnomaly)
        {
            return Say(
                $"Like I said, I'm going to {destination}.",
                $"Same as before - {destination}.",
                $"I already told you, I'm getting off at {destination}."
            );
        }

        return Say(
            $"Same as before - {destination}.",
            "I already answered that.",
            "Same answer."
        );
    }

    private string GetSeatedRepeatLine()
    {
        if (!IsAnomaly)
        {
            return Say(
                "I'm already seated now.",
                "I've already sat down.",
                "Already in my seat."
            );
        }

        return Say(
            "I'm already seated.",
            "I've already taken my seat.",
            "Why are you asking again?"
        );
    }

    public string GetAnswer(PassengerQuestionType questionType)
    {
        return UsesDayRider ? GetTicketAwareAnswer(questionType) : GetCashAwareAnswer(questionType);
    }

    private string GetTicketAwareAnswer(PassengerQuestionType questionType)
    {
        switch (questionType)
        {
            case PassengerQuestionType.CurrentStop:
                return Say(
                    $"We're at {GetCurrentStopName()}.",
                    $"This stop's {GetCurrentStopName()}.",
                    GetCurrentStopName()
                );

            case PassengerQuestionType.DestinationStop:
                return Say(
                    $"I'm getting off at {GetPublicDestinationName()}.",
                    GetPublicDestinationName(),
                    $"My stop is {GetPublicDestinationName()}."
                );

            case PassengerQuestionType.Seat:
                string seatName = GetActualSeatName();
                if (string.IsNullOrEmpty(seatName))
                    return Say("I don't have a seat yet.", "Not seated yet.");
                return Say($"Seat {seatName}.", $"I'm in {seatName}.");

            case PassengerQuestionType.Fare:
                return ticketState switch
                {
                    PassengerTicketState.Valid => Say(
                        "I've got a valid DayRider.",
                        "DayRider for today.",
                        $"DayRider - {ticketDateLabel}."
                    ),
                    PassengerTicketState.Old => Say(
                        "It's an old DayRider.",
                        $"DayRider from {ticketDateLabel}.",
                        "It's yesterday's one."
                    ),
                    PassengerTicketState.Fake => Say(
                        "It's a DayRider.",
                        "DayRider. Looks fine.",
                        $"DayRider - {ticketDateLabel}."
                    ),
                    _ => "I've got a pass."
                };

            default:
                return "...";
        }
    }

    private string GetCashAwareAnswer(PassengerQuestionType questionType)
    {
        switch (questionType)
        {
            case PassengerQuestionType.CurrentStop:
                return Say(
                    $"We're at {GetCurrentStopName()}.",
                    $"This stop's {GetCurrentStopName()}.",
                    GetCurrentStopName()
                );

            case PassengerQuestionType.DestinationStop:
                return Say(
                    $"I'm getting off at {GetPublicDestinationName()}.",
                    GetPublicDestinationName(),
                    $"My stop is {GetPublicDestinationName()}."
                );

            case PassengerQuestionType.Seat:
                string seatName = GetActualSeatName();
                if (string.IsNullOrEmpty(seatName))
                {
                    return hasBeenProcessed
                        ? Say("I haven't got one somehow.", "Still not seated.")
                        : Say("I don't have a seat yet.", "Not seated yet.");
                }

                return Say(
                    $"I'm in seat {seatName}.",
                    $"Seat {seatName}.",
                    $"That's my seat - {seatName}."
                );

            case PassengerQuestionType.Fare:
                return IsAnomaly && CashTenderedPence < ExpectedFare
                    ? Say(
                        "That should be enough.",
                        $"I've got {FareTable.FormatMoney(CashTenderedPence)}.",
                        "Close enough."
                    )
                    : Say(
                        $"I handed over {FareTable.FormatMoney(CashTenderedPence)}.",
                        $"{FareTable.FormatMoney(CashTenderedPence)}.",
                        $"I gave you {FareTable.FormatMoney(CashTenderedPence)}."
                    );

            default:
                return "...";
        }
    }

    public string GetPaymentShortLabel()
    {
        if (UsesDayRider)
        {
            return ticketState switch
            {
                PassengerTicketState.Valid => "DayRider",
                PassengerTicketState.Old => "Old DayRider",
                PassengerTicketState.Fake => "Suspicious DayRider",
                _ => "Pass"
            };
        }

        return "Cash";
    }

    public string BuildTenderSummary()
    {
        if (!UsesCash)
            return "Tendered: No cash - using ticket";

        string list = string.Empty;
        for (int i = 0; i < tenderedDenominations.Length; i++)
        {
            if (i > 0) list += ", ";
            list += FareTable.FormatMoney(tenderedDenominations[i]);
        }

        if (string.IsNullOrEmpty(list))
            list = "Nothing";

        return $"Tendered: {FareTable.FormatMoney(CashTenderedPence)} ({list})";
    }

    public string BuildTicketStatusSummary()
    {
        if (!UsesDayRider)
            return "Ticket: None";

        string status = ticketState switch
        {
            PassengerTicketState.Valid => "Valid",
            PassengerTicketState.Old => "Old",
            PassengerTicketState.Fake => "Suspicious",
            _ => "Unknown"
        };

        string extra = string.IsNullOrWhiteSpace(ticketDateLabel) ? string.Empty : $" ({ticketDateLabel})";
        return $"Ticket: {ticketLabel} - {status}{extra}";
    }

    public string GetPublicDestinationName()
    {
        if (RouteStops.Instance != null)
            return RouteStops.Instance.GetDestinationName(dropOffStopIndex);

        if (dropOffStopIndex >= 0)
            return $"Stop {dropOffStopIndex + 1}";

        return $"{ClaimedStopsRemaining} stop(s) away";
    }

    private string GetCurrentStopName()
    {
        if (RouteStops.Instance == null)
            return "Unknown Stop";

        return RouteStops.Instance.GetStopNameSafe(RouteStops.Instance.NextStopIndex);
    }

    private string GetActualSeatName()
    {
        if (SeatManager.Instance == null)
            return null;

        SeatAnchor seat = SeatManager.Instance.GetSeatForPassenger(this);
        return seat != null ? seat.name : null;
    }

    private void EnsureIdData()
    {
        int seed = Mathf.Abs(GetInstanceID());

        if (string.IsNullOrWhiteSpace(dateOfBirth))
        {
            int day = (seed % 28) + 1;
            int month = ((seed / 28) % 12) + 1;
            int year = 1970 + ((seed / 336) % 30);
            dateOfBirth = $"{day:00}/{month:00}/{year}";
        }

        if (string.IsNullOrWhiteSpace(idNumber))
            idNumber = $"ID-{(100000 + (seed % 900000))}";

        if (string.IsNullOrWhiteSpace(expiryDate))
        {
            int day = ((seed / 7) % 28) + 1;
            int month = ((seed / 13) % 12) + 1;
            int year = 2027 + ((seed / 19) % 5);
            expiryDate = $"{day:00}/{month:00}/{year}";
        }
    }

    private int SumTendered()
    {
        if (tenderedDenominations == null)
            return 0;

        int total = 0;
        for (int i = 0; i < tenderedDenominations.Length; i++)
            total += Mathf.Max(0, tenderedDenominations[i]);
        return total;
    }

    private static int[] CopyArray(int[] source)
    {
        if (source == null)
            return System.Array.Empty<int>();

        int[] copy = new int[source.Length];
        for (int i = 0; i < source.Length; i++)
            copy[i] = Mathf.Max(0, source[i]);
        return copy;
    }

    private static string Say(params string[] options)
    {
        if (options == null || options.Length == 0)
            return "...";

        return options[Random.Range(0, options.Length)];
    }
}
