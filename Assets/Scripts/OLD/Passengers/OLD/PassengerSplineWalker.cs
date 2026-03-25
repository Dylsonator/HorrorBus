//using UnityEngine;
//using UnityEngine.Splines;

//public sealed class PassengerSplineWalker : MonoBehaviour
//{
//    [SerializeField] private float moveSpeed = 1.4f;      // meters/sec
//    [SerializeField] private float rotateSpeed = 10f;
//    [SerializeField] private float arriveEpsilon = 0.03f; // meters
//    [SerializeField] private float retargetEpsilon = 0.005f; // when a “new target” is considered different

//    private SplineContainer path;
//    private float pathLength = -1f;

//    private float currentDist;
//    private float targetDist;
//    private bool hasTarget;

//    public bool Arrived { get; private set; }

//    // Useful for debugging / UI if needed
//    public float CurrentDistance => currentDist;
//    public float TargetDistance => targetDist;

//    public void Init(SplineContainer spline, float startDistanceMeters)
//    {
//        path = spline;
//        pathLength = (path != null) ? path.CalculateLength() : -1f;

//        currentDist = Mathf.Max(0f, startDistanceMeters);
//        hasTarget = false;
//        Arrived = false;

//        SnapToCurrent();
//    }

//    public void SetTargetDistance(float distanceMeters)
//    {
//        float newTarget = Mathf.Max(0f, distanceMeters);

//        // If the “new” target is basically the same, don’t nuke Arrived.
//        if (hasTarget && Mathf.Abs(newTarget - targetDist) <= retargetEpsilon)
//            return;

//        targetDist = newTarget;
//        hasTarget = true;

//        // Only mark not-arrived if we’re not already basically there.
//        Arrived = Mathf.Abs(targetDist - currentDist) <= arriveEpsilon;

//        if (Arrived)
//        {
//            currentDist = targetDist;
//            SnapToCurrent();
//        }
//    }

//    public void StopMoving()
//    {
//        hasTarget = false;
//        Arrived = true;
//        enabled = false;
//    }

//    private void Update()
//    {
//        if (!hasTarget || path == null) return;

//        if (pathLength <= 0f) pathLength = path.CalculateLength();
//        if (pathLength <= 0f) return;

//        float delta = targetDist - currentDist;

//        if (Mathf.Abs(delta) <= arriveEpsilon)
//        {
//            currentDist = targetDist;
//            Arrived = true;
//            SnapToCurrent();
//            return;
//        }

//        float step = moveSpeed * Time.deltaTime;
//        currentDist += Mathf.Sign(delta) * Mathf.Min(Mathf.Abs(delta), step);

//        SnapToCurrent();
//    }

//    private void SnapToCurrent()
//    {
//        if (path == null) return;
//        if (pathLength <= 0f) pathLength = path.CalculateLength();
//        if (pathLength <= 0f) return;

//        float t = Mathf.Clamp01(currentDist / pathLength);

//        Vector3 pos = path.EvaluatePosition(t);
//        Vector3 tan = path.EvaluateTangent(t);

//        transform.position = pos;

//        tan.y = 0f;
//        if (tan.sqrMagnitude > 0.0001f)
//        {
//            Quaternion desired = Quaternion.LookRotation(tan.normalized, Vector3.up);
//            transform.rotation = Quaternion.Slerp(transform.rotation, desired, rotateSpeed * Time.deltaTime);
//        }
//    }
//}
