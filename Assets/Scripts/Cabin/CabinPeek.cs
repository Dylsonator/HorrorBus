using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CabinPeek : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private float sensitivityX = 150f;
    [SerializeField] private float sensitivityY = 120f;

    [Header("Clamp Angles")]
    [SerializeField] private float minYaw = -140f;
    [SerializeField] private float maxYaw = 120f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 35f;

    [Header("Lean-Out Settings")]
    [SerializeField] private float leanStartYaw = -95f;
    [SerializeField] private float leanMaxYaw = -130f;
    [SerializeField] private Vector3 leanOffset = new Vector3(-0.35f, 0f, 0.25f);
    [SerializeField] private float leanSmooth = 6f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnStart = true;

    private float yaw;
    private float pitch;
    private Vector3 defaultLocalPos;
    private bool lookEnabled = true;

    public float Yaw => yaw;
    public float Pitch => pitch;
    public bool LookEnabled => lookEnabled;

    public bool IsLookingBack(float thresholdYawAbs) => Mathf.Abs(yaw) >= thresholdYawAbs;

    private void Start()
    {
        defaultLocalPos = transform.localPosition;

        Vector3 euler = transform.localEulerAngles;
        yaw = NormalizeAngle(euler.y);
        pitch = NormalizeAngle(euler.x);

        if (lockCursorOnStart)
            SetCursorLocked(true);
    }

    private void Update()
    {
        if (!lookEnabled)
            return;

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
            SetCursorLocked(false);
    }

    public void SetLookEnabled(bool enabled)
    {
        lookEnabled = enabled;
    }

    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void UpdateLean()
    {
        float leanT = 0f;

        if (yaw <= leanStartYaw)
            leanT = Mathf.InverseLerp(leanStartYaw, leanMaxYaw, yaw);

        Vector3 targetPos = defaultLocalPos + leanOffset * leanT;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, leanSmooth * Time.deltaTime);
    }

    private static float NormalizeAngle(float degrees)
    {
        if (degrees > 180f) degrees -= 360f;
        return degrees;
    }
}