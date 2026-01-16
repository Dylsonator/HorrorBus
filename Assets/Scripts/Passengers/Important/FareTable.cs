using UnityEngine;

[CreateAssetMenu(menuName = "HorrorBus/Fare Table")]
public sealed class FareTable : ScriptableObject
{
    [Tooltip("Index = stops ahead. e.g. fares[1] = fare for 1 stop ahead.")]
    public int[] fares = { 0, 2, 3, 4, 5, 6, 7 };

    public int GetFare(int stopsAhead)
    {
        stopsAhead = Mathf.Clamp(stopsAhead, 1, fares.Length - 1);
        return fares[stopsAhead];
    }
}
