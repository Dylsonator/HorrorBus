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

public enum PassengerSpeechStyle
{
    Auto,
    Reserved,
    Polite,
    Casual,
    Nervous,
    Blunt
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

    [Header("Speech")]
    [SerializeField] private PassengerSpeechStyle speechStyle = PassengerSpeechStyle.Auto;

    [Header("Disable These When Seated")]
    [SerializeField] private MonoBehaviour[] disableWhenSeated;

    private bool lastObserved;
    private PassengerSpeechStyle resolvedSpeechStyle = PassengerSpeechStyle.Reserved;

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
    public int ExpectedFare { get; private set; }
    public int PaidAmount { get; private set; }
    public PassengerSpeechStyle SpeechStyle => resolvedSpeechStyle;

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
        ResolveSpeechStyle();
    }

    private void Update()
    {
        if (IsObserved != lastObserved)
        {
            Debug.Log($"{PassengerName} observed = {IsObserved}");
            lastObserved = IsObserved;
        }
    }

    public void SetPassengerName(string newName)
    {
        passengerName = newName;
        ResolveSpeechStyle();
    }

    public void SetDropOffStopIndex(int index) => dropOffStopIndex = index;

    public void SetStopsInfo(int valueA, int valueB, StopInfoAccuracy accuracy)
    {
        stopsInfoA = valueA;
        stopsInfoB = valueB;
        stopsInfoAccuracy = accuracy;
    }

    public void SetIsAnomaly(bool value)
    {
        isAnomaly = value;
        ResolveSpeechStyle();
    }

    public void SetFare(int expected, int paid)
    {
        ExpectedFare = Mathf.Max(0, expected);
        PaidAmount = Mathf.Max(0, paid);
    }

    public void SetIdVisual(PassengerIdVisual visual) => idVisual = visual;

    public void SetDateOfBirth(string value)
    {
        dateOfBirth = value;
    }

    public void SetIdNumber(string value)
    {
        idNumber = value;
    }

    public void SetExpiryDate(string value)
    {
        expiryDate = value;
    }

    public void SetVisibleIdentity(string visibleName, string dob, string number, string expiry)
    {
        passengerName = visibleName;
        dateOfBirth = dob;
        idNumber = number;
        expiryDate = expiry;
        EnsureIdData();
        ResolveSpeechStyle();
    }

    public void CopyVisibleIdentityFrom(Passenger other, bool copyCardVisual = false)
    {
        if (other == null)
            return;

        SetVisibleIdentity(other.PassengerName, other.DateOfBirth, other.IdNumber, other.ExpiryDate);

        if (copyCardVisual)
            idVisual = other.IdVisual;
    }

    public void MarkProcessed(bool seated)
    {
        hasBeenProcessed = true;
        isSeatedPassenger = seated;

        if (seated)
        {
            for (int i = 0; i < disableWhenSeated.Length; i++)
            {
                if (disableWhenSeated[i] != null)
                    disableWhenSeated[i].enabled = false;
            }
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

        string destination = GetDestinationName();
        int stops = ClaimedStopsRemaining;
        int paid = PaidAmount;

        if (!IsAnomaly)
        {
            if (PaidAmount == ExpectedFare)
            {
                return Say(
                    $"I'm heading to {destination}. That's {stops} stop(s). Here's {paid}.",
                    $"{destination}. {stops} stop(s) to go. Here's {paid}.",
                    $"I'm getting off at {destination}. I've got {paid} here.",
                    $"{destination}, {stops} stop(s). Here you go — {paid}.",
                    $"Going to {destination}. {stops} stop(s) left. I've got {paid}.",
                    $"{destination} for me. Paid {paid}."
                );
            }

            return Say(
                $"I'm heading to {destination}. I've only got {paid}. That should be enough, right?",
                $"{destination}. I've got {paid} on me — is that alright?",
                $"I'm getting off at {destination}. I've only got {paid}.",
                $"{destination}, and I've got {paid}. That's enough for it, isn't it?",
                $"{destination}. I'm a bit short — I've only got {paid}.",
                $"I'm headed to {destination}. {paid} is all I've got on me."
            );
        }

        AnomalySkill skill = GetAnomalySkill();

        switch (skill)
        {
            case AnomalySkill.Low:
                return Say(
                    $"Uh... {destination}. {stops} stop(s). Here, {paid}.",
                    $"I'm going to {destination}. That's enough.",
                    $"{destination}. I've got {paid}.",
                    $"Right, uh... {destination}. Here.",
                    $"{destination}. I just need to get there.",
                    $"I'm going to {destination}. Here's what I've got."
                );

            case AnomalySkill.Mid:
                return PaidAmount == ExpectedFare
                    ? Say(
                        $"I'm getting off at {destination}. Here's {paid}.",
                        $"{destination}. I've got {paid} for it.",
                        $"Heading to {destination}. Here's {paid}.",
                        $"{destination}. Paid {paid}.",
                        $"I'm headed to {destination}. That should all be right."
                    )
                    : Say(
                        $"I'm getting off at {destination}. That should cover it.",
                        $"{destination}. This should be enough.",
                        $"Heading to {destination}. I've only got {paid}, but that'll do.",
                        $"{destination}. It's short, but it's close enough.",
                        $"I'm going to {destination}. That's all I've got."
                    );

            case AnomalySkill.High:
                return PaidAmount == ExpectedFare
                    ? Say(
                        $"I'm getting off at {destination}. I paid {paid}.",
                        $"{destination}. That's {stops} stop(s), and I've got {paid}.",
                        $"I'm headed to {destination}. Here's {paid}.",
                        $"{destination}. Fare's {paid}.",
                        $"I'm getting off at {destination}. Everything should be in order."
                    )
                    : Say(
                        $"I'm getting off at {destination}. I've only got {paid} on me.",
                        $"{destination}. I've got {paid} — that's all I've got.",
                        $"I'm headed to {destination}. I've only got {paid}.",
                        $"{destination}. I'm short, but only by a little.",
                        $"I'm going to {destination}. {paid} is all I have on me."
                    );

            default:
                return Say($"I'm heading to {destination}. Here's {paid}.");
        }
    }

    private string GetStandingRepeatLine()
    {
        string destination = GetDestinationName();

        if (!IsAnomaly)
        {
            return Say(
                $"Like I said, I'm going to {destination}.",
                $"Same as before — {destination}.",
                $"I already told you, I'm getting off at {destination}.",
                $"{destination}. Same as I said.",
                $"Still {destination}.",
                $"I said {destination} already."
            );
        }

        AnomalySkill skill = GetAnomalySkill();

        return skill switch
        {
            AnomalySkill.Low => Say("I already told you.", "Why are you asking again?", "Didn't you hear me?", "I said it already."),
            AnomalySkill.Mid => Say($"Same as before — {destination}.", "I already answered that.", "Same answer.", $"Still {destination}."),
            AnomalySkill.High => Say($"Still {destination}.", $"I'm still getting off at {destination}.", "Same as before.", $"Nothing changed — {destination}."),
            _ => Say("Same as before.")
        };
    }

    private string GetSeatedRepeatLine()
    {
        if (!IsAnomaly)
        {
            return Say(
                "I'm already seated now.",
                "I've already sat down.",
                "I'm fine here.",
                "Already in my seat.",
                "I'm sorted now.",
                "Already sat, thanks."
            );
        }

        AnomalySkill skill = GetAnomalySkill();

        return skill switch
        {
            AnomalySkill.Low => Say("Why are you asking again?", "I'm already sitting.", "Leave me alone.", "I'm sat already."),
            AnomalySkill.Mid => Say("I've already sat down.", "I'm already seated.", "I'm in my seat now.", "Already sat."),
            AnomalySkill.High => Say("I'm already seated.", "I'm settled, thanks.", "I've already taken my seat.", "Already in place."),
            _ => Say("I'm already seated.")
        };
    }

    public string GetAnswer(PassengerQuestionType questionType)
    {
        if (!IsAnomaly)
            return GetHumanAnswer(questionType);

        return GetAnomalyAnswer(questionType, GetAnomalySkill());
    }

    private string GetHumanAnswer(PassengerQuestionType questionType)
    {
        switch (questionType)
        {
            case PassengerQuestionType.BoardingStop:
                return Say(
                    $"I got on {GetDisplayedBoardingStopsAgo()} stop(s) ago.",
                    $"About {GetDisplayedBoardingStopsAgo()} stop(s) back.",
                    $"{GetDisplayedBoardingStopsAgo()} stop(s) ago.",
                    $"A few stops back — {GetDisplayedBoardingStopsAgo()}, I think.",
                    $"I boarded {GetDisplayedBoardingStopsAgo()} stop(s) ago.",
                    $"Back around {GetDisplayedBoardingStopsAgo()} stop(s) ago."
                );

            case PassengerQuestionType.DestinationStop:
                string destination = GetDestinationName();
                return Say(
                    $"I'm getting off at {destination}.",
                    $"{destination}.",
                    $"My stop is {destination}.",
                    $"I'm headed to {destination}.",
                    $"I'll be getting off at {destination}.",
                    $"{destination} is where I'm getting off.",
                    $"I'm staying on until {destination}.",
                    $"It should be {destination}."
                );

            case PassengerQuestionType.Seat:
                string seatName = GetActualSeatName();
                if (string.IsNullOrEmpty(seatName))
                {
                    return hasBeenProcessed
                        ? Say("I haven't got one somehow.", "Not in a seat yet.", "Still not seated.", "Haven't ended up in one yet.")
                        : Say("I don't have a seat yet.", "Not seated yet.", "I haven't sat down yet.", "Still waiting to sit down.");
                }

                return Say(
                    $"I'm in seat {seatName}.",
                    $"Seat {seatName}.",
                    $"That's my seat — {seatName}.",
                    $"{seatName}.",
                    $"I'm sat in {seatName}.",
                    $"It's {seatName}.",
                    $"I ended up in {seatName}."
                );

            case PassengerQuestionType.Fare:
                return Say(
                    $"I paid {PaidAmount}.",
                    $"{PaidAmount}.",
                    $"It was {PaidAmount}.",
                    $"I gave you {PaidAmount}.",
                    $"Paid {PaidAmount}.",
                    $"That was {PaidAmount}.",
                    $"I put down {PaidAmount}."
                );

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
        string destination = GetDestinationName();

        switch (questionType)
        {
            case PassengerQuestionType.BoardingStop:
                return Say(
                    "I got on... here. No, wait.",
                    $"{Mathf.Max(0, StopsInfoB + 2)} stop(s) ago.",
                    "A while back.",
                    "I don't remember.",
                    "Back there somewhere.",
                    "A few stops. Maybe."
                );

            case PassengerQuestionType.DestinationStop:
                return Say(
                    $"I'm getting off at... {destination}.",
                    $"{destination}, I think.",
                    "That stop up ahead.",
                    "Where everyone else gets off.",
                    $"Probably {destination}.",
                    $"Somewhere near {destination}."
                );

            case PassengerQuestionType.Seat:
                if (isSeatedPassenger)
                    return Say($"Seat {GetActualSeatName()}.", $"I'm in {GetActualSeatName()}.", $"That one — {GetActualSeatName()}.");
                return Say("I was just about to sit down.", "Haven't sat yet.", "Still finding one.", "Not in one yet.");

            case PassengerQuestionType.Fare:
                return PaidAmount == ExpectedFare
                    ? Say($"I paid {PaidAmount}.", $"{PaidAmount}.", $"It was {PaidAmount}.")
                    : Say("That should be enough.", "It's enough.", "Close enough.", $"That covers it. {PaidAmount}.");

            default:
                return "...";
        }
    }

    private string GetMidSkillAnomalyAnswer(PassengerQuestionType questionType)
    {
        bool subtleSlip = StableBool(17);
        string destination = GetDestinationName();

        switch (questionType)
        {
            case PassengerQuestionType.BoardingStop:
                return subtleSlip
                    ? Say(
                        $"I got on {Mathf.Max(0, StopsInfoB + 1)} stop(s) ago.",
                        $"About {Mathf.Max(0, StopsInfoB + 1)} stop(s) back.",
                        $"Should be {Mathf.Max(0, StopsInfoB + 1)} stop(s) ago."
                    )
                    : Say(
                        $"I got on {StopsInfoB} stop(s) ago.",
                        $"{StopsInfoB} stop(s) ago.",
                        $"Back {StopsInfoB} stop(s)."
                    );

            case PassengerQuestionType.DestinationStop:
                return subtleSlip
                    ? Say(
                        $"I'm getting off at {destination}, I think.",
                        $"Should be {destination}.",
                        $"Somewhere near {destination}.",
                        $"It should be {destination}."
                    )
                    : Say(
                        $"I'm getting off at {destination}.",
                        $"{destination}.",
                        $"My stop is {destination}.",
                        $"I'm due off at {destination}."
                    );

            case PassengerQuestionType.Seat:
                {
                    string actualSeat = GetActualSeatName();
                    if (string.IsNullOrEmpty(actualSeat))
                        return Say("I don't have a seat yet.", "Not seated yet.", "Still waiting on one.");

                    return subtleSlip
                        ? Say("Somewhere near the middle.", "A bit further back.", "One of those seats there.", "Round there somewhere.")
                        : Say($"Seat {actualSeat}.", $"I'm in {actualSeat}.", $"{actualSeat}.", $"That should be {actualSeat}.");
                }

            case PassengerQuestionType.Fare:
                if (PaidAmount == ExpectedFare)
                    return Say($"I paid {PaidAmount}.", $"{PaidAmount}.", $"It was {PaidAmount}.", $"I gave {PaidAmount}.");

                return subtleSlip
                    ? Say($"I paid {PaidAmount + 1}.", $"Pretty sure it was {PaidAmount + 1}.", "That should cover it.")
                    : Say("That should cover it.", "That should be enough.", $"I gave you {PaidAmount}.", $"It was {PaidAmount}.");

            default:
                return "...";
        }
    }

    private string GetHighSkillAnomalyAnswer(PassengerQuestionType questionType)
    {
        bool tinyTell = StableBool(31);
        string destination = GetDestinationName();

        switch (questionType)
        {
            case PassengerQuestionType.BoardingStop:
                return tinyTell
                    ? Say($"I got on {StopsInfoB} stop(s) ago, I think.", $"Should've been {StopsInfoB} stop(s) ago.", $"Roughly {StopsInfoB} stop(s) ago.")
                    : Say($"I got on {StopsInfoB} stop(s) ago.", $"{StopsInfoB} stop(s) ago.", $"Back {StopsInfoB} stop(s).");

            case PassengerQuestionType.DestinationStop:
                return tinyTell
                    ? Say($"I should be getting off at {destination}.", $"{destination}, I think.", $"It should be {destination}.")
                    : Say($"I'm getting off at {destination}.", $"{destination}.", $"That's my stop — {destination}.", $"I'm due off at {destination}.");

            case PassengerQuestionType.Seat:
                {
                    string actualSeat = GetActualSeatName();
                    if (string.IsNullOrEmpty(actualSeat))
                        return Say("I don't have a seat yet.", "Not seated yet.", "Still not in one.");

                    return tinyTell
                        ? Say($"I think it's seat {actualSeat}.", $"Should be {actualSeat}.", $"It should be {actualSeat}.")
                        : Say($"Seat {actualSeat}.", $"I'm in {actualSeat}.", $"{actualSeat}.", $"That's mine — {actualSeat}.");
                }

            case PassengerQuestionType.Fare:
                if (PaidAmount == ExpectedFare)
                    return tinyTell
                        ? Say($"Pretty sure I paid {PaidAmount}.", $"It should've been {PaidAmount}.", $"I believe it was {PaidAmount}.")
                        : Say($"I paid {PaidAmount}.", $"{PaidAmount}.", $"It was {PaidAmount}.", $"I gave {PaidAmount}.");

                return tinyTell
                    ? Say($"I've only got {PaidAmount} on me.", $"I only had {PaidAmount}.", $"I've only got {PaidAmount} right now.")
                    : Say("I've only got that much on me.", $"I've only got {PaidAmount}.", $"That's all I had — {PaidAmount}.");

            default:
                return "...";
        }
    }

    private string GetDestinationName()
    {
        if (RouteStops.Instance != null)
            return RouteStops.Instance.GetDestinationName(dropOffStopIndex);

        if (dropOffStopIndex >= 0)
            return $"Stop {dropOffStopIndex + 1}";

        return $"{ClaimedStopsRemaining} stop(s) away";
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

    private string GetActualSeatName()
    {
        if (SeatManager.Instance == null)
            return null;

        SeatAnchor seat = SeatManager.Instance.GetSeatForPassenger(this);
        return seat != null ? seat.name : null;
    }

    private AnomalySkill GetAnomalySkill()
    {
        AnomalyController controller = GetComponent<AnomalyController>();
        return controller != null ? controller.Skill : AnomalySkill.Mid;
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
            idNumber = $"ID-{100000 + (seed % 900000)}";

        if (string.IsNullOrWhiteSpace(expiryDate))
        {
            int day = ((seed / 7) % 28) + 1;
            int month = ((seed / 13) % 12) + 1;
            int year = 2027 + ((seed / 19) % 5);
            expiryDate = $"{day:00}/{month:00}/{year}";
        }
    }

    private void ResolveSpeechStyle()
    {
        if (speechStyle != PassengerSpeechStyle.Auto)
        {
            resolvedSpeechStyle = speechStyle;
            return;
        }

        PassengerSpeechStyle[] pool = IsAnomaly
            ? new[] { PassengerSpeechStyle.Reserved, PassengerSpeechStyle.Casual, PassengerSpeechStyle.Nervous, PassengerSpeechStyle.Blunt, PassengerSpeechStyle.Polite }
            : new[] { PassengerSpeechStyle.Reserved, PassengerSpeechStyle.Polite, PassengerSpeechStyle.Casual, PassengerSpeechStyle.Nervous, PassengerSpeechStyle.Blunt };

        int seed = Mathf.Abs((StableStringHash(passengerName) * 31) ^ GetInstanceID() ^ (IsAnomaly ? 97 : 13));
        resolvedSpeechStyle = pool[seed % pool.Length];
    }

    private bool StableBool(int salt)
    {
        int seed = Mathf.Abs((GetInstanceID() * 73856093) ^ (salt * 19349663));
        return (seed % 100) < 50;
    }

    private bool StableLineBool(string line, int salt)
    {
        int seed = Mathf.Abs((GetInstanceID() * 83492791) ^ (StableStringHash(line) * 19349663) ^ salt ^ (StableStringHash(passengerName) * 31));
        return (seed % 100) < 50;
    }

    private string Say(params string[] options)
    {
        if (options == null || options.Length == 0)
            return "...";

        string picked = options[Random.Range(0, options.Length)];
        return ApplySpeechFlavour(picked);
    }

    private string ApplySpeechFlavour(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return "...";

        line = line.Trim();

        return IsAnomaly
            ? ApplyAnomalySpeechFlavour(line)
            : ApplyHumanSpeechFlavour(line);
    }

    private string ApplyHumanSpeechFlavour(string line)
    {
        switch (resolvedSpeechStyle)
        {
            case PassengerSpeechStyle.Polite:
                if (!line.EndsWith("?") && !line.Contains("thanks") && StableLineBool(line, 101))
                    return $"{TrimEndPunctuation(line)}, thanks.";
                if (StableLineBool(line, 102))
                    return $"Sorry, {line}";
                return line;

            case PassengerSpeechStyle.Casual:
                if (StableLineBool(line, 103))
                    return $"Yeah, {line}";
                return line;

            case PassengerSpeechStyle.Nervous:
                if (StableLineBool(line, 104))
                    return $"Uh... {line}";
                return $"Sorry, {line}";

            case PassengerSpeechStyle.Blunt:
                return ShortenBluntLine(line);

            case PassengerSpeechStyle.Reserved:
            case PassengerSpeechStyle.Auto:
            default:
                return line;
        }
    }

    private string ApplyAnomalySpeechFlavour(string line)
    {
        AnomalySkill skill = GetAnomalySkill();

        switch (skill)
        {
            case AnomalySkill.Low:
                if (!line.StartsWith("Uh") && StableLineBool(line, 201))
                    line = $"Uh... {line}";
                if (!line.EndsWith("?") && StableLineBool(line, 202))
                    line = $"{TrimEndPunctuation(line)}...";
                return line;

            case AnomalySkill.Mid:
                if (resolvedSpeechStyle == PassengerSpeechStyle.Polite && StableLineBool(line, 203))
                    return $"Right, {line}";
                if (resolvedSpeechStyle == PassengerSpeechStyle.Nervous && StableLineBool(line, 204))
                    return $"I think... {line}";
                if (resolvedSpeechStyle == PassengerSpeechStyle.Casual && StableLineBool(line, 205))
                    return $"Yeah... {line}";
                return line;

            case AnomalySkill.High:
                if (resolvedSpeechStyle == PassengerSpeechStyle.Blunt && StableLineBool(line, 206))
                    return ShortenBluntLine(line);
                if (resolvedSpeechStyle == PassengerSpeechStyle.Polite && !line.EndsWith("?") && !line.Contains("thanks") && StableLineBool(line, 207))
                    return $"{TrimEndPunctuation(line)}, thanks.";
                return line;

            default:
                return line;
        }
    }

    private static string ShortenBluntLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return "...";

        string result = line
            .Replace("I'm getting off at ", string.Empty)
            .Replace("I'm headed to ", string.Empty)
            .Replace("I'm heading to ", string.Empty)
            .Replace("I'll be getting off at ", string.Empty)
            .Replace("My stop is ", string.Empty)
            .Replace("I got on ", string.Empty)
            .Replace("I boarded ", string.Empty)
            .Replace("I paid ", string.Empty)
            .Replace("I gave you ", string.Empty)
            .Replace("I gave ", string.Empty);

        return string.IsNullOrWhiteSpace(result) ? line : result;
    }

    private static string TrimEndPunctuation(string line)
    {
        return line.TrimEnd(' ', '.', '!', '?');
    }

    private static int StableStringHash(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        unchecked
        {
            int hash = 23;
            for (int i = 0; i < value.Length; i++)
                hash = (hash * 31) + value[i];
            return hash;
        }
    }
}
