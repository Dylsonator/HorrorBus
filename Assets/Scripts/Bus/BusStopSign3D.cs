using TMPro;
using UnityEngine;

public sealed class BusStopSign3D : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text text3D;
    [SerializeField] private RouteStops routeStops;

    [Header("Text")]
    [SerializeField] private string prefix = "STOP: ";
    [SerializeField] private string betweenStopsText = "EN ROUTE";
    [SerializeField] private bool showCurrentStopOnlyWhenWaiting = true;

    [Header("Optional")]
    [SerializeField] private bool useNextStopWhenMoving = true;

    private string lastShown = string.Empty;

    private void Awake()
    {
        if (text3D == null)
            text3D = GetComponent<TMP_Text>();

        if (routeStops == null)
            routeStops = FindFirstObjectByType<RouteStops>();

        RefreshNow();
    }

    private void Update()
    {
        RefreshNow();
    }

    public void RefreshNow()
    {
        if (text3D == null || routeStops == null)
            return;

        string newText;

        if (routeStops.WaitingAtStop)
        {
            newText = prefix + routeStops.GetStopNameSafe(routeStops.CurrentStopIndex);
        }
        else
        {
            if (showCurrentStopOnlyWhenWaiting)
            {
                if (useNextStopWhenMoving)
                    newText = prefix + routeStops.GetStopNameSafe(routeStops.NextStopIndex);
                else
                    newText = betweenStopsText;
            }
            else
            {
                newText = prefix + routeStops.CurrentStopName;
            }
        }

        if (newText == lastShown)
            return;

        lastShown = newText;
        text3D.text = newText;
    }
}