using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public sealed class QueueManagerSpline : MonoBehaviour
{
    [Header("Spline")]
    [SerializeField] private SplineContainer spline;

    [Header("Queue layout")]
    [Tooltip("Meters along spline where the FRONT of the queue stands (door).")]
    [SerializeField] private float frontSplineDistance = 0.35f;

    [Tooltip("Space between people (meters).")]
    [SerializeField] private float gap = 0.45f;

    [Tooltip("ON: behind = smaller distances. OFF: behind = larger distances.")]
    [SerializeField] private bool frontIsAtLargerDistance = false;

    [Header("Limits")]
    [SerializeField] private int maxQueueSize = 8;

    private readonly List<Passenger> queue = new();
    private int inFlightJoiners = 0;
    private float splineLength = -1f;

    public Passenger FrontPassenger => queue.Count > 0 ? queue[0] : null;
    public bool IsFront(Passenger p) => p != null && queue.Count > 0 && queue[0] == p;

    private void Awake() => CacheLength();
    private void OnValidate() => CacheLength();

    private void CacheLength()
    {
        if (spline != null)
            splineLength = Mathf.Max(0.01f, spline.CalculateLength());
    }

    public int ReserveIndex()
    {
        // IMPORTANT: includes joiners still walking (prevents two people targeting slot 0/1 weirdly)
        int idx = queue.Count + inFlightJoiners;
        inFlightJoiners++;
        return idx;
    }

    public void ReleaseReservation()
    {
        inFlightJoiners = Mathf.Max(0, inFlightJoiners - 1);
    }

    public bool Remove(Passenger p)
    {
        if (p == null) return false;
        bool removed = queue.Remove(p);
        if (removed) AssignTargets();
        return removed;
    }

    public float GetTargetDistanceForIndex(int indexFromFront)
    {
        CacheLength();
        float step = gap * Mathf.Max(0, indexFromFront);
        float d = frontIsAtLargerDistance ? (frontSplineDistance - step) : (frontSplineDistance + step);
        return Mathf.Clamp(d, 0f, splineLength);
    }

    public Vector3 GetWorldPositionAtDistance(float distMeters)
    {
        CacheLength();
        float d = Mathf.Clamp(distMeters, 0f, splineLength);
        float t = Mathf.Clamp01(d / splineLength);
        return spline.EvaluatePosition(t);
    }

    // This is the key: insert based on reserved index, not "who arrived first"
    public bool EnqueueReserved(Passenger p, int reservedIndexFromFront, float startDistanceMeters)
    {
        if (p == null) return false;
        if (spline == null) return false;

        if (maxQueueSize > 0 && queue.Count >= maxQueueSize)
            return false;

        if (queue.Contains(p))
            return true;

        CacheLength();
        startDistanceMeters = Mathf.Clamp(startDistanceMeters, 0f, splineLength);

        int insertIndex = Mathf.Clamp(reservedIndexFromFront, 0, queue.Count);
        queue.Insert(insertIndex, p);

        var w = p.GetComponent<QueuedSplineWalker>();
        if (w == null) w = p.gameObject.AddComponent<QueuedSplineWalker>();
        w.Init(spline, startDistanceMeters);
        w.enabled = true;

        AssignTargets();
        return true;
    }

    private void AssignTargets()
    {
        if (queue.Count == 0 || spline == null) return;

        CacheLength();

        for (int i = 0; i < queue.Count; i++)
        {
            Passenger p = queue[i];
            if (p == null) continue;

            float slotTarget = GetTargetDistanceForIndex(i);

            // Optional: clamp behind the person ahead so nobody overtakes
            if (i > 0)
            {
                var ahead = queue[i - 1];
                if (ahead != null)
                {
                    var aw = ahead.GetComponent<QueuedSplineWalker>();
                    if (aw != null)
                    {
                        float followLimit = frontIsAtLargerDistance
                            ? (aw.CurrentDistance - gap)
                            : (aw.CurrentDistance + gap);

                        slotTarget = frontIsAtLargerDistance
                            ? Mathf.Min(slotTarget, followLimit)
                            : Mathf.Max(slotTarget, followLimit);
                    }
                }
            }

            var w = p.GetComponent<QueuedSplineWalker>();
            if (w != null) w.SetTargetDistance(slotTarget);
        }
    }
}
