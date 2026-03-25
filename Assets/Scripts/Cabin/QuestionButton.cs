using UnityEngine;

public sealed class QuestionButton : MonoBehaviour
{
    [SerializeField] private PassengerInspection inspection;
    [SerializeField] private PassengerQuestionType questionType;

    private void Awake()
    {
        if (inspection == null)
            inspection = FindFirstObjectByType<PassengerInspection>();
    }

    public void Press()
    {
        if (inspection == null)
            return;

        inspection.AskCurrentQuestion(questionType);
    }
}