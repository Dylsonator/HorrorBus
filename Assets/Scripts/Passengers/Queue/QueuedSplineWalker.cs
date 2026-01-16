using UnityEngine;
using UnityEngine.Splines;

public sealed class QueuedSplineWalker : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 1.4f;
    [SerializeField] private float accel = 6f;
    [SerializeField] private float decel = 10f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float arriveEpsilon = 0.03f;

    private SplineContainer path;
    private float pathLength = -1f;

    private float currentDist;
    private float targetDist;
    private float speed;
    private bool hasTarget;

    public bool Arrived { get; private set; }
    public float CurrentDistance => currentDist;

    public void Init(SplineContainer spline, float startDistanceMeters)
    {
        path = spline;
        pathLength = (path != null) ? path.CalculateLength() : -1f;

        currentDist = Mathf.Max(0f, startDistanceMeters);
        targetDist = currentDist;
        speed = 0f;

        hasTarget = true;
        Arrived = true;

        SnapToCurrent();
    }

    public void SetTargetDistance(float distanceMeters)
    {
        targetDist = Mathf.Max(0f, distanceMeters);
        hasTarget = true;
        Arrived = false;
    }

    public void StopMoving()
    {
        hasTarget = false;
        Arrived = true;
        speed = 0f;
        enabled = false;
    }

    private void Update()
    {
        if (!hasTarget || path == null) return;

        if (pathLength <= 0f) pathLength = path.CalculateLength();
        if (pathLength <= 0f) return;

        float delta = targetDist - currentDist;
        float abs = Mathf.Abs(delta);

        if (abs <= arriveEpsilon)
        {
            currentDist = targetDist;
            speed = Mathf.MoveTowards(speed, 0f, decel * Time.deltaTime);
            Arrived = true;
            SnapToCurrent();
            return;
        }

        Arrived = false;

        // accelerate towards max speed, decelerate a bit when close
        float desiredSpeed = maxSpeed;
        if (abs < 0.35f)
            desiredSpeed = Mathf.Lerp(0.2f, maxSpeed, Mathf.Clamp01(abs / 0.35f));

        speed = Mathf.MoveTowards(speed, desiredSpeed, accel * Time.deltaTime);

        float step = speed * Time.deltaTime;
        currentDist += Mathf.Sign(delta) * Mathf.Min(abs, step);

        SnapToCurrent();
    }

    private void SnapToCurrent()
    {
        if (path == null) return;
        if (pathLength <= 0f) pathLength = path.CalculateLength();
        if (pathLength <= 0f) return;

        float t = Mathf.Clamp01(currentDist / pathLength);

        Vector3 pos = path.EvaluatePosition(t);
        Vector3 tan = path.EvaluateTangent(t);

        transform.position = pos;

        tan.y = 0f;
        if (tan.sqrMagnitude > 0.0001f)
        {
            Quaternion desired = Quaternion.LookRotation(tan.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, rotateSpeed * Time.deltaTime);
        }
    }
}
