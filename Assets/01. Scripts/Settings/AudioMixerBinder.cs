using UnityEngine;
using UnityEngine.Audio;

public static class AudioMixerBinder
{
    private const string MixerResourcePath = "AudioMixer/MainMixer";

    private const string ParamMaster = "MasterVolume";
    private const string ParamMusic = "MusicVolume";
    private const string ParamSfx = "SfxVolume";
    private const string ParamHit = "HitVolume";

    private const float MinDb = -80f;

    private static AudioMixer _mixer;

    public static AudioMixer Mixer => _mixer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        _mixer = Resources.Load<AudioMixer>(MixerResourcePath);
        if (_mixer == null)
        {
            Debug.LogError("[AudioMixerBinder] MainMixer not found in Resources/. Place MainMixer.mixer under Assets/Resources/.");
            return;
        }

        ApplyAll();
        SettingsConfig.OnChanged -= OnSettingsChanged;
        SettingsConfig.OnChanged += OnSettingsChanged;
    }

    private static void OnSettingsChanged(SettingsConfig.Category cat)
    {
        if (cat == SettingsConfig.Category.Audio) ApplyAll();
    }

    private static void ApplyAll()
    {
        if (_mixer == null) return;
        SetLinear(ParamMaster, SettingsConfig.MasterVolume);
        SetLinear(ParamMusic, SettingsConfig.MusicVolume);
        SetLinear(ParamSfx, SettingsConfig.SfxVolume);
        SetLinear(ParamHit, SettingsConfig.HitVolume);
    }

    private static void SetLinear(string param, float linear01)
    {
        float db = linear01 <= 0.0001f ? MinDb : Mathf.Log10(linear01) * 20f;
        _mixer.SetFloat(param, db);
    }

    public static AudioMixerGroup GetGroup(string groupName)
    {
        if (_mixer == null) return null;
        var groups = _mixer.FindMatchingGroups(groupName);
        return (groups != null && groups.Length > 0) ? groups[0] : null;
    }
}
