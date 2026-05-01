using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EditorLoadSong : MonoBehaviour
{
    [SerializeField] private EditorChart _chart;
    [SerializeField] private EditorPlayback _playback;
    [SerializeField] private DifficultyType _currentDifficulty = DifficultyType.Easy;

    [Header("Default Song (Auto Load on Start)")]
    [SerializeField] private SongData _defaultSong;

    public DifficultyType CurrentDifficulty
    {
        get => _currentDifficulty;
        set => _currentDifficulty = value;
    }

    public SongData CurrentSong { get; private set; }

    private void Awake()
    {
        if (_chart == null) _chart = GetComponent<EditorChart>();
        if (_playback == null) _playback = GetComponent<EditorPlayback>();
    }

    private void Start()
    {
        if (_defaultSong != null && CurrentSong == null)
        {
            ApplySong(_defaultSong);
        }
    }

    public void OpenLoadDialog()
    {
#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Select SongData", Application.dataPath, "asset");
        if (string.IsNullOrEmpty(path)) return;

        if (path.StartsWith(Application.dataPath))
        {
            path = "Assets" + path.Substring(Application.dataPath.Length);
        }

        var song = AssetDatabase.LoadAssetAtPath<SongData>(path);
        if (song == null)
        {
            Debug.LogWarning("[EditorLoadSong] Not a SongData asset: " + path);
            return;
        }

        ApplySong(song);
#else
        Debug.LogWarning("[EditorLoadSong] LoadSong is only available in Unity Editor mode.");
#endif
    }

    public void ApplySong(SongData song)
    {
        if (song == null) return;
        CurrentSong = song;

        if (_playback != null)
        {
            _playback.SetClip(song.fullClip, song.bpm, song.audioOffsetSec);
        }

        ReloadChartForCurrentSongDifficulty();
    }

    public void ApplySongOnly(SongData song)
    {
        if (song == null) return;
        CurrentSong = song;
        if (_playback != null)
        {
            _playback.SetClip(song.fullClip, song.bpm, song.audioOffsetSec);
        }
    }

    public void ReloadChartForCurrentSongDifficulty()
    {
        if (CurrentSong == null || _chart == null) return;

        string songName = CurrentSong.songName;
        string difficulty = _currentDifficulty.ToString();
        string chartPath = ChartUtility.GetChartPath(songName, difficulty);

        var existing = ChartUtility.LoadFromFile(chartPath);
        if (existing != null)
        {
            _chart.LoadChart(existing);
        }
        else
        {
            _chart.NewChart(songName, difficulty, CurrentSong.bpm);
        }
    }
}
