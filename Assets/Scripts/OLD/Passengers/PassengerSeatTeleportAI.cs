using UnityEngine;

[RequireComponent(typeof(Passenger))]
public class PassengerSeatTeleporterAI : MonoBehaviour
{
    public Passenger CurrentTarget => currentTarget;

    [Header("Timing (only counts while unobserved)")]
    [SerializeField] private Vector2 humanTeleportDelay = new Vector2(3f, 7f);
    [SerializeField] private Vector2 anomalyTeleportDelay = new Vector2(2f, 6f);

    [Header("Seat rules")]
    [SerializeField] private bool humansCanSwap = false;
    [SerializeField] private bool anomaliesCanSwap = false;

    [Header("Human flee behaviour")]
    [SerializeField] private float fearRadius = 4.5f;
    [SerializeField] private int considerTopSeats = 6;
    [SerializeField] private float minSafetyImprove = 0.8f;
    [SerializeField] private float movePenalty = 0.35f;

    [Header("Anomaly hunt (single target)")]
    [SerializeField] private float acquireRadius = 12f;
    [SerializeField] private float loseTargetRadius = 18f;
    [SerializeField] private float retargetCooldown = 6f;

    [Header("Anomaly stalking")]
    [SerializeField] private bool excludeTargetSeat = true;
    [SerializeField] private float minDistanceFromTarget = 0.0f; // 0 allows adjacent

    [Header("Debug")]
    [SerializeField] private bool debugAnomalyTarget = true;
    [SerializeField] private float debugInterval = 0.5f;
    [SerializeField] private bool debugAdjacency = true;

    private float _debugTimer;

    private Passenger passenger;

    private bool pending;
    private float timer;

    private Passenger currentTarget;
    private float retargetTimer;

    private void Awake()
    {
        passenger = GetComponent<Passenger>();
    }

