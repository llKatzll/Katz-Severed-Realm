using UnityEngine;

public static class ScoreRecord
{
    private const string PREFIX = "katz_record_";

    private static string ScoreKey(string song, DifficultyType diff) => PREFIX + song + "_" + diff + "_score";
    private static string AccKey(string song, DifficultyType diff) => PREFIX + song + "_" + diff + "_acc";
    private static string RankKey(string song, DifficultyType diff) => PREFIX + song + "_" + diff + "_rank";
    private static string TotalKey(string song, DifficultyType diff) => PREFIX + song + "_" + diff + "_total";

    public static int GetHighScore(string song, DifficultyType diff)
    {
        return PlayerPrefs.GetInt(ScoreKey(song, diff), 0);
    }

    public static float GetAccuracy(string song, DifficultyType diff)
    {
        return PlayerPrefs.GetFloat(AccKey(song, diff), 0f);
    }

    public static string GetRank(string song, DifficultyType diff)
    {
        return PlayerPrefs.GetString(RankKey(song, diff), "L");
    }

    public static int GetTotalNoteCount(string song, DifficultyType diff)
    {
        return PlayerPrefs.GetInt(TotalKey(song, diff), 0);
    }

    public static bool HasRecord(string song, DifficultyType diff)
    {
        return PlayerPrefs.HasKey(ScoreKey(song, diff));
    }

    public static bool SaveIfBetter(string song, DifficultyType diff, int score, float accuracy, int totalNotes)
    {
        int oldScore = GetHighScore(song, diff);
        if (score <= oldScore) return false;

        string rank = RankUtility.GetRank(score, totalNotes);

        PlayerPrefs.SetInt(ScoreKey(song, diff), score);
        PlayerPrefs.SetFloat(AccKey(song, diff), accuracy);
        PlayerPrefs.SetString(RankKey(song, diff), rank);
        PlayerPrefs.SetInt(TotalKey(song, diff), totalNotes);
        PlayerPrefs.Save();
        return true;
    }
}
