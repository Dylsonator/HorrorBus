using System.Collections.Generic;
using UnityEngine;

public sealed class QueueManagerNodes : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private QueueNodePath path;

    [Header("Spacing")]
    [SerializeField] private float baseGap = 0.85f;
    [SerializeField] private Vector2 gapJitter = new Vector2(-0.05f, 0.08f);

    [Header("Behaviour")]
    [SerializeField] private float tooCloseStopDistance = 0.7f;
    [SerializeField] private bool updateEveryFrame = true;

    [Header("Joining")]
    [SerializeField] private float joinBackOffset = 1.1f;
    [SerializeField] private float joinSideOffset = 0.35f;

    private readonly List<Passenger> queue = new();
    private readonly Dictionary<Passenger, float> personalGap = new();
    private readonly Dictionary<Passenger, int> joinOrder = new();
    private int joinCounter;

    public Passenger FrontPassenger => queue.Count > 0 ? queue[0] : null;
    public bool IsFront(Passenger p) => p != null && queue.Count > 0 && queue[0] == p;

    private Transform DoorNode => path != null ? path.GetNode(0) : null;
    private Transform BackNode => path != null ? path.GetNode(path.Count - 1) : null;

    private void Awake()
    {
        if (path == null)
            path = FindFirstObjectByType<QueueNodePath>();
    }

    private float GetGap(Passenger p)
    {
        if (p == null)
            return baseGap;

        if (personalGap.TryGetValue(p, out float g))
            return g;

        g = Mathf.Max(0.5f, baseGap + Random.Range(gapJitter.x, gapJitter.y));
        personalGap[p] = g;
        return g;
    }

    public Vector3 GetJoinTargetFor(Passenger p, float extraBackOffset = 0f)
    {
        Transform back = BackNode;
        if (back == null)
            return transform.position;

        Vector3 basePos = back.position;

        Vector3 queueForward = Vector3.forward;
        if (path != null && path.Count >= 2)
        {
            Transform prev = path.GetNode(path.Count - 2);
            if (prev != null)
            {
                Vector3 dir = (back.position - prev.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    queueForward = dir.normalized;
            }
        }

        Vector3 right = Vector3.Cross(Vector3.up, queueForward).normalized;
        float side = Random.Range(-joinSideOffset, joinSideOffset);

        return basePos + (queueForward * (joinBackOffset + extraBackOffset)) + (right * side);
    }

    public bool AddToQueue(Passenger p)
    {
        if (p == null) return false;
        if (path == null || path.Count < 2) return false;
        if (p.HasBeenProcessed || p.IsSeatedPassenger) return false;
        if (SeatManager.Instance != null && SeatManager.Instance.GetSeatForPassenger(p) != null) return false;
        if (queue.Contains(p)) return true;

        NodeQueueWalker w = p.GetComponent<NodeQueueWalker>();
        if (w == null) w = p.gameObject.AddComponent<NodeQueueWalker>();
        w.enabled = true;
        w.Init(this);

        queue.Add(p);
        joinOrder[p] = joinCounter++;
        GetGap(p);

        RebuildAndAssign();
        return true;
    }

    public bool Remove(Passenger p)
    {
        if (p == null) return false;

        bool removed = queue.Remove(p);
        personalGap.Remove(p);
        joinOrder.Remove(p);

        if (removed)
            RebuildAndAssign();

        return removed;
    }

    private void LateUpdate()
    {
        if (!updateEveryFrame)
            return;

        if (queue.Count == 0)
            return;

        bool changed = false;

        for (int i = queue.Count - 1; i >= 0; i--)
        {
            Passenger p = queue[i];
            bool remove = false;

            if (p == null)
                remove = true;
            else if (p.HasBeenProcessed || p.IsSeatedPassenger)
                remove = true;
            else if (SeatManager.Instance != null && SeatManager.Instance.GetSeatForPassenger(p) != null)
                remove = true;

            if (!remove)
                continue;

            if (p != null && p.TryGetComponent(out NodeQueueWalker walker))
                walker.StopMoving();

            queue.RemoveAt(i);

            if (p != null)
            {
                personalGap.Remove(p);
                joinOrder.Remove(p);
            }

            changed = true;
        }

        if (queue.Count == 0)
            return;

        if (changed || updateEveryFrame)
            RebuildAndAssign();
    }

    private void RebuildAndAssign()
    {
        if (path == null || path.Count < 2)
            return;

        List<Transform> nodes = new List<Transform>(path.Nodes.Count);
        for (int i = 0; i < path.Nodes.Count; i++)
        {
            if (path.Nodes[i] != null)
                nodes.Add(path.Nodes[i]);
        }

        if (nodes.Count < 2)
            return;

        Vector3[] pts = new Vector3[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
            pts[i] = nodes[i].position;

        float[] segLen = new float[pts.Length - 1];
        float totalLen = 0f;

        for (int i = 0; i < segLen.Length; i++)
        {
            segLen[i] = Vector3.Distance(pts[i], pts[i + 1]);
            totalLen += segLen[i];
        }

        Vector3 SampleAlong(float d)
        {
            d = Mathf.Clamp(d, 0f, totalLen);

            for (int s = 0; s < segLen.Length; s++)
            {
                float L = segLen[s];
                if (L <= 0.0001f)
                    continue;

                if (d <= L)
                    return Vector3.Lerp(pts[s], pts[s + 1], d / L);

                d -= L;
            }

            return pts[pts.Length - 1];
        }

        float distFromDoor = 0f;

        for (int i = 0; i < queue.Count; i++)
        {
            Passenger p = queue[i];
            if (p == null)
                continue;

            if (!p.TryGetComponent(out NodeQueueWalker w))
                continue;

            if (i == 0)
                distFromDoor = 0f;
            else
                distFromDoor += GetGap(p);

            Vector3 target = SampleAlong(distFromDoor);
            Transform aheadT = (i == 0) ? null : queue[i - 1]?.transform;

            w.SetTarget(target, aheadT, tooCloseStopDistance);
        }
    }
}