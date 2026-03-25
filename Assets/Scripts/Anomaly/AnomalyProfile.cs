using UnityEngine;

[CreateAssetMenu(menuName = "Anomaly/Profile")]
public class AnomalyProfile : ScriptableObject
{
    [System.Serializable]
    public struct WeightedAction
    {
        public AnomalyActionBase action;
        [Min(0f)] public float weight;
    }

    [Header("Delay while unobserved (seconds)")]
    public Vector2 lowDelay = new Vector2(1.5f, 4.0f);
    public Vector2 midDelay = new Vector2(3.0f, 7.0f);
    public Vector2 highDelay = new Vector2(5.0f, 12.0f);

    [Header("Action Pool")]
    public WeightedAction[] actions;

    public float PickDelay(AnomalySkill skill)
    {
        Vector2 range = skill switch
        {
            AnomalySkill.Low => lowDelay,
            AnomalySkill.Mid => midDelay,
            AnomalySkill.High => highDelay,
            _ => midDelay
        };

        float min = Mathf.Min(range.x, range.y);
        float max = Mathf.Max(range.x, range.y);
        return Random.Range(min, max);
    }

    public AnomalyActionBase PickAction()
    {
        if (actions == null || actions.Length == 0) return null;

        float total = 0f;
        for (int i = 0; i < actions.Length; i++)
        {
            if (actions[i].action != null && actions[i].weight > 0f)
                total += actions[i].weight;
        }

        if (total <= 0f) return null;

        float r = Random.Range(0f, total);
        float acc = 0f;

        for (int i = 0; i < actions.Length; i++)
        {
            var wa = actions[i];
            if (wa.action == null || wa.weight <= 0f) continue;

            acc += wa.weight;
            if (r <= acc)
                return wa.action;
        }

        // Fallback
        for (int i = 0; i < actions.Length; i++)
            if (actions[i].action != null)
                return actions[i].action;

        return null;
    }
}
