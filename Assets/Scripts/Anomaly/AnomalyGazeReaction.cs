using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AnomalyGazeReaction
{
    private sealed class Runner : MonoBehaviour { }
    private static Runner runner;

    private static Runner GetRunner()
    {
        if (runner != null) return runner;

        var go = new GameObject("_AnomalyGazeReactionRunner");
        Object.DontDestroyOnLoad(go);
        runner = go.AddComponent<Runner>();
        return runner;
    }

    /// <summary>
    /// After a kill/giveaway: only SOME nearby humans react (avoid looking),
    /// and the anomaly looks at a random world point (acts innocent).
    /// </summary>
    public static void TriggerAfterKill(
        Passenger anomaly,
        float radius = 7f,
        int minReactors = 1,
        int maxReactors = 2)
    {
        if (anomaly == null) return;
        GetRunner().StartCoroutine(DoReaction(anomaly, radius, minReactors, maxReactors));
    }

    private static IEnumerator DoReaction(Passenger anomaly, float radius, int minReactors, int maxReactors)
    {
        // Collect nearby humans
        List<Passenger> nearby = new();
        foreach (var p in PassengerRegistry.All)
        {
            if (p == null || p == anomaly) continue;
            if (p.IsAnomaly) continue;

            if (Vector3.Distance(p.transform.position, anomaly.transform.position) <= radius)
                nearby.Add(p);
        }

        if (nearby.Count == 0)
            yield break;

        // Choose random subset size
        int count = Mathf.Clamp(Random.Range(minReactors, maxReactors + 1), 0, nearby.Count);

        // Shuffle and take first "count"
        for (int i = 0; i < nearby.Count; i++)
        {
            int j = Random.Range(i, nearby.Count);
            (nearby[i], nearby[j]) = (nearby[j], nearby[i]);
        }

        const float avoidSeconds = 1.2f;
        const float glanceSeconds = 0.18f;
        const float glanceChance = 0.20f;

        // Humans react
        for (int i = 0; i < count; i++)
        {
            var human = nearby[i];
            if (human == null) continue;

            var look = human.GetComponent<PassengerHeadLook>();
            if (look == null) continue;

            if (Random.value > glanceChance)
                look.LookAwayFrom(anomaly.transform, avoidSeconds);
            else
                look.LookAt(anomaly.transform, glanceSeconds);
        }

        // After micro-glance window, force those glancers to avert too
        yield return new WaitForSeconds(glanceSeconds + 0.02f);

        for (int i = 0; i < count; i++)
        {
            var human = nearby[i];
            if (human == null) continue;

            var look = human.GetComponent<PassengerHeadLook>();
            if (look == null) continue;

            look.LookAwayFrom(anomaly.transform, avoidSeconds);
        }

        // Anomaly looks at a random point (acts innocent)
        var anomLook = anomaly.GetComponent<PassengerHeadLook>();
        if (anomLook != null)
        {
            Vector3 origin = anomaly.transform.position;
            Vector3 forward = anomaly.transform.forward;

            // Pick a point roughly ahead/side, not directly at any person.
            Vector3 randomDir = Quaternion.Euler(
                Random.Range(-5f, 5f),          // tiny pitch
                Random.Range(-120f, 120f),      // big yaw range
                0f) * forward;

            float dist = Random.Range(2.5f, 6.0f);
            Vector3 point = origin + randomDir.normalized * dist;
            point.y = origin.y + Random.Range(-0.2f, 0.2f);

            anomLook.LookAtWorldPoint(point, seconds: 0.8f);
        }
    }
}
