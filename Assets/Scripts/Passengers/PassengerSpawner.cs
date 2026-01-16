using System.Collections.Generic;
using UnityEngine;

public sealed class PassengerSpawner : MonoBehaviour
{
    [System.Serializable]
    public sealed class StopSpawnSet
    {
        public int stopIndex;
        public Transform[] spawnPoints;
    }

    [Header("Prefabs")]
    [SerializeField] private Passenger[] normalPassengerPrefabs;
    [SerializeField] private Passenger[] anomalyPassengerPrefabs;
    [SerializeField, Range(0f, 1f)] private float anomalyChance = 0.15f;

    [Header("Per-stop fixed spawn points")]
    [SerializeField] private StopSpawnSet[] stopSpawnSets;
    [SerializeField] private Transform[] fallbackSpawnPoints;

    [Header("Joining the queue")]
    [SerializeField] private QueueManagerSpline queueManager;
    [SerializeField] private Transform queueEntryPoint;

    [Header("Spawn count")]
    [SerializeField] private int minSpawnCount = 1;
    [SerializeField] private int maxSpawnCount = 4;

    [Header("Naming")]
    [SerializeField] private NameGenerator nameGenerator;

    [Header("Stop info behaviour")]
    [SerializeField, Range(0f, 1f)] private float humanMistakeChance = 0.10f;
    [SerializeField, Range(0f, 1f)] private float anomalyLieChance = 0.40f;

    [Header("Seats")]
    [SerializeField] private SeatAnchor[] seats;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private readonly HashSet<Passenger> activePassengers = new();
    private readonly List<Passenger> seatedPassengers = new();

    private void Awake()
    {
        if (queueManager == null) queueManager = FindFirstObjectByType<QueueManagerSpline>();
        if (nameGenerator == null) nameGenerator = FindFirstObjectByType<NameGenerator>();

        if (seats == null || seats.Length == 0)
            seats = FindObjectsByType<SeatAnchor>(FindObjectsSortMode.None);

        if (debugLogs)
            Debug.Log($"[Spawner] Awake seats={seats?.Length ?? 0}");
    }

    public void SpawnPassengers(int currentStopIndex, int stopCount)
    {
        if (queueManager == null)
        {
            Debug.LogWarning("PassengerSpawner: No QueueManagerSpline assigned.");
            return;
        }

        if (queueEntryPoint == null)
        {
            Debug.LogWarning("PassengerSpawner: No queueEntryPoint assigned.");
            return;
        }

        if (stopCount < 2)
        {
            Debug.LogWarning("PassengerSpawner: stopCount must be >= 2.");
            return;
        }

        if (maxSpawnCount < minSpawnCount) maxSpawnCount = minSpawnCount;
        int spawnCount = Random.Range(minSpawnCount, maxSpawnCount + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            Passenger prefab = PickPrefab();
            if (prefab == null)
            {
                Debug.LogWarning("PassengerSpawner: No prefabs configured.");
                return;
            }

            Transform spawnT = GetSpawnPointForStop(currentStopIndex, i);
            Vector3 spawnPos = spawnT != null ? spawnT.position : queueEntryPoint.position;
            Quaternion spawnRot = spawnT != null ? spawnT.rotation : queueEntryPoint.rotation;

            Passenger p = Instantiate(prefab, spawnPos, spawnRot);

            if (nameGenerator != null)
                p.SetPassengerName(nameGenerator.GenerateName(p.IsAnomaly));

            int drop = PickFutureStop(currentStopIndex, stopCount);
            p.SetDropOffStopIndex(drop);

            int trueStops = StopsAhead(currentStopIndex, drop, stopCount);
            Passenger.StopInfoAccuracy accuracy;
            int claimedStops = PickClaimedStops(p, trueStops, stopCount, out accuracy);
            p.SetStopsInfo(trueStops, claimedStops, accuracy);

            // Walk to entry, then enqueue at a good "back of line" distance
            int idx = queueManager.ReserveIndex();

            var join = p.GetComponent<PassengerJoinQueue>();
            if (join == null) join = p.gameObject.AddComponent<PassengerJoinQueue>();

            join.Begin(p, queueManager, idx);


            activePassengers.Add(p);

   
        }
    }

    public void DismissPassengersForStop(int arrivedStopIndex)
    {
        if (activePassengers.Count == 0) return;

        List<Passenger> toRemove = new();

        foreach (var p in activePassengers)
        {
            if (p == null) continue;
            if (p.DropOffStopIndex == arrivedStopIndex)
                toRemove.Add(p);
        }

        for (int i = 0; i < toRemove.Count; i++)
            DismissPassenger(toRemove[i]);
    }

