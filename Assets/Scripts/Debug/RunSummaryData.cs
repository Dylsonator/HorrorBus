using System.Collections.Generic;

public static class RunSummaryData
{
    public static int ScorePoints;
    public static int ScoreMoneyPence;
    public static int DeskMoneyPence;
    public static int TotalMoneyPence;
    public static int BestTotalMoneyPence;
    public static bool IsNewHighScore;

    public static List<InspectionDeskItemState> DeskItems = new List<InspectionDeskItemState>();

    public static void Clear()
    {
        ScorePoints = 0;
        ScoreMoneyPence = 0;
        DeskMoneyPence = 0;
        TotalMoneyPence = 0;
        BestTotalMoneyPence = 0;
        IsNewHighScore = false;
        DeskItems.Clear();
    }
}