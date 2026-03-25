using System.Collections.Generic;
using UnityEngine;

public sealed class StopGate : MonoBehaviour
{
    private readonly HashSet<Passenger> pending = new HashSet<Passenger>();

    public int PendingCount => pending.Count;
    public bool CanDepart => pending.Count == 0;

    public void ResetGate()
    {
        pending.Clear();
    }

    public void Register(Passenger p)
    {
        if (p == null) return;
        pending.Add(p);
    }

    public void Resolve(Passenger p)
    {
        if (p == null) return;
        pending.Remove(p);
    }
}
