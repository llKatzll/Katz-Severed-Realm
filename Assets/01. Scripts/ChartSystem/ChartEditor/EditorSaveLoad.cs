using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EditorSaveLoad : MonoBehaviour
{
    [SerializeField] private EditorChart _chart;
    [SerializeField] private EditorLoadSong _loadSong;
    [SerializeField] private EffectSaveLoad _effectSaveLoad;
    [SerializeField] private EditorUI _editorUI;
    [SerializeField] private GameObject _effectModeRoot;

    private void Awake()
    {
        if (_chart == null) _chart = GetComponent<EditorChart>();
        if (_loadSong == null) _loadSong = GetComponent<EditorLoadSong>();
        if (_effectSaveLoad == null) _effectSaveLoad = FindAnyObjectByType<EffectSaveLoad>();
        if (_editorUI == null) _editorUI = GetComponent<EditorUI>();
    }

    private bool IsEffectMode()
    {
        return _effectModeRoot != null && _effectModeRoot.activeInHierarchy;
    }

    public void Save()
    {
        if (IsEffectMode())
        {
            if (_effectSaveLoad != null) _effectSaveLoad.Save();
            return;
        }

        if (_chart == null) return;
        var data = _chart.Chart;
        if (data == null) return;

        if (string.IsNullOrEmpty(data.songName) || string.IsNullOrEmpty(data.difficulty))
        {
            Debug.LogWarning("[EditorSaveLoad] songName or difficulty is empty");
            return;
        }

        string path = ChartUtility.GetChartPath(data.songName, data.difficulty);
        bool ok = ChartUtility.SaveToFile(data, path);
        Debug.Log("[EditorSaveLoad] Save " + (ok ? "OK" : "FAIL") + ": " + path);

        if (ok) RegisterDifficultyToSongData();
    }

    private void RegisterDifficultyToSongData()
    {
#if UNITY_EDITOR
        if (_loadSong == null || _loadSong.CurrentSong == null) return;

        var song = _loadSong.CurrentSong;
        var diff = _loadSong.CurrentDifficulty;

        if (song.HasDifficulty(diff)) return;

        var list = new System.Collections.Generic.List<DifficultyData>();
        if (song.difficulties != null) list.AddRange(song.difficulties);
        list.Add(new DifficultyData { type = diff, level = 1, constant = 0f });
        song.difficulties = list.ToArray();

        EditorUtility.SetDirty(song);
        AssetDatabase.SaveAssets();
        Debug.Log("[EditorSaveLoad] Registered " + diff + " to SongData '" + song.songName + "'");
#endif
    }

    public void OpenLoadDialog()
    {
        if (IsEffectMode())
        {
            if (_effectSaveLoad != null) _effectSaveLoad.OpenLoadDialog();
            return;
        }

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
        if (_editorUI != null) _editorUI.RefreshDisplay();
        Debug.Log("[EditorSaveLoad] Loaded: " + path);
#else
        if (_loadSong != null)
        {
            _loadSong.ReloadChartForCurrentSongDifficulty();
            Debug.Log("[EditorSaveLoad] Reloaded current chart");
        }
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
