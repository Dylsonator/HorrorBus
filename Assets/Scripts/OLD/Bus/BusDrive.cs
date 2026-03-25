using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Moves this object along a SplineContainer at a constant speed (units/sec).
/// </summary>
public class BusDrive : MonoBehaviour
{
    [Header("Spline")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private int splineIndex = 0;

    [Header("Movement")]
    [Tooltip("Units per second along the spline.")]
    [SerializeField] private float speed = 6f;

    [Tooltip("If true, wraps back to the start when reaching the end.")]
    [SerializeField] private bool loop = true;

    [Header("Orientation")]
    [SerializeField] private bool rotateAlongSpline = true;
    [SerializeField] private Vector3 up = Vector3.up;

    [Range(0f, 1f)]
    [SerializeField] private float t;
    private float splineLength;
    public float NormalizedT => t;
    public float SplineLength => splineLength;
    public bool IsPaused { get; private set; }


    

    // --- Exposed value for debug UI
    public float CurrentSpeed => speed;

    private void Awake()
    {
        RecalculateLength();
    }

    private void OnValidate()
    {
        if (splineContainer != null)
            RecalculateLength();
    }

    private void Update()
    {
        if (IsPaused)
            return;

        if (splineContainer == null)
            return;

        if (splineLength <= 0.0001f)
            RecalculateLength();

        float dtNormalized = (speed / splineLength) * Time.deltaTime;
        t += dtNormalized;

        if (loop) t = Mathf.Repeat(t, 1f);
        else t = Mathf.Clamp01(t);

        Vector3 pos = splineContainer.EvaluatePosition(splineIndex, t);
        transform.position = pos;

        if (rotateAlongSpline)
        {
            Vector3 tangent = splineContainer.EvaluateTangent(splineIndex, t);
            if (tangent.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(tangent.normalized, up);
        }
    }

    public void SetSpeed(float newSpeed)
    {
        speed = Mathf.Max(0f, newSpeed);
    }

    public void RecalculateLength()
    {
        if (splineContainer == null) return;
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

}
