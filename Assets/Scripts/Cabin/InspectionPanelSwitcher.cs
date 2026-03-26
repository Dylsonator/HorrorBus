using UnityEngine;

public enum InspectionPanelType
{
    Id,
    Questions,
    Fare
}

public sealed class InspectionPanelSwitcher : MonoBehaviour
{
    [Header("Panel Roots")]
    [SerializeField] private GameObject idPanel;
    [SerializeField] private GameObject questionsPanel;
    [SerializeField] private GameObject farePanel;

    [Header("Optional")]
    [SerializeField] private InspectionPanelType defaultPanel = InspectionPanelType.Id;

    public InspectionPanelType CurrentPanel { get; private set; }

    private void Awake()
    {
        Show(defaultPanel);
    }

    public void Show(InspectionPanelType panel)
    {
        CurrentPanel = panel;

        if (idPanel != null)
            idPanel.SetActive(panel == InspectionPanelType.Id);

        if (questionsPanel != null)
            questionsPanel.SetActive(panel == InspectionPanelType.Questions);

        if (farePanel != null)
            farePanel.SetActive(panel == InspectionPanelType.Fare);
    }

    public void ShowDefault()
    {
        Show(defaultPanel);
    }

    public void HideAll()
    {
        if (idPanel != null) idPanel.SetActive(false);
        if (questionsPanel != null) questionsPanel.SetActive(false);
        if (farePanel != null) farePanel.SetActive(false);
    }
}