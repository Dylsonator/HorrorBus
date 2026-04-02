
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class InspectionDeskClickableRegion : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private InspectionDeskClickTopic topic = InspectionDeskClickTopic.Generic;
    [SerializeField] private InspectionDeskItemView itemView;

    private void Awake()
    {
        if (itemView == null)
            itemView = GetComponentInParent<InspectionDeskItemView>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemView == null)
            return;

        itemView.NotifyRegionClicked(topic, eventData.position);
    }
}
