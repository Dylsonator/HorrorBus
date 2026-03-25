using UnityEngine;

public sealed class PassengerHeadLook : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Passenger passenger;
    [SerializeField] private Transform head;              // rotate this
    [SerializeField] private Transform body;              // yaw reference (usually root)

    [Header("Tuning")]
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float maxYaw = 60f;          // left/right
    [SerializeField] private float maxPitch = 25f;        // up/down
    [SerializeField] private bool onlyWhenUnobserved = false; // for anomaly "acting" look

    [Header("Idle look-around")]
    [SerializeField] private bool enableIdle = true;
    [SerializeField] private Vector2 idleHoldTime = new Vector2(1.5f, 4.0f);
    [SerializeField] private float idleYaw = 20f;
    [SerializeField] private float idlePitch = 8f;
    [SerializeField] private float idleJitterSpeed = 1.0f;

    private bool useWorldPoint;
    private Vector3 worldPointTarget;

    private float idleTimer;
    private Quaternion idleLocalTarget;
    private bool hasIdleTarget;


    private Transform lookTarget;
    private float lookTimer;

    private Quaternion headNeutralLocalRot;
    private bool cachedNeutral;

    private void Awake()
    {
        if (passenger == null) passenger = GetComponent<Passenger>();
        if (head == null && passenger != null && passenger.Head != null) head = passenger.Head;
        if (body == null) body = transform;

        if (head != null)
        {
            headNeutralLocalRot = head.localRotation;
            cachedNeutral = true;
        }
        idleTimer = Random.Range(idleHoldTime.x, idleHoldTime.y);
        idleLocalTarget = headNeutralLocalRot;
        hasIdleTarget = true;

    }

    public void LookAt(Transform target, float seconds)
    {
        if (head == null) return;
        lookTarget = target;
        lookTimer = Mathf.Max(0f, seconds);
    }

    public void ClearLook(float blendOutSeconds = 0f)
    {
        lookTarget = null;
        lookTimer = Mathf.Max(0f, blendOutSeconds);
    }

    private void LateUpdate()
    {
        if (head == null || body == null) return;
        if (!cachedNeutral) return;

        // Only block DIRECTED looks when observed, not idle look-around
        if (onlyWhenUnobserved && passenger != null && passenger.IsObserved)
        {
            if (lookTarget != null || useWorldPoint)
            {
                ReturnToNeutral();
                return;
            }
            // else: allow idle look-around to continue
        }



        // Tick timer if we have any active target
        if (lookTarget != null || useWorldPoint)
        {
            lookTimer -= Time.deltaTime;
            if (lookTimer <= 0f)
            {
                lookTarget = null;
                useWorldPoint = false;
            }
        }

        // No active target => go back to neutral
        if (lookTarget == null && !useWorldPoint)
        {
            // Idle look-around
            if (enableIdle)
            {
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0f || !hasIdleTarget)
                {
                    idleTimer = Random.Range(Mathf.Min(idleHoldTime.x, idleHoldTime.y), Mathf.Max(idleHoldTime.x, idleHoldTime.y));

                    float yaw = Random.Range(-idleYaw, idleYaw);
                    float pitch = Random.Range(-idlePitch, idlePitch);

                    idleLocalTarget = Quaternion.Euler(pitch, yaw, 0f) * headNeutralLocalRot;
                    hasIdleTarget = true;
                }

                // Add tiny noise so it doesn't look robotic
                float noise = Mathf.Sin(Time.time * idleJitterSpeed) * 0.25f;
                Quaternion noisy = Quaternion.Euler(noise, -noise, 0f) * idleLocalTarget;

                head.localRotation = Quaternion.Slerp(
                    head.localRotation,
                    noisy,
                    1f - Mathf.Exp(-turnSpeed * Time.deltaTime)
                );
                return;
            }

            ReturnToNeutral();
            return;
        }


        Vector3 targetPos = useWorldPoint ? worldPointTarget : lookTarget.position;

        Vector3 to = (targetPos - head.position);
        if (to.sqrMagnitude < 0.0001f)
        {
            ReturnToNeutral();
            return;
        }

        Quaternion desiredWorld = Quaternion.LookRotation(to.normalized, Vector3.up);
        Quaternion desiredLocal = Quaternion.Inverse(body.rotation) * desiredWorld;

        Vector3 e = desiredLocal.eulerAngles;
        e.x = NormalizeAngle(e.x);
        e.y = NormalizeAngle(e.y);

        e.x = Mathf.Clamp(e.x, -maxPitch, maxPitch);
        e.y = Mathf.Clamp(e.y, -maxYaw, maxYaw);
        e.z = 0f;

        Quaternion clampedLocal = Quaternion.Euler(e);

        head.localRotation = Quaternion.Slerp(
            head.localRotation,
            clampedLocal,
            1f - Mathf.Exp(-turnSpeed * Time.deltaTime)
        );
    }


    private void ReturnToNeutral()
    {
        head.localRotation = Quaternion.Slerp(head.localRotation, headNeutralLocalRot, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }

    private static float NormalizeAngle(float a)
    {
        while (a > 180f) a -= 360f;
        while (a < -180f) a += 360f;
        return a;
    }
    public void LookAtWorldPoint(Vector3 point, float seconds)
    {
        if (head == null) return;
        worldPointTarget = point;
        useWorldPoint = true;
        lookTarget = null;
        lookTimer = Mathf.Max(0f, seconds);
    }

    public void LookAwayFrom(Transform threat, float seconds, float distance = 3f)
    {
        if (head == null || body == null || threat == null)
        {
            ClearLook(seconds);
            return;
        }

        // "Away" direction on the horizontal plane
        Vector3 away = (body.position - threat.position);
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f)
            away = -body.forward;

        away.Normalize();
        Vector3 point = head.position + away * distance;

        LookAtWorldPoint(point, seconds);
    }

}

