using UnityEngine;
using System.IO;

public static class ChartUtility
{
    public static string ToJson(ChartData data)
    {
        return JsonUtility.ToJson(data, true);
    }

    public static ChartData FromJson(string json)
    {
        return JsonUtility.FromJson<ChartData>(json);
    }

    public static void SaveToFile(ChartData data, string filePath)
    {
        string dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string json = ToJson(data);
        File.WriteAllText(filePath, json);
        Debug.Log("[ChartUtility] Saved: " + filePath);
    }

    public static ChartData LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogWarning("[ChartUtility] File not found: " + filePath);
            return null;
        }

        string json = File.ReadAllText(filePath);
        return FromJson(json);
    }

    public static string GetChartDirectory()
    {
        return Path.Combine(Application.dataPath, "Charts");
    }

    public static string GetChartPath(string songName, string difficulty)
    {
        string fileName = songName + "_" + difficulty + ".json";
        return Path.Combine(GetChartDirectory(), fileName);
    }
}
