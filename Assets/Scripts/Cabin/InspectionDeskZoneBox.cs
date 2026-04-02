
using UnityEngine;

[ExecuteAlways]
public sealed class InspectionDeskZoneBox : MonoBehaviour
{
    [SerializeField] private InspectionDeskZoneKind zoneKind = InspectionDeskZoneKind.None;
    [SerializeField] private RectTransform rect;
    [SerializeField] private float padding = 18f;

    public InspectionDeskZoneKind ZoneKind => zoneKind;
    public RectTransform Rect => rect != null ? rect : (rect = transform as RectTransform);

    private void Reset()
    {
        rect = transform as RectTransform;
    }

    public bool ContainsScreenPoint(Canvas canvas, Camera eventCamera, Vector2 screenPoint)
    {
        RectTransform target = Rect;
        if (target == null)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(target, screenPoint, eventCamera);
    }

    public bool ContainsAnchoredPosition(Vector2 anchoredPosition)
    {
        RectTransform target = Rect;
        if (target == null)
            return false;

        Vector2 local = anchoredPosition - target.anchoredPosition;
        Rect area = target.rect;
        return area.Contains(local);
    }

    public Vector2 GetRandomAnchoredPointWithin(Transform relativeTo)
    {
        RectTransform target = Rect;
        RectTransform relativeRect = relativeTo as RectTransform;
        if (target == null || relativeRect == null)
            return Vector2.zero;

        Rect rectArea = target.rect;
        float minX = rectArea.xMin + padding;
        float maxX = rectArea.xMax - padding;
        float minY = rectArea.yMin + padding;
        float maxY = rectArea.yMax - padding;

        if (maxX < minX) maxX = minX;
        if (maxY < minY) maxY = minY;

        Vector2 localPoint = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
        Vector3 world = target.TransformPoint(localPoint);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(relativeRect, RectTransformUtility.WorldToScreenPoint(null, world), null, out Vector2 anchored);
        return anchored;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        RectTransform target = Rect;
        if (target == null)
            return;

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Gizmos.color = zoneKind switch
        {
            InspectionDeskZoneKind.SharedTray => new Color(0.2f, 0.8f, 1f, 0.65f),
            InspectionDeskZoneKind.FloatTray => new Color(0.2f, 1f, 0.4f, 0.65f),
            InspectionDeskZoneKind.Bin => new Color(1f, 0.2f, 0.2f, 0.65f),
            InspectionDeskZoneKind.ReviewArea => new Color(1f, 0.85f, 0.2f, 0.45f),
            _ => new Color(1f, 1f, 1f, 0.3f)
        };

        for (int i = 0; i < 4; i++)
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
    }
#endif
}
