
using UnityEngine;

public enum InspectionDeskPromptKind
{
    MissingId,
    MissingTicket,
    MissingPayment,
    CurrentStop,
    Destination,
    Seat,
    Fare
}

public sealed class InspectionDeskPromptButton : MonoBehaviour
{
    [SerializeField] private InspectionDeskUI deskUI;
    [SerializeField] private InspectionDeskPromptKind promptKind = InspectionDeskPromptKind.MissingId;

    private void Awake()
    {
        if (deskUI == null)
            deskUI = FindFirstObjectByType<InspectionDeskUI>(FindObjectsInactive.Include);
    }

    public void Press()
    {
        if (deskUI == null)
            return;

        switch (promptKind)
        {
            case InspectionDeskPromptKind.MissingId:
                deskUI.PromptMissingId();
                break;
            case InspectionDeskPromptKind.MissingTicket:
                deskUI.PromptMissingTicket();
                break;
            case InspectionDeskPromptKind.MissingPayment:
                deskUI.PromptMissingPayment();
                break;
            case InspectionDeskPromptKind.CurrentStop:
                deskUI.AskLegacyQuestion(PassengerQuestionType.CurrentStop);
                break;
            case InspectionDeskPromptKind.Destination:
                deskUI.AskLegacyQuestion(PassengerQuestionType.DestinationStop);
                break;
            case InspectionDeskPromptKind.Seat:
                deskUI.AskLegacyQuestion(PassengerQuestionType.Seat);
                break;
            case InspectionDeskPromptKind.Fare:
                deskUI.AskLegacyQuestion(PassengerQuestionType.Fare);
                break;
        }
    }
}
