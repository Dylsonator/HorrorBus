using System.Text;
using UnityEngine;

[CreateAssetMenu(menuName = "HorrorBus/Fare Table")]
public sealed class FareTable : ScriptableObject
{
    [System.Serializable]
    public struct Denomination
    {
        public string label;
        public int valuePence;
        public int startingCount;
    }

    [Header("Single fares in pence")]
    [Tooltip("Index = stops ahead. Example: faresByStopsAhead[1] = fare for 1 stop ahead.")]
    [SerializeField] private int[] faresByStopsAhead = { 0, 180, 220, 260, 300, 340, 380 };

    [Header("Passes")]
    [SerializeField] private int dayRiderPricePence = 450;

    [Header("Driver float denominations")]
    [SerializeField]
    private Denomination[] denominations =
    {
        new Denomination { label = "£5",   valuePence = 500, startingCount = 2 },
        new Denomination { label = "£2",   valuePence = 200, startingCount = 4 },
        new Denomination { label = "£1",   valuePence = 100, startingCount = 8 },
        new Denomination { label = "50p",  valuePence = 50,  startingCount = 8 },
        new Denomination { label = "20p",  valuePence = 20,  startingCount = 12 },
        new Denomination { label = "10p",  valuePence = 10,  startingCount = 12 },
        new Denomination { label = "5p",   valuePence = 5,   startingCount = 12 }
    };

    public int GetFare(int stopsAhead)
    {
        if (faresByStopsAhead == null || faresByStopsAhead.Length == 0)
            return 0;

        stopsAhead = Mathf.Clamp(stopsAhead, 1, faresByStopsAhead.Length - 1);
        return Mathf.Max(0, faresByStopsAhead[stopsAhead]);
    }

    public int GetDayRiderPrice() => Mathf.Max(0, dayRiderPricePence);

    public int[] GetDenominationValuesDescending()
    {
        if (denominations == null || denominations.Length == 0)
            return new[] { 500, 200, 100, 50, 20, 10, 5 };

        int[] values = new int[denominations.Length];
        for (int i = 0; i < denominations.Length; i++)
            values[i] = Mathf.Max(1, denominations[i].valuePence);

        System.Array.Sort(values, (a, b) => b.CompareTo(a));
        return values;
    }

    public int[] GetStartingCountsMatchingValues(int[] valuesDescending)
    {
        if (valuesDescending == null)
            return System.Array.Empty<int>();

        int[] counts = new int[valuesDescending.Length];
        for (int i = 0; i < valuesDescending.Length; i++)
        {
            counts[i] = 0;

            if (denominations == null)
                continue;

            for (int j = 0; j < denominations.Length; j++)
            {
                if (denominations[j].valuePence == valuesDescending[i])
                {
                    counts[i] = Mathf.Max(0, denominations[j].startingCount);
                    break;
                }
            }
        }

        return counts;
    }

    public string GetLabelForValue(int valuePence)
    {
        if (denominations != null)
        {
            for (int i = 0; i < denominations.Length; i++)
            {
                if (denominations[i].valuePence == valuePence && !string.IsNullOrWhiteSpace(denominations[i].label))
                    return denominations[i].label;
            }
        }

        return FormatMoney(valuePence);
    }

    public string BuildFareChartText()
    {
        StringBuilder sb = new StringBuilder();

        if (faresByStopsAhead != null)
        {
            for (int i = 1; i < faresByStopsAhead.Length; i++)
                sb.AppendLine($"{i} stop: {FormatMoney(faresByStopsAhead[i])}");
        }

        sb.Append($"DayRider: {FormatMoney(dayRiderPricePence)}");
        return sb.ToString();
    }

    public static string FormatMoney(int valuePence)
    {
        valuePence = Mathf.Max(0, valuePence);
        return $"£{valuePence / 100f:0.00}";
    }
}
