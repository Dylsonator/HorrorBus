using UnityEngine;

public sealed class Passenger : MonoBehaviour
{
    public enum StopInfoAccuracy
    {
        Correct,
        AccidentalMistake,
        IntentionalLie
    }

    [Header("Public-facing")]
    [SerializeField] private string passengerName;

    [Header("Hidden data")]
    [SerializeField] private bool isAnomaly;

    [Header("Route (assigned at spawn)")]
    [SerializeField] private int dropOffStopIndex = -1;

    [Header("Stops info (assigned at spawn)")]
    [SerializeField] private int trueStopsRemaining = -1;
    [SerializeField] private int claimedStopsRemaining = -1;
    [SerializeField] private StopInfoAccuracy stopInfoAccuracy = StopInfoAccuracy.Correct;

    public string PassengerName => passengerName;

    // Keep hidden unless you explicitly need it
    public bool IsAnomaly => isAnomaly;

    public int DropOffStopIndex => dropOffStopIndex;

    // What the passenger CLAIMS
    public int ClaimedStopsRemaining => claimedStopsRemaining;

    // Optional: useful for debugging, don’t show to player
    public int TrueStopsRemaining => trueStopsRemaining;
    public StopInfoAccuracy ClaimedStopAccuracy => stopInfoAccuracy;

    public void SetDropOffStopIndex(int stopIndex)
    {
        dropOffStopIndex = stopIndex;
    }

    public void SetStopsInfo(int trueStops, int claimedStops, StopInfoAccuracy accuracy)
    {
        trueStopsRemaining = trueStops;
        claimedStopsRemaining = claimedStops;
        stopInfoAccuracy = accuracy;
    }
    public void SetPassengerName(string newName)
    {
        passengerName = newName;
    }
}
