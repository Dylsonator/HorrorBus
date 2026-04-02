
using UnityEngine;

public sealed class InspectionDeskTicketChoiceButton : MonoBehaviour
{
    [SerializeField] private InspectionDeskUI deskUI;
    [SerializeField] private TicketBand ticketBand = TicketBand.None;

    private void Awake()
    {
        if (deskUI == null)
            deskUI = FindFirstObjectByType<InspectionDeskUI>(FindObjectsInactive.Include);
    }

    public void Press()
    {
        if (deskUI == null)
            return;

        deskUI.SetSelectedTicketBand(ticketBand);
    }
}
