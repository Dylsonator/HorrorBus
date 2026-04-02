
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

    [Header("Band prices")]
    [SerializeField] private int shortFarePence = 180;
    [SerializeField] private int mediumFarePence = 260;
    [SerializeField] private int longFarePence = 340;
    [SerializeField] private int dayRiderPricePence = 450;

    [Header("Stops per band")]
    [SerializeField] private int shortMaxStops = 2;
    [SerializeField] private int mediumMaxStops = 4;

    [Header("Denominations")]
    [SerializeField]
    private Denomination[] denominations =
    {
        new Denomination { label = "£20", valuePence = 2000, startingCount = 2 },
        new Denomination { label = "£10", valuePence = 1000, startingCount = 2 },
        new Denomination { label = "£5",  valuePence = 500,  startingCount = 4 },
        new Denomination { label = "£2",  valuePence = 200,  startingCount = 6 },
        new Denomination { label = "£1",  valuePence = 100,  startingCount = 8 },
        new Denomination { label = "50p", valuePence = 50,   startingCount = 12 },
        new Denomination { label = "20p", valuePence = 20,   startingCount = 14 },
        new Denomination { label = "10p", valuePence = 10,   startingCount = 14 },
        new Denomination { label = "5p",  valuePence = 5,    startingCount = 16 }
    };

    public int GetFare(int stopsAhead) => GetBandFare(GetBandForStops(stopsAhead));
    public int GetBandFare(TicketBand band)
    {
        return band switch
        {
            TicketBand.Short => Mathf.Max(0, shortFarePence),
            TicketBand.Medium => Mathf.Max(0, mediumFarePence),
            TicketBand.Long => Mathf.Max(0, longFarePence),
            TicketBand.DayRider => Mathf.Max(0, dayRiderPricePence),
            _ => 0
        };
    }

    public TicketBand GetBandForStops(int stopsAhead)
    {
        stopsAhead = Mathf.Max(1, stopsAhead);
        if (stopsAhead <= Mathf.Max(1, shortMaxStops))
            return TicketBand.Short;
        if (stopsAhead <= Mathf.Max(shortMaxStops + 1, mediumMaxStops))
            return TicketBand.Medium;
        return TicketBand.Long;
    }

    public int GetDayRiderPrice() => Mathf.Max(0, dayRiderPricePence);

    public int[] GetDenominationValuesDescending()
    {
        if (denominations == null || denominations.Length == 0)
            return new[] { 2000, 1000, 500, 200, 100, 50, 20, 10, 5 };

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
        sb.AppendLine($"Short ({shortMaxStops} stop max): {FormatMoney(shortFarePence)}");
        sb.AppendLine($"Medium ({mediumMaxStops} stop max): {FormatMoney(mediumFarePence)}");
        sb.AppendLine($"Long: {FormatMoney(longFarePence)}");
        sb.Append($"DayRider: {FormatMoney(dayRiderPricePence)}");
        return sb.ToString();
    }

    public static string FormatMoney(int valuePence)
    {
        return $"£{Mathf.Max(0, valuePence) / 100f:0.00}";
    }
}
