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
    [SerializeField] private QueueManagerNodes queueManager;
    [SerializeField] private PassengerSpawner passengerSpawner;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Passenger lookTarget;
    private Passenger selectedPassenger;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (inspectUI == null) inspectUI = FindFirstObjectByType<PassengerInspectUI>();
        if (queueManager == null) queueManager = FindFirstObjectByType<QueueManagerNodes>();
        if (passengerSpawner == null) passengerSpawner = FindFirstObjectByType<PassengerSpawner>();
    }

    private void Update()
    {
        UpdateLookTarget();

        var kb = Keyboard.current;
        if (kb == null) return;

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
                if (debugLogs) Debug.Log("[Inspection] E -> no valid target (queue empty).");
                return;
            }

            selectedPassenger = target;
            inspectUI.Show(selectedPassenger);
            if (debugLogs) Debug.Log($"[Inspection] UI OPEN for {selectedPassenger.PassengerName}");
        }

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
            if (debugLogs) Debug.Log(ok ? $"[Inspection] SEAT OK: {target.PassengerName}" : $"[Inspection] SEAT FAIL: {target.PassengerName}");

            if (ok) CleanupAfterDecision(target);
        }

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
            if (debugLogs) Debug.Log(ok ? $"[Inspection] DISMISS OK: {target.PassengerName}" : $"[Inspection] DISMISS FAIL: {target.PassengerName}");

            if (ok) CleanupAfterDecision(target);
        }
    }

    private void CleanupAfterDecision(Passenger decided)
    {
        if (inspectUI != null && inspectUI.IsVisible) inspectUI.Hide();
        selectedPassenger = null;

        if (queueManager != null)
            queueManager.Remove(decided);

        var walker = decided != null ? decided.GetComponent<NodeQueueWalker>() : null;
        if (walker != null)
            walker.StopMoving();
    }

    private Passenger GetBestTargetForDecision()
    {
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

        if (lookTarget != null && lookTarget == front)
            return lookTarget;

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
