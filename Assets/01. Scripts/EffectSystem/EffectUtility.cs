using System.IO;
using UnityEngine;

public static class EffectUtility
{
    public static string ToJson(EffectData data) => JsonUtility.ToJson(data, true);

    public static EffectData FromJson(string json)
        => string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<EffectData>(json);

    public static bool SaveToFile(EffectData data, string filePath)
    {
        try
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            data.SortByBeat();
            File.WriteAllText(filePath, ToJson(data));
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[EffectUtility] Save failed: " + e.Message);
            return false;
        }
    }

    public static EffectData LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            string json = File.ReadAllText(filePath);
            EffectData data = FromJson(json);
            if (data != null) data.SortByBeat();
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[EffectUtility] Load failed: " + e.Message);
            return null;
        }
    }

    public static string GetEffectDirectory()
        => Path.Combine(Application.streamingAssetsPath, "Effects");

    public static string GetEffectPath(string songName, string difficulty)
        => Path.Combine(GetEffectDirectory(), songName + "_" + difficulty + ".eff.json");
}
