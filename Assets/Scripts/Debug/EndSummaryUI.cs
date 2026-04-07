using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EndSummaryUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text scorePointsText;
    [SerializeField] private TMP_Text scoreMoneyText;
    [SerializeField] private TMP_Text deskMoneyText;
    [SerializeField] private TMP_Text totalMoneyText;
    [SerializeField] private TMP_Text bestMoneyText;
    [SerializeField] private TMP_Text newHighScoreText;
    [SerializeField] private TMP_Text deskItemsText;

    [Header("Scene Names")]
    [SerializeField] private string gameplaySceneName = "Game";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Refresh();
    }

    public void Refresh()
    {
        if (scorePointsText != null)
            scorePointsText.text = $"Score: {RunSummaryData.ScorePoints}";

        if (scoreMoneyText != null)
            scoreMoneyText.text = $"Score Money: £{FormatMoney(RunSummaryData.ScoreMoneyPence)}";

        if (deskMoneyText != null)
            deskMoneyText.text = $"Desk Value: £{FormatMoney(RunSummaryData.DeskMoneyPence)}";

        if (totalMoneyText != null)
            totalMoneyText.text = $"Total: £{FormatMoney(RunSummaryData.TotalMoneyPence)}";

        if (bestMoneyText != null)
            bestMoneyText.text = $"Best Total: £{FormatMoney(RunSummaryData.BestTotalMoneyPence)}";

        if (newHighScoreText != null)
        {
            newHighScoreText.gameObject.SetActive(RunSummaryData.IsNewHighScore);
            newHighScoreText.text = RunSummaryData.IsNewHighScore ? "NEW HIGH SCORE!" : string.Empty;
        }

        if (deskItemsText != null)
            deskItemsText.text = BuildDeskItemsBreakdown();
    }

    public void RestartRun()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void ClearSavedHighScore()
    {
        HighScoreSave.ClearBestTotalMoneyPence();
        RunSummaryData.BestTotalMoneyPence = 0;
        RunSummaryData.IsNewHighScore = false;
        Refresh();
    }

    private string BuildDeskItemsBreakdown()
    {
        if (RunSummaryData.DeskItems == null || RunSummaryData.DeskItems.Count == 0)
            return "Desk Items: None";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Desk Items:");

        for (int i = 0; i < RunSummaryData.DeskItems.Count; i++)
        {
            InspectionDeskItemState item = RunSummaryData.DeskItems[i];
            if (item == null)
                continue;

            string label = !string.IsNullOrWhiteSpace(item.title)
                ? item.title
                : item.kind.ToString();

            if (item.kind == InspectionDeskItemKind.Cash)
                sb.AppendLine($"- {label} (£{FormatMoney(item.moneyValuePence)})");
            else
                sb.AppendLine($"- {label}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatMoney(int pence)
    {
        int safe = Mathf.Max(0, pence);
        return (safe / 100f).ToString("0.00");
    }
}