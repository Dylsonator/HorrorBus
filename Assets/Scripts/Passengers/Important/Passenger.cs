using UnityEngine;

public class Passenger : MonoBehaviour
{
    [Header("Identity")]
    public string PassengerName;

    [Header("Observation")]
    public Transform Head; // temporary head transform with forward direction
    public bool IsObserved { get; internal set; }

    [Header("Seating / Movement (optional for now)")]
    public Transform CurrentSeatTarget;

    private void Awake()
    {
        PassengerRegistry.Register(this);

        if (Head == null)
        {
            // fallback: use transform (not ideal, but avoids null refs)
            Head = transform;
        }
    }

    private void OnDestroy()
    {
        PassengerRegistry.Unregister(this);
    }
}
