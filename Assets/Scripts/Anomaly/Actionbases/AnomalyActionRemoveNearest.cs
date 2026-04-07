using UnityEngine;

[CreateAssetMenu(menuName = "Anomaly/Actions/Remove Nearest Passenger")]
public class AnomalyActionRemoveNearest : AnomalyActionBase
{
    [SerializeField] private float radius = 6f;

    [Header("Rules")]
    [SerializeField] private bool seatedVictimsOnly = true;

    [Header("After kill")]
    [SerializeField, Range(0f, 1f)] private float takeVictimSeatChance = 0.6f;
    [SerializeField] private bool forceSwap = false; // normally false

    public override bool TryExecute(AnomalyController controller, Passenger self)
    {
        if (self == null)
            return false;

        // If the killer itself is seated, never allow it to kill boarding / queue passengers.
        bool requireSeatedVictim = seatedVictimsOnly || self.IsSeatedPassenger;

        var victim = PassengerUtil.FindNearest(
            self.transform.position,
            radius,
            exclude: self,
            seatedOnly: requireSeatedVictim);

        if (victim == null)
            return false;

        // Extra safety: do not kill unseated victims from a seated anomaly.
        if (self.IsSeatedPassenger && !victim.IsSeatedPassenger)
            return false;

        // Cache victim seat (if any) BEFORE destroy
        SeatAnchor victimSeat = null;
        if (SeatManager.Instance != null)
            victimSeat = SeatManager.Instance.GetSeatForPassenger(victim);

        // Free victim seat in manager
        if (SeatManager.Instance != null)
            SeatManager.Instance.NotifyPassengerRemoved(victim);

        Debug.Log($"[ANOMALY] {self.PassengerName} removed {victim.PassengerName}");

        Object.Destroy(victim.gameObject);

        // Sometimes take their seat
        if (victimSeat != null && Random.value <= takeVictimSeatChance && SeatManager.Instance != null)
        {
            SeatManager.Instance.TryTeleportToSeat(self, victimSeat, forceSwap);
            Debug.Log($"[ANOMALY] {self.PassengerName} took victim seat");
        }

        return true;
    }
}