    public bool SeatPassenger(Passenger p)
    {
        if (p == null)
        {
            Debug.LogWarning("[Spawner] Seat failed: passenger NULL");
            return false;
        }

        if (queueManager == null)
        {
            Debug.LogWarning("[Spawner] Seat failed: queueManager NULL");
            return false;
        }

        if (!queueManager.IsFront(p))
        {
            Debug.LogWarning($"[Spawner] Seat failed: {p.PassengerName} is not front. Front={(queueManager.FrontPassenger ? queueManager.FrontPassenger.PassengerName : "NULL")}");
            return false;
        }

        SeatAnchor seat = FindFreeSeat();
        if (seat == null)
        {
            Debug.LogWarning("[Spawner] Seat failed: no free seats");
            return false;
        }

        // Remove from queue first
        queueManager.Remove(p);

        // Stop any queue movement scripts
        var join = p.GetComponent<PassengerJoinQueue>();

        if (join != null) Destroy(join);

        var walker = p.GetComponent<QueuedSplineWalker>();
        if (walker != null) walker.StopMoving();

        // Physics off
        var rb = p.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Mark seat + parent passenger so they move with the bus
        seat.Occupied = true;

        p.transform.SetParent(seat.transform, false);
        p.transform.localPosition = Vector3.zero;
        p.transform.localRotation = Quaternion.identity;

        seatedPassengers.Add(p);

        if (debugLogs)
            Debug.Log($"[Spawner] SEATED {p.PassengerName} on {seat.name}");

        return true;
    }

    public bool DismissPassenger(Passenger p)
    {
        if (p == null) return false;

        if (queueManager != null)
            queueManager.Remove(p);

        activePassengers.Remove(p);
        Destroy(p.gameObject);

        if (debugLogs)
            Debug.Log($"[Spawner] DISMISSED {p.PassengerName}");

        return true;
    }

    // ---------- Seats ----------

    private SeatAnchor FindFreeSeat()
    {
        if (seats == null) return null;
        for (int i = 0; i < seats.Length; i++)
        {
            if (seats[i] != null && !seats[i].Occupied)
                return seats[i];
        }
        return null;
    }

    // ---------- Fixed spawn points helpers ----------

    private Transform GetSpawnPointForStop(int stopIndex, int passengerNumberThisStop)
    {
        if (stopSpawnSets != null)
        {
            for (int s = 0; s < stopSpawnSets.Length; s++)
            {
                var set = stopSpawnSets[s];
                if (set == null) continue;
                if (set.stopIndex != stopIndex) continue;
                return PickPoint(set.spawnPoints, passengerNumberThisStop);
            }
        }

        return PickPoint(fallbackSpawnPoints, passengerNumberThisStop);
    }

    private static Transform PickPoint(Transform[] points, int passengerNumberThisStop)
    {
        if (points == null || points.Length == 0) return null;
        int idx = Mathf.Abs(passengerNumberThisStop) % points.Length;
        return points[idx];
    }

    // ---------- Prefab selection ----------

    private Passenger PickPrefab()
    {
        bool wantAnomaly = Random.value < anomalyChance;
        Passenger[] list = wantAnomaly ? anomalyPassengerPrefabs : normalPassengerPrefabs;

        if (list == null || list.Length == 0)
            list = normalPassengerPrefabs;

        if (list == null || list.Length == 0)
            return null;

        return list[Random.Range(0, list.Length)];
    }

    // ---------- Stop helpers ----------

    private static int PickFutureStop(int currentStopIndex, int stopCount)
    {
        int offset = Random.Range(1, stopCount);
        return (currentStopIndex + offset) % stopCount;
    }

    private static int StopsAhead(int currentStopIndex, int dropOffStopIndex, int stopCount)
    {
        int dist = (dropOffStopIndex - currentStopIndex + stopCount) % stopCount;
        return Mathf.Clamp(dist, 1, stopCount - 1);
    }

    private int PickClaimedStops(Passenger p, int trueStops, int stopCount, out Passenger.StopInfoAccuracy accuracy)
    {
        accuracy = Passenger.StopInfoAccuracy.Correct;
        if (p == null) return trueStops;

        if (!p.IsAnomaly)
        {
            if (Random.value < humanMistakeChance)
            {
                accuracy = Passenger.StopInfoAccuracy.AccidentalMistake;
                return MakeAccidentalMistake(trueStops, stopCount);
            }
            return trueStops;
        }

        if (Random.value < anomalyLieChance)
        {
            accuracy = Passenger.StopInfoAccuracy.IntentionalLie;
            return MakeIntentionalLie(trueStops, stopCount);
        }

        return trueStops;
    }

    private static int MakeAccidentalMistake(int trueStops, int stopCount)
    {
        int delta = Random.value < 0.5f ? -1 : 1;
        int claimed = Mathf.Clamp(trueStops + delta, 1, stopCount - 1);

        if (claimed == trueStops)
            claimed = Mathf.Clamp(trueStops == 1 ? 2 : trueStops - 1, 1, stopCount - 1);

        return claimed;
    }

    private static int MakeIntentionalLie(int trueStops, int stopCount)
    {
        int wrong;
        do { wrong = Random.Range(1, stopCount); }
        while (wrong == trueStops);
        return wrong;
    }
}
