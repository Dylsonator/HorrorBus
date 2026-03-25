using System.Collections.Generic;

public static class PassengerRegistry
{
    private static readonly List<Passenger> AllPassengers = new();

    public static IReadOnlyList<Passenger> All => AllPassengers;

    public static void Register(Passenger p)
    {
        if (p != null && !AllPassengers.Contains(p))
            AllPassengers.Add(p);
    }

    public static void Unregister(Passenger p)
    {
        if (p != null)
            AllPassengers.Remove(p);
    }
}
