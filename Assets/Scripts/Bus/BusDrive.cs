using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Moves this object along a SplineContainer with smooth acceleration/deceleration
/// and optional slope-based speed changes for ramps.
/// </summary>
public class BusDrive : MonoBehaviour
{
    [Header("Spline")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private int splineIndex = 0;

    [Header("Speed")]
    [Tooltip("Top cruise speed the bus tries to reach in normal conditions.")]
    [SerializeField] private float maxCruiseSpeed = 6f;

    [Tooltip("How quickly the bus speeds up toward its target speed.")]
    [SerializeField] private float acceleration = 1.8f;

    [Tooltip("How quickly the bus slows down toward its target speed.")]
    [SerializeField] private float deceleration = 2.8f;

    [Tooltip("Minimum movement speed while still considered moving.")]
    [SerializeField] private float minMovingSpeed = 0.05f;

    [Header("Ramps / Slope Response")]
    [SerializeField] private bool useSlopeSpeedModifier = true;

    [Tooltip("How strongly uphill/downhill affects speed. Higher = more effect.")]
    [SerializeField] private float slopeStrength = 1.25f;

    [Tooltip("Smallest multiplier allowed after uphill slowdown.")]
    [SerializeField] private float minSlopeMultiplier = 0.55f;

    [Tooltip("Largest multiplier allowed after downhill boost.")]
    [SerializeField] private float maxSlopeMultiplier = 1.15f;

    [Header("Looping")]
    [SerializeField] private bool loop = true;

    [Header("Orientation")]
    [SerializeField] private bool rotateAlongSpline = true;
    [SerializeField] private Vector3 up = Vector3.up;

    [Range(0f, 1f)]
    [SerializeField] private float t;

    private float splineLength = 1f;
    private float targetSpeed;
    private float currentSpeed;
    private float lastSlopeMultiplier = 1f;

    public float NormalizedT => t;
    public float SplineLength => splineLength;
    public bool IsPaused { get; private set; }

    /// <summary>
    /// Actual speed currently being applied this frame.
    /// </summary>
    public float CurrentSpeed => currentSpeed;

    /// <summary>
    /// Requested base speed before slope effects.
    /// </summary>
    public float TargetSpeed => targetSpeed;

    private void Awake()
    {
        targetSpeed = Mathf.Max(0f, maxCruiseSpeed);
        currentSpeed = targetSpeed;
        RecalculateLength();
    }

    private void OnValidate()
    {
        maxCruiseSpeed = Mathf.Max(0f, maxCruiseSpeed);
        acceleration = Mathf.Max(0f, acceleration);
        deceleration = Mathf.Max(0f, deceleration);
        minMovingSpeed = Mathf.Max(0f, minMovingSpeed);
        slopeStrength = Mathf.Max(0f, slopeStrength);
        minSlopeMultiplier = Mathf.Clamp(minSlopeMultiplier, 0.05f, 2f);
        maxSlopeMultiplier = Mathf.Clamp(maxSlopeMultiplier, minSlopeMultiplier, 3f);

        RecalculateLength();

        if (!Application.isPlaying)
        {
            targetSpeed = Mathf.Max(0f, maxCruiseSpeed);
            currentSpeed = targetSpeed;
        }
    }

    private void Update()
    {
        if (IsPaused)
            return;

        if (!HasValidSpline())
            return;

        if (splineLength <= 0.0001f)
            RecalculateLength();

        float desiredBaseSpeed = Mathf.Clamp(targetSpeed, 0f, maxCruiseSpeed);
        float desiredFinalSpeed = desiredBaseSpeed;

        Vector3 tangent = splineContainer.EvaluateTangent(splineIndex, t);
        if (useSlopeSpeedModifier && tangent.sqrMagnitude > 0.0001f)
        {
            lastSlopeMultiplier = ComputeSlopeMultiplier(tangent.normalized);
            desiredFinalSpeed *= lastSlopeMultiplier;
        }
        else
        {
            lastSlopeMultiplier = 1f;
        }

        float rate = desiredFinalSpeed > currentSpeed ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredFinalSpeed, rate * Time.deltaTime);

        if (currentSpeed < minMovingSpeed)
            currentSpeed = desiredFinalSpeed <= 0f ? 0f : minMovingSpeed;

        float dtNormalized = (currentSpeed / splineLength) * Time.deltaTime;
        t += dtNormalized;

        if (loop)
            t = Mathf.Repeat(t, 1f);
        else
            t = Mathf.Clamp01(t);

        Vector3 pos = splineContainer.EvaluatePosition(splineIndex, t);
        transform.position = pos;

        if (rotateAlongSpline)
        {
            Vector3 lookTangent = splineContainer.EvaluateTangent(splineIndex, t);
            if (lookTangent.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(lookTangent.normalized, up);
        }
    }

    /// <summary>
    /// Sets the bus' requested speed. The bus will ease toward this speed rather than snapping.
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        targetSpeed = Mathf.Max(0f, newSpeed);
    }

    /// <summary>
    /// Sets a new cruise cap and also clamps the current target to it.
    /// </summary>
    public void SetMaxCruiseSpeed(float newMaxCruiseSpeed)
    {
        maxCruiseSpeed = Mathf.Max(0f, newMaxCruiseSpeed);
        targetSpeed = Mathf.Min(targetSpeed, maxCruiseSpeed);
    }

    public void RecalculateLength()
    {
        if (splineContainer == null)
        {
            splineLength = 1f;
            return;
        }

        int count = splineContainer.Splines != null ? splineContainer.Splines.Count : 0;
        if (count <= 0)
        {
            splineLength = 1f;
            splineIndex = 0;
            return;
        }

        splineIndex = Mathf.Clamp(splineIndex, 0, count - 1);

        var spline = splineContainer.Splines[splineIndex];
        splineLength = Mathf.Max(0.0001f, spline.GetLength());
    }

    public void Pause()
    {
        IsPaused = true;
    }

    public void Resume()
    {
        IsPaused = false;
    }

    private bool HasValidSpline()
    {
        if (splineContainer == null)
            return false;

        int count = splineContainer.Splines != null ? splineContainer.Splines.Count : 0;
        if (count <= 0)
            return false;

        if (splineIndex < 0 || splineIndex >= count)
            return false;

        return true;
    }

    private float ComputeSlopeMultiplier(Vector3 tangentNormalized)
    {
        // Positive Y = uphill, negative Y = downhill
        float slopeY = tangentNormalized.y;

        // Uphill should slow the bus, downhill can help it a bit.
        float rawMultiplier = 1f - (slopeY * slopeStrength);

        return Mathf.Clamp(rawMultiplier, minSlopeMultiplier, maxSlopeMultiplier);
    }
}