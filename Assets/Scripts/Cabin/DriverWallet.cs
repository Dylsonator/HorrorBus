using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class DriverWallet : MonoBehaviour
{
    [SerializeField] private FareTable fareTable;

    private readonly Dictionary<int, int> countsByValue = new Dictionary<int, int>();
    private int[] valuesDescending = System.Array.Empty<int>();

    public int TotalPence
    {
        get
        {
            int total = 0;
            foreach (KeyValuePair<int, int> pair in countsByValue)
                total += pair.Key * Mathf.Max(0, pair.Value);
            return total;
        }
    }

    private void Awake()
    {
        ResetWallet();
    }

    public void ResetWallet()
    {
        countsByValue.Clear();

        valuesDescending = fareTable != null
            ? fareTable.GetDenominationValuesDescending()
            : new[] { 500, 200, 100, 50, 20, 10, 5 };

        int[] counts = fareTable != null
            ? fareTable.GetStartingCountsMatchingValues(valuesDescending)
            : new[] { 2, 4, 8, 8, 12, 12, 12 };

        for (int i = 0; i < valuesDescending.Length; i++)
            countsByValue[valuesDescending[i]] = i < counts.Length ? Mathf.Max(0, counts[i]) : 0;
    }

    public int GetCount(int valuePence)
    {
        return countsByValue.TryGetValue(valuePence, out int count) ? count : 0;
    }

    public string BuildWalletSummary()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Float Total: {FareTable.FormatMoney(TotalPence)}");

        if (valuesDescending == null || valuesDescending.Length == 0)
            return sb.ToString().TrimEnd();

        for (int i = 0; i < valuesDescending.Length; i++)
        {
            int value = valuesDescending[i];
            string label = fareTable != null ? fareTable.GetLabelForValue(value) : FareTable.FormatMoney(value);
            sb.AppendLine($"{label} x{GetCount(value)}");
        }

        return sb.ToString().TrimEnd();
    }

    public bool TryPreviewExactChangeAfterTender(int[] tenderedDenominations, int amountPence, out List<int> plan)
    {
        Dictionary<int, int> working = CloneCounts();
        AddValuesToCounts(working, tenderedDenominations);
        return TryBuildGreedyPlan(working, amountPence, out plan);
    }

    public bool TryApplyCashTransaction(int[] tenderedDenominations, IReadOnlyList<int> returnedDenominations, out string failureReason)
    {
        failureReason = string.Empty;

        Dictionary<int, int> working = CloneCounts();
        AddValuesToCounts(working, tenderedDenominations);

        if (returnedDenominations != null)
        {
            for (int i = 0; i < returnedDenominations.Count; i++)
            {
                int value = returnedDenominations[i];
                if (!working.ContainsKey(value))
                {
                    failureReason = $"Wallet doesn't support {FareTable.FormatMoney(value)}.";
                    return false;
                }

                if (working[value] <= 0)
                {
                    failureReason = $"Not enough {GetLabel(value)} in the float.";
                    return false;
                }

                working[value]--;
            }
        }

        countsByValue.Clear();
        foreach (KeyValuePair<int, int> pair in working)
            countsByValue[pair.Key] = pair.Value;

        return true;
    }

    private Dictionary<int, int> CloneCounts()
    {
        Dictionary<int, int> copy = new Dictionary<int, int>();
        foreach (KeyValuePair<int, int> pair in countsByValue)
            copy[pair.Key] = pair.Value;
        return copy;
    }

    private void AddValuesToCounts(Dictionary<int, int> target, int[] values)
    {
        if (values == null)
            return;

        for (int i = 0; i < values.Length; i++)
        {
            int value = values[i];
            if (value <= 0)
                continue;

            if (!target.ContainsKey(value))
                target[value] = 0;

            target[value]++;
        }
    }

    private bool TryBuildGreedyPlan(Dictionary<int, int> availableCounts, int amountPence, out List<int> plan)
    {
        plan = new List<int>();
        amountPence = Mathf.Max(0, amountPence);

        if (amountPence == 0)
            return true;

        if (valuesDescending == null || valuesDescending.Length == 0)
            return false;

        int remaining = amountPence;

        for (int i = 0; i < valuesDescending.Length; i++)
        {
            int value = valuesDescending[i];
            int available = availableCounts.TryGetValue(value, out int count) ? count : 0;

            while (available > 0 && remaining >= value)
            {
                remaining -= value;
                available--;
                plan.Add(value);
            }
        }

        return remaining == 0;
    }

    private string GetLabel(int valuePence)
    {
        return fareTable != null ? fareTable.GetLabelForValue(valuePence) : FareTable.FormatMoney(valuePence);
    }
}
