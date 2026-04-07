using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EndOfDayManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RouteStops routeStops;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private InspectionDeskUI deskUI;

    [Header("Scene")]
    [SerializeField] private string endSummarySceneName = "EndSummary";

    [Header("Score Conversion")]
    [SerializeField] private int scorePointValuePence = 100; // 1 point = £1.00

    [Header("Desk Item Values")]
    [SerializeField] private int idCardValuePence = 100;
    [SerializeField] private int ticketValuePence = 150;
    [SerializeField] private int clutterValuePence = 25;
    [SerializeField] private int noteValuePence = 75;
    [SerializeField] private int evidenceValuePence = 200;

    private void Awake()
    {
        if (routeStops == null)
            routeStops = FindFirstObjectByType<RouteStops>();

        if (scoreManager == null)
            scoreManager = FindFirstObjectByType<ScoreManager>();

        if (deskUI == null)
            deskUI = FindFirstObjectByType<InspectionDeskUI>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        if (routeStops != null)
            routeStops.FinalStopDeparted += HandleFinalStopDeparted;
    }

    private void OnDisable()
    {
        if (routeStops != null)
            routeStops.FinalStopDeparted -= HandleFinalStopDeparted;
    }

    private void HandleFinalStopDeparted()
    {
        BuildRunSummary();
        SceneManager.LoadScene(endSummarySceneName);
    }

    private void BuildRunSummary()
    {
        RunSummaryData.Clear();

        int scorePoints = scoreManager != null ? scoreManager.Score : 0;
        int scoreMoneyPence = Mathf.Max(0, scorePoints * Mathf.Max(0, scorePointValuePence));

        List<InspectionDeskItemState> items = deskUI != null
            ? deskUI.GetDeskItemsSnapshot()
            : new List<InspectionDeskItemState>();

        int deskMoneyPence = 0;

        for (int i = 0; i < items.Count; i++)
        {
            InspectionDeskItemState item = items[i];
            if (item == null)
                continue;

            deskMoneyPence += GetItemValuePence(item);
        }

        int totalMoneyPence = scoreMoneyPence + deskMoneyPence;
        bool isNewHighScore = HighScoreSave.TrySetBestTotalMoneyPence(totalMoneyPence);
        int bestMoneyPence = HighScoreSave.GetBestTotalMoneyPence();

        RunSummaryData.ScorePoints = scorePoints;
        RunSummaryData.ScoreMoneyPence = scoreMoneyPence;
        RunSummaryData.DeskMoneyPence = deskMoneyPence;
        RunSummaryData.TotalMoneyPence = totalMoneyPence;
        RunSummaryData.BestTotalMoneyPence = bestMoneyPence;
        RunSummaryData.IsNewHighScore = isNewHighScore;
        RunSummaryData.DeskItems = items;
    }

    private int GetItemValuePence(InspectionDeskItemState item)
    {
        if (item == null)
            return 0;

        return item.kind switch
        {
            InspectionDeskItemKind.Cash => Mathf.Max(0, item.moneyValuePence),
            InspectionDeskItemKind.IdCard => idCardValuePence,
            InspectionDeskItemKind.Ticket => ticketValuePence,
            InspectionDeskItemKind.Clutter => clutterValuePence,
            InspectionDeskItemKind.Note => noteValuePence,
            InspectionDeskItemKind.Evidence => evidenceValuePence,
            _ => 0
        };
    }
}