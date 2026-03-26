using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PassengerQuestionUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text answerText;
    [SerializeField, Min(1f)] private float charactersPerSecond = 45f;
    [SerializeField] private bool allowInstantReveal = true;

    private Coroutine typeRoutine;
    private string currentFullAnswer = string.Empty;
    private bool isTyping;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        Clear();
    }

    private void Update()
    {
        if (!allowInstantReveal || !isTyping)
            return;

        bool revealPressed = false;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            revealPressed = true;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            revealPressed = true;

        if (revealPressed)
            RevealInstantly();
    }

    public void Show(string prompt, string answer)
    {
        if (root == null)
            root = gameObject;

        if (promptText != null)
            promptText.text = prompt ?? string.Empty;

        currentFullAnswer = answer ?? string.Empty;

        if (!gameObject.activeInHierarchy)
            return;

        if (root != null && !root.activeSelf)
            root.SetActive(true);

        BeginType(currentFullAnswer);
    }

    public void ActivateAndReplayLast()
    {
        if (root == null)
            root = gameObject;

        if (root != null)
            root.SetActive(true);

        if (!string.IsNullOrEmpty(currentFullAnswer))
            BeginType(currentFullAnswer);
    }

    public void Clear()
    {
        if (typeRoutine != null)
        {
            StopCoroutine(typeRoutine);
            typeRoutine = null;
        }

        isTyping = false;
        currentFullAnswer = string.Empty;

        if (promptText != null)
            promptText.text = string.Empty;

        if (answerText != null)
        {
            answerText.text = string.Empty;
            answerText.maxVisibleCharacters = int.MaxValue;
        }

        if (root != null)
            root.SetActive(false);
    }

    private void BeginType(string fullText)
    {
        if (answerText == null)
            return;

        if (!gameObject.activeInHierarchy)
        {
            answerText.text = fullText;
            answerText.maxVisibleCharacters = int.MaxValue;
            isTyping = false;
            return;
        }

        if (typeRoutine != null)
        {
            StopCoroutine(typeRoutine);
            typeRoutine = null;
        }

        answerText.text = fullText;
        answerText.maxVisibleCharacters = 0;
        isTyping = true;
        typeRoutine = StartCoroutine(TypeRoutine());
    }

    private void RevealInstantly()
    {
        if (answerText == null)
            return;

        if (typeRoutine != null)
        {
            StopCoroutine(typeRoutine);
            typeRoutine = null;
        }

        answerText.text = currentFullAnswer;
        answerText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
    }

    private IEnumerator TypeRoutine()
    {
        answerText.ForceMeshUpdate();

        int totalVisibleChars = answerText.textInfo.characterCount;
        float visible = 0f;

        while (visible < totalVisibleChars)
        {
            visible += charactersPerSecond * Time.unscaledDeltaTime;
            answerText.maxVisibleCharacters = Mathf.Clamp(Mathf.FloorToInt(visible), 0, totalVisibleChars);
            yield return null;
        }

        answerText.maxVisibleCharacters = totalVisibleChars;
        isTyping = false;
        typeRoutine = null;
    }
}
