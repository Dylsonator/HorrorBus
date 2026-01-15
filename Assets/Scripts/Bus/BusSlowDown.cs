using UnityEngine;

public sealed class BusSlowDown : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CabinPeek cabinLook;
    [SerializeField] private BusDrive follower;

    [Header("Speed")]
    [SerializeField] private float baseSpeed = 6f;

    [Header("Look Back")]
    [SerializeField] private float lookBackYawThreshold = 70f;
    [Range(0.05f, 1f)]
    [SerializeField] private float lookBackSpeedMultiplier = 0.35f;

    public bool IsLookingBack { get; private set; }

    private void Reset()
    {
        if (cabinLook == null)
            cabinLook = GetComponentInChildren<CabinPeek>();

        if (follower == null)
            follower = GetComponent<BusDrive>();
    }

    private void Update()
    {
        if (cabinLook == null || follower == null)
            return;

        IsLookingBack = cabinLook.IsLookingBack(lookBackYawThreshold);

        float speed = baseSpeed * (IsLookingBack ? lookBackSpeedMultiplier : 1f);
        follower.SetSpeed(speed);
    }
}
