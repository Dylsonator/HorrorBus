using UnityEngine;

public sealed class QuestionButton : MonoBehaviour
{
    [SerializeField] private InspectionDeskPromptButton promptButton;

    private void Awake()
    {
        if (promptButton == null)
            promptButton = GetComponent<InspectionDeskPromptButton>();
    }

    public void Press()
    {
        if (promptButton != null)
            promptButton.Press();
    }
}