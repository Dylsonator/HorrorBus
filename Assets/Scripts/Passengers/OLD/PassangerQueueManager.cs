using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public sealed class PassengerQueueManager : MonoBehaviour
{
    [Header("Spline path (aisle/queue path)")]
    [SerializeField] private SplineContainer path;

    [Header("Queue slots (meters along spline)")]
    [Tooltip("Slot 0 is the FRONT of the queue. These are distances along the spline in meters.")]
    [SerializeField] private float[] slotDistances = { 0.5f, 1.3f, 2.1f, 2.9f };

    [Header("Debug")]
    [SerializeField] private bool logAssignments = false;

    private readonly List<Passenger> queue = new();

    public int Count => queue.Count;
    public int Capacity => (slotDistances != null) ? slotDistances.Length : 0;
    public Passenger FrontPassenger => queue.Count > 0 ? queue[0] : null;
    public bool IsFull => Capacity > 0 && queue.Count >= Capacity;

    public bool Enqueue(Passenger p, float spawnDistanceMeters)
    {
        if (p == null) return false;

        if (path == null)
        {
            Debug.LogWarning("PassengerQueueManagerSpline: No SplineContainer assigned.");
            return false;
        }

        if (slotDistances == null || slotDistances.Length == 0)
        {
            Debug.LogWarning("PassengerQueueManagerSpline: No slotDistances set.");
            return false;
        }

        if (queue.Count >= slotDistances.Length)
        {
            if (logAssignments) Debug.Log("PassengerQueueManagerSpline: Queue is full.");
            return false;
        }

        if (queue.Contains(p))
            return true;

        queue.Add(p);

        PassengerSplineWalker w = p.GetComponent<PassengerSplineWalker>();
        if (w == null) w = p.gameObject.AddComponent<PassengerSplineWalker>();

        w.Init(path, spawnDistanceMeters);

        AssignTargets();
        return true;
    }

    public bool Remove(Passenger p)
    {
        if (p == null) return false;

        bool removed = queue.Remove(p);
        if (removed) AssignTargets();
        return removed;
    }

    public void Clear()
    {
        queue.Clear();
    }

    private void AssignTargets()
    {
        for (int i = 0; i < queue.Count; i++)
        {
            Passenger p = queue[i];
            if (p == null) continue;

            float target = slotDistances[Mathf.Min(i, slotDistances.Length - 1)];

            PassengerSplineWalker w = p.GetComponent<PassengerSplineWalker>();
            if (w == null) w = p.gameObject.AddComponent<PassengerSplineWalker>();

            // If someone added the walker but not initialised it yet, do it now from 0
            // (better than doing nothing)
           // w.Init(path, w.CurrentDistance);

            w.SetTargetDistance(target);

            if (logAssignments)
                Debug.Log($"SplineQueue: {p.name} -> slot {i} dist {target:0.00}m");
        }
    }

    public bool IsFront(Passenger p) => p != null && queue.Count > 0 && queue[0] == p;
}
