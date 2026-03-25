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
    [SerializeField] private float actionCooldownSeconds = 5f;
    [SerializeField] private float killCooldownSeconds = 20f;

    [Header("Limits (optional safety)")]
    [SerializeField] private int maxKillsPerAnomaly = 2;

    [Header("Only act while bus moving")]
    [SerializeField] private bool onlyActWhileBusMoving = true;
    [SerializeField] private BusDrive busDrive;
    [SerializeField] private RouteStops routeStops;
    [SerializeField] private float movingSpeedThreshold = 0.05f;

    private bool wasObserved;
    private bool pendingSlip;
    private float slipTimer;

    private float actionCooldownTimer;
    private float killCooldownTimer;
    private int killsDone;

    public AnomalySkill Skill => skill;

    private void Awake()
    {
        if (passenger == null) passenger = GetComponent<Passenger>();

        if (busDrive == null) busDrive = FindFirstObjectByType<BusDrive>();
        if (routeStops == null) routeStops = FindFirstObjectByType<RouteStops>();

        if (passenger != null && passenger.IsAnomaly && randomiseSkillOnSpawn)
            AssignRandomSkill();

        AssignIdVisualBySkill();
    }

    public void SetSkill(AnomalySkill newSkill)
    {
        skill = newSkill;
        AssignIdVisualBySkill();
    }

    private void AssignRandomSkill()
    {
        float low = Mathf.Max(0f, skillChanceLow);
        float mid = Mathf.Max(0f, skillChanceMid);
        float high = Mathf.Max(0f, skillChanceHigh);

        float total = low + mid + high;
        if (total <= 0f)
        {
            skill = AnomalySkill.Mid;
            if (passenger != null)
                Debug.Log($"[ANOMALY] {passenger.PassengerName} assigned skill {skill} (fallback, weights were zero)");
            return;
        }

        float r = Random.value * total;

        if (r < low) skill = AnomalySkill.Low;
        else if (r < low + mid) skill = AnomalySkill.Mid;
        else skill = AnomalySkill.High;

        if (passenger != null)
            Debug.Log($"[ANOMALY] {passenger.PassengerName} assigned skill {skill}");
    }

    private void AssignIdVisualBySkill()
    {
        if (passenger == null)
            return;

        if (!passenger.IsAnomaly)
        {
            passenger.SetIdVisual(PassengerIdVisual.Real);
            return;
        }

        switch (skill)
        {
            case AnomalySkill.Low:
                passenger.SetIdVisual(PassengerIdVisual.ObviousFake);
                break;

            case AnomalySkill.Mid:
                passenger.SetIdVisual((PassengerIdVisual)Random.Range(2, 5)); // FakeAlt1..FakeAlt3
                break;

            case AnomalySkill.High:
                passenger.SetIdVisual(PassengerIdVisual.Real);
                break;

            default:
                passenger.SetIdVisual(PassengerIdVisual.Real);
                break;
        }
    }

    private void Update()
    {
        if (passenger == null || profile == null)
            return;

        if (onlyActWhileBusMoving && !IsBusMovingNow())
        {
            pendingSlip = false;
            slipTimer = 0f;
            wasObserved = passenger.IsObserved;
            return;
        }

        actionCooldownTimer -= Time.deltaTime;
        killCooldownTimer -= Time.deltaTime;

        bool observed = passenger.IsObserved;

        if (observed)
        {
            pendingSlip = false;
            slipTimer = 0f;
            wasObserved = true;
            return;
        }

        if (actionCooldownTimer > 0f)
        {
            wasObserved = false;
            return;
        }

        if (wasObserved && !pendingSlip)
        {
            pendingSlip = true;
            slipTimer = profile.PickDelay(skill);
        }

        if (pendingSlip)
        {
            slipTimer -= Time.deltaTime;

            if (slipTimer <= 0f)
            {
                bool didAnything = TryActOnce();

                if (didAnything)
                    actionCooldownTimer = actionCooldownSeconds;

                pendingSlip = false;
                slipTimer = 0f;
            }
        }

        wasObserved = false;
    }

    private bool TryActOnce()
    {
        if (CanAttemptKill(out Passenger victim))
        {
            float chance = GetKillChanceBySkill();
            if (Random.value <= chance)
            {
                if (removeAction != null && removeAction.TryExecute(this, passenger))
                {
                    killsDone++;
                    killCooldownTimer = killCooldownSeconds;
                    AnomalyGazeReaction.TriggerAfterKill(passenger, radius: 7f, minReactors: 1, maxReactors: 2);
                    return true;
                }
            }
        }

        const int retries = 3;
        for (int i = 0; i < retries; i++)
        {
            AnomalyActionBase chosen = profile.PickAction();
            if (chosen == null)
                break;

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
            return true;

        if (busDrive.IsPaused)
            return false;

        return busDrive.CurrentSpeed > movingSpeedThreshold;
    }
}