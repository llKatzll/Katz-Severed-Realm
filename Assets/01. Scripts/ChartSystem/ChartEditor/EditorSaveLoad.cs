using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EditorSaveLoad : MonoBehaviour
{
    [SerializeField] private EditorChart _chart;
    [SerializeField] private EditorLoadSong _loadSong;

    private void Awake()
    {
        if (_chart == null) _chart = GetComponent<EditorChart>();
        if (_loadSong == null) _loadSong = GetComponent<EditorLoadSong>();
    }

    public bool Save()
    {
        if (_chart == null) return false;
        var data = _chart.Chart;
        if (data == null) return false;

        if (string.IsNullOrEmpty(data.songName) || string.IsNullOrEmpty(data.difficulty))
        {
            Debug.LogWarning("[EditorSaveLoad] songName or difficulty is empty");
            return false;
        }

        string path = ChartUtility.GetChartPath(data.songName, data.difficulty);
        bool ok = ChartUtility.SaveToFile(data, path);
        Debug.Log("[EditorSaveLoad] Save " + (ok ? "OK" : "FAIL") + ": " + path);
        return ok;
    }

    public void OpenLoadDialog()
    {
#if UNITY_EDITOR
        string startDir = ChartUtility.GetChartDirectory();
        string path = EditorUtility.OpenFilePanel("Select Chart JSON", startDir, "json");
        if (string.IsNullOrEmpty(path)) return;

        var data = ChartUtility.LoadFromFile(path);
        if (data == null)
        {
            Debug.LogWarning("[EditorSaveLoad] Failed to load: " + path);
            return;
        }

        if (_loadSong != null && !string.IsNullOrEmpty(data.songName))
        {
            var song = FindSongDataByName(data.songName);
            if (song != null)
            {
                if (System.Enum.TryParse<DifficultyType>(data.difficulty, out var diff))
                {
                    _loadSong.CurrentDifficulty = diff;
                }
                _loadSong.ApplySongOnly(song);
            }
        }

        if (_chart != null) _chart.LoadChart(data);
        Debug.Log("[EditorSaveLoad] Loaded: " + path);
#else
        Debug.LogWarning("[EditorSaveLoad] LoadDialog is only available in Unity Editor mode.");
#endif
    }

#if UNITY_EDITOR
    private SongData FindSongDataByName(string songName)
    {
        var guids = AssetDatabase.FindAssets("t:SongData");
        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var sd = AssetDatabase.LoadAssetAtPath<SongData>(assetPath);
            if (sd != null && sd.songName == songName) return sd;
        }
        return null;
    }
#endif
}
