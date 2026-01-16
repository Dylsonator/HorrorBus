//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Splines;

//public sealed class PassengerQueueManagerSpline : MonoBehaviour
//{
//    [Header("Boarding spline (outside -> door -> just inside)")]
//    [SerializeField] private SplineContainer path;

//    [Header("Queue slots (meters along spline)")]
//    [Tooltip("Slot 0 = FRONT at the door. Slot 1 behind them, etc.")]
//    [SerializeField] private float[] slotDistances = { 0.35f, 0.25f, 0.15f, 0.05f };

//    [Header("Spline direction")]
//    [Tooltip("ON = distance increases as you move toward the door/front. OFF = distance increases away from the door (queue will flip).")]
//    [SerializeField] private bool frontIsAtLargerDistance = true;

//    [Header("Debug")]
//    [SerializeField] private bool logAssignments = false;

//    private readonly List<Passenger> queue = new();

//    public int Count => queue.Count;
//    public int Capacity => slotDistances != null ? slotDistances.Length : 0;
//    public Passenger FrontPassenger => queue.Count > 0 ? queue[0] : null;

//    public bool IsFront(Passenger p) => p != null && queue.Count > 0 && queue[0] == p;

//    public float FrontSlotDistance => (slotDistances != null && slotDistances.Length > 0)
//        ? MapDistance(slotDistances[0])
//        : 0f;

//    public bool Enqueue(Passenger p, float spawnDistanceMeters)
//    {
//        if (p == null) return false;

//        if (path == null)
//        {
//            Debug.LogWarning("PassengerQueueManagerSpline: No SplineContainer assigned.");
//            return false;
//        }

//        if (slotDistances == null || slotDistances.Length == 0)
//        {
//            Debug.LogWarning("PassengerQueueManagerSpline: No slotDistances set.");
//            return false;
//        }

//        if (queue.Count >= slotDistances.Length)
//            return false;

//        if (queue.Contains(p))
//            return true;

//        queue.Add(p);

//        PassengerSplineWalker w = p.GetComponent<PassengerSplineWalker>();
//        if (w == null) w = p.gameObject.AddComponent<PassengerSplineWalker>();

//        // Init ONCE when they enter the queue
//        w.Init(path, MapDistance(spawnDistanceMeters));

//        AssignTargets();
//        return true;
//    }

//    public bool Remove(Passenger p)
//    {
//        if (p == null) return false;

//        bool removed = queue.Remove(p);
//        if (removed) AssignTargets();
//        return removed;
//    }

//    public void Clear()
//    {
//        queue.Clear();
//    }

//    private void AssignTargets()
//    {
//        if (slotDistances == null || slotDistances.Length == 0) return;

//        for (int i = 0; i < queue.Count; i++)
//        {
//            Passenger p = queue[i];
//            if (p == null) continue;

//            float rawTarget = slotDistances[Mathf.Min(i, slotDistances.Length - 1)];
//            float targetDist = MapDistance(rawTarget);

//            PassengerSplineWalker w = p.GetComponent<PassengerSplineWalker>();
//            if (w == null) w = p.gameObject.AddComponent<PassengerSplineWalker>();

//            w.enabled = true;
//            w.SetTargetDistance(targetDist);

//            if (logAssignments)
//                Debug.Log($"Queue: {p.name} -> slot {i} at {targetDist:0.00}m (raw {rawTarget:0.00})");
//        }
//    }

//    // Maps “designer distances” to actual spline direction.
//    // If your spline is reversed, this flips the distance along the spline length.
//    private float MapDistance(float designerDistanceMeters)
//    {
//        if (path == null) return designerDistanceMeters;

//        float len = path.CalculateLength();
//        if (len <= 0f) return designerDistanceMeters;

//        if (frontIsAtLargerDistance)
//            return Mathf.Clamp(designerDistanceMeters, 0f, len);

//        // Flip along spline if front is actually at SMALLER distance
//        return Mathf.Clamp(len - designerDistanceMeters, 0f, len);
//    }
//}
