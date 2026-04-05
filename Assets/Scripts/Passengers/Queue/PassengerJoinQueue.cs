using UnityEngine;

public sealed class PassengerJoinQueue : MonoBehaviour
{
    [Header("Walk to queue back")]
    [SerializeField] private float moveSpeed = 1.7f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float arriveDistance = 0.35f;

    [Header("Join staggering")]
    [SerializeField] private float joinRepathInterval = 0.2f;
    [SerializeField] private float personalBackOffset = 0.35f;

    [Header("Local avoidance")]
    [SerializeField] private bool waitIfBlocked = true;
    [SerializeField] private float blockRadius = 0.45f;
    [SerializeField] private float blockCheckAhead = 0.7f;
    [SerializeField] private LayerMask passengerLayerMask = ~0;

    [Header("Grounding")]
    [SerializeField] private bool stickToGround = true;
    [SerializeField] private float groundRayStartHeight = 2f;
    [SerializeField] private float groundRayDistance = 6f;
    [SerializeField] private float groundOffset = 0f;
    [SerializeField] private LayerMask groundMask = ~0;

    private Passenger passenger;
    private QueueManagerNodes queue;
    private int requiredStopIndex = -1;

    private float repathTimer;
    private Vector3 currentTarget;
    private bool hasTarget;
    private float personalOffsetAmount;

    public void Begin(Passenger p, QueueManagerNodes q, Transform entry)
    {
        Begin(p, q, entry, -1);
    }

    public void Begin(Passenger p, QueueManagerNodes q, Transform entry, int stopIndex)
    {
        passenger = p;
        queue = q;
        requiredStopIndex = stopIndex;

        personalOffsetAmount = Random.Range(0f, Mathf.Max(0f, personalBackOffset));
        repathTimer = 0f;
        hasTarget = false;

        enabled = (passenger != null && queue != null);
    }

    private void Update()
    {
        if (passenger == null || queue == null)
            return;

        if (passenger.HasBeenProcessed || passenger.IsSeatedPassenger)
        {
            Destroy(this);
            return;
        }

        if (SeatManager.Instance != null && SeatManager.Instance.GetSeatForPassenger(passenger) != null)
        {
            Destroy(this);
            return;
        }

        if (requiredStopIndex >= 0 && RouteStops.Instance != null)
        {
            if (!RouteStops.Instance.WaitingAtStop)
                return;

            if (RouteStops.Instance.CurrentStopIndex != requiredStopIndex)
                return;
        }

        repathTimer -= Time.deltaTime;
        if (!hasTarget || repathTimer <= 0f)
        {
            currentTarget = queue.GetJoinTargetFor(passenger, personalOffsetAmount);
            hasTarget = true;
            repathTimer = joinRepathInterval;
        }

        Vector3 pos = transform.position;
        Vector3 target = currentTarget;

        Vector3 to = target - pos;
        to.y = 0f;

        float dist = to.magnitude;

        if (dist <= arriveDistance)
        {
            if (!passenger.HasBeenProcessed && !passenger.IsSeatedPassenger)
                queue.AddToQueue(passenger);

            Destroy(this);
            return;
        }

        Vector3 dir = to.sqrMagnitude > 0.0001f ? to.normalized : transform.forward;

        if (waitIfBlocked && IsBlocked(pos, dir))
        {
            Face(dir);
            if (stickToGround)
                SnapToGround();
            return;
        }

        transform.position += dir * (moveSpeed * Time.deltaTime);
        Face(dir);

        if (stickToGround)
            SnapToGround();
    }

    private bool IsBlocked(Vector3 pos, Vector3 dir)
    {
        Vector3 origin = pos + Vector3.up * 0.5f;
        Vector3 aheadPoint = origin + dir * blockCheckAhead;

        Collider[] hits = Physics.OverlapSphere(aheadPoint, blockRadius, passengerLayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (c == null)
                continue;

            if (c.transform == transform)
                continue;

            Passenger other = c.GetComponentInParent<Passenger>();
            if (other == null || other == passenger)
                continue;

            if (other.HasBeenProcessed || other.IsSeatedPassenger)
                continue;

            return true;
        }

        return false;
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (waitIfBlocked)
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.25f);
            Vector3 pos = transform.position + Vector3.up * 0.5f;
            Vector3 dir = transform.forward;
            Vector3 p = pos + dir * blockCheckAhead;
            Gizmos.DrawSphere(p, blockRadius);
        }

        if (stickToGround)
        {
            Gizmos.color = Color.cyan;
            Vector3 origin = transform.position + Vector3.up * groundRayStartHeight;
            Gizmos.DrawLine(origin, origin + Vector3.down * groundRayDistance);
        }
    }
#endif
}