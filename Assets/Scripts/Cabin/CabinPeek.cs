using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person look system with clamped rotation and a subtle lean-out effect
/// when looking far left, simulating the player leaning out of the cabin.
/// </summary>
public sealed class CabinPeek : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private float sensitivityX = 150f;
    [SerializeField] private float sensitivityY = 120f;

    [Header("Clamp Angles")]
    [SerializeField] private float minYaw = -140f; // allow more left turn
    [SerializeField] private float maxYaw = 120f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 35f;

    [Header("Lean-Out Settings")]
    [Tooltip("Yaw (degrees) at which leaning starts.")]
    [SerializeField] private float leanStartYaw = -95f;

    [Tooltip("Maximum yaw where lean is fully applied.")]
    [SerializeField] private float leanMaxYaw = -130f;

    [Tooltip("Local position offset when fully leaning.")]
    [SerializeField] private Vector3 leanOffset = new Vector3(-0.35f, 0f, 0.25f);

    [Tooltip("How fast the camera moves when leaning.")]
    [SerializeField] private float leanSmooth = 6f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursor = true;

    private float yaw;
    private float pitch;

    private Vector3 defaultLocalPos;

    // --- Exposed values for other systems / debug UI
    public float Yaw => yaw;
    public float Pitch => pitch;

    /// <summary>Returns true if the current yaw is beyond the threshold (absolute degrees).</summary>
    public bool IsLookingBack(float thresholdYawAbs) => Mathf.Abs(yaw) >= thresholdYawAbs;

    private void Start()
    {
        defaultLocalPos = transform.localPosition;

        Vector3 euler = transform.localEulerAngles;
        yaw = NormalizeAngle(euler.y);
        pitch = NormalizeAngle(euler.x);

        if (lockCursor)
            LockCursor(true);
    }

    private void Update()
    {
        Vector2 mouseDelta = Vector2.zero;
        if (Mouse.current != null)
            mouseDelta = Mouse.current.delta.ReadValue();

        yaw += mouseDelta.x * sensitivityX * Time.deltaTime;
        pitch -= mouseDelta.y * sensitivityY * Time.deltaTime;

        yaw = Mathf.Clamp(yaw, minYaw, maxYaw);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);

        UpdateLean();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            LockCursor(false);
    }

    private void UpdateLean()
    {
        float leanT = 0f;

        if (yaw <= leanStartYaw)
            leanT = Mathf.InverseLerp(leanStartYaw, leanMaxYaw, yaw);

        Vector3 targetPos = defaultLocalPos + leanOffset * leanT;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, leanSmooth * Time.deltaTime);
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private static float NormalizeAngle(float degrees)
    {
        if (degrees > 180f) degrees -= 360f;
        return degrees;
    }
}
