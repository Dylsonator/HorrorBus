using UnityEngine;

public class AnomalyController : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Passenger passenger;

    [Header("Competence")]
    [SerializeField] private AnomalySkill skill = AnomalySkill.Mid;

    [Header("Randomise Skill On Spawn")]
    [SerializeField] private bool randomiseSkillOnSpawn = true;
    [SerializeField, Range(0f, 1f)] private float skillChanceLow = 0.45f;
    [SerializeField, Range(0f, 1f)] private float skillChanceMid = 0.35f;
    [SerializeField, Range(0f, 1f)] private float skillChanceHigh = 0.20f;

    [Header("Profile (non-lethal action pool)")]
    [SerializeField] private AnomalyProfile profile;

    [Header("Kill Action (main goal, not guaranteed)")]
    [SerializeField] private AnomalyActionRemoveNearest removeAction;
    [SerializeField] private float killCheckRadius = 8f;

    [Header("Kill Intent (chance when victim available)")]
    [SerializeField, Range(0f, 1f)] private float killChanceLow = 0.45f;
    [SerializeField, Range(0f, 1f)] private float killChanceMid = 0.25f;
    [SerializeField, Range(0f, 1f)] private float killChanceHigh = 0.12f;

    [Header("Cooldowns")]
    [SerializeField] private float actionCooldownSeconds = 5f; // any action
    [SerializeField] private float killCooldownSeconds = 20f;  // kills specifically

    [Header("Limits (optional safety)")]
    [SerializeField] private int maxKillsPerAnomaly = 2;       // set 0 for unlimited

    [Header("Only act while bus moving")]
    [SerializeField] private bool onlyActWhileBusMoving = true;
    [SerializeField] private BusDrive busDrive;
    [SerializeField] private RouteStops routeStops;
    [SerializeField] private float movingSpeedThreshold = 0.05f;

    // Internal state
    private bool wasObserved;
    private bool pendingSlip;
    private float slipTimer;

    private float actionCooldownTimer;
    private float killCooldownTimer;
    private int killsDone;

    private void Awake()
    {
        if (passenger == null) passenger = GetComponent<Passenger>();

        if (busDrive == null) busDrive = FindFirstObjectByType<BusDrive>();
        if (routeStops == null) routeStops = FindFirstObjectByType<RouteStops>();

        if (randomiseSkillOnSpawn && passenger != null && passenger.IsAnomaly)
            AssignRandomSkill();
    }

    // Optional: lets your spawner force a skill
    public void SetSkill(AnomalySkill newSkill)
    {
        skill = newSkill;
    }

    private void AssignRandomSkill()
    {
        // Normalise weights safely (so you can type anything in inspector)
        float low = Mathf.Max(0f, skillChanceLow);
        float mid = Mathf.Max(0f, skillChanceMid);
        float high = Mathf.Max(0f, skillChanceHigh);

        float total = low + mid + high;
        if (total <= 0f)
        {
            skill = AnomalySkill.Mid;
            Debug.Log($"[ANOMALY] {passenger.PassengerName} assigned skill {skill} (fallback, weights were zero)");
            return;
        }

        float r = Random.value * total;

        if (r < low) skill = AnomalySkill.Low;
        else if (r < low + mid) skill = AnomalySkill.Mid;
        else skill = AnomalySkill.High;

        Debug.Log($"[ANOMALY] {passenger.PassengerName} assigned skill {skill}");
    }

    private void Update()
    {
        if (passenger == null || profile == null)
            return;

        if (onlyActWhileBusMoving && !IsBusMovingNow())
        {
            // Important: cancel any pending countdown so it can't "finish" at a stop
            pendingSlip = false;
            slipTimer = 0f;

            // Keep this consistent for your observed->unobserved transition logic
            wasObserved = passenger.IsObserved;
            return;
        }

        // Tick cooldowns
        actionCooldownTimer -= Time.deltaTime;
        killCooldownTimer -= Time.deltaTime;

        bool observed = passenger.IsObserved;

        // Observed: freeze deception + cancel pending countdown
        if (observed)
        {
            pendingSlip = false;
            slipTimer = 0f;
            wasObserved = true;
            return;
        }

        // Only allow starting a countdown if we're not on action cooldown
        if (actionCooldownTimer > 0f)
        {
            wasObserved = false;
            return;
        }

        // Start delayed attempt when we transition observed -> unobserved
        if (wasObserved && !pendingSlip)
        {
            pendingSlip = true;
            slipTimer = profile.PickDelay(skill);
        }

        // Countdown only while unobserved
        if (pendingSlip)
        {
            slipTimer -= Time.deltaTime;

            if (slipTimer <= 0f)
            {
                bool didAnything = TryActOnce();

                // If something happened, start cooldown
                if (didAnything)
                {
                    actionCooldownTimer = actionCooldownSeconds;
                }

                pendingSlip = false;
                slipTimer = 0f;
            }
        }

        wasObserved = false;
    }

    private bool TryActOnce()
    {
        // 1) Sometimes kill (main goal), but not always
        if (CanAttemptKill(out Passenger victim))
        {
            float chance = GetKillChanceBySkill();
            if (Random.value <= chance)
            {
                if (removeAction != null && removeAction.TryExecute(this, passenger))
                {
                    killsDone++;
                    killCooldownTimer = killCooldownSeconds;

                    // After-kill reaction: nearby humans avert gaze, anomaly looks "innocent"
                    AnomalyGazeReaction.TriggerAfterKill(passenger, radius: 7f, minReactors: 1, maxReactors: 2);

                    return true;
                }
            }
        }

        // 2) Otherwise non-lethal action from profile (retry a bit)
        const int retries = 3;
        for (int i = 0; i < retries; i++)
        {
            var chosen = profile.PickAction();
            if (chosen == null) break;

            if (chosen.TryExecute(this, passenger))
                return true;
        }

        return false;
    }

    private bool CanAttemptKill(out Passenger victim)
    {
        victim = null;

        if (removeAction == null) return false;
        if (killCooldownTimer > 0f) return false;

        if (maxKillsPerAnomaly > 0 && killsDone >= maxKillsPerAnomaly)
            return false;

        victim = PassengerUtil.FindNearest(passenger.transform.position, killCheckRadius, exclude: passenger);
        return victim != null;
    }

    private float GetKillChanceBySkill()
    {
        return skill switch
        {
            AnomalySkill.Low => killChanceLow,
            AnomalySkill.Mid => killChanceMid,
            AnomalySkill.High => killChanceHigh,
            _ => killChanceMid
        };
    }

    private bool IsBusMovingNow()
    {
        if (routeStops != null && routeStops.WaitingAtStop)
            return false;

        if (busDrive == null)
            return true; // fail-open if not wired

        if (busDrive.IsPaused)
            return false;

        return busDrive.CurrentSpeed > movingSpeedThreshold;
    }
}
