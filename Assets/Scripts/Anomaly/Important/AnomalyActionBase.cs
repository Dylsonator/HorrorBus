using UnityEngine;

public abstract class AnomalyActionBase : ScriptableObject
{
    public abstract bool TryExecute(AnomalyController controller, Passenger self);
}
