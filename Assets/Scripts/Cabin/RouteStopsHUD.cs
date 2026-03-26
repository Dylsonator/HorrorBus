using TMPro;
using UnityEngine;

public sealed class RouteStopHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text currentStopText;
    [SerializeField] private TMP_Text nextStopText;
    [SerializeField] private TMP_Text distanceText;

    private void Update()
    {
        RouteStops route = RouteStops.Instance;
        if (route == null)
            return;

        string currentStop = route.GetStopNameSafe(route.NextStopIndex);

        if (currentStopText != null)
        {
            currentStopText.text = route.WaitingAtStop
                ? $"At Stop: {currentStop}"
                : "At Stop: Between stops";
        }

        if (nextStopText != null)
            nextStopText.text = $"Next Stop: {currentStop}";

        if (distanceText != null)
        {
            distanceText.text = route.WaitingAtStop
                ? "Distance: Stopped"
                : $"Distance: {route.DistanceToNextStop:0.0}m";
        }
    }
}
