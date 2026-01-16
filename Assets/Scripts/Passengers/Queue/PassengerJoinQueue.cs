using UnityEngine;

public sealed class PassengerJoinQueue : MonoBehaviour
{
    [Header("Walk to reserved slot")]
    [SerializeField] private float moveSpeed = 1.7f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float arriveDistance = 0.18f;

    [Header("Yield (stop and wait)")]
    [Tooltip("Radius of the forward probe used to detect a blocker.")]
    [SerializeField] private float probeRadius = 0.22f;

    [Tooltip("How far ahead we check for a blocker (start slowing inside this).")]
    [SerializeField] private float probeDistance = 0.9f;

    [Tooltip("Hard stop if another passenger is within this distance in front.")]
    [SerializeField] private float stopDistance = 0.45f;

    [Tooltip("Resume moving when the blocker is at least this far away.")]
    [SerializeField] private float resumeDistance = 0.60f;

    [Tooltip("Only collide with passenger colliders (recommended: set passengers to a Passengers layer).")]
    [SerializeField] private LayerMask passengerMask = ~0;

    private Passenger passenger;
    private QueueManagerSpline queue;
    private float targetDistOnSpline;

    private bool active;
    private bool releasedReservation;
    private bool waiting; // hysteresis

    private int reservedIndex;

    public void Begin(Passenger p, QueueManagerSpline queueManager, int reservedIndexFromFront)
    {
        passenger = p;
        queue = queueManager;
        reservedIndex = reservedIndexFromFront;
        targetDistOnSpline = queue.GetTargetDistanceForIndex(reservedIndex);

        active = (passenger != null && queue != null);
        enabled = active;

        if (!active) return;

        targetDistOnSpline = queue.GetTargetDistanceForIndex(reservedIndexFromFront);
    }

    private void Update()
    {
        if (!active || passenger == null || queue == null) return;

        Vector3 target = queue.GetWorldPositionAtDistance(targetDistOnSpline);
        Vector3 pos = transform.position;

        target.y = pos.y;

        Vector3 toTarget = target - pos;
        toTarget.y = 0f;

        float distToTarget = toTarget.magnitude;
        if (distToTarget <= arriveDistance)
        {
            queue.EnqueueReserved(passenger, reservedIndex, targetDistOnSpline);
            ReleaseOnce();
            Destroy(this);
            return;
        }

        Vector3 dir = toTarget.normalized;

        float speedScale = ComputeYieldSpeedScale(pos, dir);

        // Move
        transform.position += dir * (moveSpeed * speedScale * Time.deltaTime);

        // Rotate
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotateSpeed * Time.deltaTime);
        }
    }

    private float ComputeYieldSpeedScale(Vector3 pos, Vector3 dir)
    {
        // Probe ahead for another passenger
        Ray ray = new Ray(pos + Vector3.up * 0.05f, dir);

        // Use Collide so triggers still block if your passengers use trigger colliders
        bool hit = Physics.SphereCast(
            ray,
            probeRadius,
            out RaycastHit hitInfo,
            probeDistance,
            passengerMask,
            QueryTriggerInteraction.Collide
        );

        if (!hit)
        {
            waiting = false;
            return 1f;
        }

        // Ignore self
        if (hitInfo.collider != null && (hitInfo.collider.transform == transform || hitInfo.collider.transform.IsChildOf(transform)))
        {
            waiting = false;
            return 1f;
        }

        // Only treat actual passengers as blockers
        if (hitInfo.collider == null || hitInfo.collider.GetComponentInParent<Passenger>() == null)
        {
            waiting = false;
            return 1f;
        }

        Passenger otherPassenger = hitInfo.collider.GetComponentInParent<Passenger>();
        if (otherPassenger == null)
        {
            waiting = false;
            return 1f;
        }

        // ✅ NEW: don't yield to people who are still trying to join the queue
        if (otherPassenger.GetComponent<PassengerJoinQueue>() != null)
        {
            waiting = false;
            return 1f;
        }


        float d = hitInfo.distance;

        // Hysteresis: once waiting, keep waiting until we have enough space
        if (waiting)
        {
            if (d >= resumeDistance)
                waiting = false;
            else
                return 0f;
        }

        if (d <= stopDistance)
        {
            waiting = true;
            return 0f;
        }

        // Slow down smoothly between stopDistance and probeDistance
        float t = Mathf.InverseLerp(stopDistance, probeDistance, d);
        return Mathf.Clamp01(t);
    }

    private void OnDestroy()
    {
        ReleaseOnce();
    }

    private void ReleaseOnce()
    {
        if (releasedReservation) return;
        releasedReservation = true;

        if (queue != null)
            queue.ReleaseReservation();
    }
}
