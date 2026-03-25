using UnityEngine;

[CreateAssetMenu(menuName = "Anomaly/Actions/Swap Positions")]
public class AnomalyActionSwapPositions : AnomalyActionBase
{
    [SerializeField] private float searchRadius = 4f;

    public override bool TryExecute(AnomalyController controller, Passenger self)
    {
        if (SeatManager.Instance == null || self == null)
            return false;

        // Get stalker AI (if any)
        var stalker = self.GetComponent<PassengerSeatTeleporterAI>();
        Passenger protectedTarget = stalker != null ? stalker.CurrentTarget : null;

        Passenger best = null;
        float bestD = float.MaxValue;

        foreach (var p in PassengerRegistry.All)
        {
            if (p == null || p == self) continue;
            if (p.IsAnomaly) continue;

            // 🔒 DO NOT swap with the stalk target
            if (protectedTarget != null && p == protectedTarget)
                continue;

            float d = Vector3.Distance(self.transform.position, p.transform.position);
            if (d > searchRadius) continue;

            if (d < bestD)
            {
                best = p;
                bestD = d;
            }
        }

        if (best == null)
            return false;

        var mgr = SeatManager.Instance;

        var mySeat = mgr.GetSeatForPassenger(self);
        var otherSeat = mgr.GetSeatForPassenger(best);

        // Must be seated for a seat swap to make sense
        if (mySeat == null || otherSeat == null)
            return false;

        // Swap via SeatManager (keeps parenting + occupancy correct)
        bool okA = mgr.TryTeleportToSeat(self, otherSeat, forceSwap: true);
        bool okB = mgr.TryTeleportToSeat(best, mySeat, forceSwap: true);

        if (okA && okB)
        {
            Debug.Log($"[ANOMALY] {self.PassengerName} swapped seats with {best.PassengerName}");
            return true;
        }

        return false;
    }
}
