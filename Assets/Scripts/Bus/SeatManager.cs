using System.Collections.Generic;
using UnityEngine;

public class SeatManager : MonoBehaviour
{
    public static SeatManager Instance { get; private set; }

    [Header("All seats (SeatAnchor components)")]
    [SerializeField] private List<SeatAnchor> seats = new();

    [Header("Init")]
    [SerializeField] private bool rebuildOnStart = true;
    [SerializeField] private float rebuildDelay = 0.1f;

    // passenger -> seat (cached, but hierarchy is the source of truth)
    private readonly Dictionary<Passenger, SeatAnchor> passengerToSeat = new();

    private void Awake()
    {
        Instance = this;

        // Safety: auto-fill if empty
        if (seats == null || seats.Count == 0)
        {
            seats = new List<SeatAnchor>(FindObjectsByType<SeatAnchor>(FindObjectsSortMode.None));
        }
    }

    private void Start()
    {
        if (rebuildOnStart)
            Invoke(nameof(RebuildOccupancyFromHierarchy), rebuildDelay);
    }

    public int SeatCount => seats != null ? seats.Count : 0;

    public SeatAnchor GetSeat(int index)
    {
        if (seats == null) return null;
        if (index < 0 || index >= seats.Count) return null;
        return seats[index];
    }

    public bool IsSeatFree(SeatAnchor seat) => seat != null && !seat.Occupied;

    /// <summary>
    /// Hierarchy is the source of truth:
    /// If a passenger is parented under a SeatAnchor, that's their seat.
    /// </summary>
    public SeatAnchor GetSeatForPassenger(Passenger p)
    {
        if (p == null) return null;

        // First: authoritative answer from hierarchy
        var seatFromParent = p.GetComponentInParent<SeatAnchor>();
        if (seatFromParent != null)
        {
            passengerToSeat[p] = seatFromParent;
            return seatFromParent;
        }

        // Fallback: cached mapping (may be null / stale)
        passengerToSeat.TryGetValue(p, out var seat);
        return seat;
    }

    /// <summary>
    /// Rebuild occupancy/mapping by scanning hierarchy, NOT by distance.
    /// </summary>
    public void RebuildOccupancyFromHierarchy()
    {
        if (seats == null) return;

        // Clear flags
        for (int i = 0; i < seats.Count; i++)
            if (seats[i] != null) seats[i].Occupied = false;

        passengerToSeat.Clear();

        foreach (var p in PassengerRegistry.All)
        {
            if (p == null) continue;

            var seat = p.GetComponentInParent<SeatAnchor>();
            if (seat == null) continue;

            // Seat is occupied
            seat.Occupied = true;
            passengerToSeat[p] = seat;
        }
    }

    /// <summary>
    /// Still useful as a fallback for "where am I roughly?" but should not define occupancy.
    /// </summary>
    public SeatAnchor FindNearestSeat(Vector3 worldPos)
    {
        if (seats == null) return null;

        SeatAnchor best = null;
        float bestD = float.MaxValue;

        for (int i = 0; i < seats.Count; i++)
        {
            var s = seats[i];
            if (s == null) continue;

            float d = Vector3.Distance(worldPos, s.transform.position);
            if (d < bestD)
            {
                bestD = d;
                best = s;
            }
        }

        return best;
    }

    public bool TryTeleportToSeat(Passenger p, SeatAnchor targetSeat, bool forceSwap)
    {
        if (p == null || targetSeat == null) return false;

        // IMPORTANT: only a real SeatAnchor parent counts as the current seat.
        // Do not use nearest-seat fallback here or standing passengers can fail valid teleports.
        var currentSeat = GetSeatForPassenger(p);

        if (targetSeat == currentSeat)
            return false;

        if (targetSeat.Occupied)
        {
            if (!forceSwap) return false;

            var other = FindPassengerInSeat(targetSeat);
            if (other == null) return false;
            if (currentSeat == null) return false;

            TeleportPassengerToSeat(p, targetSeat);
            TeleportPassengerToSeat(other, currentSeat);

            currentSeat.Occupied = true;
            targetSeat.Occupied = true;

            passengerToSeat[p] = targetSeat;
            passengerToSeat[other] = currentSeat;
            return true;
        }

        if (currentSeat != null)
            currentSeat.Occupied = false;

        targetSeat.Occupied = true;

        TeleportPassengerToSeat(p, targetSeat);
        passengerToSeat[p] = targetSeat;

        return true;
    }

    public void NotifyPassengerRemoved(Passenger p)
    {
        if (p == null) return;

        // Clear occupancy of the seat they're in (prefer hierarchy)
        var seat = GetSeatForPassenger(p);
        if (seat != null)
            seat.Occupied = false;

        passengerToSeat.Remove(p);
    }

    private Passenger FindPassengerInSeat(SeatAnchor seat)
    {
        if (seat == null) return null;

        // Strongest: find a Passenger in children of that seat
        var childPassenger = seat.GetComponentInChildren<Passenger>();
        if (childPassenger != null) return childPassenger;

        // Fallback: cache lookup
        foreach (var kvp in passengerToSeat)
        {
            if (kvp.Key == null) continue;
            if (kvp.Value == seat) return kvp.Key;
        }

        return null;
    }

    private static void TeleportPassengerToSeat(Passenger p, SeatAnchor seat)
    {
        if (p == null || seat == null) return;

        // IMPORTANT: parenting is the seat truth
        p.transform.SetParent(seat.transform, worldPositionStays: false);
        p.transform.localPosition = Vector3.zero;
        p.transform.localRotation = Quaternion.identity;
    }
}