    private void Update()
    {
        if (passenger == null) return;
        if (SeatManager.Instance == null) return;

        retargetTimer -= Time.deltaTime;

        // Freeze + cancel countdown when observed
        if (passenger.IsObserved)
        {
            pending = false;
            timer = 0f;
            return;
        }

        // Start countdown when unobserved
        if (!pending)
        {
            pending = true;
            timer = PickDelaySeconds();
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        if (passenger.IsAnomaly)
            TeleportAsAnomalyFollower();
        else
            TeleportAsHuman();

        pending = false;
        timer = 0f;

        DebugAnomalyState();
    }

    private float PickDelaySeconds()
    {
        Vector2 range = passenger.IsAnomaly ? anomalyTeleportDelay : humanTeleportDelay;
        float min = Mathf.Min(range.x, range.y);
        float max = Mathf.Max(range.x, range.y);
        return Random.Range(min, max);
    }

    // ---------------- HUMANS ----------------

    private void TeleportAsHuman()
    {
        var mgr = SeatManager.Instance;

        Passenger nearestAnomaly = FindNearestAnomaly(passenger.transform.position, fearRadius);
        if (nearestAnomaly == null) return;

        var mySeat = mgr.GetSeatForPassenger(passenger) ?? mgr.FindNearestSeat(passenger.transform.position);
        if (mySeat == null) return;

        // If blocked in: cannot move
        if (mySeat.IsBlocked())
            return;

        var chosen = PickHumanSeatSmart(nearestAnomaly.transform.position, mySeat);
        if (chosen == null) return;

        if (chosen.IsBlocked())
            return;

        mgr.TryTeleportToSeat(passenger, chosen, forceSwap: humansCanSwap);
    }

    private SeatAnchor PickHumanSeatSmart(Vector3 threatPos, SeatAnchor currentSeat)
    {
        var mgr = SeatManager.Instance;
        float currentSafety = Vector3.Distance(threatPos, currentSeat.transform.position);

        SeatAnchor[] topSeats = new SeatAnchor[Mathf.Max(1, considerTopSeats)];
        float[] topScore = new float[topSeats.Length];
        for (int i = 0; i < topSeats.Length; i++) { topSeats[i] = null; topScore[i] = float.NegativeInfinity; }

        for (int i = 0; i < mgr.SeatCount; i++)
        {
            var seat = mgr.GetSeat(i);
            if (seat == null || seat == currentSeat) continue;

            if (!mgr.IsSeatFree(seat)) continue;
            if (seat.IsBlocked()) continue;

            float safety = Vector3.Distance(threatPos, seat.transform.position);
            if (safety < currentSafety + minSafetyImprove) continue;

            float penalty = movePenalty * Vector3.Distance(currentSeat.transform.position, seat.transform.position);
            float score = safety - penalty;

            for (int k = 0; k < topSeats.Length; k++)
            {
                if (score > topScore[k])
                {
                    for (int j = topSeats.Length - 1; j > k; j--)
                    {
                        topSeats[j] = topSeats[j - 1];
                        topScore[j] = topScore[j - 1];
                    }
                    topSeats[k] = seat;
                    topScore[k] = score;
                    break;
                }
            }
        }

        int count = 0;
        for (int i = 0; i < topSeats.Length; i++)
            if (topSeats[i] != null) count++;

        if (count == 0) return null;

        return topSeats[Random.Range(0, count)];
    }

    // ---------------- ANOMALY ----------------

    private void DebugAnomalyState(string reason = "")
    {
        if (!debugAnomalyTarget) return;
        if (passenger == null || !passenger.IsAnomaly) return;

        _debugTimer -= Time.deltaTime;
        if (_debugTimer > 0f) return;
        _debugTimer = debugInterval;

        var mgr = SeatManager.Instance;
        string mySeat = mgr != null ? (mgr.GetSeatForPassenger(passenger)?.name ?? "none") : "no SeatManager";
        string tgtName = currentTarget != null ? $"{currentTarget.PassengerName} ({currentTarget.name})" : "NONE";
        string tgtSeat = (mgr != null && currentTarget != null) ? (mgr.GetSeatForPassenger(currentTarget)?.name ?? "none") : "n/a";

        Debug.Log($"[AnomTarget] {passenger.PassengerName} obs={passenger.IsObserved} pending={pending} t={timer:0.00} " +
                  $"mySeat={mySeat} target={tgtName} tgtSeat={tgtSeat} {reason}");
    }

    private void TeleportAsAnomalyFollower()
    {
        var mgr = SeatManager.Instance;

        EnsureTarget();
        if (currentTarget == null) return;

        float d = Vector3.Distance(passenger.transform.position, currentTarget.transform.position);
        if (d > loseTargetRadius)
        {
            currentTarget = null;
            retargetTimer = retargetCooldown;
            return;
        }

        SeatAnchor targetSeat = mgr.GetSeatForPassenger(currentTarget);
        if (targetSeat == null)
        {
            DebugAnomalyState("-> target not seated");
            return;
        }

        SeatAnchor exclude = excludeTargetSeat ? targetSeat : null;

        SeatAnchor best = null;
        string chosenBy = "NONE";

        // 1) Adjacent-first
        best = PickAdjacentSeatToTarget(targetSeat, exclude);
        if (best != null) chosenBy = "ADJ";

        // 2) Fallback: closest seat
        if (best == null)
        {
            best = PickClosestSeatToTargetExcluding(targetSeat.transform.position, exclude);
            if (best != null) chosenBy = "FALLBACK";
        }

        if (best == null)
        {
            if (debugAdjacency)
            {
                Debug.Log($"[STALK] {passenger.PassengerName} targetSeat={targetSeat.name} -> NO VALID SEAT (adj + fallback failed)");
            }
            return;
        }

        var mySeat = mgr.GetSeatForPassenger(passenger) ?? mgr.FindNearestSeat(passenger.transform.position);
        if (mySeat == best) return;

        if (debugAdjacency)
        {
            Debug.Log($"[STALK] {passenger.PassengerName} targetSeat={targetSeat.name} chose={best.name} via={chosenBy} " +
                      $"swap={anomaliesCanSwap} bestOccupied={best.Occupied}");
        }

        mgr.TryTeleportToSeat(passenger, best, forceSwap: anomaliesCanSwap);
    }

    private SeatAnchor PickAdjacentSeatToTarget(SeatAnchor targetSeat, SeatAnchor excludeSeat)
    {
        if (targetSeat == null) return null;

        var mgr = SeatManager.Instance;
        var adj = targetSeat.AdjacentSeats;

        if (debugAdjacency)
        {
            Debug.Log($"[STALK] {passenger.PassengerName} targetSeat={targetSeat.name} adjCount={(adj == null ? 0 : adj.Count)} " +
                      $"swap={anomaliesCanSwap} exclude={(excludeSeat != null ? excludeSeat.name : "none")}");
        }

        if (adj == null || adj.Count == 0)
            return null;

        SeatAnchor best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < adj.Count; i++)
        {
            var seat = adj[i];
            if (seat == null)
            {
                if (debugAdjacency) Debug.Log("[STALK] adj -> null (skip)");
                continue;
            }

            if (seat == excludeSeat)
            {
                if (debugAdjacency) Debug.Log($"[STALK] adj={seat.name} REJECT: is excludeSeat");
                continue;
            }

            if (!anomaliesCanSwap && !mgr.IsSeatFree(seat))
            {
                if (debugAdjacency) Debug.Log($"[STALK] adj={seat.name} REJECT: occupied and swap OFF");
                continue;
            }

            if (minDistanceFromTarget > 0f)
            {
                float seatToTarget = Vector3.Distance(seat.transform.position, targetSeat.transform.position);
                if (seatToTarget < minDistanceFromTarget)
                {
                    if (debugAdjacency) Debug.Log($"[STALK] adj={seat.name} REJECT: minDistanceFromTarget ({seatToTarget:0.00} < {minDistanceFromTarget:0.00})");
                    continue;
                }
            }

            float score = 0f;
            if (mgr.IsSeatFree(seat)) score += 1000f;
            score -= Vector3.Distance(seat.transform.position, targetSeat.transform.position);

            if (debugAdjacency) Debug.Log($"[STALK] adj={seat.name} OK: occupied={seat.Occupied} score={score:0.00}");

            if (score > bestScore)
            {
                bestScore = score;
                best = seat;
            }
        }

        return best;
    }

