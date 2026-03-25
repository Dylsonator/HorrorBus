using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public sealed class PassengerQuestionUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text answerText;
    [SerializeField, Min(1f)] private float charactersPerSecond = 45f;

    private Coroutine typeRoutine;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        Clear();
    }

    public void Show(string prompt, string answer)
    {
        if (root != null)
            root.SetActive(true);

        if (promptText != null)
            promptText.text = prompt ?? string.Empty;

        BeginType(answer ?? string.Empty);
    }

    public void Clear()
    {
        if (typeRoutine != null)
        {
            StopCoroutine(typeRoutine);
            typeRoutine = null;
        }

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

    private void Update()
    {
        if (root == null || !root.activeSelf)
            return;

        if (typeRoutine == null)
            return;

        bool reveal = false;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            reveal = true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            reveal = true;

        if (reveal)
            RevealInstantly();
    }

    private void BeginType(string fullText)
    {
        if (answerText == null)
            return;

        if (typeRoutine != null)
        {
            StopCoroutine(typeRoutine);
            typeRoutine = null;
        }

        answerText.text = fullText;
        answerText.maxVisibleCharacters = 0;
        typeRoutine = StartCoroutine(TypeRoutine());
    }

    public void RevealInstantly()
    {
        if (answerText == null)
            return;

        if (typeRoutine != null)
        {
            StopCoroutine(typeRoutine);
            typeRoutine = null;
        }

        answerText.ForceMeshUpdate();
        answerText.maxVisibleCharacters = answerText.textInfo.characterCount;
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
        typeRoutine = null;
    }
}