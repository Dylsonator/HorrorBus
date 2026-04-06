using UnityEngine;

public sealed class NodeQueueWalker : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float maxSpeed = 1.6f;
    [SerializeField] private float acceleration = 6f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float rotateSpeed = 10f;

    [Header("Follow")]
    [SerializeField] private float slowDownDistance = 0.9f;
    [SerializeField] private float minimumMoveDistance = 0.04f;

    [Header("Grounding")]
    [SerializeField] private bool stickToGround = true;
    [SerializeField] private float groundRayStartHeight = 2f;
    [SerializeField] private float groundRayDistance = 6f;
    [SerializeField] private float groundOffset = 2f;
    [SerializeField] private LayerMask groundMask = 0;

    private Vector3 targetPos;
    private Transform ahead;
    private float stopDistance = 0.7f;

    private float speed;
    private bool hasTarget;

    private QueueManagerNodes manager;


    private void Awake()
    {
        if (groundMask == 0)
            groundMask = LayerMask.GetMask("Terrain", "Bus");
    }
    public void Init(QueueManagerNodes m)
    {
        manager = m;
        speed = 0f;
        hasTarget = false;
        ahead = null;
    }

    public void SetTarget(Vector3 pos, Transform aheadPassenger, float stopDist)
    {
        targetPos = pos;
        ahead = aheadPassenger;
        stopDistance = Mathf.Max(0.35f, stopDist);
        hasTarget = true;
        enabled = true;
    }

    public void StopMoving()
    {
        hasTarget = false;
        speed = 0f;
        enabled = false;
    }

    private void Update()
    {
        if (!hasTarget)
            return;

        Passenger passenger = GetComponent<Passenger>();
        if (passenger != null)
        {
            if (passenger.HasBeenProcessed || passenger.IsSeatedPassenger)
            {
                StopMoving();
                return;
            }

            if (SeatManager.Instance != null && SeatManager.Instance.GetSeatForPassenger(passenger) != null)
            {
                StopMoving();
                return;
            }
        }

        Vector3 pos = transform.position;
        Vector3 toTarget = targetPos - pos;
        toTarget.y = 0f;
        float distToTarget = toTarget.magnitude;

        float aheadDistance = float.MaxValue;
        Vector3 flatToAhead = Vector3.zero;

        if (ahead != null)
        {
            flatToAhead = ahead.position - pos;
            flatToAhead.y = 0f;
            aheadDistance = flatToAhead.magnitude;
        }

        if (ahead != null && aheadDistance <= stopDistance)
        {
            speed = Mathf.MoveTowards(speed, 0f, deceleration * Time.deltaTime);
            Face(flatToAhead.sqrMagnitude > 0.0001f ? flatToAhead.normalized : transform.forward);

            if (stickToGround)
                SnapToGround();

            return;
        }

        if (distToTarget <= minimumMoveDistance)
        {
            speed = Mathf.MoveTowards(speed, 0f, deceleration * Time.deltaTime);

            if (stickToGround)
                SnapToGround();

            return;
        }

        float desiredSpeed = maxSpeed;

        if (distToTarget < slowDownDistance)
            desiredSpeed *= Mathf.Clamp01(distToTarget / slowDownDistance);

        if (ahead != null && aheadDistance < stopDistance + slowDownDistance)
        {
            float followFactor = Mathf.InverseLerp(stopDistance, stopDistance + slowDownDistance, aheadDistance);
            desiredSpeed *= Mathf.Clamp01(followFactor);
        }

        speed = Mathf.MoveTowards(speed, desiredSpeed, acceleration * Time.deltaTime);

        float step = speed * Time.deltaTime;
        Vector3 dir = toTarget.normalized;
        Vector3 move = dir * Mathf.Min(step, distToTarget);
        transform.position += move;

        Face(dir);

        if (stickToGround)
            SnapToGround();
    }

    private void SnapToGround()
    {
        Vector3 origin = transform.position + Vector3.up * groundRayStartHeight;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 p = transform.position;
            p.y = hit.point.y + groundOffset;
            transform.position = p;
        }
    }

    private void Face(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.0001f)
            return;

        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotateSpeed * Time.deltaTime);
    }
}