    private void EnsureTarget()
    {
        if (currentTarget != null && currentTarget.IsAnomaly)
        {
            DebugAnomalyState("-> target invalid (is anomaly), clearing");
            currentTarget = null;
        }

        if (currentTarget == null)
        {
            if (retargetTimer > 0f) return;

            var newTarget = FindNearestHuman(passenger.transform.position, acquireRadius);
            currentTarget = newTarget;
            retargetTimer = retargetCooldown;

            DebugAnomalyState(newTarget != null ? "-> acquired target" : "-> no target in range");
        }
    }

    private SeatAnchor PickClosestSeatToTargetExcluding(Vector3 targetPos, SeatAnchor excludeSeat)
    {
        var mgr = SeatManager.Instance;

        SeatAnchor best = null;
        float bestD = float.MaxValue;

        for (int i = 0; i < mgr.SeatCount; i++)
        {
            var seat = mgr.GetSeat(i);
            if (seat == null) continue;
            if (seat == excludeSeat) continue;

            if (minDistanceFromTarget > 0f)
            {
                float seatToTarget = Vector3.Distance(seat.transform.position, targetPos);
                if (seatToTarget < minDistanceFromTarget)
                    continue;
            }

            if (!anomaliesCanSwap && !mgr.IsSeatFree(seat))
                continue;

            float d = Vector3.Distance(seat.transform.position, targetPos);
            if (d < bestD)
            {
                bestD = d;
                best = seat;
            }
        }

        return best;
    }

    // ---------------- FIND PASSENGERS ----------------

    private Passenger FindNearestAnomaly(Vector3 pos, float radius)
    {
        Passenger best = null;
        float bestD = float.MaxValue;

        foreach (var p in PassengerRegistry.All)
        {
            if (p == null || !p.IsAnomaly) continue;

            float d = Vector3.Distance(pos, p.transform.position);
            if (d <= radius && d < bestD)
            {
                best = p;
                bestD = d;
            }
        }

        return best;
    }

    private Passenger FindNearestHuman(Vector3 pos, float radius)
    {
        Passenger best = null;
        float bestD = float.MaxValue;

        foreach (var p in PassengerRegistry.All)
        {
            if (p == null || p.IsAnomaly) continue;

            float d = Vector3.Distance(pos, p.transform.position);
            if (d <= radius && d < bestD)
            {
                best = p;
                bestD = d;
            }
        }

        return best;
    }
}
