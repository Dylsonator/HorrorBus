using UnityEngine;
using UnityEngine.EventSystems;

public sealed class InspectionDeskZoneClickable : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private InspectionDeskUI deskUI;
    [SerializeField] private InspectionDeskZoneBox zone;

    private void Awake()
    {
        if (deskUI == null)
            deskUI = FindFirstObjectByType<InspectionDeskUI>(FindObjectsInactive.Include);

        if (zone == null)
            zone = GetComponent<InspectionDeskZoneBox>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (deskUI == null || zone == null)
            return;

        deskUI.HandleZoneClick(zone, eventData.position, eventData.pressEventCamera);
    }
}