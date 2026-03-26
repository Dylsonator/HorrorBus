using UnityEngine;

public sealed class PassengerJoinQueue : MonoBehaviour
{
    [Header("Walk to entry")]
    [SerializeField] private float moveSpeed = 1.7f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private float arriveDistance = 0.55f;

    [Header("Entry spread")]
    [SerializeField] private float entryOffsetRadius = 0.6f;

    [Header("Join-point avoidance")]
    [SerializeField] private bool waitIfBlocked = true;
    [SerializeField] private float blockRadius = 0.55f;          // how close is "too close"
    [SerializeField] private float blockCheckAhead = 0.8f;       // how far forward to check
    [SerializeField] private LayerMask passengerLayerMask = ~0;  // set to Passenger layer for best results

    private Passenger passenger;
    private QueueManagerNodes queue;
    private Transform entryPoint;
    private int requiredStopIndex = -1;

    private Vector3 entryOffset;
    private bool offsetChosen;

    public void Begin(Passenger p, QueueManagerNodes q, Transform entry)
    {
        Begin(p, q, entry, -1);
    }

    public void Begin(Passenger p, QueueManagerNodes q, Transform entry, int stopIndex)
    {
        passenger = p;
        queue = q;
        entryPoint = entry;
        requiredStopIndex = stopIndex;

        offsetChosen = false;
        enabled = (passenger != null && queue != null && entryPoint != null);
    }

    private void Update()
    {
        if (passenger == null || queue == null || entryPoint == null) return;

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

        if (!offsetChosen)
        {
            Vector2 r = Random.insideUnitCircle * entryOffsetRadius;
            entryOffset = new Vector3(r.x, 0f, r.y);
            offsetChosen = true;
        }

        Vector3 pos = transform.position;

        Vector3 target = entryPoint.position + entryOffset;
        target.y = pos.y;

        Vector3 to = target - pos;
        to.y = 0f;

        float dist = to.magnitude;

        // Close enough: join queue system
        if (dist <= arriveDistance)
        {
            if (!passenger.HasBeenProcessed && !passenger.IsSeatedPassenger)
                queue.AddToQueue(passenger);

            Destroy(this);
            return;
        }

        Vector3 dir = to.sqrMagnitude > 0.0001f ? to.normalized : transform.forward;

        // Simple "don't walk through someone" rule
        if (waitIfBlocked && IsBlocked(pos, dir))
        {
            // Stop, rotate to face target
            Face(dir);
            return;
        }

        transform.position += dir * (moveSpeed * Time.deltaTime);
        Face(dir);
    }

    private bool IsBlocked(Vector3 pos, Vector3 dir)
    {
        // Check for another passenger ahead in our movement direction
        Vector3 origin = pos + Vector3.up * 0.5f;
        Vector3 aheadPoint = origin + dir * blockCheckAhead;

        Collider[] hits = Physics.OverlapSphere(aheadPoint, blockRadius, passengerLayerMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            var c = hits[i];
            if (c == null) continue;

            // ignore self
            if (c.transform == transform) continue;

            var other = c.GetComponentInParent<Passenger>();
            if (other == null) continue;
            if (other == passenger) continue;

            if (other.HasBeenProcessed || other.IsSeatedPassenger) continue;

            // Found another passenger ahead close enough -> blocked
            return true;
        }

        return false;
    }

    private void Face(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.0001f) return;

        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotateSpeed * Time.deltaTime);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!waitIfBlocked) return;
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.25f);

        Vector3 pos = transform.position + Vector3.up * 0.5f;
        Vector3 dir = transform.forward;
        Vector3 p = pos + dir * blockCheckAhead;
        Gizmos.DrawSphere(p, blockRadius);
    }
#endif
}
