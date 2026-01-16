using UnityEngine;
using UnityEngine.Splines;

public sealed class OrganicSplineWalker : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float maxSpeed = 1.5f;          // m/s
    [SerializeField] private float acceleration = 6f;        // m/s^2
    [SerializeField] private float deceleration = 10f;       // m/s^2
    [SerializeField] private float rotateSpeed = 10f;

    [Header("Arrival")]
    [SerializeField] private float arriveEpsilon = 0.04f;    // meters
    [SerializeField] private float retargetEpsilon = 0.01f;  // meters (ignore tiny target changes)

    [Header("Human feel")]
    [SerializeField] private Vector2 reactionDelayRange = new Vector2(0.05f, 0.35f);
    [SerializeField] private Vector2 lateralOffsetRange = new Vector2(-0.18f, 0.18f);

    [Header("Idle sway")]
    [SerializeField] private float idleSwayDegrees = 2f;
    [SerializeField] private float idleSwaySpeed = 1.25f;

    private SplineContainer path;
    private float pathLength = -1f;

    private float currentDist;
    private float targetDist;
    private float currentSpeed;
    private float reactionTimer;

    private float lateralOffset;
    private float idleSeed;

    private bool hasTarget;

    public bool Arrived { get; private set; }
    public float CurrentDistance => currentDist;
    public float TargetDistance => targetDist;

    public void Init(SplineContainer spline, float startDistanceMeters)
    {
        path = spline;
        pathLength = (path != null) ? path.CalculateLength() : -1f;

        currentDist = Mathf.Max(0f, startDistanceMeters);
        targetDist = currentDist;
        currentSpeed = 0f;
        reactionTimer = 0f;

        hasTarget = true;
        Arrived = true;

        lateralOffset = Random.Range(lateralOffsetRange.x, lateralOffsetRange.y);
        idleSeed = Random.Range(0f, 1000f);

        SnapToDistance(currentDist, applyIdle: false);
    }

    public void SetTargetDistance(float distanceMeters)
    {
        float newTarget = Mathf.Max(0f, distanceMeters);

        // Ignore tiny target wobbles (prevents jitter)
        if (hasTarget && Mathf.Abs(newTarget - targetDist) <= retargetEpsilon)
            return;

        targetDist = newTarget;
        hasTarget = true;

        // Set a small reaction delay if we need to move
        float remaining = Mathf.Abs(targetDist - currentDist);
        Arrived = remaining <= arriveEpsilon;

        if (!Arrived)
            reactionTimer = Random.Range(reactionDelayRange.x, reactionDelayRange.y);
    }

    public void StopMoving()
    {
        hasTarget = false;
        currentSpeed = 0f;
        Arrived = true;
        enabled = false;
    }

    private void Update()
    {
        if (!hasTarget || path == null) return;

        if (pathLength <= 0f) pathLength = path.CalculateLength();
        if (pathLength <= 0f) return;

        float delta = targetDist - currentDist;
        float remaining = Mathf.Abs(delta);

        // Idle while effectively arrived
        if (remaining <= arriveEpsilon)
        {
            currentDist = targetDist;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
            Arrived = true;
            SnapToDistance(currentDist, applyIdle: true);
            return;
        }

        Arrived = false;

        // Human reaction delay before moving
        if (reactionTimer > 0f)
        {
            reactionTimer -= Time.deltaTime;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
            SnapToDistance(currentDist, applyIdle: false);
            return;
        }

        // Smooth accelerate/decelerate
        float desiredSpeed = maxSpeed;

        // Slow down when close to target
        if (remaining < 0.4f)
            desiredSpeed = Mathf.Lerp(0.2f, maxSpeed, Mathf.Clamp01(remaining / 0.4f));

        // Accelerate towards desired speed
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, acceleration * Time.deltaTime);

        // Move
        float step = currentSpeed * Time.deltaTime;
        float move = Mathf.Min(remaining, step);
        currentDist += Mathf.Sign(delta) * move;

        SnapToDistance(currentDist, applyIdle: false);
    }

    private void SnapToDistance(float distMeters, bool applyIdle)
    {
        float t = Mathf.Clamp01(distMeters / pathLength);

        Vector3 pos = path.EvaluatePosition(t);
        Vector3 tan = path.EvaluateTangent(t);
        tan.y = 0f;

        // Lateral offset (queue looks less perfect)
        if (tan.sqrMagnitude > 0.0001f)
        {
            Vector3 right = Vector3.Cross(Vector3.up, tan.normalized);
            pos += right * lateralOffset;
        }

        transform.position = pos;

        // Face tangent direction
        if (tan.sqrMagnitude > 0.0001f)
        {
            Quaternion desiredRot = Quaternion.LookRotation(tan.normalized, Vector3.up);

            // Idle sway while waiting
            if (applyIdle && idleSwayDegrees > 0f)
            {
                float sway = Mathf.Sin((Time.time + idleSeed) * idleSwaySpeed) * idleSwayDegrees;
                desiredRot = desiredRot * Quaternion.Euler(0f, sway, 0f);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotateSpeed * Time.deltaTime);
        }
    }
}
