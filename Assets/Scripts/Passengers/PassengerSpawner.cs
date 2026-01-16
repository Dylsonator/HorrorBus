using System.Collections.Generic;
using System.Xml;
using UnityEngine;

public sealed class PassengerSpawner : MonoBehaviour
{
    [System.Serializable]
    public sealed class StopSpawnSet
    {
        public int stopIndex;
        public Transform[] spawnPoints;
    }
    [Header("Stop gate")]
    
    [SerializeField] private StopGate decisionGate;


    [Header("Prefabs")]
    [SerializeField] private Passenger[] normalPassengerPrefabs;
    [SerializeField] private Passenger[] anomalyPassengerPrefabs;
    [SerializeField, Range(0f, 1f)] private float anomalyChance = 0.15f;

    [Header("Per-stop fixed spawn points")]
    [SerializeField] private StopSpawnSet[] stopSpawnSets;
    [SerializeField] private Transform[] fallbackSpawnPoints;

    [Header("Queue")]
    [SerializeField] private QueueManagerNodes queueManager;
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
        if (queueManager == null) queueManager = FindFirstObjectByType<QueueManagerNodes>();
        if (nameGenerator == null) nameGenerator = FindFirstObjectByType<NameGenerator>();

        if (seats == null || seats.Length == 0)
            seats = FindObjectsByType<SeatAnchor>(FindObjectsSortMode.None);

        if (debugLogs)
            Debug.Log($"[Spawner] Awake seats={seats?.Length ?? 0}");
        if (decisionGate == null) decisionGate = FindFirstObjectByType<StopGate>();

    }

    public void SpawnPassengers(int currentStopIndex, int stopCount)
    {
        if (queueManager == null)
        {
            Debug.LogWarning("[Spawner] No QueueManagerNodes assigned.");
            return;
        }

        if (queueEntryPoint == null)
        {
            Debug.LogWarning("[Spawner] No queueEntryPoint assigned.");
            return;
        }

        if (stopCount < 2)
        {
            Debug.LogWarning("[Spawner] stopCount must be >= 2.");
            return;
        }

        if (maxSpawnCount < minSpawnCount) maxSpawnCount = minSpawnCount;
        int spawnCount = Random.Range(minSpawnCount, maxSpawnCount + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            Passenger prefab = PickPrefab();
            if (prefab == null)
            {
                Debug.LogWarning("[Spawner] No prefabs configured.");
                return;
            }

            Transform spawnT = GetSpawnPointForStop(currentStopIndex, i);
            Vector3 spawnPos = spawnT != null ? spawnT.position : queueEntryPoint.position;
            Quaternion spawnRot = spawnT != null ? spawnT.rotation : queueEntryPoint.rotation;

            Passenger p = Instantiate(prefab, spawnPos, spawnRot);

            if (decisionGate != null)
                decisionGate.Register(p);
            if (nameGenerator != null)
                p.SetPassengerName(nameGenerator.GenerateName(p.IsAnomaly));


            // now randomise
            var app = p.GetComponent<PassengerAppearance>();
            if (app != null)
            {
                int seed = p.PassengerName.GetHashCode() ^ Random.Range(int.MinValue, int.MaxValue);
                app.Randomize(seed);
            }


            int drop = PickFutureStop(currentStopIndex, stopCount);
            p.SetDropOffStopIndex(drop);

            int trueStops = StopsAhead(currentStopIndex, drop, stopCount);
            Passenger.StopInfoAccuracy accuracy;
            int claimedStops = PickClaimedStops(p, trueStops, stopCount, out accuracy);
            p.SetStopsInfo(trueStops, claimedStops, accuracy);
            int expectedFare = fareTable.GetFare(claimedStops);
            int paid = GeneratePaidAmount(expectedFare, p.IsAnomaly, accuracy);
            p.SetFare(expectedFare, paid);


            // Walk to entry point in world-space, THEN join node-queue
            var join = p.GetComponent<PassengerJoinQueue>();
            if (join == null) join = p.gameObject.AddComponent<PassengerJoinQueue>();

            join.Begin(p, queueManager, queueEntryPoint);

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

        // Remove from queue so others advance
        queueManager.Remove(p);

        // Remove join behaviour
        var join = p.GetComponent<PassengerJoinQueue>();
        if (join != null) Destroy(join);

        // Stop queue walker (node version)
        var walker = p.GetComponent<NodeQueueWalker>();
        if (walker != null) walker.StopMoving();

        // Physics off (optional, if you use RBs)
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
        // Resolve decision: they've been handled
        if (decisionGate != null)
            decisionGate.Resolve(p);

        return true;
    }

    public bool DismissPassenger(Passenger p)
    {
        if (p == null) return false;

        // Remove from queue first (safe if they weren't queued)
        if (queueManager != null)
            queueManager.Remove(p);

        activePassengers.Remove(p);
        seatedPassengers.Remove(p);
        // Resolve decision: they've been handled
        if (decisionGate != null)
            decisionGate.Resolve(p);

        Destroy(p.gameObject);

        if (debugLogs)
            Debug.Log($"[Spawner] DISMISSED {p.PassengerName}");

        return true;
    }

    // ---------- Seats ----------

    private SeatAnchor FindFreeSeat()
    {
        if (seats == null || seats.Length == 0) return null;

        // collect free seats
        int freeCount = 0;
        for (int i = 0; i < seats.Length; i++)
            if (seats[i] != null && !seats[i].Occupied)
                freeCount++;

        if (freeCount == 0) return null;

        // pick random free index
        int pick = Random.Range(0, freeCount);
        for (int i = 0; i < seats.Length; i++)
        {
            if (seats[i] == null || seats[i].Occupied) continue;
            if (pick-- == 0) return seats[i];
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
    [Header("Fare")]
    [SerializeField] private FareTable fareTable;

    [SerializeField, Range(0f, 1f)] private float humanUnderpayChance = 0.08f;
    [SerializeField, Range(0f, 1f)] private float anomalyWrongPayChance = 0.55f;

    private int GeneratePaidAmount(int expected, bool isAnomaly, Passenger.StopInfoAccuracy accuracy)
    {
        if (!isAnomaly)
        {
            // humans mostly pay correct, sometimes +/- 1
            if (Random.value < humanUnderpayChance)
                return Mathf.Max(0, expected + (Random.value < 0.5f ? -1 : +1));
            return expected;
        }

        // anomalies often pay wrong on purpose
        if (Random.value < anomalyWrongPayChance)
        {
            int delta = Random.value < 0.5f ? -1 : +1;
            int wrong = Mathf.Max(0, expected + delta);

            if (wrong == expected) wrong = expected + 1;
            return wrong;
        }

        return expected;
    }

}
