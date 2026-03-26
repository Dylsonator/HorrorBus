using UnityEngine;

public sealed class InspectionTabButton : MonoBehaviour
{
    [SerializeField] private InspectionPanelSwitcher switcher;
    [SerializeField] private InspectionPanelType panelType;
    [SerializeField] private PassengerInspection inspection;

    private void Awake()
    {
        if (switcher == null)
            switcher = FindFirstObjectByType<InspectionPanelSwitcher>();

        if (inspection == null)
            inspection = FindFirstObjectByType<PassengerInspection>();
    }

    public void Press()
    {
        if (inspection == null)
            return;

        switch (panelType)
        {
            case InspectionPanelType.Id:
                inspection.OpenIdTab();
                break;
            case InspectionPanelType.Questions:
                inspection.OpenQuestionsTab();
                break;
            case InspectionPanelType.Fare:
                inspection.OpenFareTab();
                break;
        }
    }
}
