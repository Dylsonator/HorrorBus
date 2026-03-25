using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public sealed class ClickInteractor : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private float maxDistance = 3.0f;
    [SerializeField] private LayerMask mask = ~0;
    [SerializeField] private PassengerInspection inspection;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (inspection == null) inspection = FindFirstObjectByType<PassengerInspection>();
    }

    private void Update()
    {
        if (cam == null)
            return;

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;
        Ray r = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(r, out RaycastHit hit, maxDistance, mask, QueryTriggerInteraction.Ignore))
            return;

        if (hit.collider.TryGetComponent(out PurgeButton purgeButton))
        {
            purgeButton.Press();
            return;
        }

        if (hit.collider.TryGetComponent(out DecisionButton decision))
        {
            decision.Press();
            return;
        }

        if (hit.collider.TryGetComponent(out QuestionButton questionButton))
        {
            questionButton.Press();
            return;
        }

        if (hit.collider.TryGetComponent(out Passenger passenger))
        {
            inspection?.Inspect(passenger);
            return;
        }

        Passenger passengerInParent = hit.collider.GetComponentInParent<Passenger>();
        if (passengerInParent != null)
            inspection?.Inspect(passengerInParent);
    }
}