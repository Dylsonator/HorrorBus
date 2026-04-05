using System.Collections.Generic;
using UnityEngine;

public enum PassengerIdVisual
{
    Real,
    ObviousFake,
    FakeAlt1,
    FakeAlt2,
    FakeAlt3,
    FakeAlt4,
    FakeAlt5,
    FakeAlt6,
    FakeAlt7,
    FakeAlt8,
    FakeAlt9,
    FakeAlt10,
    FakeAlt11,
    FakeAlt12,
    FakeAlt13,
    FakeAlt14,
    FakeAlt15,
    FakeAlt16,
    FakeAlt17,
    FakeAlt18,
    FakeAlt19,
    FakeAlt20
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

    [Header("Visible ID Identity (optional override)")]
    [SerializeField] private bool useVisibleIdentityOverride;
    [SerializeField] private string visiblePassengerName;
    [SerializeField] private string visibleDateOfBirth;
    [SerializeField] private string visibleIdNumber;
    [SerializeField] private string visibleExpiryDate;

    [Header("Anomaly Flag (hidden)")]
    [SerializeField] private bool isAnomaly;

    [Header("Stops")]
    [SerializeField] private int dropOffStopIndex = -1;
    [SerializeField] private int actualStopsRemaining = 1;
    [SerializeField] private int claimedStopsRemaining = 1;
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
    [SerializeField] private string ticketLabel = "DayRider";
    [SerializeField] private string ticketDateLabel = "";
    [SerializeField] private int[] tenderedDenominations = System.Array.Empty<int>();

    [Header("Desk Behaviour")]
    [SerializeField, Range(0f, 1f)] private float forgetIdChance = 0.18f;
    [SerializeField, Range(0f, 1f)] private float forgetTicketChance = 0.16f;
    [SerializeField, Range(0f, 1f)] private float forgetPaymentChance = 0.14f;
    [SerializeField, Range(0f, 1f)] private float autoRevealMissingChance = 0.35f;
    [SerializeField, Range(0f, 1f)] private float trayInterferenceChance = 0.10f;
    [SerializeField, Range(0f, 1f)] private float clutterChance = 0.15f;
    [SerializeField, Range(0f, 1f)] private float fakeMoneyChanceHuman = 0.08f;
    [SerializeField, Range(0f, 1f)] private float fakeMoneyChanceAnomaly = 0.35f;
    [SerializeField] private int minTolerance = 2;
    [SerializeField] private int maxTolerance = 6;

    [Header("Disable These When Seated")]
    [SerializeField] private MonoBehaviour[] disableWhenSeated;

    private bool deskPrepared;
    private bool deskIdPresented;
    private bool deskTicketPresented;
    private bool deskPaymentPresented;
    private bool deskClutterPresented;
    private int deskTolerance;
    private int deskQuestionsAsked;
    private bool askedAboutMissingId;
    private bool askedAboutMissingTicket;
    private bool askedAboutMissingPayment;
    private TicketBand spokenTicketBand = TicketBand.None;
    private bool spokenTicketBandIsWrong;
    private readonly HashSet<string> spawnedDeskIds = new HashSet<string>();
    private readonly List<int> runtimeTendered = new List<int>();
    private readonly HashSet<int> fakeMoneyIndices = new HashSet<int>();

    public bool IsAnomaly => isAnomaly;
    public int DropOffStopIndex => dropOffStopIndex;

    // Spoken / true identity
    public string PassengerName => passengerName;
    public string DateOfBirth => dateOfBirth;
    public string IdNumber => idNumber;
    public string ExpiryDate => expiryDate;

