using System.Collections.Generic;
using UnityEngine;

public sealed class QueueManagerNodes : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private QueueNodePath path;

    [Header("Spacing")]
    [SerializeField] private float baseGap = 0.6f;
    [SerializeField] private Vector2 gapJitter = new(-0.1f, 0.12f);

    [Header("Behaviour")]
    [SerializeField] private float tooCloseStopDistance = 0.55f;
    [SerializeField] private bool updateEveryFrame = true;

    private readonly List<Passenger> queue = new();
    private readonly Dictionary<Passenger, float> personalGap = new();
    private readonly Dictionary<Passenger, int> joinOrder = new();
    private int joinCounter;

    public Passenger FrontPassenger => queue.Count > 0 ? queue[0] : null;
    public bool IsFront(Passenger p) => p != null && queue.Count > 0 && queue[0] == p;

    private Transform DoorNode => path != null ? path.GetNode(0) : null;

    private void Awake()
    {
        if (path == null) path = FindFirstObjectByType<QueueNodePath>();
    }

    private float GetGap(Passenger p)
    {
        if (p == null) return baseGap;
        if (personalGap.TryGetValue(p, out var g)) return g;

        g = Mathf.Max(0.25f, baseGap + Random.Range(gapJitter.x, gapJitter.y));
        personalGap[p] = g;
        return g;
    }

    public bool AddToQueue(Passenger p)
    {
        if (p == null) return false;
        if (path == null || path.Count < 1) return false;
        if (p.HasBeenProcessed || p.IsSeatedPassenger) return false;
        if (SeatManager.Instance != null && SeatManager.Instance.GetSeatForPassenger(p) != null) return false;
        if (queue.Contains(p)) return true;

        // Ensure walker
        var w = p.GetComponent<NodeQueueWalker>();
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

        if (removed) RebuildAndAssign();
        return removed;
    }

    private void LateUpdate()
    {
        if (!updateEveryFrame) return;
        if (queue.Count == 0) return;

        bool changed = false;

        for (int i = queue.Count - 1; i >= 0; i--)
        {
            Passenger p = queue[i];
            bool remove = false;

            if (p == null)
            {
                remove = true;
            }
            else if (p.HasBeenProcessed || p.IsSeatedPassenger)
            {
                remove = true;
            }
            else if (SeatManager.Instance != null && SeatManager.Instance.GetSeatForPassenger(p) != null)
            {
                remove = true;
            }

            if (!remove) continue;

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

        if (queue.Count == 0) return;

        if (changed || updateEveryFrame)
            RebuildAndAssign();
    }

    private int CompareAhead(Passenger a, Passenger b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        Transform door = DoorNode;
        if (door == null) return 0;

        // Smaller distance to door = ahead
        float da = Vector3.Distance(a.transform.position, door.position);
        float db = Vector3.Distance(b.transform.position, door.position);

        // If extremely close, keep stable by join order to prevent jitter swapping
        if (Mathf.Abs(da - db) < 0.15f)
        {
            int oa = joinOrder.TryGetValue(a, out var va) ? va : int.MaxValue;
            int ob = joinOrder.TryGetValue(b, out var vb) ? vb : int.MaxValue;
            return oa.CompareTo(ob);
        }

        return da.CompareTo(db);
    }

    private void RebuildAndAssign()
    {
        if (path == null || path.Count < 2) return;

        queue.Sort(CompareAhead);

        // Build a polyline of node positions (0 = door/front, increasing = further back)
        // We will walk along this polyline and place each passenger at increasing "distance from door".
        var nodes = path.Nodes;
        Vector3[] pts = new Vector3[nodes.Count];
        for (int i = 0; i < nodes.Count; i++)
            pts[i] = nodes[i] != null ? nodes[i].position : transform.position;

        // Precompute segment lengths
        float[] segLen = new float[pts.Length - 1];
        float totalLen = 0f;
        for (int i = 0; i < segLen.Length; i++)
        {
            segLen[i] = Vector3.Distance(pts[i], pts[i + 1]);
            totalLen += segLen[i];
        }

        // Helper: sample a position at distance d from the door along the node polyline
        Vector3 SampleAlong(float d)
        {
            d = Mathf.Clamp(d, 0f, totalLen);

            for (int s = 0; s < segLen.Length; s++)
            {
                float L = segLen[s];
                if (L <= 0.0001f) continue;

                if (d <= L)
                    return Vector3.Lerp(pts[s], pts[s + 1], d / L);

                d -= L;
            }

            return pts[pts.Length - 1];
        }

        // Place each passenger at increasing distance from door
        float distFromDoor = 0f;

        for (int i = 0; i < queue.Count; i++)
        {
            Passenger p = queue[i];
            if (p == null) continue;

            if (!p.TryGetComponent(out NodeQueueWalker w)) continue;

            // Each passenger gets their own gap "personality"
            float gap = GetGap(p);

            // Front passenger goes at distance 0 (Node_00 area)
            // Everyone else gets placed further back by previous passenger's gap
            if (i == 0) distFromDoor = 0f;
            else distFromDoor += gap;

            Vector3 target = SampleAlong(distFromDoor);

            // "ahead" reference only used for stop-distance waiting (optional)
            Transform aheadT = (i == 0) ? null : queue[i - 1]?.transform;

            w.SetTarget(target, aheadT, tooCloseStopDistance);
        }
    }

}
