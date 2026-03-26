using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public sealed class BusFareUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text currentStopText;
    [SerializeField] private TMP_Text destinationText;
    [SerializeField] private TMP_Text paymentTypeText;
    [SerializeField] private TMP_Text fareRequiredText;
    [SerializeField] private TMP_Text tenderedText;
    [SerializeField] private TMP_Text changeDueText;
    [SerializeField] private TMP_Text selectedChangeText;
    [SerializeField] private TMP_Text ticketStatusText;
    [SerializeField] private TMP_Text walletText;
    [SerializeField] private TMP_Text fareChartText;

    private Passenger currentPassenger;
    private DriverWallet currentWallet;
    private FareTable currentFareTable;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        Hide();
    }

    private void Update()
    {
        if (currentPassenger != null && root != null && root.activeSelf)
            Refresh(null);
    }

    public void Show(Passenger passenger, DriverWallet wallet, FareTable fareTable)
    {
        currentPassenger = passenger;
        currentWallet = wallet;
        currentFareTable = fareTable;

        if (root != null)
            root.SetActive(true);

        Refresh(null);
    }

    public void Refresh(IReadOnlyList<int> selectedChange)
    {
        if (currentPassenger == null)
            return;

        RouteStops route = RouteStops.Instance;

        if (currentStopText != null)
        {
            string stopName = route != null ? route.GetStopNameSafe(route.NextStopIndex) : "Unknown Stop";
            currentStopText.text = $"Current Stop: {stopName}";
        }

        if (destinationText != null)
            destinationText.text = $"Destination: {currentPassenger.GetPublicDestinationName()}";

        if (paymentTypeText != null)
            paymentTypeText.text = $"Payment: {currentPassenger.GetPaymentShortLabel()}";

        if (fareRequiredText != null)
            fareRequiredText.text = $"Fare: {FareTable.FormatMoney(currentPassenger.ExpectedFare)}";

        if (tenderedText != null)
            tenderedText.text = currentPassenger.BuildTenderSummary();

        if (changeDueText != null)
            changeDueText.text = $"Correct Change: {FareTable.FormatMoney(currentPassenger.ChangeDuePence)}";

        if (ticketStatusText != null)
            ticketStatusText.text = currentPassenger.BuildTicketStatusSummary();

        if (selectedChangeText != null)
        {
            int selectedTotal = Sum(selectedChange);
            selectedChangeText.text = BuildSelectedChangeText(selectedChange, selectedTotal);
        }

        if (walletText != null)
            walletText.text = currentWallet != null ? currentWallet.BuildWalletSummary() : "No wallet assigned.";

        if (fareChartText != null)
            fareChartText.text = currentFareTable != null ? currentFareTable.BuildFareChartText() : string.Empty;
    }

    public void Hide()
    {
        currentPassenger = null;

        if (currentStopText != null) currentStopText.text = string.Empty;
        if (destinationText != null) destinationText.text = string.Empty;
        if (paymentTypeText != null) paymentTypeText.text = string.Empty;
        if (fareRequiredText != null) fareRequiredText.text = string.Empty;
        if (tenderedText != null) tenderedText.text = string.Empty;
        if (changeDueText != null) changeDueText.text = string.Empty;
        if (selectedChangeText != null) selectedChangeText.text = string.Empty;
        if (ticketStatusText != null) ticketStatusText.text = string.Empty;
        if (walletText != null) walletText.text = string.Empty;
        if (fareChartText != null) fareChartText.text = string.Empty;

        if (root != null)
            root.SetActive(false);
    }

    private string BuildSelectedChangeText(IReadOnlyList<int> selectedChange, int selectedTotal)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Selected Change: {FareTable.FormatMoney(selectedTotal)}");

        if (selectedChange == null || selectedChange.Count == 0)
        {
            sb.Append("Nothing selected.");
            return sb.ToString();
        }

        for (int i = 0; i < selectedChange.Count; i++)
        {
            int value = selectedChange[i];
            sb.AppendLine(currentFareTable != null ? currentFareTable.GetLabelForValue(value) : FareTable.FormatMoney(value));
        }

        return sb.ToString().TrimEnd();
    }

    private static int Sum(IReadOnlyList<int> values)
    {
        if (values == null)
            return 0;

        int total = 0;
        for (int i = 0; i < values.Count; i++)
            total += Mathf.Max(0, values[i]);
        return total;
    }
}
