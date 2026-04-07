using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class SeatedRequestionUI : MonoBehaviour
{
    [Serializable]
    public sealed class RequestionButtonDef
    {
        public string id;
        public string label;
        public bool enabled = true;
    }

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Canvas canvas;

    [Header("Header / speech")]
    [SerializeField] private TMP_Text passengerNameText;
    [SerializeField] private TMP_Text seatText;
    [SerializeField] private TMP_Text speechBodyText;

    [Header("Optional speaker label (can be left empty)")]
    [SerializeField] private TMP_Text speechNameText;

    [Header("Dynamic Buttons")]
    [SerializeField] private RectTransform buttonContainer;
    [SerializeField] private Button buttonPrefab;

    [Header("Static Buttons")]
    [SerializeField] private Button kickOffButton;

    [Header("Question Set")]
    [SerializeField]
    private List<RequestionButtonDef> questionButtons = new List<RequestionButtonDef>
    {
        new RequestionButtonDef { id = "ask_current_stop", label = "What's this stop?" },
        new RequestionButtonDef { id = "ask_destination", label = "Where to?" },
        new RequestionButtonDef { id = "ask_seat", label = "What seat are you in?" },
        new RequestionButtonDef { id = "ask_fare", label = "How much are you paying?" },
        new RequestionButtonDef { id = "behaviour_nervous", label = "Why are you acting strange?" },
        new RequestionButtonDef { id = "generic_repeat", label = "Say that again." }
    };

    [Header("Behaviour")]
    [SerializeField] private bool relockCursorOnClose = true;
    [SerializeField] private bool blockKickOffForUnseated = true;
    [SerializeField] private bool hideSpeakerLabel = true;

    public event Action<Passenger> PassengerKickedOff;
    public event Action Closed;

    private readonly List<Button> runtimeButtons = new List<Button>();
    private Passenger currentPassenger;

    public Passenger CurrentPassenger => currentPassenger;
    public bool IsOpen => root != null && root.activeSelf;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (kickOffButton != null)
            kickOffButton.onClick.AddListener(KickOffCurrentPassenger);

        if (root != null)
            root.SetActive(false);

        ClearRuntimeButtons();
        ClearTexts();
        ApplySpeakerLabelVisibility();
    }

    private void Update()
    {
        if (!IsOpen)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Hide();
    }

    private void OnDestroy()
    {
        ClearRuntimeButtons();
    }

    public void Show(Passenger passenger)
    {
        if (passenger == null)
            return;

        currentPassenger = passenger;

        if (root != null)
            root.SetActive(true);

        ApplySpeakerLabelVisibility();
        RefreshHeader();
        RebuildQuestionButtons();
        RefreshButtons();

        Say(currentPassenger.GetOpeningStatement());

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ForceRefreshLayout();
    }

    public void Hide()
    {
        currentPassenger = null;

        ClearTexts();

        if (root != null)
            root.SetActive(false);

        if (relockCursorOnClose)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Closed?.Invoke();
    }

    public void RefreshHeader()
    {
        if (currentPassenger == null)
            return;

        if (passengerNameText != null)
            passengerNameText.text = currentPassenger.PassengerName;

        if (seatText != null)
            seatText.text = BuildSeatLabel(currentPassenger);
    }

    public void RefreshButtons()
    {
        bool hasPassenger = currentPassenger != null;
        bool seated = hasPassenger && currentPassenger.IsSeatedPassenger;

        for (int i = 0; i < runtimeButtons.Count; i++)
        {
            if (runtimeButtons[i] != null)
                runtimeButtons[i].interactable = hasPassenger;
        }

        if (kickOffButton != null)
            kickOffButton.interactable = hasPassenger && (!blockKickOffForUnseated || seated);
    }

    public void RebuildQuestionButtons()
    {
        ClearRuntimeButtons();

        if (buttonContainer == null || buttonPrefab == null)
            return;

        for (int i = 0; i < questionButtons.Count; i++)
        {
            RequestionButtonDef def = questionButtons[i];
            if (def == null || !def.enabled || string.IsNullOrWhiteSpace(def.id))
                continue;

            Button button = Instantiate(buttonPrefab, buttonContainer);
            runtimeButtons.Add(button);

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = string.IsNullOrWhiteSpace(def.label) ? def.id : def.label;

            string capturedId = def.id;
            button.onClick.AddListener(() => HandleQuestion(capturedId));
        }

        ForceRefreshLayout();
    }

    public void AskLegacyQuestion(PassengerQuestionType questionType)
    {
        if (currentPassenger == null)
            return;

        string reply = currentPassenger.GetAnswer(questionType);
        if (!string.IsNullOrWhiteSpace(reply))
            Say(reply);
    }

    public void AskBehaviourQuestion()
    {
        if (currentPassenger == null)
            return;

        string reply = currentPassenger.AnswerDeskQuestion(
            InspectionDeskClickTopic.Generic,
            "behaviour_nervous",
            null,
            null,
            null);

        if (!string.IsNullOrWhiteSpace(reply))
            Say(reply);
    }

    public void AskRepeatQuestion()
    {
        if (currentPassenger == null)
            return;

        string reply = currentPassenger.AnswerDeskQuestion(
            InspectionDeskClickTopic.Generic,
            "generic_repeat",
            null,
            null,
            null);

        if (!string.IsNullOrWhiteSpace(reply))
            Say(reply);
    }

    public void KickOffCurrentPassenger()
    {
        if (currentPassenger == null)
            return;

        if (blockKickOffForUnseated && !currentPassenger.IsSeatedPassenger)
        {
            Say("They are not seated yet.");
            return;
        }

        Passenger target = currentPassenger;

        if (SeatManager.Instance != null)
            SeatManager.Instance.NotifyPassengerRemoved(target);

        PassengerKickedOff?.Invoke(target);

        Destroy(target.gameObject);
        Hide();
    }

    public void Say(string line)
    {
        if (speechBodyText != null)
            speechBodyText.text = line ?? string.Empty;

        if (speechNameText != null)
            speechNameText.text = string.Empty;
    }

    private void HandleQuestion(string id)
    {
        if (currentPassenger == null || string.IsNullOrWhiteSpace(id))
            return;

        switch (id)
        {
            case "ask_current_stop":
                AskLegacyQuestion(PassengerQuestionType.CurrentStop);
                break;

            case "ask_destination":
                AskLegacyQuestion(PassengerQuestionType.DestinationStop);
                break;

            case "ask_seat":
                AskLegacyQuestion(PassengerQuestionType.Seat);
                break;

            case "ask_fare":
                AskLegacyQuestion(PassengerQuestionType.Fare);
                break;

            case "behaviour_nervous":
                AskBehaviourQuestion();
                break;

            case "generic_repeat":
                AskRepeatQuestion();
                break;

            default:
                Say("...");
                break;
        }
    }

    private void ForceRefreshLayout()
    {
        if (buttonContainer != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonContainer);

        if (root != null)
        {
            RectTransform rootRect = root.transform as RectTransform;
            if (rootRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        }

        Canvas.ForceUpdateCanvases();
    }

    private void ClearRuntimeButtons()
    {
        for (int i = 0; i < runtimeButtons.Count; i++)
        {
            if (runtimeButtons[i] != null)
                Destroy(runtimeButtons[i].gameObject);
        }

        runtimeButtons.Clear();
    }

    private void ClearTexts()
    {
        if (passengerNameText != null)
            passengerNameText.text = string.Empty;

        if (seatText != null)
            seatText.text = string.Empty;

        if (speechBodyText != null)
            speechBodyText.text = string.Empty;

        if (speechNameText != null)
            speechNameText.text = string.Empty;
    }

    private void ApplySpeakerLabelVisibility()
    {
        if (speechNameText != null)
            speechNameText.gameObject.SetActive(!hideSpeakerLabel);
    }

    private static string BuildSeatLabel(Passenger passenger)
    {
        if (passenger == null || SeatManager.Instance == null)
            return "Seat: Unknown";

        SeatAnchor seat = SeatManager.Instance.GetSeatForPassenger(passenger);
        if (seat == null)
            return "Seat: Unknown";

        return $"Seat: {seat.name}";
    }
}