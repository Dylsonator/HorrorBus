using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class RouteStops : MonoBehaviour
{
    public static RouteStops Instance { get; private set; }

    public event Action ArrivedAtStop;
    public event Action LeavingStop;

    [Header("References")]
    [SerializeField] private BusDrive busDrive;
    [SerializeField] private BusSlowDown slowDown; // optional
    [SerializeField] private PassengerSpawner passengerSpawner;
    private bool stopTriggerArmed = true;

    [SerializeField] private StopGate decisionGate;

    [Header("Stops (0..1 along spline)")]
    [SerializeField] private float[] stopTs = { 0.10f, 0.35f, 0.60f, 0.85f };
    [SerializeField] private string[] stopNames = { "Depot", "Market Street", "Riverside", "Hilltop" };
    [SerializeField] private float stopTolerance = 0.01f;

    [Header("Resume key")]
    [SerializeField] private Key resumeKey = Key.Space;

    private int nextStopIndex = 0;
    private bool waitingAtStop;

    public bool WaitingAtStop => waitingAtStop;
    public int NextStopIndex => nextStopIndex;
    public int StopCount => stopTs != null ? stopTs.Length : 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Reset()
    {
        if (busDrive == null) busDrive = GetComponent<BusDrive>();
        if (slowDown == null) slowDown = GetComponent<BusSlowDown>();
        if (passengerSpawner == null) passengerSpawner = GetComponentInChildren<PassengerSpawner>();
        if (decisionGate == null) decisionGate = GetComponentInChildren<StopGate>();
    }

    private void Update()
    {
        if (busDrive == null || stopTs == null || stopTs.Length == 0)
            return;

        if (Keyboard.current != null && Keyboard.current[resumeKey].wasPressedThisFrame)
        {
            if (decisionGate != null && !decisionGate.CanDepart)
            {
                Debug.Log($"Cannot leave stop: {decisionGate.PendingCount} passenger(s) still need a decision.");
                return;
            }

            ResumeFromStop();
        }

        float t = busDrive.NormalizedT;
        float targetStopT = stopTs[nextStopIndex];

        bool inZone = IsWithinToleranceLooped(t, targetStopT, stopTolerance);

        if (inZone)
        {
            if (stopTriggerArmed)
            {
                stopTriggerArmed = false;
                ArriveAtStopInternal();
            }
        }
        else
        {
            stopTriggerArmed = true;
        }
    }

    public float DistanceToNextStop
    {
        get
        {
            if (busDrive == null || stopTs == null || stopTs.Length == 0)
                return 0f;

            float t = busDrive.NormalizedT;
            float target = stopTs[nextStopIndex];
            float deltaT = (target - t + 1f) % 1f;

            return deltaT * busDrive.SplineLength;
        }
    }

    public string GetStopNameSafe(int stopIndex)
    {
        if (stopTs == null || stopTs.Length == 0)
            return "Unknown Stop";

        int count = stopTs.Length;
        if (count <= 0)
            return "Unknown Stop";

        stopIndex = ((stopIndex % count) + count) % count;

        if (stopNames != null && stopIndex >= 0 && stopIndex < stopNames.Length && !string.IsNullOrWhiteSpace(stopNames[stopIndex]))
            return stopNames[stopIndex];

        return $"Stop {stopIndex + 1}";
    }

    public string GetDestinationName(int dropOffStopIndex)
    {
        if (dropOffStopIndex < 0)
            return "Unknown Stop";

        return GetStopNameSafe(dropOffStopIndex);
    }

    private void ArriveAtStopInternal()
    {
        waitingAtStop = true;

        busDrive.SetSpeed(0f);

        if (slowDown != null) slowDown.enabled = false;

        int arrivedIndex = nextStopIndex;
        int stopCount = stopTs.Length;

        if (passengerSpawner != null)
        {
            if (decisionGate != null)
                decisionGate.ResetGate();

            passengerSpawner.DismissPassengersForStop(arrivedIndex);
            passengerSpawner.SpawnPassengers(arrivedIndex, stopCount);
        }

        ArrivedAtStop?.Invoke();
        Debug.Log($"Arrived at {GetStopNameSafe(arrivedIndex)}. Press {resumeKey} to continue.");
    }

    private void ResumeFromStop()
    {
        waitingAtStop = false;

        nextStopIndex++;
        if (nextStopIndex >= stopTs.Length)
            nextStopIndex = 0;

        if (slowDown != null) slowDown.enabled = true;

        LeavingStop?.Invoke();
        Debug.Log("Leaving stop.");
    }

    private static bool IsWithinToleranceLooped(float a, float b, float tol)
    {
        float diff = Mathf.Abs(a - b);
        diff = Mathf.Min(diff, 1f - diff);
        return diff <= tol;
    }
}