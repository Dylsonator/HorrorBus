using UnityEngine;

public static class AnomalyLookReaction
{
    public static void Trigger(Passenger anomaly, float radius, int watchers, float watcherLookSeconds, float anomalyLookSeconds)
    {
        if (anomaly == null) return;

        // Find nearby humans to look at the anomaly
        Passenger[] picked = new Passenger[Mathf.Max(0, watchers)];
        float[] pickedD = new float[picked.Length];
        for (int i = 0; i < pickedD.Length; i++) pickedD[i] = float.MaxValue;

        foreach (var p in PassengerRegistry.All)
        {
            if (p == null) continue;
            if (p == anomaly) continue;
            if (p.IsAnomaly) continue;

            float d = Vector3.Distance(p.transform.position, anomaly.transform.position);
            if (d > radius) continue;

            // insert into nearest list
            for (int i = 0; i < picked.Length; i++)
            {
                if (d < pickedD[i])
                {
                    for (int j = picked.Length - 1; j > i; j--)
                    {
                        picked[j] = picked[j - 1];
                        pickedD[j] = pickedD[j - 1];
                    }
                    picked[i] = p;
                    pickedD[i] = d;
                    break;
                }
            }
        }

        // Make those humans glance at the anomaly
        for (int i = 0; i < picked.Length; i++)
        {
            var human = picked[i];
            if (human == null) continue;

            var look = human.GetComponent<PassengerHeadLook>();
            if (look != null)
                look.LookAt(anomaly.transform, watcherLookSeconds);
        }

        // Anomaly looks at someone else (decoy): pick the nearest human NOT in watchers if possible
        Passenger decoy = null;
        float best = float.MaxValue;

        foreach (var p in PassengerRegistry.All)
        {
            if (p == null || p == anomaly || p.IsAnomaly) continue;

            bool isWatcher = false;
            for (int i = 0; i < picked.Length; i++)
                if (picked[i] == p) { isWatcher = true; break; }

            if (isWatcher) continue;

            float d = Vector3.Distance(p.transform.position, anomaly.transform.position);
            if (d < best)
            {
                best = d;
                decoy = p;
            }
        }

        // If everyone nearby is a watcher, just pick any human
        if (decoy == null)
        {
            foreach (var p in PassengerRegistry.All)
            {
                if (p == null || p == anomaly || p.IsAnomaly) continue;
                decoy = p;
                break;
            }
        }

        if (decoy != null)
        {
            var anomLook = anomaly.GetComponent<PassengerHeadLook>();
            if (anomLook != null)
                anomLook.LookAt(decoy.transform, anomalyLookSeconds);
        }
    }
}
