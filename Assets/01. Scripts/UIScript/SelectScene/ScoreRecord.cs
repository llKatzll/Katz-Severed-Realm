using UnityEngine;

public static class ScoreRecord
{
    private const string PREFIX = "katz_record_";

    private static string Suffix(bool anomaly) => anomaly ? "" : "_noanom";

    private static string ScoreKey(string song, DifficultyType diff, bool anomaly) => PREFIX + song + "_" + diff + Suffix(anomaly) + "_score";
    private static string AccKey(string song, DifficultyType diff, bool anomaly) => PREFIX + song + "_" + diff + Suffix(anomaly) + "_acc";
    private static string RankKey(string song, DifficultyType diff, bool anomaly) => PREFIX + song + "_" + diff + Suffix(anomaly) + "_rank";
    private static string TotalKey(string song, DifficultyType diff, bool anomaly) => PREFIX + song + "_" + diff + Suffix(anomaly) + "_total";

    public static int GetHighScore(string song, DifficultyType diff, bool anomaly)
    {
        return PlayerPrefs.GetInt(ScoreKey(song, diff, anomaly), 0);
    }

    public static float GetAccuracy(string song, DifficultyType diff, bool anomaly)
    {
        return PlayerPrefs.GetFloat(AccKey(song, diff, anomaly), 0f);
    }

    public static string GetRank(string song, DifficultyType diff, bool anomaly)
    {
        return PlayerPrefs.GetString(RankKey(song, diff, anomaly), "L");
    }

    public static int GetTotalNoteCount(string song, DifficultyType diff, bool anomaly)
    {
        return PlayerPrefs.GetInt(TotalKey(song, diff, anomaly), 0);
    }

    public static bool HasRecord(string song, DifficultyType diff, bool anomaly)
    {
        return PlayerPrefs.HasKey(ScoreKey(song, diff, anomaly));
    }

    public static bool SaveIfBetter(string song, DifficultyType diff, bool anomaly, int score, float accuracy, int totalNotes)
    {
        int oldScore = GetHighScore(song, diff, anomaly);
        if (score <= oldScore) return false;

        string rank = RankUtility.GetRank(score, totalNotes);

        PlayerPrefs.SetInt(ScoreKey(song, diff, anomaly), score);
        PlayerPrefs.SetFloat(AccKey(song, diff, anomaly), accuracy);
        PlayerPrefs.SetString(RankKey(song, diff, anomaly), rank);
        PlayerPrefs.SetInt(TotalKey(song, diff, anomaly), totalNotes);
        PlayerPrefs.Save();
        return true;
    }
}
