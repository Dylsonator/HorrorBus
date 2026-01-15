using UnityEngine;

public sealed class SeatAnchor : MonoBehaviour
{
    [SerializeField] private bool occupied;

    public bool Occupied
    {
        get => occupied;
        set => occupied = value;
    }
}
