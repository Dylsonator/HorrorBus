using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InspectionDeskUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform itemLayer;
    [SerializeField] private InspectionDeskItemView itemPrefab;

    [Header("Zones")]
    [SerializeField] private InspectionDeskZoneBox sharedTrayZone;
    [SerializeField] private InspectionDeskZoneBox floatTrayZone;
    [SerializeField] private InspectionDeskZoneBox binZone;
    [SerializeField] private InspectionDeskZoneBox reviewAreaZone;

    [Header("Speech / labels")]
    [SerializeField] private TMP_Text speechNameText;
    [SerializeField] private TMP_Text speechBodyText;
    [SerializeField] private TMP_Text currentStopText;
    [SerializeField] private TMP_Text selectedTicketBandText;
    [SerializeField] private TMP_Text fareHintText;

    [Header("Popup")]
    [SerializeField] private InspectionDeskQuestionPopup questionPopup;

    [Header("Buttons")]
    [SerializeField] private Button seatButton;
    [SerializeField] private Button denyButton;

    [Header("Art / layout")]
    [SerializeField] private InspectionDeskArtLibrary artLibrary;
    [SerializeField] private InspectionDeskFloatSlot[] floatSlots;

    [Header("Behaviour")]
    [SerializeField] private bool keepFakeEvidenceBetweenPassengers = true;
    [SerializeField] private float autoRevealInterval = 4f;
    [SerializeField] private float interferenceInterval = 7f;

    [Header("Debug")]
    [SerializeField] private bool debugFloatSlots = false;

    public event Action SeatRequested;
    public event Action DenyRequested;

    private readonly List<InspectionDeskItemView> runtimeItems = new List<InspectionDeskItemView>();
    private Passenger currentPassenger;
    private DriverWallet currentWallet;
    private FareTable currentFareTable;
    private float revealTimer;
    private float interferenceTimer;
    private TicketBand selectedTicketBand = TicketBand.None;

    public Passenger CurrentPassenger => currentPassenger;
    public bool IsOpen => root != null && root.activeSelf;
    public RectTransform ItemLayer => itemLayer;

    private void Awake()
    {
        if (root == null) root = gameObject;
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (itemLayer == null) itemLayer = transform as RectTransform;
        if (artLibrary == null) artLibrary = FindFirstObjectByType<InspectionDeskArtLibrary>(FindObjectsInactive.Include);

        RefreshFloatSlots();

        if (seatButton != null) seatButton.onClick.AddListener(OnSeatPressed);
        if (denyButton != null) denyButton.onClick.AddListener(() => DenyRequested?.Invoke());

        if (root != null)
            root.SetActive(false);
    }

    private void OnValidate()
    {
        RefreshFloatSlots();
    }

    private void Update()
    {
        if (!IsOpen || currentPassenger == null)
            return;

        UpdateHeaderTexts();

        revealTimer += Time.unscaledDeltaTime;
        if (revealTimer >= autoRevealInterval)
        {
            revealTimer = 0f;

            if (currentPassenger.TryAutoRevealMissingItem(currentFareTable, out InspectionDeskItemState revealedItem, out string revealLine))
            {
                if (revealedItem != null)
                {
                    if (revealedItem.kind == InspectionDeskItemKind.Cash && currentPassenger.UsesCash)
                    {
                        List<InspectionDeskItemState> remainingPaymentItems = new List<InspectionDeskItemState>();
                        currentPassenger.CollectCurrentPaymentItems(remainingPaymentItems);

                        SpawnItemInSharedTray(revealedItem);

                        for (int i = 0; i < remainingPaymentItems.Count; i++)
                            SpawnItemInSharedTray(remainingPaymentItems[i]);
                    }
                    else
                    {
                        SpawnItemInSharedTray(revealedItem);
                    }
                }

                if (!string.IsNullOrWhiteSpace(revealLine))
                    Say(revealLine);
            }
            else if (currentPassenger.TryGetIdleChatter(out string chatter))
            {
                Say(chatter);
            }
        }

        interferenceTimer += Time.unscaledDeltaTime;
        if (interferenceTimer >= interferenceInterval)
        {
            interferenceTimer = 0f;
            TryRarePassengerInterference();
        }
    }

    public void Open(Passenger passenger, DriverWallet wallet, FareTable fareTable)
    {
        // Reopen same passenger = keep current desk exactly as it is.
        if (currentPassenger != null && passenger == currentPassenger)
        {
            currentWallet = wallet;
            currentFareTable = fareTable;
            UpdateHeaderTexts();

            if (root != null)
                root.SetActive(true);

            questionPopup?.Hide();
            return;
        }

        currentPassenger = passenger;
        currentWallet = wallet;
        currentFareTable = fareTable;
        selectedTicketBand = TicketBand.None;
        revealTimer = 0f;
        interferenceTimer = 0f;

        RefreshFloatSlots();

        if (currentPassenger != null)
            currentPassenger.PrepareDeskSession(currentFareTable);

        ClearTransientItemsForNewPassenger();
        EnsureFloatTemplates();

        if (currentPassenger != null)
        {
            List<InspectionDeskItemState> items = new List<InspectionDeskItemState>();
            currentPassenger.BuildInitialDeskItems(currentFareTable, items);

            for (int i = 0; i < items.Count; i++)
                SpawnItemInSharedTray(items[i]);

            Say(currentPassenger.GetOpeningStatement(), "Passenger");
        }

        UpdateHeaderTexts();

        if (root != null)
            root.SetActive(true);

        questionPopup?.Hide();
    }

    public void ShowExistingSession()
    {
        if (currentPassenger == null)
            return;

        UpdateHeaderTexts();

        if (root != null)
            root.SetActive(true);

        questionPopup?.Hide();
    }

    public void HideViewOnly()
    {
        questionPopup?.Hide();

        if (root != null)
            root.SetActive(false);
    }

    public void CloseAndForgetCurrent(bool accepted)
    {
        ResolveCurrentPassengerItems(accepted);
        currentPassenger = null;
        selectedTicketBand = TicketBand.None;
        questionPopup?.Hide();

        if (root != null)
            root.SetActive(false);
    }

    public List<InspectionDeskItemState> GetDeskItemsSnapshot()
    {
        List<InspectionDeskItemState> result = new List<InspectionDeskItemState>();

        for (int i = 0; i < runtimeItems.Count; i++)
        {
            InspectionDeskItemView item = runtimeItems[i];
            if (item == null || item.Data == null)
                continue;

            if (item.Data.isTemplateSource)
                continue;

            result.Add(item.Data.Clone());
        }

        return result;
    }

    private void OnSeatPressed()
    {
        if (currentPassenger != null && !currentPassenger.IsAnomaly)
        {
            bool humanHasIdInSharedTray = false;

            for (int i = 0; i < runtimeItems.Count; i++)
            {
                InspectionDeskItemView item = runtimeItems[i];
                if (item == null || item.Data == null)
                    continue;

                if (!item.Data.isPassengerOwned)
                    continue;

                if (item.Data.kind != InspectionDeskItemKind.IdCard)
                    continue;

                if (!IsItemInsideZone(item, sharedTrayZone))
                    continue;

                humanHasIdInSharedTray = true;
                break;
            }

            if (!humanHasIdInSharedTray)
            {
                Say("Hang on. You still need to give them their ID back.", "Driver");
                return;
            }
        }

        SeatRequested?.Invoke();
    }

    public void SetSelectedTicketBand(TicketBand band)
    {
        selectedTicketBand = band;
        UpdateHeaderTexts();

        if (band == TicketBand.DayRider)
            Say("You marked it as a DayRider.");
        else if (band != TicketBand.None)
            Say($"You marked it as {band}.");
    }

    public TicketBand GetSelectedTicketBand() => selectedTicketBand;

    public void PrintTicket(TicketBand band)
    {
        if (currentPassenger == null)
            return;

        if (band == TicketBand.None)
            return;

        SetSelectedTicketBand(band);
        RemoveExistingIssuedTickets();

        string todayText = System.DateTime.Now.ToString("dd/MM/yyyy");

        string title = band switch
        {
            TicketBand.Short => "SHORT",
            TicketBand.Medium => "MEDIUM",
            TicketBand.Long => "LONG",
            TicketBand.DayRider => "DAYRIDER",
            _ => "TICKET"
        };

        string dateLine = todayText;
        string routeLine = currentFareTable != null
            ? currentFareTable.GetBandStopsLabel(band)
            : (band == TicketBand.DayRider ? "ALL DAY" : "ROUTE");

        InspectionDeskItemState printed = new InspectionDeskItemState
        {
            uniqueId = $"issued_ticket_{band}",
            kind = InspectionDeskItemKind.Ticket,
            title = title,
            subtitle = $"{dateLine}\n{routeLine}",
            isPassengerOwned = false,
            isImportant = true,
            isFake = false,
            artKey = InspectionDeskArtLibrary.GetTicketArtKey(PassengerTicketState.Valid),
            preferredSize = new Vector2(120f, 60f),
            preferArtOnly = false,
            defaultTopic = InspectionDeskClickTopic.Ticket,
            supportedTopics = new List<InspectionDeskClickTopic>
        {
            InspectionDeskClickTopic.Ticket,
            InspectionDeskClickTopic.TicketValidity,
            InspectionDeskClickTopic.TicketRoute
        }
        };

        Vector2 pos = reviewAreaZone != null
            ? reviewAreaZone.GetRandomAnchoredPointWithin(itemLayer)
            : Vector2.zero;

        SpawnView(printed, pos);
        Say($"Printed {title} ticket.");
    }

    public int GetImportantIssuedTicketsOutsideSharedTrayCount()
    {
        int total = 0;

        for (int i = 0; i < runtimeItems.Count; i++)
        {
            InspectionDeskItemView item = runtimeItems[i];
            if (item == null || item.Data == null)
                continue;

            if (item.Data.kind != InspectionDeskItemKind.Ticket)
                continue;

            if (item.Data.isPassengerOwned)
                continue;

            if (!item.Data.isImportant)
                continue;

            if (item.Data.isEvidenceReference)
                continue;

            if (IsItemInsideZone(item, sharedTrayZone))
                continue;

            total++;
        }

        return total;
    }

    public bool IsFareCorrect()
    {
        if (currentPassenger == null)
            return true;

        if (currentPassenger.UsesDayRider)
            return currentPassenger.IsDayRiderValid;

        if (currentFareTable == null)
        {
            int returned = GetReturnedChangeToPassengerPence();
            return currentPassenger.CashTenderedPence >= currentPassenger.ExpectedFare &&
                   returned == currentPassenger.ChangeDuePence;
        }

        TicketBand correctBand = currentPassenger.GetCorrectTicketBand(currentFareTable);
        int returnedChange = GetReturnedChangeToPassengerPence();

        return selectedTicketBand == correctBand &&
               currentPassenger.CashTenderedPence >= currentPassenger.ExpectedFare &&
               returnedChange == currentPassenger.ChangeDuePence;
    }

    public int GetReturnedChangeToPassengerPence()
    {
        int total = 0;

        for (int i = 0; i < runtimeItems.Count; i++)
        {
            InspectionDeskItemView item = runtimeItems[i];
            if (item == null || item.Data == null)
                continue;

            if (item.Data.kind != InspectionDeskItemKind.Cash)
                continue;

            if (item.Data.isTemplateSource)
                continue;

            if (item.Data.isPassengerOwned)
                continue;

            if (!IsItemInsideZone(item, sharedTrayZone))
                continue;

            total += Mathf.Max(0, item.Data.moneyValuePence);
        }

        return total;
    }

    public int GetImportantPassengerItemsOutsideSharedTrayCount()
    {
        int total = 0;

        for (int i = 0; i < runtimeItems.Count; i++)
        {
            InspectionDeskItemView item = runtimeItems[i];
            if (item == null || item.Data == null)
                continue;

            if (item.Data.isTemplateSource)
                continue;

            if (!item.Data.isPassengerOwned)
                continue;

            if (!item.Data.isImportant)
                continue;

            if (IsItemInsideZone(item, sharedTrayZone))
                continue;

            total++;
        }

        return total;
    }

    public void ResolveCurrentPassengerItems(bool accepted)
    {
        for (int i = runtimeItems.Count - 1; i >= 0; i--)
        {
            InspectionDeskItemView item = runtimeItems[i];
            if (item == null || item.Data == null)
                continue;

            if (item.Data.isTemplateSource)
                continue;

            if (!item.Data.isPassengerOwned)
            {
                if ((item.Data.kind == InspectionDeskItemKind.Cash || item.Data.kind == InspectionDeskItemKind.Ticket) && !item.Data.isEvidenceReference)
                {
                    Destroy(item.gameObject);
                    runtimeItems.RemoveAt(i);
                }
                continue;
            }

            bool keepAsEvidence = keepFakeEvidenceBetweenPassengers &&
                                  !accepted &&
                                  (item.Data.isFake || item.Data.kind == InspectionDeskItemKind.Clutter || item.Data.kind == InspectionDeskItemKind.Note || item.Data.kind == InspectionDeskItemKind.Evidence);

            if (keepAsEvidence)
            {
                item.Data.isPassengerOwned = false;
                item.Data.isEvidenceReference = true;
                item.RefreshVisuals();
                continue;
            }

            Destroy(item.gameObject);
            runtimeItems.RemoveAt(i);
        }
    }

    public void NotifyBeginDrag(InspectionDeskItemView item)
    {
        if (item != null)
            item.transform.SetAsLastSibling();

        questionPopup?.Hide();
    }

    public void NotifyEndDrag(InspectionDeskItemView item, Vector2 screenPoint)
    {
        if (item == null || item.Data == null)
            return;

        if (item.Data.isTemplateSource)
        {
            if (floatTrayZone == null || !floatTrayZone.ContainsScreenPoint(canvas, null, screenPoint))
                CreateActualCopyFromTemplate(item);
            return;
        }

        if (binZone != null && binZone.ContainsScreenPoint(canvas, null, screenPoint))
            TryBinItem(item);

        ClampToCanvas(item);
    }

    public void HandleItemClick(InspectionDeskItemView item, Vector2 screenPoint, Camera eventCamera, InspectionDeskClickTopic? forcedTopic)
    {
        if (item == null || item.Data == null || currentPassenger == null)
            return;

        questionPopup?.Hide();

        InspectionDeskClickTopic primaryTopic = forcedTopic ?? item.Data.defaultTopic;
        List<InspectionDeskQuestionOption> options = new List<InspectionDeskQuestionOption>();
        HashSet<string> seenIds = new HashSet<string>();

        void AddOptionsForTopic(InspectionDeskClickTopic topic)
        {
            List<InspectionDeskQuestionOption> temp = new List<InspectionDeskQuestionOption>();
            currentPassenger.BuildQuestionOptions(topic, item.Data, temp);

            for (int i = 0; i < temp.Count; i++)
            {
                InspectionDeskQuestionOption opt = temp[i];
                if (opt == null || string.IsNullOrWhiteSpace(opt.id))
                    continue;

                if (seenIds.Add(opt.id))
                    options.Add(opt);
            }
        }

        AddOptionsForTopic(primaryTopic);

        if (item.Data.supportedTopics != null)
        {
            for (int i = 0; i < item.Data.supportedTopics.Count; i++)
            {
                InspectionDeskClickTopic extra = item.Data.supportedTopics[i];
                if (extra == primaryTopic)
                    continue;

                AddOptionsForTopic(extra);
            }
        }

        AddOwnershipQuestionsForItem(item.Data, options, seenIds);

        if (options.Count == 0)
            return;

        questionPopup?.ShowAtScreenPoint(canvas, eventCamera, screenPoint, BuildTopicTitle(primaryTopic), options, option =>
        {
            List<InspectionDeskItemState> spawned = new List<InspectionDeskItemState>();
            string reply = currentPassenger.AnswerDeskQuestion(primaryTopic, option.id, currentFareTable, item.Data, spawned);

            for (int i = 0; i < spawned.Count; i++)
                SpawnItemInSharedTray(spawned[i]);

            if (!string.IsNullOrWhiteSpace(reply))
                Say(reply);
        });
    }

    public void PromptMissingId()
    {
        PromptMissingInternal(InspectionDeskClickTopic.MissingId, "request_id");
    }

    public void PromptMissingTicket()
    {
        PromptMissingInternal(InspectionDeskClickTopic.MissingTicket, "request_ticket");
    }

    public void PromptMissingPayment()
    {
        PromptMissingInternal(InspectionDeskClickTopic.MissingPayment, "request_payment");
    }

    public void AskLegacyQuestion(PassengerQuestionType questionType)
    {
        if (currentPassenger == null)
            return;

        string prompt = questionType switch
        {
            PassengerQuestionType.CurrentStop => "What stop is this?",
            PassengerQuestionType.DestinationStop => "Where are you getting off?",
            PassengerQuestionType.Seat => "Which seat is yours?",
            PassengerQuestionType.Fare => "How are you paying?",
            _ => "Question"
        };

        Say(currentPassenger.GetAnswer(questionType), prompt);
    }

    public bool TryResolveArt(InspectionDeskItemState state, out Sprite baseSprite, out Sprite overlaySprite, out Vector2 size, out bool hideText, out Color tint)
    {
        if (artLibrary != null)
            return artLibrary.TryGetArt(state, out baseSprite, out overlaySprite, out size, out hideText, out tint);

        baseSprite = null;
        overlaySprite = null;
        size = state != null ? state.preferredSize : Vector2.zero;
        hideText = false;
        tint = Color.white;
        return false;
    }

    public Color GetSuggestedTint(InspectionDeskItemState state)
    {
        if (state == null)
            return new Color(0.88f, 0.88f, 0.88f, 0.96f);

        if (state.isTemplateSource)
            return new Color(0.85f, 0.95f, 1f, 0.98f);

        if (state.isFake)
            return new Color(1f, 0.82f, 0.82f, 0.98f);

        return state.kind switch
        {
            InspectionDeskItemKind.IdCard => new Color(0.88f, 0.92f, 1f, 0.98f),
            InspectionDeskItemKind.Ticket => new Color(0.92f, 1f, 0.88f, 0.98f),
            InspectionDeskItemKind.Cash => new Color(0.92f, 1f, 0.92f, 0.98f),
            InspectionDeskItemKind.Clutter => new Color(0.95f, 0.92f, 0.85f, 0.98f),
            InspectionDeskItemKind.Note => new Color(1f, 0.97f, 0.80f, 0.98f),
            _ => new Color(0.9f, 0.9f, 0.9f, 0.98f)
        };
    }

    public void ClampToCanvas(InspectionDeskItemView item)
    {
        if (item == null || itemLayer == null)
            return;

        RectTransform itemRect = item.Rect;
        Rect bounds = itemLayer.rect;
        Vector2 pos = itemRect.anchoredPosition;
        Vector2 size = itemRect.rect.size * 0.5f;

        pos.x = Mathf.Clamp(pos.x, bounds.xMin + size.x, bounds.xMax - size.x);
        pos.y = Mathf.Clamp(pos.y, bounds.yMin + size.y, bounds.yMax - size.y);

        itemRect.anchoredPosition = pos;
    }

    public bool IsItemInsideZone(InspectionDeskItemView item, InspectionDeskZoneBox zone)
    {
        if (item == null || zone == null || itemLayer == null)
            return false;

        Vector3 world = item.Rect.TransformPoint(item.Rect.rect.center);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
        return zone.ContainsScreenPoint(canvas, null, screen);
    }

    public void Say(string line, string speaker = "Passenger")
    {
        if (speechNameText != null)
            speechNameText.text = speaker;

        if (speechBodyText != null)
            speechBodyText.text = line;
    }

    private void PromptMissingInternal(InspectionDeskClickTopic topic, string optionId)
    {
        if (currentPassenger == null)
            return;

        List<InspectionDeskItemState> spawned = new List<InspectionDeskItemState>();
        string reply = currentPassenger.AnswerDeskQuestion(topic, optionId, currentFareTable, null, spawned);

        for (int i = 0; i < spawned.Count; i++)
            SpawnItemInSharedTray(spawned[i]);

        if (!string.IsNullOrWhiteSpace(reply))
            Say(reply);
    }

    private void UpdateHeaderTexts()
    {
        if (currentStopText != null)
        {
            string stopName = RouteStops.Instance != null ? RouteStops.Instance.GetStopNameSafe(RouteStops.Instance.NextStopIndex) : "Unknown Stop";
            currentStopText.text = stopName;
        }

        if (selectedTicketBandText != null)
        {
            selectedTicketBandText.text = selectedTicketBand == TicketBand.None
                ? "Ticket: none selected"
                : $"Ticket: {selectedTicketBand}";
        }

        if (fareHintText != null)
        {
            if (currentPassenger == null || currentFareTable == null)
            {
                fareHintText.text = string.Empty;
            }
            else if (currentPassenger.UsesDayRider)
            {
                fareHintText.text = "Check the pass and the date.";
            }
            else
            {
                TicketBand correctBand = currentPassenger.GetCorrectTicketBand(currentFareTable);
                fareHintText.text = $"Expected band is based on stops. Current correct band: {correctBand} / fare {FareTable.FormatMoney(currentPassenger.ExpectedFare)}";
            }
        }
    }

    private void ClearTransientItemsForNewPassenger()
    {
        for (int i = runtimeItems.Count - 1; i >= 0; i--)
        {
            InspectionDeskItemView item = runtimeItems[i];
            if (item == null || item.Data == null)
            {
                runtimeItems.RemoveAt(i);
                continue;
            }

            if (item.Data.isTemplateSource)
                continue;

            if (item.Data.isEvidenceReference)
                continue;

            Destroy(item.gameObject);
            runtimeItems.RemoveAt(i);
        }
    }

    private void RefreshFloatSlots()
    {
        if (floatTrayZone == null)
            return;

        floatSlots = floatTrayZone.GetComponentsInChildren<InspectionDeskFloatSlot>(true);

        if (!debugFloatSlots)
            return;

        if (floatSlots == null || floatSlots.Length == 0)
        {
            Debug.LogWarning("[DeskUI] No float slots found under FloatTrayZone.");
            return;
        }

        for (int i = 0; i < floatSlots.Length; i++)
        {
            if (floatSlots[i] == null)
                continue;

            Debug.Log($"[DeskUI] Float slot found: {floatSlots[i].name} -> {floatSlots[i].DenominationPence}p");
        }
    }

    private void EnsureFloatTemplates()
    {
        if (itemPrefab == null || itemLayer == null || floatTrayZone == null)
            return;

        RefreshFloatSlots();

        int[] values = new[] { 2000, 1000, 500, 200, 100, 50, 20, 10, 5 };

        for (int i = 0; i < values.Length; i++)
        {
            int value = values[i];
            string wantedId = $"float_template_{value}";

            bool alreadyExists = false;

            for (int j = 0; j < runtimeItems.Count; j++)
            {
                InspectionDeskItemView existing = runtimeItems[j];
                if (existing == null || existing.Data == null)
                    continue;

                if (!existing.Data.isTemplateSource)
                    continue;

                if (existing.Data.uniqueId == wantedId)
                {
                    alreadyExists = true;
                    break;
                }
            }

            if (alreadyExists)
                continue;

            InspectionDeskItemState state = new InspectionDeskItemState
            {
                uniqueId = wantedId,
                kind = InspectionDeskItemKind.Cash,
                title = currentFareTable != null ? currentFareTable.GetLabelForValue(value) : FareTable.FormatMoney(value),
                subtitle = "Float",
                moneyValuePence = value,
                isPassengerOwned = false,
                isImportant = false,
                isTemplateSource = true,
                artKey = InspectionDeskArtLibrary.GetMoneyArtKey(value),
                preferredSize = value >= 500 ? new Vector2(136f, 92f) : new Vector2(64f, 64f),
                preferArtOnly = true,
                defaultTopic = InspectionDeskClickTopic.Money,
                supportedTopics = new List<InspectionDeskClickTopic>
                {
                    InspectionDeskClickTopic.Money,
                    InspectionDeskClickTopic.MoneyAmount,
                    InspectionDeskClickTopic.MoneyAuthenticity
                }
            };

            Vector2 pos = TryGetFloatSlotPosition(value, out Vector2 slotPos)
                ? slotPos
                : floatTrayZone.GetRandomAnchoredPointWithin(itemLayer);

            SpawnView(state, pos);

            if (debugFloatSlots)
                Debug.Log($"[DeskUI] Spawned float template {wantedId} value={value} at {pos}");
        }
    }

    private bool TryGetFloatSlotPosition(int valuePence, out Vector2 anchoredPos)
    {
        anchoredPos = Vector2.zero;

        RefreshFloatSlots();

        if (floatSlots == null || floatSlots.Length == 0 || itemLayer == null)
        {
            if (debugFloatSlots)
                Debug.LogWarning($"[DeskUI] No float slots available for {valuePence}p.");
            return false;
        }

        for (int i = 0; i < floatSlots.Length; i++)
        {
            InspectionDeskFloatSlot slot = floatSlots[i];
            if (slot == null)
                continue;

            if (debugFloatSlots)
                Debug.Log($"[DeskUI] Checking slot {slot.name} value {slot.DenominationPence}p against {valuePence}p");

            if (slot.DenominationPence != valuePence)
                continue;

            anchoredPos = slot.GetAnchoredPointWithin(itemLayer);

            if (debugFloatSlots)
                Debug.Log($"[DeskUI] Matched slot {slot.name} for {valuePence}p at {anchoredPos}");

            return true;
        }

        if (debugFloatSlots)
            Debug.LogWarning($"[DeskUI] No matching float slot found for {valuePence}p. Falling back to random position.");

        return false;
    }

    private void SpawnItemInSharedTray(InspectionDeskItemState state)
    {
        if (state == null)
            return;

        Vector2 pos = sharedTrayZone != null ? sharedTrayZone.GetRandomAnchoredPointWithin(itemLayer) : Vector2.zero;
        SpawnView(state, pos);
    }

    private InspectionDeskItemView SpawnView(InspectionDeskItemState state, Vector2 anchoredPos)
    {
        if (itemPrefab == null || itemLayer == null)
            return null;

        InspectionDeskItemView view = Instantiate(itemPrefab, itemLayer);

        view.name = state != null && !string.IsNullOrWhiteSpace(state.uniqueId)
            ? state.uniqueId
            : "DeskItem";

        view.Initialise(this, canvas, state, anchoredPos);
        runtimeItems.Add(view);

        if (debugFloatSlots)
            Debug.Log($"[DeskUI] SpawnView -> {view.name}, value={state.moneyValuePence}, pos={anchoredPos}");

        return view;
    }

    private void CreateActualCopyFromTemplate(InspectionDeskItemView template)
    {
        if (template == null || template.Data == null)
            return;

        InspectionDeskItemState clone = template.Data.Clone();
        clone.uniqueId = Guid.NewGuid().ToString("N");
        clone.isTemplateSource = false;
        clone.subtitle = string.Empty;
        clone.isPassengerOwned = false;
        clone.isImportant = false;
        clone.defaultTopic = InspectionDeskClickTopic.Money;

        Vector2 spawnPos = template.Rect.anchoredPosition + new Vector2(UnityEngine.Random.Range(30f, 55f), UnityEngine.Random.Range(-20f, 20f));
        SpawnView(clone, spawnPos);
    }

    private void TryBinItem(InspectionDeskItemView item)
    {
        if (item == null || item.Data == null)
            return;

        InspectionDeskItemState data = item.Data;
        bool protectedPassengerItem = data.isPassengerOwned && data.isImportant && !data.isFake && !data.isTrash;

        if (protectedPassengerItem)
        {
            Say("Hey, I still need that back.");
            return;
        }

        runtimeItems.Remove(item);
        Destroy(item.gameObject);
    }

    private void TryRarePassengerInterference()
    {
        if (currentPassenger == null)
            return;

        if (!currentPassenger.TryPassengerTrayInterference(out string line))
            return;

        List<InspectionDeskItemView> trayReachable = new List<InspectionDeskItemView>();
        for (int i = 0; i < runtimeItems.Count; i++)
        {
            InspectionDeskItemView view = runtimeItems[i];
            if (view == null || view.Data == null || view.Data.isTemplateSource)
                continue;

            if (!view.Data.isPassengerOwned)
                continue;

            if (!IsItemInsideZone(view, sharedTrayZone))
                continue;

            trayReachable.Add(view);
        }

        if (trayReachable.Count == 0)
            return;

        InspectionDeskItemView picked = trayReachable[UnityEngine.Random.Range(0, trayReachable.Count)];
        Vector2 newPos = sharedTrayZone.GetRandomAnchoredPointWithin(itemLayer);
        picked.SetAnchoredPosition(newPos);

        if (!string.IsNullOrWhiteSpace(line))
            Say(line);
    }

    private void RemoveExistingIssuedTickets()
    {
        for (int i = runtimeItems.Count - 1; i >= 0; i--)
        {
            InspectionDeskItemView item = runtimeItems[i];
            if (item == null || item.Data == null)
                continue;

            if (item.Data.kind != InspectionDeskItemKind.Ticket)
                continue;

            if (item.Data.isPassengerOwned)
                continue;

            if (item.Data.isEvidenceReference)
                continue;

            Destroy(item.gameObject);
            runtimeItems.RemoveAt(i);
        }
    }

    private string BuildTopicTitle(InspectionDeskClickTopic topic)
    {
        return topic switch
        {
            InspectionDeskClickTopic.IdPhoto => "Photo",
            InspectionDeskClickTopic.IdName => "Name",
            InspectionDeskClickTopic.IdDob => "Date of Birth",
            InspectionDeskClickTopic.IdExpiry => "Expiry",
            InspectionDeskClickTopic.IdNumber => "ID Number",
            InspectionDeskClickTopic.Ticket => "Ticket",
            InspectionDeskClickTopic.TicketValidity => "Ticket Validity",
            InspectionDeskClickTopic.TicketRoute => "Route",
            InspectionDeskClickTopic.Money => "Money",
            InspectionDeskClickTopic.MoneyAmount => "Amount",
            InspectionDeskClickTopic.MoneyAuthenticity => "Money",
            InspectionDeskClickTopic.PassengerFace => "Face",
            InspectionDeskClickTopic.PassengerHair => "Hair",
            InspectionDeskClickTopic.PassengerClothes => "Clothes",
            InspectionDeskClickTopic.PassengerBehaviour => "Behaviour",
            InspectionDeskClickTopic.MissingId => "ID",
            InspectionDeskClickTopic.MissingTicket => "Ticket",
            InspectionDeskClickTopic.MissingPayment => "Payment",
            _ => "Question"
        };
    }

    private void AddOwnershipQuestionsForItem(InspectionDeskItemState item, List<InspectionDeskQuestionOption> options, HashSet<string> seenIds)
    {
        if (item == null || options == null || seenIds == null)
            return;

        void Add(string id, string label)
        {
            if (string.IsNullOrWhiteSpace(id) || seenIds.Contains(id))
                return;

            seenIds.Add(id);
            options.Add(new InspectionDeskQuestionOption(id, label));
        }

        if (item.kind == InspectionDeskItemKind.IdCard)
        {
            Add("ownership_id", "Whose ID is this?");
            Add("ownership_why_have_id", "Why have you got this ID?");
        }
        else if (item.kind == InspectionDeskItemKind.Ticket)
        {
            Add("ownership_ticket", "Is this ticket yours?");
            Add("ownership_why_have_ticket", "Why have you got this ticket?");
        }
        else if (item.kind == InspectionDeskItemKind.Cash)
        {
            Add("ownership_money", "Is this all your money?");
        }
        else
        {
            Add("ownership_item", "Whose is this?");
            Add("ownership_why_have_item", "Why have you got this?");
        }
    }

    public void HandleZoneClick(InspectionDeskZoneBox zone, Vector2 screenPoint, Camera eventCamera)
    {
        if (zone == null || currentPassenger == null || questionPopup == null)
            return;

        questionPopup.Hide();

        List<InspectionDeskQuestionOption> options = new List<InspectionDeskQuestionOption>();

        if (zone.ZoneKind == InspectionDeskZoneKind.SharedTray)
        {
            bool hasPassengerId = false;
            bool hasPassengerTicket = false;
            int passengerMoneyTotal = 0;

            for (int i = 0; i < runtimeItems.Count; i++)
            {
                InspectionDeskItemView view = runtimeItems[i];
                if (view == null || view.Data == null)
                    continue;

                if (!view.Data.isPassengerOwned)
                    continue;

                if (!IsItemInsideZone(view, sharedTrayZone))
                    continue;

                switch (view.Data.kind)
                {
                    case InspectionDeskItemKind.IdCard:
                        hasPassengerId = true;
                        break;

                    case InspectionDeskItemKind.Ticket:
                        hasPassengerTicket = true;
                        break;

                    case InspectionDeskItemKind.Cash:
                        passengerMoneyTotal += Mathf.Max(0, view.Data.moneyValuePence);
                        break;
                }
            }

            if (!hasPassengerId)
                options.Add(new InspectionDeskQuestionOption("request_id", "Where's your ID?"));

            if (currentPassenger.UsesDayRider && !hasPassengerTicket)
                options.Add(new InspectionDeskQuestionOption("request_ticket", "Where's your ticket?"));

            if (currentPassenger.UsesCash)
            {
                if (passengerMoneyTotal <= 0)
                    options.Add(new InspectionDeskQuestionOption("request_payment", "Where's the payment?"));
                else if (passengerMoneyTotal < currentPassenger.ExpectedFare)
                    options.Add(new InspectionDeskQuestionOption("challenge_no_payment", "This isn't enough."));
                else
                    options.Add(new InspectionDeskQuestionOption("money_all", "Is this all you're paying?"));
            }

            options.Add(new InspectionDeskQuestionOption("ask_current_stop", "What's this stop?"));
            options.Add(new InspectionDeskQuestionOption("ask_destination", "Where to?"));
            options.Add(new InspectionDeskQuestionOption("ask_seat", "What seat are you in?"));
            options.Add(new InspectionDeskQuestionOption("ask_fare", "How much are you paying?"));
            options.Add(new InspectionDeskQuestionOption("generic_repeat", "Say that again."));
        }
        else if (zone.ZoneKind == InspectionDeskZoneKind.ReviewArea)
        {
            options.Add(new InspectionDeskQuestionOption("ask_current_stop", "What's this stop?"));
            options.Add(new InspectionDeskQuestionOption("ask_destination", "Where to?"));
            options.Add(new InspectionDeskQuestionOption("ask_seat", "What seat are you in?"));
            options.Add(new InspectionDeskQuestionOption("ask_fare", "How much are you paying?"));
            options.Add(new InspectionDeskQuestionOption("behaviour_repeat", "Answer me properly."));
            options.Add(new InspectionDeskQuestionOption("behaviour_nervous", "Why are you acting strange?"));
        }

        if (options.Count == 0)
            return;

        questionPopup.ShowAtScreenPoint(canvas, eventCamera, screenPoint, BuildZoneTitle(zone.ZoneKind), options, option =>
        {
            List<InspectionDeskItemState> spawned = new List<InspectionDeskItemState>();
            string reply = currentPassenger.AnswerDeskQuestion(InspectionDeskClickTopic.Generic, option.id, currentFareTable, null, spawned);

            for (int i = 0; i < spawned.Count; i++)
                SpawnItemInSharedTray(spawned[i]);

            if (!string.IsNullOrWhiteSpace(reply))
                Say(reply);
        });
    }

    private string BuildZoneTitle(InspectionDeskZoneKind zoneKind)
    {
        return zoneKind switch
        {
            InspectionDeskZoneKind.SharedTray => "Shared Tray",
            InspectionDeskZoneKind.FloatTray => "Float",
            InspectionDeskZoneKind.Bin => "Bin",
            InspectionDeskZoneKind.ReviewArea => "Desk",
            _ => "Question"
        };
    }
}