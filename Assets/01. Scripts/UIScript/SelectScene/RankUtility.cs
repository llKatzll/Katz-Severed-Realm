using UnityEngine;

public static class RankUtility
{
    public static string GetRank(int score, int maxNotes)
    {
        int theoreticalMax = 10000000 + maxNotes;

        if (score >= theoreticalMax)
            return "SV";
        if (score >= 10000000 + 1)
            return "SSS";
        if (score >= 9900000)
            return "SS";
        if (score >= 9800000)
            return "S";
        if (score >= 9500000)
            return "A+";
        if (score >= 9200000)
            return "A";
        if (score >= 8900000)
            return "B";
        if (score >= 8600000)
            return "C";

        return "L";
    }

    public static Color GetRankColor(string rank) => rank switch
    {
        "SV" => new Color(1f, 0.84f, 0f),
        "SSS" => new Color(1f, 0.9f, 0.5f),
        "SS" => new Color(1f, 1f, 0.6f),
        "S" => new Color(1f, 0.5f, 0.5f),
        "A+" => new Color(1f, 0.3f, 0.3f),
        "A" => new Color(1f, 0.4f, 0.2f),
        "B" => new Color(0.2f, 0.6f, 1f),
        "C" => new Color(0.5f, 0.5f, 0.5f),
        "L" => new Color(0.3f, 0.3f, 0.3f),
        _ => Color.white,
    };
}

public static class DifficultyUtility
{
    public static Color GetDifficultyColor(DifficultyType type) => type switch
    {
        DifficultyType.Easy => new Color(0.2f, 0.9f, 0.3f),
        DifficultyType.Medium => new Color(1f, 0.9f, 0.2f),
        DifficultyType.Hard => new Color(1f, 0.5f, 0.1f),
        DifficultyType.Insane => new Color(1f, 0.2f, 0.2f),
        DifficultyType.Master => new Color(0.7f, 0.3f, 1f),
        DifficultyType.Del => Color.white,
        _ => Color.white,
    };

    public static string GetDifficultyName(DifficultyType type) => type switch
    {
        DifficultyType.Easy => "EASY",
        DifficultyType.Medium => "MEDIUM",
        DifficultyType.Hard => "HARD",
        DifficultyType.Insane => "INSANE",
        DifficultyType.Master => "MASTER",
        DifficultyType.Del => "DEL",
        _ => "WHEREISIT",
    };

    public static string FormatLevel(int level, float constant)
        => constant > level ? level + "+" : level.ToString();
}