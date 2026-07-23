using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EffectSaveLoad : MonoBehaviour
{
    [SerializeField] private EffectChart _chart;
    [SerializeField] private EditorLoadSong _loadSong;

    private void Awake()
    {
        if (_chart == null) _chart = GetComponent<EffectChart>();
    }

    private void Start()
    {
        ReloadCurrent();
    }

    public void Save()
    {
        if (_chart == null) return;
        if (_loadSong == null || _loadSong.CurrentSong == null)
        {
            Debug.LogWarning("[EffectSaveLoad] No song loaded");
            return;
        }

        var data = _chart.Data;
        if (data == null || data.triggers == null)
        {
            Debug.LogWarning("[EffectSaveLoad] No effect data");
            return;
        }

        string songName = _loadSong.CurrentSong.songName;
        string difficulty = _loadSong.CurrentDifficulty.ToString();

        data.songName = songName;
        data.difficulty = difficulty;

        string path = EffectUtility.GetEffectPath(songName, difficulty);
        bool ok = EffectUtility.SaveToFile(data, path);
        Debug.Log("[EffectSaveLoad] Save " + (ok ? "OK" : "FAIL") + ": " + path);
    }

    public void OpenLoadDialog()
    {
#if UNITY_EDITOR
        string startDir = EffectUtility.GetEffectDirectory();
        string path = EditorUtility.OpenFilePanel("Select Effect JSON", startDir, "json");
        if (string.IsNullOrEmpty(path)) return;

        var data = EffectUtility.LoadFromFile(path);
        if (data == null)
        {
            Debug.LogWarning("[EffectSaveLoad] Failed to load: " + path);
            return;
        }

        if (_chart != null) _chart.LoadData(data);
        Debug.Log("[EffectSaveLoad] Loaded: " + path);
#else
        ReloadCurrent();
#endif
    }

    public void ReloadCurrent()
    {
        if (_chart == null) return;
        if (_loadSong == null || _loadSong.CurrentSong == null) return;

        string songName = _loadSong.CurrentSong.songName;
        string difficulty = _loadSong.CurrentDifficulty.ToString();
        string path = EffectUtility.GetEffectPath(songName, difficulty);

        var data = EffectUtility.LoadFromFile(path);
        if (data != null)
        {
            _chart.LoadData(data);
        }
        else
        {
            _chart.NewData(songName, difficulty);
        }
    }
}
