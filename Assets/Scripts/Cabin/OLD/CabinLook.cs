using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person look for the bus cabin using the NEW Input System.
/// Attach to a pivot transform (CameraPivot) with the Camera as a child.
/// </summary>
public class CabinLook : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private float sensitivityX = 150f;
    [SerializeField] private float sensitivityY = 120f;

    [Header("Clamp Angles")]
    [SerializeField] private float minYaw = -120f;
    [SerializeField] private float maxYaw = 120f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 35f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursor = true;

    private float yaw;
    private float pitch;

    public float Yaw => yaw;
    public float Pitch => pitch;

    private void Start()
    {
        Vector3 euler = transform.localEulerAngles;
        yaw = NormalizeAngle(euler.y);
        pitch = NormalizeAngle(euler.x);

        if (lockCursor)
            LockCursor(true);
    }

    private void Update()
    {
        // NEW Input System mouse delta (pixels since last frame)
        Vector2 mouseDelta = Vector2.zero;
        if (Mouse.current != null)
            mouseDelta = Mouse.current.delta.ReadValue();

        yaw += mouseDelta.x * sensitivityX * Time.deltaTime;
        pitch -= mouseDelta.y * sensitivityY * Time.deltaTime;

        yaw = Mathf.Clamp(yaw, minYaw, maxYaw);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            LockCursor(false);
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
