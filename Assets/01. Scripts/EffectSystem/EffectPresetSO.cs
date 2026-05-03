using UnityEngine;

[CreateAssetMenu(menuName = "Katz/Effects/Effect Preset", fileName = "NewEffectPreset")]
public class EffectPresetSO : ScriptableObject
{
    [Header("Identity")]
    public string presetId;
    public string displayName;
    public EffectCategory category;
    public TriggerType triggerType;
    public float defaultDurationSec = 1f;

    [Header("Eff (Burst / OnOff)")]
    public GameObject particlePrefab;
    public Vector3 spawnOffset;

    [Header("Cam / Rail (Animation)")]
    public AnimationClip animationClip;
    public string targetAnimatorPath;

    [Header("Scr")]
    public ScrEffectType scrType;
    public float scrIntensityFrom;
    public float scrIntensityTo = 1f;
    public Color scrColor = Color.white;
    public AnimationCurve scrCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
}
