using System.Collections.Generic;
using UnityEngine;

public sealed class PassengerSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private Passenger[] normalPassengerPrefabs;
    [SerializeField] private Passenger[] anomalyPassengerPrefabs;
    [SerializeField, Range(0f, 1f)] private float anomalyChance = 0.15f;

    [SerializeField] private Transform spawnRoot;
    [SerializeField] private float spawnDistanceOnSpline = 0f;
    [SerializeField] private int defaultSpawnPerStop = 3;

    [Header("Refs")]
    [SerializeField] private PassengerQueueManagerSpline queueManager;
    [SerializeField] private NameGenerator nameGenerator;

    [Header("Seating")]
    [SerializeField] private SeatAnchor[] seats;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly List<Passenger> seatedPassengers = new();

    private void Awake()
    {
        if (queueManager == null)
            queueManager = FindFirstObjectByType<PassengerQueueManagerSpline>();

        if (nameGenerator == null)
            nameGenerator = FindFirstObjectByType<NameGenerator>();

        if (spawnRoot == null)
            spawnRoot = transform;

        if (seats == null || seats.Length == 0)
            seats = GetComponentsInChildren<SeatAnchor>(true);

        if (debugLogs)
        {
            Debug.Log($"[Spawner] Awake | normal={normalPassengerPrefabs?.Length ?? 0} " +
                      $"anomaly={anomalyPassengerPrefabs?.Length ?? 0} seats={seats.Length}");
        }
    }

    // =====================================================
    // CALLED BY INSPECTION (1 / 2)
    // =====================================================

    public bool SeatPassenger(Passenger p)
    {
        if (p == null || queueManager == null) return false;
        if (!queueManager.IsFront(p)) return false;

        SeatAnchor free = FindFreeSeat();
        if (free == null)
        {
            Debug.LogWarning("[Spawner] No free seats.");
            return false;
        }

        queueManager.Remove(p);

        var walker = p.GetComponent<PassengerSplineWalker>();
        if (walker != null) walker.StopMoving();

        var rb = p.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        free.Occupied = true;

        // 🔒 LOCK TO SEAT
        p.transform.SetParent(free.transform, false);
        p.transform.localPosition = Vector3.zero;
        p.transform.localRotation = Quaternion.identity;

        seatedPassengers.Add(p);

        if (debugLogs)
            Debug.Log($"[Spawner] SEATED {p.PassengerName} | anomaly={p.IsAnomaly}");

        return true;
    }

    public bool DismissPassenger(Passenger p)
    {
        if (p == null || queueManager == null) return false;
        if (!queueManager.IsFront(p)) return false;

        queueManager.Remove(p);
        Destroy(p.gameObject);

        if (debugLogs)
            Debug.Log($"[Spawner] DISMISSED {p.PassengerName}");

        return true;
    }

    // =====================================================
    // CALLED BY RouteStops
    // =====================================================

    public void DismissPassengersForStop(int stopIndex)
    {
        for (int i = seatedPassengers.Count - 1; i >= 0; i--)
        {
            Passenger p = seatedPassengers[i];
            if (p == null)
            {
                seatedPassengers.RemoveAt(i);
                continue;
            }

            if (p.DropOffStopIndex == stopIndex)
            {
                FreeNearestSeat(p.transform.position);
                Destroy(p.gameObject);
                seatedPassengers.RemoveAt(i);

                if (debugLogs)
                    Debug.Log($"[Spawner] STOP EXIT {p.PassengerName} at stop {stopIndex}");
            }
        }
    }

    public void SpawnPassengers(int currentStopIndex, int stopCount)
    {
        if (queueManager == null) return;

        for (int i = 0; i < defaultSpawnPerStop; i++)
        {
            Passenger prefab = PickPrefab();
            if (prefab == null)
            {
                Debug.LogWarning("[Spawner] No passenger prefabs configured.");
                return;
            }

            Passenger p = Instantiate(prefab, spawnRoot.position, spawnRoot.rotation);

            // Name
            if (nameGenerator != null)
                p.SetPassengerName(nameGenerator.GenerateName(p.IsAnomaly));

            // Drop-off
            int minDrop = currentStopIndex + 1;
            int maxDrop = Mathf.Max(minDrop + 1, stopCount);
            int dropOff = (stopCount > minDrop)
                ? Random.Range(minDrop, maxDrop)
                : currentStopIndex;

            p.SetDropOffStopIndex(dropOff);

            // Stops info (fixes -1 bug)
            int trueStops = Mathf.Max(0, dropOff - currentStopIndex);
            int claimedStops = p.IsAnomaly
                ? Mathf.Clamp(trueStops + Random.Range(-2, 3), 0, stopCount)
                : trueStops;

            p.SetStopsInfo(trueStops, claimedStops,
                p.IsAnomaly
                    ? Passenger.StopInfoAccuracy.IntentionalLie
                    : Passenger.StopInfoAccuracy.Correct);

            bool queued = queueManager.Enqueue(p, spawnDistanceOnSpline);
            if (!queued) Destroy(p.gameObject);

            if (debugLogs)
                Debug.Log($"[Spawner] SPAWNED {p.PassengerName} anomaly={p.IsAnomaly}");
        }
    }

    // =====================================================
    // INTERNAL HELPERS
    // =====================================================

    private Passenger PickPrefab()
    {
        bool spawnAnomaly = Random.value < anomalyChance;

        Passenger[] list = spawnAnomaly ? anomalyPassengerPrefabs : normalPassengerPrefabs;

        if (list == null || list.Length == 0)
            list = normalPassengerPrefabs;

        if (list == null || list.Length == 0)
            return null;

        return list[Random.Range(0, list.Length)];
    }

    private SeatAnchor FindFreeSeat()
    {
        foreach (var s in seats)
            if (s != null && !s.Occupied)
                return s;

        return null;
    }

    private void FreeNearestSeat(Vector3 pos)
    {
        SeatAnchor best = null;
        float bestDist = float.MaxValue;

        foreach (var s in seats)
        {
            if (s == null) continue;
            float d = (s.transform.position - pos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = s;
            }
        }

        if (best != null)
            best.Occupied = false;
    }
}
