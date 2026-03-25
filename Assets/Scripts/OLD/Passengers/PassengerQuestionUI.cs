using TMPro;
using UnityEngine;

public sealed class PassengerQuestionUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text answerText;

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
            promptText.text = prompt;

        if (answerText != null)
            answerText.text = answer;
    }

    public void Clear()
    {
        if (promptText != null)
            promptText.text = string.Empty;

        if (answerText != null)
            answerText.text = string.Empty;

        if (root != null)
            root.SetActive(false);
    }
}