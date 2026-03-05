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

    public static Color GetRankColor(string rank)
    {
        switch (rank)
        {
            case "SV": return new Color(1f, 0.84f, 0f);
            case "SSS": return new Color(1f, 0.9f, 0.5f);
            case "SS": return new Color(1f, 1f, 0.6f);
            case "S": return new Color(1f, 0.5f, 0.5f);
            case "A+": return new Color(1f, 0.3f, 0.3f);
            case "A": return new Color(1f, 0.4f, 0.2f);
            case "B": return new Color(0.2f, 0.6f, 1f);
            case "C": return new Color(0.5f, 0.5f, 0.5f);
            case "L": return new Color(0.3f, 0.3f, 0.3f);
            default: return Color.white;
        }
    }
}

public static class DifficultyUtility
{
    public static Color GetDifficultyColor(DifficultyType type)
    {
        switch (type)
        {
            case DifficultyType.Easy: return new Color(0.2f, 0.9f, 0.3f);
            case DifficultyType.Medium: return new Color(1f, 0.9f, 0.2f);
            case DifficultyType.Hard: return new Color(1f, 0.5f, 0.1f);
            case DifficultyType.Insane: return new Color(1f, 0.2f, 0.2f);
            case DifficultyType.Master: return new Color(0.7f, 0.3f, 1f);
            case DifficultyType.Del: return Color.white;
            default: return Color.white;
        }
    }

    public static string GetDifficultyName(DifficultyType type)
    {
        switch (type)
        {
            case DifficultyType.Easy: return "EASY";
            case DifficultyType.Medium: return "MEDIUM";
            case DifficultyType.Hard: return "HARD";
            case DifficultyType.Insane: return "INSANE";
            case DifficultyType.Master: return "MASTER";
            case DifficultyType.Del: return "DEL";
            default: return "WHEREISIT";
        }
    }

    public static string FormatLevel(int level, float constant)
    {
        if (constant > level)
            return level + "+";
        return level.ToString();
    }
}