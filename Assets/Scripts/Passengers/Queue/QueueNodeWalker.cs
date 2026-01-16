using UnityEngine;

public sealed class NodeQueueWalker : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float maxSpeed = 1.6f;
    [SerializeField] private float acceleration = 6f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float rotateSpeed = 10f;

    private Vector3 targetPos;
    private Transform ahead;
    private float stopDistance = 0.55f;

    private float speed;
    private bool hasTarget;

    private QueueManagerNodes manager;

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
        stopDistance = Mathf.Max(0.2f, stopDist);
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
        if (!hasTarget) return;

        Vector3 pos = transform.position;

        // If too close to the person ahead, wait.
        if (ahead != null)
        {
            Vector3 flatToAhead = ahead.position - pos;
            flatToAhead.y = 0f;
            if (flatToAhead.magnitude <= stopDistance)
            {
                speed = Mathf.MoveTowards(speed, 0f, deceleration * Time.deltaTime);
                Face(flatToAhead.sqrMagnitude > 0.0001f ? flatToAhead.normalized : transform.forward);
                return;
            }
        }

        Vector3 to = targetPos - pos;
        to.y = 0f;

        float dist = to.magnitude;
        if (dist < 0.05f)
        {
            speed = Mathf.MoveTowards(speed, 0f, deceleration * Time.deltaTime);
            return;
        }

        float desiredSpeed = maxSpeed;
        if (dist < 0.6f)
            desiredSpeed = Mathf.Lerp(0.15f, maxSpeed, Mathf.Clamp01(dist / 0.6f));

        speed = Mathf.MoveTowards(speed, desiredSpeed, acceleration * Time.deltaTime);

        float step = speed * Time.deltaTime;
        Vector3 move = to.normalized * Mathf.Min(step, dist);
        transform.position += move;

        Face(to.normalized);
    }

    private void Face(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude <= 0.0001f) return;

        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotateSpeed * Time.deltaTime);
    }
}
