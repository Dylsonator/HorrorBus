using System.Collections.Generic;
using UnityEngine;

public sealed class PassengerPaymentDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FareTable fareTable;
    [SerializeField] private RouteStops routeStops;

    [Header("Humans")]
    [SerializeField, Range(0f, 1f)] private float humanDayRiderChance = 0.18f;
    [SerializeField, Range(0f, 1f)] private float humanOldDayRiderChance = 0.05f;
    [SerializeField, Range(0f, 1f)] private float humanExactCashChance = 0.45f;
    [SerializeField, Range(0f, 1f)] private float humanRoundUpCashChance = 0.45f;

    [Header("Anomalies")]
    [SerializeField, Range(0f, 1f)] private float anomalyDayRiderChance = 0.22f;
    [SerializeField, Range(0f, 1f)] private float anomalyOldDayRiderChance = 0.25f;
    [SerializeField, Range(0f, 1f)] private float anomalyFakeDayRiderChance = 0.22f;
    [SerializeField, Range(0f, 1f)] private float anomalyUnderpayCashChance = 0.25f;
    [SerializeField, Range(0f, 1f)] private float anomalyOddCashChance = 0.40f;

    private readonly HashSet<Passenger> configuredPassengers = new HashSet<Passenger>();

    private void Awake()
    {
        if (fareTable == null)
            fareTable = FindFirstObjectByType<FareTable>();

        if (routeStops == null)
            routeStops = FindFirstObjectByType<RouteStops>();
    }

    private void OnEnable()
    {
        if (routeStops != null)
            routeStops.ArrivedAtStop += HandleArrivedAtStop;
    }

    private void OnDisable()
    {
        if (routeStops != null)
            routeStops.ArrivedAtStop -= HandleArrivedAtStop;
    }

    private void Update()
    {
        ConfigureUnconfiguredPassengers();
    }

    private void HandleArrivedAtStop()
    {
        ConfigureUnconfiguredPassengers();
    }

    private void ConfigureUnconfiguredPassengers()
    {
        configuredPassengers.RemoveWhere(p => p == null);

        Passenger[] passengers = FindObjectsByType<Passenger>(FindObjectsSortMode.None);
        for (int i = 0; i < passengers.Length; i++)
        {
            Passenger passenger = passengers[i];
            if (passenger == null || configuredPassengers.Contains(passenger))
                continue;

            if (passenger.HasBeenProcessed || passenger.IsSeatedPassenger)
                continue;

            ConfigurePayment(passenger);
            configuredPassengers.Add(passenger);
        }
    }

    private void ConfigurePayment(Passenger passenger)
    {
        if (passenger == null)
            return;

        int expectedFare = Mathf.Max(0, passenger.ExpectedFare);
        if (expectedFare <= 0 && fareTable != null)
            expectedFare = fareTable.GetFare(Mathf.Max(1, passenger.ClaimedStopsRemaining));

        if (expectedFare <= 0)
            expectedFare = 180;

        if (ShouldUseDayRider(passenger.IsAnomaly))
        {
            bool oldTicket = passenger.IsAnomaly
                ? Random.value < anomalyOldDayRiderChance
                : Random.value < humanOldDayRiderChance;

            bool fakeTicket = passenger.IsAnomaly && !oldTicket && Random.value < anomalyFakeDayRiderChance;
            bool validTicket = !oldTicket && !fakeTicket;
            string dateLabel = validTicket ? "Today" : (oldTicket ? "Yesterday" : "Looks wrong");

            passenger.SetDayRiderPayment(expectedFare, validTicket, oldTicket, fakeTicket, dateLabel);
            return;
        }

        int[] tendered = BuildTenderedDenominations(expectedFare, passenger.IsAnomaly);
        passenger.SetCashPayment(expectedFare, tendered);
    }

    private bool ShouldUseDayRider(bool anomaly)
    {
        return Random.value < (anomaly ? anomalyDayRiderChance : humanDayRiderChance);
    }

    private int[] BuildTenderedDenominations(int expectedFare, bool anomaly)
    {
        int[] values = fareTable != null ? fareTable.GetDenominationValuesDescending() : new[] { 500, 200, 100, 50, 20, 10, 5 };
        int target = expectedFare;

        if (!anomaly)
        {
            float roll = Random.value;
            if (roll < humanExactCashChance)
            {
                target = expectedFare;
            }
            else if (roll < humanExactCashChance + humanRoundUpCashChance)
            {
                target = NextReasonableHumanTender(expectedFare, values);
            }
            else
            {
                target = expectedFare + PickSmallStep(values);
            }
        }
        else
        {
            float roll = Random.value;
            if (roll < anomalyUnderpayCashChance)
            {
                target = Mathf.Max(5, expectedFare - PickSmallStep(values));
            }
            else if (roll < anomalyUnderpayCashChance + anomalyOddCashChance)
            {
                target = expectedFare + PickSmallStep(values);
            }
            else
            {
                target = expectedFare;
            }
        }

        return BuildBreakdown(target, values);
    }

    private int NextReasonableHumanTender(int expectedFare, int[] values)
    {
        int[] candidates =
        {
            expectedFare + 20,
            expectedFare + 40,
            expectedFare + 50,
            RoundUp(expectedFare, 100),
            RoundUp(expectedFare, 200)
        };

        int pick = candidates[Random.Range(0, candidates.Length)];
        return Mathf.Max(expectedFare, pick);
    }

    private static int RoundUp(int value, int step)
    {
        if (step <= 0)
            return value;

        int rem = value % step;
        return rem == 0 ? value : value + (step - rem);
    }

    private int PickSmallStep(int[] values)
    {
        List<int> small = new List<int>();
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] <= 100)
                small.Add(values[i]);
        }

        if (small.Count == 0)
            return 20;

        return small[Random.Range(0, small.Count)];
    }

    private static int[] BuildBreakdown(int targetPence, int[] valuesDescending)
    {
        List<int> result = new List<int>();
        targetPence = Mathf.Max(0, targetPence);

        for (int i = 0; i < valuesDescending.Length; i++)
        {
            int value = valuesDescending[i];
            if (value <= 0)
                continue;

            while (targetPence >= value)
            {
                result.Add(value);
                targetPence -= value;
            }
        }

        if (result.Count == 0 && valuesDescending.Length > 0)
            result.Add(valuesDescending[valuesDescending.Length - 1]);

        return result.ToArray();
    }
}
