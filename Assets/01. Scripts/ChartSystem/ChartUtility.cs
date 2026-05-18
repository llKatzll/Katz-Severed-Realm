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
        if (string.IsNullOrEmpty(json)) return null;
        return JsonUtility.FromJson<ChartData>(json);
    }

    public static bool SaveToFile(ChartData data, string filePath)
    {
        try
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            data.SortAll();
            File.WriteAllText(filePath, ToJson(data));
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[ChartUtility] Save failed: " + e.Message);
            return false;
        }
    }

    public static ChartData LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            string json = File.ReadAllText(filePath);
            ChartData data = FromJson(json);
            if (data != null) data.SortAll();
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[ChartUtility] Load failed: " + e.Message);
            return null;
        }
    }

    public static string GetChartDirectory()
    {
        return Path.Combine(Application.streamingAssetsPath, "Charts");
    }

    public static string GetChartPath(string songName, string difficulty)
    {
        return Path.Combine(GetChartDirectory(), songName + "_" + difficulty + ".json");
    }
}
