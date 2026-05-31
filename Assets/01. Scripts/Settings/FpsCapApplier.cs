using UnityEngine;

public static class FpsCapApplier
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        Apply();
        SettingsConfig.OnChanged -= OnSettingsChanged;
        SettingsConfig.OnChanged += OnSettingsChanged;
    }

    private static void OnSettingsChanged(SettingsConfig.Category cat)
    {
        if (cat == SettingsConfig.Category.Display) Apply();
    }

    private static void Apply()
    {
        QualitySettings.vSyncCount = 0;
        int cap = SettingsConfig.FpsCap;
        Application.targetFrameRate = cap > 0 ? cap : -1;
    }
}
