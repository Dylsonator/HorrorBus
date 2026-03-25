using UnityEngine;

public class PlayerGazeObserver : MonoBehaviour
{
    [Header("Gaze Settings")]
    [SerializeField] private float maxDistance = 12f;
    [SerializeField, Range(1f, 179f)] private float viewAngle = 35f;
    [SerializeField] private LayerMask occlusionMask; // bus geometry etc.

    [Header("Debug")]
    [SerializeField] private bool debugDraw = false;

    private float _cosThreshold;

    private void Awake()
    {
        _cosThreshold = Mathf.Cos(viewAngle * Mathf.Deg2Rad);
    }

    private void LateUpdate()
    {
        var camPos = transform.position;
        var camFwd = transform.forward;

        foreach (var p in PassengerRegistry.All)
        {
            if (p == null || p.Head == null)
                continue;

            bool observed = IsObserved(camPos, camFwd, p.Head.position, p.Head, out RaycastHit hit);

            p.IsObserved = observed;

            if (debugDraw)
            {
                Color c = observed ? Color.green : Color.red;
                Debug.DrawLine(camPos, p.Head.position, c);

                if (!observed && hit.collider != null)
                    Debug.DrawLine(camPos, hit.point, Color.yellow);
            }
        }
    }

    private bool IsObserved(Vector3 camPos, Vector3 camFwd, Vector3 targetPos, Transform targetHead, out RaycastHit hit)
    {
        hit = default;

        Vector3 to = targetPos - camPos;
        float dist = to.magnitude;
        if (dist > maxDistance) return false;

        Vector3 dir = to / Mathf.Max(dist, 0.0001f);
        float dot = Vector3.Dot(camFwd, dir);
        if (dot < _cosThreshold) return false;

        // LOS check
        if (Physics.Raycast(camPos, dir, out hit, dist, occlusionMask, QueryTriggerInteraction.Ignore))
        {
            // Something blocks sight
            Debug.Log($"LOS BLOCKED by {hit.collider.name} while looking at head {targetHead.name}");
            return false;
        }

        return true;
    }
}