    // Visible / card identity
    public string VisiblePassengerName => useVisibleIdentityOverride && !string.IsNullOrWhiteSpace(visiblePassengerName) ? visiblePassengerName : passengerName;
    public string VisibleDateOfBirth => useVisibleIdentityOverride && !string.IsNullOrWhiteSpace(visibleDateOfBirth) ? visibleDateOfBirth : dateOfBirth;
    public string VisibleIdNumber => useVisibleIdentityOverride && !string.IsNullOrWhiteSpace(visibleIdNumber) ? visibleIdNumber : idNumber;
    public string VisibleExpiryDate => useVisibleIdentityOverride && !string.IsNullOrWhiteSpace(visibleExpiryDate) ? visibleExpiryDate : expiryDate;
    public int StopsInfoA => actualStopsRemaining;
    public int StopsInfoB => claimedStopsRemaining;
    public StopInfoAccuracy StopsAccuracy => stopsInfoAccuracy;
    public int TrueStopsRemaining => Mathf.Max(1, actualStopsRemaining);
    public int ClaimedStopsRemaining => Mathf.Max(1, claimedStopsRemaining <= 0 ? actualStopsRemaining : claimedStopsRemaining);
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
    public int CashTenderedPence => Sum(runtimeTendered);
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
        ResetDeskSession();
    }

    public void ResetDeskSession()
    {
        deskPrepared = false;
        deskIdPresented = false;
        deskTicketPresented = false;
        deskPaymentPresented = false;
        deskClutterPresented = false;
        deskQuestionsAsked = 0;
        askedAboutMissingId = false;
        askedAboutMissingTicket = false;
        askedAboutMissingPayment = false;
        spokenTicketBand = TicketBand.None;
        spokenTicketBandIsWrong = false;
        spawnedDeskIds.Clear();
        runtimeTendered.Clear();
        fakeMoneyIndices.Clear();
    }

    public void PrepareDeskSession(FareTable fareTable)
    {
        if (deskPrepared)
            return;

        EnsureIdData();
        EnsureTenderedBreakdown(fareTable);

        deskTolerance = Random.Range(Mathf.Max(1, minTolerance), Mathf.Max(minTolerance + 1, maxTolerance + 1));
        deskIdPresented = Random.value >= forgetIdChance;
        deskTicketPresented = !UsesDayRider || Random.value >= forgetTicketChance;
        deskPaymentPresented = !UsesCash || Random.value >= forgetPaymentChance;
        deskClutterPresented = false;

        spokenTicketBand = GetCorrectTicketBand(fareTable);
        spokenTicketBandIsWrong = Random.value < (IsAnomaly ? 0.45f : 0.18f);
        if (spokenTicketBandIsWrong)
            spokenTicketBand = PickWrongBand(spokenTicketBand);

        deskPrepared = true;
    }

    public void BuildInitialDeskItems(FareTable fareTable, List<InspectionDeskItemState> output)
    {
        if (output == null)
            return;

        PrepareDeskSession(fareTable);

        if (deskIdPresented)
            AddIdItem(output);

        if (deskTicketPresented)
            AddTicketItem(output);

        if (deskPaymentPresented)
            AddPaymentItems(output);

        if (!deskClutterPresented && Random.value < clutterChance)
        {
            deskClutterPresented = true;
            AddClutterItem(output);
        }
    }

    public bool TryAutoRevealMissingItem(FareTable fareTable, out InspectionDeskItemState newItem, out string line)
    {
        newItem = null;
        line = string.Empty;

        PrepareDeskSession(fareTable);

        if (Random.value > autoRevealMissingChance)
            return false;

        if (!deskPaymentPresented && UsesCash)
        {
            deskPaymentPresented = true;
            List<InspectionDeskItemState> temp = new List<InspectionDeskItemState>();
            AddPaymentItems(temp);

            if (temp.Count > 0)
            {
                newItem = temp[0];

                // Leave the other cash items available so the desk UI can spawn them too.
                for (int i = 1; i < temp.Count; i++)
                    spawnedDeskIds.Remove(temp[i].uniqueId);

                line = Say(
                    "Hang on, I forgot the money.",
                    "Right, nearly forgot to pay.",
                    "Sorry, payment - there."
                );
                return true;
            }
        }

        if (!deskTicketPresented && UsesDayRider)
        {
            deskTicketPresented = true;
            List<InspectionDeskItemState> temp = new List<InspectionDeskItemState>();
            AddTicketItem(temp);
            if (temp.Count > 0)
            {
                newItem = temp[0];
                line = Say(
                    "Wait, here's the pass.",
                    "Sorry - the ticket.",
                    "Forgot the DayRider."
                );
                return true;
            }
        }

        if (!deskIdPresented)
        {
            deskIdPresented = true;
            List<InspectionDeskItemState> temp = new List<InspectionDeskItemState>();
            AddIdItem(temp);
            if (temp.Count > 0)
            {
                newItem = temp[0];
                line = Say(
                    "Hold on, here's my ID.",
                    "Right, forgot to put the ID down.",
                    "Sorry - ID."
                );
                return true;
            }
        }

        return false;
    }

    public bool TryGetIdleChatter(out string line)
    {
        line = string.Empty;
        if (Random.value > 0.45f)
            return false;

        line = Say(
            "Long day.",
            "Bus seems slow today.",
            "Thought I'd miss this stop.",
            "Can't stand these seats.",
            "Bit cramped in here."
        );
        return true;
    }

    public bool TryPassengerTrayInterference(out string line)
    {
        line = string.Empty;
        if (Random.value > trayInterferenceChance)
            return false;

        line = Say(
            "No, look at this one.",
            "Hang on.",
            "That's mine for a second.",
            "Wait, this one."
        );
        return true;
    }

    public void BuildQuestionOptions(InspectionDeskClickTopic topic, InspectionDeskItemState item, List<InspectionDeskQuestionOption> output)
    {
        if (output == null)
            return;

        output.Clear();

        bool itemOwnedByPassenger = item != null && item.isPassengerOwned;
        bool itemLooksSuspicious = item != null && (item.isFake || item.kind == InspectionDeskItemKind.Note || item.kind == InspectionDeskItemKind.Evidence);

        switch (topic)
        {
            case InspectionDeskClickTopic.Generic:
                AddGeneralConversationOptions(output);
                if (itemOwnedByPassenger)
                    AddOwnershipOptions(output);
                output.Add(new InspectionDeskQuestionOption("generic_repeat", "Say that again."));
                return;

            case InspectionDeskClickTopic.MissingId:
                output.Add(new InspectionDeskQuestionOption("request_id", "Where's your ID?"));
                output.Add(new InspectionDeskQuestionOption("challenge_no_id", "You need to show ID."));
                AddGeneralConversationOptions(output);
                return;

            case InspectionDeskClickTopic.MissingTicket:
                output.Add(new InspectionDeskQuestionOption("request_ticket", "Where's your ticket?"));
                output.Add(new InspectionDeskQuestionOption("challenge_no_ticket", "You still need to show the pass."));
                AddGeneralConversationOptions(output);
                return;

            case InspectionDeskClickTopic.MissingPayment:
                output.Add(new InspectionDeskQuestionOption("request_payment", "Where's the payment?"));
                output.Add(new InspectionDeskQuestionOption("challenge_no_payment", "You haven't paid."));
                AddGeneralConversationOptions(output);
                return;

            case InspectionDeskClickTopic.IdCard:
                output.Add(new InspectionDeskQuestionOption("photo_match", "You don't match the photo."));
                output.Add(new InspectionDeskQuestionOption("name_repeat", "Say your full name."));
                output.Add(new InspectionDeskQuestionOption("dob_repeat", "Tell me your date of birth."));
                output.Add(new InspectionDeskQuestionOption("number_repeat", "Repeat your ID number."));
                output.Add(new InspectionDeskQuestionOption("expiry_ask", "This ID looks expired."));
                if (itemOwnedByPassenger)
                    AddOwnershipOptions(output);
                AddGeneralConversationOptions(output);
                return;

            case InspectionDeskClickTopic.IdPhoto:
                output.Add(new InspectionDeskQuestionOption("photo_match", "This photo doesn't look right."));
                output.Add(new InspectionDeskQuestionOption("photo_older", "You look different to this photo."));
                AddGeneralConversationOptions(output);
                return;

            case InspectionDeskClickTopic.IdName:
                output.Add(new InspectionDeskQuestionOption("name_repeat", "Say your full name."));
                output.Add(new InspectionDeskQuestionOption("name_match", "Why doesn't this name match?"));
                if (itemOwnedByPassenger)
                    AddOwnershipOptions(output);
                AddGeneralConversationOptions(output);
                return;

            case InspectionDeskClickTopic.IdDob:
                output.Add(new InspectionDeskQuestionOption("dob_repeat", "Tell me your date of birth."));
                output.Add(new InspectionDeskQuestionOption("dob_age", "You don't look that age."));
                AddGeneralConversationOptions(output);
                return;

            case InspectionDeskClickTopic.IdExpiry:
                output.Add(new InspectionDeskQuestionOption("expiry_ask", "This ID looks expired."));
                output.Add(new InspectionDeskQuestionOption("expiry_still_valid", "Why should I accept this expiry?"));
                AddGeneralConversationOptions(output);
                return;

            case InspectionDeskClickTopic.IdNumber:
                output.Add(new InspectionDeskQuestionOption("number_repeat", "Repeat your ID number."));
                output.Add(new InspectionDeskQuestionOption("number_replacement", "Is this a replacement card?"));
                AddGeneralConversationOptions(output);
                return;

            case InspectionDeskClickTopic.Ticket:
            case InspectionDeskClickTopic.TicketValidity:
                output.Add(new InspectionDeskQuestionOption("ticket_valid", "This ticket looks off."));
                output.Add(new InspectionDeskQuestionOption("ticket_date", "What day is this for?"));
                output.Add(new InspectionDeskQuestionOption("ticket_route", "Is this even for this route?"));
                if (itemOwnedByPassenger)
                    AddOwnershipOptions(output);
                AddGeneralConversationOptions(output);
                return;

            case InspectionDeskClickTopic.TicketRoute:
                output.Add(new InspectionDeskQuestionOption("ticket_route", "Is this even for this route?"));
                output.Add(new InspectionDeskQuestionOption("ticket_valid", "This ticket looks off."));
                if (itemOwnedByPassenger)
                    AddOwnershipOptions(output);
                AddGeneralConversationOptions(output);
                return;

            case InspectionDeskClickTopic.Money:
            case InspectionDeskClickTopic.MoneyAmount:
                output.Add(new InspectionDeskQuestionOption("money_all", "Is this all you're paying?"));
                output.Add(new InspectionDeskQuestionOption("money_exact", "Can you pay the exact fare?"));
                output.Add(new InspectionDeskQuestionOption("money_already_paid", "You said you already paid?"));
                if (itemOwnedByPassenger)
                    AddOwnershipOptions(output);
                AddGeneralConversationOptions(output);
                return;

            case InspectionDeskClickTopic.MoneyAuthenticity:
                output.Add(new InspectionDeskQuestionOption("money_real", "Are you sure this money is real?"));
                output.Add(new InspectionDeskQuestionOption("money_wrong_note", "Why does this note look wrong?"));
                if (itemOwnedByPassenger)
                    AddOwnershipOptions(output);
                AddGeneralConversationOptions(output);
                return;

            case InspectionDeskClickTopic.PassengerFace:
                output.Add(new InspectionDeskQuestionOption("face_match", "You don't match the ID."));
                output.Add(new InspectionDeskQuestionOption("face_twin", "You're saying that's your twin?"));
                AddGeneralConversationOptions(output);
                return;

            case InspectionDeskClickTopic.PassengerHair:
                output.Add(new InspectionDeskQuestionOption("hair_change", "Your hair doesn't match the photo."));
                output.Add(new InspectionDeskQuestionOption("hair_recent", "Did you change your hair recently?"));
                AddGeneralConversationOptions(output);
                return;

            case InspectionDeskClickTopic.PassengerClothes:
                output.Add(new InspectionDeskQuestionOption("clothes_mismatch", "You don't look like this ID."));
                output.Add(new InspectionDeskQuestionOption("clothes_copy", "These details don't line up."));
                AddGeneralConversationOptions(output);
                return;

            case InspectionDeskClickTopic.PassengerBehaviour:
                output.Add(new InspectionDeskQuestionOption("behaviour_nervous", "Why are you acting strange?"));
                output.Add(new InspectionDeskQuestionOption("behaviour_repeat", "Answer me properly."));
                if (itemLooksSuspicious)
                    AddOwnershipOptions(output);
                AddGeneralConversationOptions(output);
                return;

            default:
                AddGeneralConversationOptions(output);
                if (itemOwnedByPassenger)
                    AddOwnershipOptions(output);
                output.Add(new InspectionDeskQuestionOption("generic_repeat", "Say that again."));
                return;
        }
    }

    private void AddGeneralConversationOptions(List<InspectionDeskQuestionOption> output)
    {
        if (output == null)
            return;

        output.Add(new InspectionDeskQuestionOption("ask_current_stop", "What's this stop?"));
        output.Add(new InspectionDeskQuestionOption("ask_destination", "Where to?"));
        output.Add(new InspectionDeskQuestionOption("ask_seat", "What seat are you in?"));
        output.Add(new InspectionDeskQuestionOption("ask_fare", "How much are you paying?"));
    }

    private void AddOwnershipOptions(List<InspectionDeskQuestionOption> output)
    {
        if (output == null)
            return;

        output.Add(new InspectionDeskQuestionOption("who_item", "Whose is this?"));
        output.Add(new InspectionDeskQuestionOption("why_have_item", "Why have you got this?"));
        output.Add(new InspectionDeskQuestionOption("is_this_yours", "Is this even yours?"));
    }

    public string AnswerDeskQuestion(InspectionDeskClickTopic topic, string optionId, FareTable fareTable, InspectionDeskItemState clickedItem, List<InspectionDeskItemState> spawnedItems)
    {
        PrepareDeskSession(fareTable);

        if (spawnedItems == null)
            spawnedItems = new List<InspectionDeskItemState>();

        deskQuestionsAsked++;
        string tonePrefix = GetTonePrefix();

        switch (optionId)
        {
            case "request_id":
            case "challenge_no_id":
                askedAboutMissingId = true;
                if (!deskIdPresented)
                {
                    deskIdPresented = true;
                    AddIdItem(spawnedItems);
                    return tonePrefix + Say("Alright, here's my ID.", "Fine. ID's there.", "I forgot it, alright?");
                }
                return tonePrefix + Say("It's already there.", "You have it already.");

            case "request_ticket":
            case "challenge_no_ticket":
                askedAboutMissingTicket = true;
                if (UsesDayRider && !deskTicketPresented)
                {
                    deskTicketPresented = true;
                    AddTicketItem(spawnedItems);
                    return tonePrefix + Say("There's the pass.", "Alright, here's the DayRider.", "Forgot that one.");
                }
                return tonePrefix + Say("I'm not using a pass.", "No ticket. I'm paying cash.");

            case "request_payment":
            case "challenge_no_payment":
                askedAboutMissingPayment = true;
                if (UsesCash && !deskPaymentPresented)
                {
                    deskPaymentPresented = true;
                    AddPaymentItems(spawnedItems);
                    return tonePrefix + Say("There. Payment.", "Alright, here's the money.", "I forgot to put it down.");
                }
                return tonePrefix + Say("I already paid.", "The money's there somewhere.", "I've already put money down.");

            case "photo_match":
            case "face_match":
            case "clothes_mismatch":
                return tonePrefix + RespondToIdentityChallenge();

            case "photo_older":
            case "hair_change":
            case "hair_recent":
                return tonePrefix + Say(
                    "I changed it recently.",
                    "People change how they look.",
                    "That's an old photo."
                );

            case "name_repeat":
                return tonePrefix + $"{passengerName}.";

            case "name_match":
                if (useVisibleIdentityOverride && !string.Equals(VisiblePassengerName, passengerName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return tonePrefix + Say(
                        "That's not my name.",
                        "No, that's wrong.",
                        "That card isn't got my details on it."
                    );
                }

                return tonePrefix + Say(
                    "That's my name.",
                    "It matches fine.",
                    "Read it again."
                );

            case "dob_repeat":
                return tonePrefix + $"{dateOfBirth}.";

            case "dob_age":
                if (useVisibleIdentityOverride && !string.Equals(VisibleDateOfBirth, dateOfBirth, System.StringComparison.OrdinalIgnoreCase))
                {
                    return tonePrefix + Say(
                        "That's not my birth date.",
                        "No, that's wrong.",
                        "That isn't my DOB."
                    );
                }

                return tonePrefix + Say(
                    "People look older, don't they?",
                    "That's what it says.",
                    "Take it up with the card."
                );

            case "expiry_ask":
            case "expiry_still_valid":
                return tonePrefix + RespondToExpiryChallenge();

            case "number_repeat":
                return tonePrefix + $"{idNumber}.";

            case "number_replacement":
                if (useVisibleIdentityOverride && !string.Equals(VisibleIdNumber, idNumber, System.StringComparison.OrdinalIgnoreCase))
                {
                    return tonePrefix + Say(
                        "That's not my number.",
                        "No, that number's wrong.",
                        "That isn't mine."
                    );
                }

                return tonePrefix + Say(
                    "No, that's the card I've got.",
                    "It's just my card.",
                    "No replacement - just that number."
                );

            case "ticket_valid":
            case "ticket_date":
            case "ticket_route":
                return tonePrefix + RespondToTicketChallenge();

            case "money_all":
                return tonePrefix + RespondToMoneyAmount();

            case "money_exact":
                return tonePrefix + Say(
                    "No, that's what I've got.",
                    "That's all I've got on me.",
                    "Not exact, no."
                );

            case "money_already_paid":
                return tonePrefix + Say(
                    "I already put money there.",
                    "Look properly - I already paid.",
                    "It's on the tray already."
                );

            case "money_real":
            case "money_wrong_note":
                return tonePrefix + RespondToMoneyAuthenticity();

            case "face_twin":
                return tonePrefix + Say(
                    "Could be. Same family name and all.",
                    "You've never seen siblings that look alike?",
                    "People say that all the time."
                );

            case "behaviour_nervous":
            case "behaviour_repeat":
                return tonePrefix + Say(
                    "I'm just trying to get home.",
                    "You're the one dragging this out.",
                    "Can we get on with it?"
                );

            case "who_item":
            case "is_this_yours":
                return tonePrefix + RespondToOwnershipQuestion(clickedItem);

            case "why_have_item":
                return tonePrefix + RespondToWhyHaveItem(clickedItem);

            case "ask_current_stop":
                return tonePrefix + GetAnswer(PassengerQuestionType.CurrentStop);

            case "ask_destination":
                return tonePrefix + GetAnswer(PassengerQuestionType.DestinationStop);

            case "ask_seat":
                return tonePrefix + GetAnswer(PassengerQuestionType.Seat);

            case "ask_fare":
                return tonePrefix + GetAnswer(PassengerQuestionType.Fare);

            default:
                return tonePrefix + Say("Same answer.", "I already told you.", "It's what I said.");
        }
    }

    public void SetPassengerName(string newName) => passengerName = newName;
    public void SetDropOffStopIndex(int index) => dropOffStopIndex = index;

    public void SetStopsInfo(int valueA, int valueB, StopInfoAccuracy accuracy)
    {
        actualStopsRemaining = Mathf.Max(1, valueA);
        claimedStopsRemaining = Mathf.Max(1, valueB);
        stopsInfoAccuracy = accuracy;
    }

    public void SetIsAnomaly(bool value) => isAnomaly = value;
    public void SetIdVisual(PassengerIdVisual visual) => idVisual = visual;
    public void SetDateOfBirth(string value) => dateOfBirth = value;
    public void SetIdNumber(string value) => idNumber = value;
    public void SetExpiryDate(string value) => expiryDate = value;
    public void SetExpectedFare(int pence) => ExpectedFare = Mathf.Max(0, pence);

    public void SetPaidAmount(int pence)
    {
        PaidAmount = Mathf.Max(0, pence);
        tenderedDenominations = System.Array.Empty<int>();
        ResetDeskSession();
    }

    public void SetVisibleIdentity(string visibleName, string dob, string number, string expiry)
    {
        useVisibleIdentityOverride = true;
        visiblePassengerName = visibleName;
        visibleDateOfBirth = dob;
        visibleIdNumber = number;
        visibleExpiryDate = expiry;
        ResetDeskSession();
    }

    public void ClearVisibleIdentityOverride()
    {
        useVisibleIdentityOverride = false;
        visiblePassengerName = string.Empty;
        visibleDateOfBirth = string.Empty;
        visibleIdNumber = string.Empty;
        visibleExpiryDate = string.Empty;
        ResetDeskSession();
    }

    public void CopyVisibleIdentityFrom(Passenger other)
    {
        if (other == null || other == this)
            return;

        SetVisibleIdentity(other.VisiblePassengerName, other.VisibleDateOfBirth, other.VisibleIdNumber, other.VisibleExpiryDate);
    }

    public void SetStolenIdentityFrom(Passenger victim)
    {
        if (victim == null || victim == this)
            return;

        SetVisibleIdentity(victim.VisiblePassengerName, victim.VisibleDateOfBirth, victim.VisibleIdNumber, victim.VisibleExpiryDate);
        SetIdVisual(PassengerIdVisual.Real);
    }

    public void SetFare(int expected, int paid)
    {
        ExpectedFare = Mathf.Max(0, expected);
        PaidAmount = Mathf.Max(0, paid);
        paymentMethod = PassengerPaymentMethod.Cash;
        ticketState = PassengerTicketState.None;
        tenderedDenominations = System.Array.Empty<int>();
        ResetDeskSession();
    }

    public void SetCashPayment(int expectedFare, int[] denominations)
    {
        paymentMethod = PassengerPaymentMethod.Cash;
        ticketState = PassengerTicketState.None;
        ticketLabel = string.Empty;
        ticketDateLabel = string.Empty;
        ExpectedFare = Mathf.Max(0, expectedFare);
        tenderedDenominations = CopyArray(denominations);
        PaidAmount = Sum(CopyToList(tenderedDenominations));
        ResetDeskSession();
    }

    public void SetDayRiderPayment(int expectedFare, bool valid, bool oldTicket, bool fakeTicket, string dateText)
    {
        paymentMethod = PassengerPaymentMethod.DayRider;
        ticketState = fakeTicket ? PassengerTicketState.Fake : (oldTicket ? PassengerTicketState.Old : (valid ? PassengerTicketState.Valid : PassengerTicketState.None));
        ticketLabel = "DayRider";
        ticketDateLabel = dateText;
        ExpectedFare = Mathf.Max(0, expectedFare);
        PaidAmount = 0;
        tenderedDenominations = System.Array.Empty<int>();
        ResetDeskSession();
    }

    public int[] GetTenderedDenominationsCopy()
    {
        if (runtimeTendered.Count > 0)
            return runtimeTendered.ToArray();

        return CopyArray(tenderedDenominations);
    }

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
        string ticketAsk = spokenTicketBand != TicketBand.None && spokenTicketBand != TicketBand.DayRider
            ? $"{spokenTicketBand}, {ClaimedStopsRemaining} stop(s)."
            : $"{ClaimedStopsRemaining} stop(s) to {destination}.";

        if (UsesDayRider)
        {
            return Say(
                $"I'm headed to {destination}. I've got a pass for it.",
                $"{destination}. DayRider.",
                $"Getting off at {destination}. Pass for today."
            );
        }

        return Say(
            $"{ticketAsk}",
            $"I'm heading to {destination}. {ticketAsk}",
            $"Getting off at {destination}. {ticketAsk}"
        );
    }

    public string GetAnswer(PassengerQuestionType questionType)
    {
        return questionType switch
        {
            PassengerQuestionType.CurrentStop => Say(
                GetSpokenCurrentStopName(),
                $"We're at {GetSpokenCurrentStopName()}.",
                $"This stop is {GetSpokenCurrentStopName()}."
            ),

            PassengerQuestionType.DestinationStop => Say(
                GetSpokenDestinationName(),
                $"I'm getting off at {GetSpokenDestinationName()}.",
                $"{GetSpokenDestinationName()}."
            ),

            PassengerQuestionType.Seat => string.IsNullOrWhiteSpace(GetActualSeatName())
                ? Say("I don't have a seat yet.", "Not seated yet.", "Not sat down yet.")
                : Say($"Seat {GetActualSeatName()}.", $"I'm in {GetActualSeatName()}.", $"{GetActualSeatName()}."),

            PassengerQuestionType.Fare => UsesDayRider
                ? BuildTicketStatusSummary()
                : GetSpokenFareAnswer(),

            _ => "..."
        };
    }

    private string GetSpokenCurrentStopName()
    {
        if (RouteStops.Instance == null)
            return "Unknown Stop";

        int nextIndex = RouteStops.Instance.NextStopIndex;
        int spokenIndex = nextIndex;

        if (IsAnomaly)
        {
            switch (stopsInfoAccuracy)
            {
                case StopInfoAccuracy.Incorrect:
                case StopInfoAccuracy.IntentionalLie:
                    spokenIndex = Mathf.Max(0, nextIndex - Random.Range(1, 3));
                    break;

                case StopInfoAccuracy.AccidentalMistake:
                    spokenIndex = Mathf.Max(0, nextIndex - 1);
                    break;
            }
        }

        return RouteStops.Instance.GetStopNameSafe(spokenIndex);
    }

    private string GetSpokenDestinationName()
    {
        if (RouteStops.Instance == null)
            return GetPublicDestinationName();

        if (!IsAnomaly)
            return GetPublicDestinationName();

        switch (stopsInfoAccuracy)
        {
            case StopInfoAccuracy.Incorrect:
            case StopInfoAccuracy.IntentionalLie:
                {
                    int fakeIndex = Mathf.Clamp(dropOffStopIndex + Random.Range(-2, 3), 0, 999);
                    if (fakeIndex == dropOffStopIndex)
                        fakeIndex = Mathf.Max(0, dropOffStopIndex - 1);

                    return RouteStops.Instance.GetDestinationName(fakeIndex);
                }

            case StopInfoAccuracy.AccidentalMistake:
                {
                    int offByOne = Mathf.Max(0, dropOffStopIndex - 1);
                    return RouteStops.Instance.GetDestinationName(offByOne);
                }

            default:
                return GetPublicDestinationName();
        }
    }

    private string GetSpokenFareAnswer()
    {
        if (UsesDayRider)
            return BuildTicketStatusSummary();

        if (!IsAnomaly)
            return $"I handed over {FareTable.FormatMoney(CashTenderedPence)}.";

        if (CashTenderedPence < ExpectedFare)
        {
            return Say(
                "That should cover it.",
                "That's enough.",
                "Yeah, that's the fare."
            );
        }

        return Say(
            $"I paid {FareTable.FormatMoney(Mathf.Max(0, CashTenderedPence - Random.Range(5, 30)))}.",
            "That's what I've paid.",
            "Should be right."
        );
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
        for (int i = 0; i < runtimeTendered.Count; i++)
        {
            if (i > 0) list += ", ";
            list += FareTable.FormatMoney(runtimeTendered[i]);
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

    public TicketBand GetCorrectTicketBand(FareTable fareTable)
    {
        if (fareTable == null)
            return TicketBand.None;

        if (UsesDayRider)
            return TicketBand.DayRider;

        return fareTable.GetBandForStops(TrueStopsRemaining);
    }

    private void AddIdItem(List<InspectionDeskItemState> output)
    {
        bool fakeId = idVisual != PassengerIdVisual.Real;
        bool obviousFake = idVisual == PassengerIdVisual.ObviousFake;

        AddIfNotSpawned(output, new InspectionDeskItemState
        {
            uniqueId = "passenger_id",
            kind = InspectionDeskItemKind.IdCard,
            title = VisiblePassengerName,
            subtitle = $"DOB {VisibleDateOfBirth}\nID {VisibleIdNumber}\nEXP {VisibleExpiryDate}",
            artKey = InspectionDeskArtLibrary.GetIdArtKey(idVisual),
            preferredSize = new Vector2(160f, 96f),

            // Real and subtle stolen IDs can show text. Only obvious fakes stay image-only.
            preferArtOnly = obviousFake,

            isPassengerOwned = true,
            isImportant = true,
            isFake = fakeId,
            defaultTopic = InspectionDeskClickTopic.IdCard,
            supportedTopics = new List<InspectionDeskClickTopic>
            {
                InspectionDeskClickTopic.IdCard,
                InspectionDeskClickTopic.IdPhoto,
                InspectionDeskClickTopic.IdName,
                InspectionDeskClickTopic.IdDob,
                InspectionDeskClickTopic.IdExpiry,
                InspectionDeskClickTopic.IdNumber
            }
        });
    }

    private void AddTicketItem(List<InspectionDeskItemState> output)
    {
        if (!UsesDayRider)
            return;

        AddIfNotSpawned(output, new InspectionDeskItemState
        {
            uniqueId = "passenger_ticket",
            kind = InspectionDeskItemKind.Ticket,
            title = string.IsNullOrWhiteSpace(ticketLabel) ? "DayRider" : ticketLabel,
            subtitle = string.IsNullOrWhiteSpace(ticketDateLabel) ? BuildTicketStatusSummary() : $"{BuildTicketStatusSummary()}\n{ticketDateLabel}",
            artKey = InspectionDeskArtLibrary.GetTicketArtKey(ticketState),
            preferredSize = new Vector2(180f, 90f),
            preferArtOnly = true,
            isPassengerOwned = true,
            isImportant = true,
            isFake = ticketState == PassengerTicketState.Fake,
            defaultTopic = InspectionDeskClickTopic.Ticket,
            supportedTopics = new List<InspectionDeskClickTopic>
            {
                InspectionDeskClickTopic.Ticket,
                InspectionDeskClickTopic.TicketValidity,
                InspectionDeskClickTopic.TicketRoute
            }
        });
    }

    private void AddPaymentItems(List<InspectionDeskItemState> output)
    {
        if (!UsesCash)
            return;

        EnsureTenderedBreakdown(null);
        EnsureFakeMoneyFlags();

        for (int i = 0; i < runtimeTendered.Count; i++)
        {
            int value = runtimeTendered[i];
            bool fake = fakeMoneyIndices.Contains(i);

            AddIfNotSpawned(output, new InspectionDeskItemState
            {
                uniqueId = $"passenger_cash_{i}",
                kind = InspectionDeskItemKind.Cash,
                title = FareTable.FormatMoney(value),
                subtitle = fake ? "looks off" : string.Empty,
                moneyValuePence = value,
                artKey = InspectionDeskArtLibrary.GetMoneyArtKey(value),
                preferredSize = value >= 500 ? new Vector2(96f, 64f) : new Vector2(24f, 24f),
                preferArtOnly = true,
                isPassengerOwned = true,
                isImportant = false,
                isFake = fake,
                defaultTopic = InspectionDeskClickTopic.Money,
                supportedTopics = new List<InspectionDeskClickTopic>
            {
                InspectionDeskClickTopic.Money,
                InspectionDeskClickTopic.MoneyAmount,
                InspectionDeskClickTopic.MoneyAuthenticity
            }
            });
        }
    }

    private void AddClutterItem(List<InspectionDeskItemState> output)
    {
        AddIfNotSpawned(output, new InspectionDeskItemState
        {
            uniqueId = "passenger_clutter",
            kind = InspectionDeskItemKind.Clutter,
            title = Say("old receipt", "crumpled paper", "used ticket stub"),
            subtitle = string.Empty,
            isPassengerOwned = true,
            isImportant = false,
            isTrash = true,
            defaultTopic = InspectionDeskClickTopic.Generic,
            supportedTopics = new List<InspectionDeskClickTopic> { InspectionDeskClickTopic.Generic }
        });
    }

    private void AddIfNotSpawned(List<InspectionDeskItemState> output, InspectionDeskItemState state)
    {
        if (state == null || output == null)
            return;

        if (spawnedDeskIds.Contains(state.uniqueId))
            return;

        spawnedDeskIds.Add(state.uniqueId);
        output.Add(state);
    }

    private void EnsureTenderedBreakdown(FareTable fareTable)
    {
        runtimeTendered.Clear();

        if (UsesDayRider)
            return;

        int[] supported = fareTable != null
            ? fareTable.GetDenominationValuesDescending()
            : new[] { 2000, 1000, 500, 200, 100, 50, 20, 10, 5 };

        if (tenderedDenominations != null && tenderedDenominations.Length > 0)
        {
            for (int i = 0; i < tenderedDenominations.Length; i++)
                AppendSupportedBreakdown(runtimeTendered, Mathf.Max(0, tenderedDenominations[i]), supported);

            PaidAmount = Sum(runtimeTendered);
            return;
        }

        int total = Mathf.Max(ExpectedFare, PaidAmount);
        if (PaidAmount > 0)
            total = PaidAmount;

        if (total <= 0)
            total = ExpectedFare;

        if (total <= 0)
            total = 5;

        // Small chance to overpay with a bigger real denomination
        if (supported != null && supported.Length > 0 && total < 2000 && Random.value < 0.18f)
        {
            int[] big = { 2000, 1000, 500 };
            int chosen = big[Random.Range(0, big.Length)];

            bool supportedBig = false;
            for (int i = 0; i < supported.Length; i++)
            {
                if (supported[i] == chosen)
                {
                    supportedBig = true;
                    break;
                }
            }

            if (supportedBig && chosen >= total)
            {
                runtimeTendered.Add(chosen);
                PaidAmount = chosen;
                return;
            }
        }

        AppendSupportedBreakdown(runtimeTendered, total, supported);

        if (runtimeTendered.Count == 0)
            runtimeTendered.Add(5);

        PaidAmount = Sum(runtimeTendered);
    }

    private static void AppendSupportedBreakdown(List<int> output, int amountPence, int[] supportedDescending)
    {
        if (output == null || amountPence <= 0)
            return;

        if (supportedDescending == null || supportedDescending.Length == 0)
        {
            output.Add(amountPence);
            return;
        }

        int remaining = amountPence;

        for (int i = 0; i < supportedDescending.Length; i++)
        {
            int value = supportedDescending[i];
            if (value <= 0)
                continue;

            while (remaining >= value)
            {
                output.Add(value);
                remaining -= value;
            }
        }

        // Round any odd remainder up to the smallest supported denomination.
        if (remaining > 0)
        {
            int smallest = supportedDescending[supportedDescending.Length - 1];
            if (smallest > 0)
                output.Add(smallest);
        }
    }

    private void EnsureFakeMoneyFlags()
    {
        fakeMoneyIndices.Clear();

        if (runtimeTendered.Count == 0)
            return;

        float chance = IsAnomaly ? fakeMoneyChanceAnomaly : fakeMoneyChanceHuman;
        if (Random.value > chance)
            return;

        int count = Mathf.Max(1, Mathf.RoundToInt(runtimeTendered.Count * (IsAnomaly ? 0.4f : 0.2f)));
        count = Mathf.Min(count, runtimeTendered.Count);

        while (fakeMoneyIndices.Count < count)
            fakeMoneyIndices.Add(Random.Range(0, runtimeTendered.Count));
    }
    public void CollectCurrentPaymentItems(List<InspectionDeskItemState> output)
    {
        if (output == null || !UsesCash)
            return;

        AddPaymentItems(output);
    }

    private string RespondToIdentityChallenge()
    {
        if (IsAnomaly)
        {
            return Say(
                "Looks close enough, doesn't it?",
                "You're overthinking it.",
                "People change."
            );
        }

        return Say(
            "It's me. Old photo, same person.",
            "I still match it well enough.",
            "It's my card."
        );
    }

    private string RespondToExpiryChallenge()
    {
        if (IsAnomaly || ticketState == PassengerTicketState.Old || string.Compare(expiryDate, "01/01/2025") < 0)
        {
            return Say(
                "It still should be alright.",
                "It's only just out.",
                "Come on, let it slide."
            );
        }

        return Say(
            "It isn't expired.",
            "Read it again.",
            "That date's fine."
        );
    }

    private string RespondToTicketChallenge()
    {
        if (!UsesDayRider)
            return Say("I'm paying cash.", "No pass - cash.");

        if (ticketState == PassengerTicketState.Valid)
            return Say("It's today's pass.", "It's valid for today.", "That's today's one.");

        if (ticketState == PassengerTicketState.Old)
            return Say("It still should work.", "It's only from yesterday.", "Close enough.");

        return Say("It's a pass, isn't it?", "Looks fine to me.", "It's good enough.");
    }

    private string RespondToMoneyAmount()
    {
        if (CashTenderedPence < ExpectedFare)
        {
            return Say(
                "That should cover it.",
                "It's near enough.",
                "I haven't got anything else."
            );
        }

        return Say(
            "That's what I'm paying.",
            "Yeah, that's the amount I've put down.",
            "That's all there."
        );
    }

    private string RespondToMoneyAuthenticity()
    {
        if (fakeMoneyIndices.Count == 0)
            return Say("It's real money.", "Course it is.", "Looks fine to me.");

        return Say(
            "It spends the same.",
            "That's what I was given.",
            "It's fine."
        );
    }

    private string RespondToOwnershipQuestion(InspectionDeskItemState item)
    {
        if (item == null)
            return Say("It's mine.", "Mine.", "That one's mine.");

        bool visibleMismatch =
            item.kind == InspectionDeskItemKind.IdCard &&
            useVisibleIdentityOverride &&
            (!string.Equals(VisiblePassengerName, passengerName, System.StringComparison.OrdinalIgnoreCase) ||
             !string.Equals(VisibleDateOfBirth, dateOfBirth, System.StringComparison.OrdinalIgnoreCase) ||
             !string.Equals(VisibleIdNumber, idNumber, System.StringComparison.OrdinalIgnoreCase));

        if (IsAnomaly && (visibleMismatch || item.isFake))
        {
            return Say(
                "It's mine. Just hand it back.",
                "Yeah, mine. Problem?",
                "Mine enough to use."
            );
        }

        if (!item.isPassengerOwned)
        {
            return Say(
                "That's yours, isn't it?",
                "That one isn't mine.",
                "I thought that was the driver's."
            );
        }

        return Say(
            "It's mine.",
            "Yeah, that's mine.",
            "Mine. I brought it with me."
        );
    }

    private string RespondToWhyHaveItem(InspectionDeskItemState item)
    {
        if (item == null)
            return Say("Because I need it.", "I brought it with me.", "It's for the journey.");

        bool visibleMismatch =
            item.kind == InspectionDeskItemKind.IdCard &&
            useVisibleIdentityOverride &&
            (!string.Equals(VisiblePassengerName, passengerName, System.StringComparison.OrdinalIgnoreCase) ||
             !string.Equals(VisibleDateOfBirth, dateOfBirth, System.StringComparison.OrdinalIgnoreCase) ||
             !string.Equals(VisibleIdNumber, idNumber, System.StringComparison.OrdinalIgnoreCase));

        if (IsAnomaly && visibleMismatch)
        {
            return Say(
                "I just picked it up in a rush.",
                "Thought it was mine.",
                "Grabbed the wrong one, didn't I?"
            );
        }

        if (item.kind == InspectionDeskItemKind.Cash)
            return Say("To pay the fare.", "Because I'm paying.", "It's my fare money.");

        if (item.kind == InspectionDeskItemKind.Ticket)
            return Say("For the bus.", "Because that's my ticket.", "It's my pass for the route.");

        if (item.kind == InspectionDeskItemKind.IdCard)
            return Say("Because that's my ID.", "I need it for boarding.", "It's my card.");

        return Say("Because I brought it with me.", "It's mine to carry.", "Had it on me already.");
    }

    private string GetStandingRepeatLine()
    {
        return Say(
            "Same as before.",
            "I already told you.",
            "Nothing changed."
        );
    }

    private string GetSeatedRepeatLine()
    {
        return Say(
            "I'm already seated.",
            "You've already let me on.",
            "Still the same answer."
        );
    }

    private string GetTonePrefix()
    {
        if (deskQuestionsAsked < deskTolerance)
            return string.Empty;

        if (deskQuestionsAsked == deskTolerance)
            return Say("Look. ", "Right. ", "Listen. ");

        return Say("Seriously. ", "Come on. ", "Enough now. ");
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
            int year = 1975 + ((seed / 336) % 25);
            dateOfBirth = $"{day:00}/{month:00}/{year}";
        }

        if (string.IsNullOrWhiteSpace(idNumber))
            idNumber = $"ID-{(100000 + (seed % 900000))}";

        if (string.IsNullOrWhiteSpace(expiryDate))
        {
            int day = ((seed / 7) % 28) + 1;
            int month = ((seed / 13) % 12) + 1;
            int year = 2027 + ((seed / 19) % 4);
            expiryDate = $"{day:00}/{month:00}/{year}";
        }

        if (!useVisibleIdentityOverride)
        {
            visiblePassengerName = passengerName;
            visibleDateOfBirth = dateOfBirth;
            visibleIdNumber = idNumber;
            visibleExpiryDate = expiryDate;
        }
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

    private static List<int> CopyToList(int[] source)
    {
        List<int> values = new List<int>();
        if (source == null)
            return values;

        for (int i = 0; i < source.Length; i++)
            values.Add(Mathf.Max(0, source[i]));
        return values;
    }

    private static int Sum(List<int> values)
    {
        int total = 0;
        if (values == null) return 0;
        for (int i = 0; i < values.Count; i++)
            total += Mathf.Max(0, values[i]);
        return total;
    }

    private static string Say(params string[] options)
    {
        if (options == null || options.Length == 0)
            return "...";

        return options[Random.Range(0, options.Length)];
    }

    private static TicketBand PickWrongBand(TicketBand correctBand)
    {
        TicketBand[] options = { TicketBand.Short, TicketBand.Medium, TicketBand.Long };
        TicketBand picked = correctBand;
        int guard = 0;

        while (picked == correctBand && guard++ < 12)
            picked = options[Random.Range(0, options.Length)];

        return picked;
    }
    public void RevealMissingPaymentItems(FareTable fareTable, List<InspectionDeskItemState> output)
    {
        if (output == null)
            return;

        PrepareDeskSession(fareTable);

        if (deskPaymentPresented || !UsesCash)
            return;

        deskPaymentPresented = true;
        AddPaymentItems(output);
    }

}