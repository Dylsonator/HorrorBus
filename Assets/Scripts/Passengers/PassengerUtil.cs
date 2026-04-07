using UnityEngine;

public static class PassengerUtil
{
    public static int CountNearby(Vector3 pos, float radius, Passenger exclude = null, bool seatedOnly = false)
    {
        int count = 0;

        foreach (var p in PassengerRegistry.All)
        {
            if (p == null || p == exclude)
                continue;

            if (seatedOnly && !p.IsSeatedPassenger)
                continue;

            if (Vector3.Distance(pos, p.transform.position) <= radius)
                count++;
        }

        return count;
    }

    public static Passenger FindNearest(Vector3 pos, float radius, Passenger exclude = null, bool seatedOnly = false)
    {
        Passenger best = null;
        float bestD = float.MaxValue;

        foreach (var p in PassengerRegistry.All)
        {
            if (p == null || p == exclude)
                continue;

            if (seatedOnly && !p.IsSeatedPassenger)
                continue;

            float d = Vector3.Distance(pos, p.transform.position);
            if (d <= radius && d < bestD)
            {
                best = p;
                bestD = d;
            }
        }

        return best;
    }
}