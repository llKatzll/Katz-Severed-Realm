using UnityEngine;

public static class AutoPlay
{
    public static bool IsOn = false;

    public static float CleanChance = 0.17f;
    public static float TraceChance = 0.03f;

    public static JudgeType RollJudge()
    {
        float r = Random.value;
        if (r < TraceChance) return JudgeType.Trace;
        if (r < TraceChance + CleanChance) return JudgeType.Clean;
        return JudgeType.Severance;
    }
}
