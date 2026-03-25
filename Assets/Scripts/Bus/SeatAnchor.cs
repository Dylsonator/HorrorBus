using System.Collections.Generic;
using UnityEngine;

public sealed class SeatAnchor : MonoBehaviour
{
    [Header("Occupancy")]
    [SerializeField] private bool occupied;

    [Header("Adjacency (for anomaly stalking)")]
    [Tooltip("Seats considered 'adjacent' to this seat. Used by the stalker to prefer next-to-target seats.")]
    [SerializeField] private List<SeatAnchor> adjacentSeats = new();

    [Header("Blocking Rules")]
    [Tooltip("If ALL of these seats are occupied, this seat is considered blocked (can't leave). " +
             "Leave empty if this seat can never be blocked.")]
    [SerializeField] private List<SeatAnchor> blockingNeighbours = new();

    public bool Occupied
    {
        get => occupied;
        set => occupied = value;
    }

    public IReadOnlyList<SeatAnchor> AdjacentSeats => adjacentSeats;
    public IReadOnlyList<SeatAnchor> BlockingNeighbours => blockingNeighbours;

    /// <summary>
    /// Blocked = every neighbour in BlockingNeighbours is occupied.
    /// If you don't assign neighbours, this returns false (not blocked).
    /// </summary>
    public bool IsBlocked()
    {
        if (blockingNeighbours == null || blockingNeighbours.Count == 0)
            return false;

        for (int i = 0; i < blockingNeighbours.Count; i++)
        {
            var n = blockingNeighbours[i];
            if (n == null) continue;

            if (!n.Occupied)
                return false; // at least one escape gap
        }

        return true;
    }
}
