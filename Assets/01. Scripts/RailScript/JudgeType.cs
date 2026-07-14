using UnityEngine;

public enum JudgeType
{
    Severance,
    Clean,
    Trace,
    Fracture,
    Ruin,
    Miss
}

public static class JudgeTypeExtensions
{
    private static readonly string[] Names =
    {
        "Severance",
        "Clean",
        "Trace",
        "Fracture",
        "Ruin",
        "Miss"
    };

    public static string ToLabel(this JudgeType judge)
    {
        int i = (int)judge;
        return (i >= 0 && i < Names.Length) ? Names[i] : judge.ToString();
    }
}

