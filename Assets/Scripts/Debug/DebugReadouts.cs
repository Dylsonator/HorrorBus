using UnityEngine;
using TMPro;

/// <summary>
/// Displays debug readouts for speed, looking back state, and distance to next stop.
/// </summary>
public sealed class DebugReadouts : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BusDrive follower;
    [SerializeField] private BusSlowDown busSlowDown;
    [SerializeField] private RouteStops routeStops;

    [Header("UI")]
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text lookBackText;
    [SerializeField] private TMP_Text stopDistanceText;

    private void Update()
    {
        if (follower != null && speedText != null)
            speedText.text = $"Speed: {follower.CurrentSpeed:0.00}";

        if (busSlowDown != null && lookBackText != null)
            lookBackText.text = $"Looking Back: {busSlowDown.IsLookingBack}";

        if (routeStops != null && stopDistanceText != null)
            stopDistanceText.text = $"To Stop: {routeStops.DistanceToNextStop:0.0}u";
    }
}
