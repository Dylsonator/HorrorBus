using UnityEngine;

public static class HighScoreSave
{
    private const string BestTotalMoneyPenceKey = "BestTotalMoneyPence";

    public static int GetBestTotalMoneyPence()
    {
        return PlayerPrefs.GetInt(BestTotalMoneyPenceKey, 0);
    }

    public static bool TrySetBestTotalMoneyPence(int valuePence)
    {
        int safeValue = Mathf.Max(0, valuePence);
        int currentBest = GetBestTotalMoneyPence();

        if (safeValue <= currentBest)
            return false;

        PlayerPrefs.SetInt(BestTotalMoneyPenceKey, safeValue);
        PlayerPrefs.Save();
        return true;
    }

    public static void ClearBestTotalMoneyPence()
    {
        PlayerPrefs.DeleteKey(BestTotalMoneyPenceKey);
        PlayerPrefs.Save();
    }
}