using UnityEngine;

[CreateAssetMenu(menuName = "Rhythm/Hit FX Palette", fileName = "HitFxPalette")]
public class HitFxPaletteSO : ScriptableObject
{
    public GameObject hitFxPrefab;
    public float fxDestroySec = 0.3f;

    public Color ground_Sev;
    public Color ground_Clea;
    public Color ground_Trs;
    public Color ground_Frac;
    public Color ground_Ru;

    public Color upper_Sev;
    public Color upper_Clea;
    public Color upper_Trs;
    public Color upper_Frac;
    public Color upper_Ru;

    [ColorUsage(true, true)] public Color ground_HoldFail;
    [ColorUsage(true, true)] public Color upper_HoldFail;

    public bool TryGetColor(NoteSpawner.NoteType laneType, JudgeType judge, out Color c)
    {
        c = Color.white;

        if (judge == JudgeType.Miss) return false;

        if (laneType == NoteSpawner.NoteType.Ground)
        {
            if (judge == JudgeType.Severance) c = ground_Sev;
            else if (judge == JudgeType.Clean) c = ground_Clea;
            else if (judge == JudgeType.Trace) c = ground_Trs;
            else if (judge == JudgeType.Fracture) c = ground_Frac;
            else if (judge == JudgeType.Ruin) c = ground_Ru;
            return true;
        }

        if (laneType == NoteSpawner.NoteType.Upper)
        {
            if (judge == JudgeType.Severance) c = upper_Sev;
            else if (judge == JudgeType.Clean) c = upper_Clea;
            else if (judge == JudgeType.Trace) c = upper_Trs;
            else if (judge == JudgeType.Fracture) c = upper_Frac;
            else if (judge == JudgeType.Ruin) c = upper_Ru;
            return true;
        }

        return false;
    }

    public bool TryGetHoldFailColor(NoteSpawner.NoteType laneType, out Color c)
    {
        c = Color.white;
        if (laneType == NoteSpawner.NoteType.Ground) { c = ground_HoldFail; return true; }
        if (laneType == NoteSpawner.NoteType.Upper) { c = upper_HoldFail; return true; }
        return false;
    }
}
