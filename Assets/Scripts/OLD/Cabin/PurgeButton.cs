using UnityEngine;

public sealed class PurgeButton : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PurgeManager purgeManager;

    [Header("Feedback")]
    [SerializeField] private Transform buttonVisual;   // optional: the part that moves
    [SerializeField] private float pressDepth = 0.015f;
    [SerializeField] private float pressReturnSpeed = 12f;

    private Vector3 visualLocalStart;
    private float press01;

    private void Awake()
    {
        if (buttonVisual == null) buttonVisual = transform;
        visualLocalStart = buttonVisual.localPosition;

        if (purgeManager == null)
            purgeManager = FindFirstObjectByType<PurgeManager>();
    }

    public void Press()
    {
        if (purgeManager == null) return;

        // Trigger purge
        purgeManager.TryPurge();

        // Simple visual press
        press01 = 1f;
    }

    private void Update()
    {
        if (buttonVisual == null) return;

        press01 = Mathf.MoveTowards(press01, 0f, Time.deltaTime * pressReturnSpeed);

        Vector3 target = visualLocalStart + Vector3.down * (pressDepth * press01);
        buttonVisual.localPosition = target;
    }
}
