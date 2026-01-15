using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PassengerInspection : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera cam;
    [SerializeField] private float inspectRange = 3.0f;
    [SerializeField] private LayerMask passengerMask;

    [Header("Refs")]
    [SerializeField] private PassengerInspectUI inspectUI;
    [SerializeField] private PassengerQueueManagerSpline queueManager;
    [SerializeField] private PassengerSpawner passengerSpawner;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Passenger lookTarget;         // who you're looking at (optional)
    private Passenger selectedPassenger;  // who UI is showing (optional)

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (inspectUI == null) inspectUI = FindFirstObjectByType<PassengerInspectUI>();
        if (queueManager == null) queueManager = FindFirstObjectByType<PassengerQueueManagerSpline>();
        if (passengerSpawner == null) passengerSpawner = FindFirstObjectByType<PassengerSpawner>();
    }

    private void Update()
    {
        UpdateLookTarget();

        var kb = Keyboard.current;
        if (kb == null) return;

        // E toggles UI
        if (kb.eKey.wasPressedThisFrame)
        {
            if (inspectUI == null || queueManager == null) return;

            if (inspectUI.IsVisible)
            {
                inspectUI.Hide();
                if (debugLogs) Debug.Log("[Inspection] E -> UI CLOSED");
                return;
            }

            Passenger target = GetBestTargetForInspect();
            if (target == null)
            {
                if (debugLogs) Debug.Log("[Inspection] E -> no valid target (no front passenger).");
                return;
            }

            selectedPassenger = target;
            inspectUI.Show(selectedPassenger);
            if (debugLogs) Debug.Log($"[Inspection] UI OPEN for {selectedPassenger.PassengerName}");
        }

        // 1 = Seat (NO LOOK REQUIRED)
        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
        {
            Passenger target = GetBestTargetForDecision();
            if (target == null)
            {
                if (debugLogs) Debug.Log("[Inspection] 1 -> no target (queue empty).");
                return;
            }

            if (passengerSpawner == null) return;

            bool ok = passengerSpawner.SeatPassenger(target);
            Debug.Log(ok
                ? $"[Inspection] SEAT OK: {target.PassengerName}"
                : $"[Inspection] SEAT FAIL: {target.PassengerName} (see spawner logs)");

            if (ok)
            {
                if (inspectUI != null && inspectUI.IsVisible) inspectUI.Hide();
                selectedPassenger = null;
            }
        }

        // 2 = Dismiss (NO LOOK REQUIRED)
        if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
        {
            Passenger target = GetBestTargetForDecision();
            if (target == null)
            {
                if (debugLogs) Debug.Log("[Inspection] 2 -> no target (queue empty).");
                return;
            }

            if (passengerSpawner == null) return;

            bool ok = passengerSpawner.DismissPassenger(target);
            Debug.Log(ok
                ? $"[Inspection] DISMISS OK: {target.PassengerName}"
                : $"[Inspection] DISMISS FAIL: {target.PassengerName} (see spawner logs)");

            if (ok)
            {
                if (inspectUI != null && inspectUI.IsVisible) inspectUI.Hide();
                selectedPassenger = null;
            }
        }
    }

    private Passenger GetBestTargetForDecision()
    {
        // Prefer whoever is currently selected (if they’re still front),
        // otherwise use front passenger.
        if (queueManager == null) return null;

        Passenger front = queueManager.FrontPassenger;
        if (front == null) return null;

        if (selectedPassenger != null && selectedPassenger == front)
            return selectedPassenger;

        return front;
    }

    private Passenger GetBestTargetForInspect()
    {
        if (queueManager == null) return null;

        Passenger front = queueManager.FrontPassenger;
        if (front == null) return null;

        // If you’re looking at the front passenger, inspect them.
        if (lookTarget != null && lookTarget == front)
            return lookTarget;

        // Otherwise inspect front anyway.
        return front;
    }

    private void UpdateLookTarget()
    {
        lookTarget = null;
        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, inspectRange, passengerMask, QueryTriggerInteraction.Ignore))
            lookTarget = hit.collider.GetComponentInParent<Passenger>();
    }
}
