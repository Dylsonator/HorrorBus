using UnityEngine;

public sealed class InspectionDeskFloatSlot : MonoBehaviour
{
    [SerializeField] private int denominationPence = 100;
    [SerializeField] private RectTransform rect;

    public int DenominationPence => Mathf.Max(0, denominationPence);
    public RectTransform Rect => rect != null ? rect : (rect = transform as RectTransform);

    private void Reset()
    {
        rect = transform as RectTransform;
    }

    public Vector2 GetAnchoredPointWithin(RectTransform relativeRect)
    {
        RectTransform target = Rect;
        if (target == null || relativeRect == null)
            return Vector2.zero;

        Vector3 world = target.TransformPoint(target.rect.center);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(relativeRect, RectTransformUtility.WorldToScreenPoint(null, world), null, out Vector2 anchored);
        return anchored;
    }
}
