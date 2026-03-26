using TMPro;
using UnityEngine;

public sealed class QuestionButton : MonoBehaviour
{
    [SerializeField] private PassengerInspection inspection;
    [SerializeField] private PassengerQuestionType questionType;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private bool autoUpdateLabel = true;

    private void Awake()
    {
        if (inspection == null)
            inspection = FindFirstObjectByType<PassengerInspection>();

        if (labelText == null)
            labelText = GetComponentInChildren<TMP_Text>(true);

        RefreshLabel();
    }

    private void OnValidate()
    {
        RefreshLabel();
    }

    public void Press()
    {
        if (inspection == null)
            return;

        inspection.AskCurrentQuestion(questionType);
    }

    private void RefreshLabel()
    {
        if (!autoUpdateLabel || labelText == null)
            return;

        labelText.text = questionType switch
        {
            PassengerQuestionType.CurrentStop => "Current Stop",
            PassengerQuestionType.DestinationStop => "Destination",
            PassengerQuestionType.Seat => "Seat",
            PassengerQuestionType.Fare => "Fare",
            _ => questionType.ToString()
        };
    }
}
