using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class RouteStops : MonoBehaviour
{
    public event Action ArrivedAtStop;
    public event Action LeavingStop;

    [Header("References")]
    [SerializeField] private BusDrive busDrive;
    [SerializeField] private BusSlowDown slowDown; // optional
    [SerializeField] private PassengerSpawner passengerSpawner;

    [Header("Stops (0..1 along spline)")]
    [SerializeField] private float[] stopTs = { 0.10f, 0.35f, 0.60f, 0.85f };
    [SerializeField] private float stopTolerance = 0.01f;

    [Header("Resume key")]
    [SerializeField] private Key resumeKey = Key.Space;

    private int nextStopIndex = 0;
    private bool waitingAtStop;

    public bool WaitingAtStop => waitingAtStop;
    public int NextStopIndex => nextStopIndex;

    private void Reset()
    {
        if (busDrive == null) busDrive = GetComponent<BusDrive>();
        if (slowDown == null) slowDown = GetComponent<BusSlowDown>();
        if (passengerSpawner == null) passengerSpawner = GetComponentInChildren<PassengerSpawner>();
    }

    private void Update()
    {
        if (busDrive == null || stopTs == null || stopTs.Length == 0)
            return;

        if (waitingAtStop)
        {
            if (Keyboard.current != null && Keyboard.current[resumeKey].wasPressedThisFrame)
                ResumeFromStop();
            return;
        }

        float t = busDrive.NormalizedT;
        float targetStopT = stopTs[nextStopIndex];

        if (IsWithinToleranceLooped(t, targetStopT, stopTolerance))
            ArriveAtStopInternal();
    }

    public float DistanceToNextStop
    {
        get
        {
            if (busDrive == null || stopTs == null || stopTs.Length == 0)
                return 0f;

            float t = busDrive.NormalizedT;
            float target = stopTs[nextStopIndex];

            // Loop-safe distance (0..1 range)
            float deltaT = (target - t + 1f) % 1f;

            // Convert normalized distance to world distance
            return deltaT * busDrive.SplineLength;
        }
    }


    private void ArriveAtStopInternal()
    {
        waitingAtStop = true;

        // Freeze bus
        busDrive.SetSpeed(0f);

        // Disable slowdown while stopped so it doesn't fight speed
        if (slowDown != null) slowDown.enabled = false;

        int arrivedIndex = nextStopIndex;
        int stopCount = stopTs.Length;

        // Passenger flow: people get off, then new people get on
        if (passengerSpawner != null)
        {
            passengerSpawner.DismissPassengersForStop(arrivedIndex);
            passengerSpawner.SpawnPassengers(arrivedIndex, stopCount);
        }

        ArrivedAtStop?.Invoke();
        Debug.Log($"Arrived at stop #{arrivedIndex}. Press {resumeKey} to continue.");
    }

    private void ResumeFromStop()
    {
        waitingAtStop = false;

        // Advance to next stop
        nextStopIndex++;
        if (nextStopIndex >= stopTs.Length)
            nextStopIndex = 0;

        // Re-enable slowdown
        if (slowDown != null) slowDown.enabled = true;

        LeavingStop?.Invoke();
        Debug.Log("Leaving stop.");
    }

    private static bool IsWithinToleranceLooped(float a, float b, float tol)
    {
        float diff = Mathf.Abs(a - b);
        diff = Mathf.Min(diff, 1f - diff); // loop distance
        return diff <= tol;
    }
}
