using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChartEditorManager : MonoBehaviour
{
    [Header("Mode Roots")]
    [SerializeField] private GameObject _chartMode;
    [SerializeField] private GameObject _effectMode;

    [Header("Song")]
    [SerializeField] private TMP_Dropdown _songDropdown;
    [SerializeField] private TMP_Dropdown _diffDropdown;
    [SerializeField] private TMP_Text _songNameText;
    [SerializeField] private TMP_Text _selectedText;
    [SerializeField] private AudioSource _audioSource;

    [Header("BPM")]
    [SerializeField] private TMP_InputField _bpmInput;

    [Header("BSD")]
    [SerializeField] private TMP_Dropdown _bsdDropdown;

    [Header("Buttons")]
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _loadButton;
    [SerializeField] private Button _exitButton;
    [SerializeField] private Button _testPlayButton;
    [SerializeField] private Button _effectModeButton;
    [SerializeField] private Button _chartModeButton;
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _backButton;

    [Header("Live Mapping")]
    [SerializeField] private TMP_Text _liveMappingText;

    [Header("Save Indicator")]
    [SerializeField] private TMP_Text _changeSaveText;

    [Header("Timeline")]
    [SerializeField] private EditorTimeline _groundTimeline;
    [SerializeField] private EditorTimeline _upperTimeline;

    private ChartData _currentChart;
    private SongData[] _allSongs;
    private bool _isEffectMode;
    private bool _isLiveMapping;
    private bool _hasUnsavedChanges;
    private string _currentSongName;
    private string _currentDifficulty = "Easy";

    private static readonly int[] BsdValues = { 1, 2, 3, 4, 6, 8, 12, 16 };

    public ChartData CurrentChart => _currentChart;
    public bool IsLiveMapping => _isLiveMapping;
    public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;
    public float CurrentBeat => _audioSource != null && _currentChart != null && _currentChart.bpm > 0
        ? (_audioSource.time - _currentChart.audioOffset) / (60f / _currentChart.bpm)
        : 0f;
    public int CurrentBsd => BsdValues[_bsdDropdown != null ? _bsdDropdown.value : 3];

    private void Start()
    {
        _allSongs = Resources.LoadAll<SongData>("Songs");
        System.Array.Sort(_allSongs, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        SetupSongDropdown();
        SetupDiffDropdown();
        SetupBsdDropdown();
        SetupButtons();
        SetupBpmInput();

        SetEffectMode(false);
        UpdateSaveIndicator();
    }

    private void SetupSongDropdown()
    {
        if (_songDropdown == null) return;
        _songDropdown.ClearOptions();
        var options = new System.Collections.Generic.List<string>();
        for (int i = 0; i < _allSongs.Length; i++)
            options.Add(_allSongs[i].songName);
        _songDropdown.AddOptions(options);
        _songDropdown.onValueChanged.AddListener(OnSongSelected);
    }

    private void SetupDiffDropdown()
    {
        if (_diffDropdown == null) return;
        _diffDropdown.ClearOptions();
        _diffDropdown.AddOptions(new System.Collections.Generic.List<string>
            { "Easy", "Normal", "Hard", "Expert", "Master" });
        _diffDropdown.onValueChanged.AddListener(OnDiffSelected);
    }

    private void SetupBsdDropdown()
    {
        if (_bsdDropdown == null) return;
        _bsdDropdown.ClearOptions();
        var options = new System.Collections.Generic.List<string>();
        for (int i = 0; i < BsdValues.Length; i++)
            options.Add("1/" + BsdValues[i]);
        _bsdDropdown.AddOptions(options);
        _bsdDropdown.value = 3;
    }

    private void SetupButtons()
    {
        if (_saveButton != null) _saveButton.onClick.AddListener(OnSave);
        if (_loadButton != null) _loadButton.onClick.AddListener(OnLoad);
        if (_exitButton != null) _exitButton.onClick.AddListener(OnExit);
        if (_effectModeButton != null) _effectModeButton.onClick.AddListener(() => SetEffectMode(true));
        if (_chartModeButton != null) _chartModeButton.onClick.AddListener(() => SetEffectMode(false));
        if (_playButton != null) _playButton.onClick.AddListener(OnPlayPause);
        if (_backButton != null) _backButton.onClick.AddListener(OnBack);
    }

    private void SetupBpmInput()
    {
        if (_bpmInput == null) return;
        _bpmInput.contentType = TMP_InputField.ContentType.DecimalNumber;
        _bpmInput.onEndEdit.AddListener(OnBpmChanged);
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Tab))
            ToggleLiveMapping();

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
            OnSave();

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
            OnUndo();
    }

    public void LoadSong(string songName, string difficulty)
    {
        _currentSongName = songName;
        _currentDifficulty = difficulty;

        SongData songData = FindSongData(songName);
        if (songData == null) return;

        if (songData.fullClip != null)
            _audioSource.clip = songData.fullClip;
        else if (songData.previewClip != null)
            _audioSource.clip = songData.previewClip;

        string path = ChartUtility.GetChartPath(songName, difficulty);
        ChartData loaded = ChartUtility.LoadFromFile(path);

        if (loaded != null)
        {
            _currentChart = loaded;
        }
        else
        {
            _currentChart = new ChartData();
            _currentChart.songName = songName;
            _currentChart.difficulty = difficulty;
            _currentChart.bpm = 120f;
            _currentChart.audioOffset = 0f;
        }

        if (_bpmInput != null)
            _bpmInput.text = _currentChart.bpm.ToString("F1");

        if (_songNameText != null)
            _songNameText.text = songName;

        if (_selectedText != null)
            _selectedText.text = "Selected";

        _hasUnsavedChanges = false;
        UpdateSaveIndicator();

        if (_groundTimeline != null) _groundTimeline.SetChart(_currentChart, ChartLaneType.Ground);
        if (_upperTimeline != null) _upperTimeline.SetChart(_currentChart, ChartLaneType.Upper);
    }

    private SongData FindSongData(string songName)
    {
        for (int i = 0; i < _allSongs.Length; i++)
        {
            if (_allSongs[i].songName == songName)
                return _allSongs[i];
        }
        return null;
    }

    private void OnSongSelected(int index)
    {
        if (index < 0 || index >= _allSongs.Length) return;
        if (_hasUnsavedChanges)
        {
            Debug.LogWarning("[ChartEditor] Unsaved changes! Save first.");
        }
        LoadSong(_allSongs[index].songName, _currentDifficulty);
    }

    private void OnDiffSelected(int index)
    {
        if (_diffDropdown == null) return;
        string diff = _diffDropdown.options[index].text;
        if (_hasUnsavedChanges)
        {
            Debug.LogWarning("[ChartEditor] Unsaved changes! Save first.");
        }
        if (!string.IsNullOrEmpty(_currentSongName))
            LoadSong(_currentSongName, diff);
    }

    private void OnBpmChanged(string val)
    {
        if (_currentChart == null) return;
        float bpm;
        if (float.TryParse(val, out bpm) && bpm > 0f)
        {
            _currentChart.bpm = bpm;
            MarkUnsaved();
        }
    }

    public void SetEffectMode(bool effect)
    {
        _isEffectMode = effect;
        if (_chartMode != null) _chartMode.SetActive(!effect);
        if (_effectMode != null) _effectMode.SetActive(effect);
    }

    private void ToggleLiveMapping()
    {
        _isLiveMapping = !_isLiveMapping;
        if (_liveMappingText != null)
            _liveMappingText.text = _isLiveMapping ? "ON" : "OFF";
    }

    private void OnPlayPause()
    {
        if (_audioSource == null || _audioSource.clip == null) return;

        if (_audioSource.isPlaying)
        {
            _audioSource.Pause();
        }
        else
        {
            _audioSource.Play();
        }
    }

    private void OnBack()
    {
        if (_audioSource == null) return;
        _audioSource.Stop();
        _audioSource.time = 0f;
    }

    public void OnSave()
    {
        if (_currentChart == null || string.IsNullOrEmpty(_currentSongName)) return;
        string path = ChartUtility.GetChartPath(_currentSongName, _currentDifficulty);
        ChartUtility.SaveToFile(_currentChart, path);
        _hasUnsavedChanges = false;
        UpdateSaveIndicator();
    }

    public void OnLoad()
    {
        if (string.IsNullOrEmpty(_currentSongName)) return;
        LoadSong(_currentSongName, _currentDifficulty);
    }

    private void OnExit()
    {
        if (_hasUnsavedChanges)
        {
            Debug.LogWarning("[ChartEditor] Unsaved changes exist!");
            return;
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene("SongSelect");
    }

    private void OnUndo()
    {
        Debug.Log("[ChartEditor] Undo (not yet implemented)");
    }

    public void MarkUnsaved()
    {
        _hasUnsavedChanges = true;
        UpdateSaveIndicator();
    }

    private void UpdateSaveIndicator()
    {
        if (_changeSaveText == null) return;
        _changeSaveText.gameObject.SetActive(_hasUnsavedChanges);
    }

    public float BeatToTime(float beat)
    {
        if (_currentChart == null || _currentChart.bpm <= 0f) return 0f;
        return beat * (60f / _currentChart.bpm) + _currentChart.audioOffset;
    }

    public float TimeToBeat(float time)
    {
        if (_currentChart == null || _currentChart.bpm <= 0f) return 0f;
        return (time - _currentChart.audioOffset) / (60f / _currentChart.bpm);
    }
}
