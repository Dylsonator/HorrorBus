using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ClickInteractor : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private float maxDistance = 3.0f;
    [SerializeField] private LayerMask mask = ~0;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        if (cam == null) return;

        // Left click
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Ray r = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(r, out RaycastHit hit, maxDistance, mask, QueryTriggerInteraction.Ignore))
            return;

        // Press purge button if clicked
        if (hit.collider.TryGetComponent(out PurgeButton purgeButton))
        {
            purgeButton.Press();
        }

        // Decision buttons 
        if (hit.collider.TryGetComponent(out DecisionButton decision))
        {
            decision.Press();
            return;
        }
    }
}

