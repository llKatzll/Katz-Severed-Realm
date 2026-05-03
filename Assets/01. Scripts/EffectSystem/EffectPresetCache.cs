using System.Collections.Generic;
using UnityEngine;

public static class EffectPresetCache
{
    private static Dictionary<string, EffectPresetSO> _cache;
    private const string DEFAULT_PATH = "EffectPresets";

    public static void LoadAll(string resourcesPath = DEFAULT_PATH)
    {
        var loaded = Resources.LoadAll<EffectPresetSO>(resourcesPath);
        _cache = new Dictionary<string, EffectPresetSO>(loaded.Length);
        for (int i = 0; i < loaded.Length; i++)
        {
            var p = loaded[i];
            if (p == null || string.IsNullOrEmpty(p.presetId)) continue;
            _cache[p.presetId] = p;
        }
    }

    public static EffectPresetSO Get(string presetId)
    {
        if (_cache == null) LoadAll();
        if (string.IsNullOrEmpty(presetId)) return null;
        EffectPresetSO p;
        return _cache.TryGetValue(presetId, out p) ? p : null;
    }

    public static IEnumerable<EffectPresetSO> AllByCategory(EffectCategory cat)
    {
        if (_cache == null) LoadAll();
        foreach (var kv in _cache)
        {
            if (kv.Value != null && kv.Value.category == cat)
                yield return kv.Value;
        }
    }
}
