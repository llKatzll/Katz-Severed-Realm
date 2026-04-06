using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChartEditorManager : MonoBehaviour
{
    [Header("Mode Roots")]
    [SerializeField] private GameObject _chartMode;
    [SerializeField] private GameObject _effectMode;

    [Header("Song Selection (ChartMode)")]
    [SerializeField] private Button _loadSongButton;
    [SerializeField] private GameObject _loadSongScrollView;
    [SerializeField] private Button _loadSongExitButton;
    [SerializeField] private Transform _loadSongContent;
    [SerializeField] private TMP_Text _songNameText;
    [SerializeField] private AudioSource _audioSource;

    [Header("Difficulty")]
    [SerializeField] private TMP_Dropdown _diffDropdown;

    [Header("BPM (both modes)")]
    [SerializeField] private TMP_InputField _bpmInputChart;
    [SerializeField] private TMP_InputField _bpmInputEffect;

    [Header("BSD")]
    [SerializeField] private TMP_Dropdown _bsdDropdown;

    [Header("Buttons (ChartMode)")]
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _loadButton;
    [SerializeField] private Button _exitButton;
    [SerializeField] private Button _testPlayButton;
    [SerializeField] private Button _effectModeButton;
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _backButton;

    [Header("Buttons (EffectMode)")]
    [SerializeField] private Button _chartModeButton;
    [SerializeField] private Button _effectPlayButton;
    [SerializeField] private Button _effectBackButton;

    [Header("Effect Mode Dropdowns")]
    [SerializeField] private TMP_Dropdown _effectsSongDropdown;
    [SerializeField] private TMP_Dropdown _camsSongDropdown;

    [Header("Live Mapping")]
    [SerializeField] private TMP_Text _liveMappingText;

    [Header("Save Indicator")]
    [SerializeField] private TMP_Text _changeSaveText;

    [Header("Timeline")]
    [SerializeField] private EditorTimeline _groundTimeline;
    [SerializeField] private EditorTimeline _upperTimeline;

    [Header("NoteType Buttons")]
    [SerializeField] private Button _tapButton;
    [SerializeField] private Button _holdButton;
    [SerializeField] private Button _dimensionButton;
    [SerializeField] private Button _svButton;

    [Header("NoteType Colors")]
    [SerializeField] private Color _noteTypeBtnSelected = Color.white;
    [SerializeField] private Color _noteTypeBtnDeselected = new Color(0.4f, 0.4f, 0.4f, 1f);

    [Header("NotePlacer")]
    [SerializeField] private EditorNotePlacer _groundPlacer;
    [SerializeField] private EditorNotePlacer _upperPlacer;

    private ChartNoteType _currentNoteType = ChartNoteType.Tap;
    private bool _svMode;

    private ChartData _currentChart;
    private SongData[] _allSongs;
    private bool _isEffectMode;
    private bool _isLiveMapping;
    private bool _hasUnsavedChanges;
    private string _currentSongName;
    private string _currentDifficulty = "Easy";

    private static readonly int[] BsdValues = { 1, 2, 3, 4, 6, 8, 12, 16 };

    public ChartData CurrentChart => _currentChart;
    public ChartNoteType CurrentNoteType => _currentNoteType;
    public bool SVMode => _svMode;
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

        SetupDiffDropdown();
        SetupBsdDropdown();
        SetupButtons();
        SetupNoteTypeButtons();
        SetupBpmInputs();
        SetupEffectDropdowns();
        SetupLoadSongUI();

        SetEffectMode(false);
        UpdateSaveIndicator();
        UpdateNoteTypeVisual();
    }

    private void SetupLoadSongUI()
    {
        if (_loadSongScrollView != null)
            _loadSongScrollView.SetActive(false);

        if (_loadSongButton != null)
            _loadSongButton.onClick.AddListener(OpenLoadSongView);

        if (_loadSongExitButton != null)
            _loadSongExitButton.onClick.AddListener(CloseLoadSongView);

        PopulateSongList();
    }

    private void PopulateSongList()
    {
        if (_loadSongContent == null) return;

        for (int i = _loadSongContent.childCount - 1; i >= 0; i--)
            Destroy(_loadSongContent.GetChild(i).gameObject);

        for (int i = 0; i < _allSongs.Length; i++)
        {
            SongData song = _allSongs[i];
            GameObject btnGo = new GameObject(song.songName, typeof(RectTransform), typeof(Button), typeof(Image));
            btnGo.transform.SetParent(_loadSongContent, false);

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 40f);

            Image img = btnGo.GetComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(btnGo.transform, false);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = song.songName;
            tmp.fontSize = 18f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            string songName = song.songName;
            btnGo.GetComponent<Button>().onClick.AddListener(() => OnSongItemClicked(songName));
        }
    }

    private void OpenLoadSongView()
    {
        if (_loadSongScrollView != null)
            _loadSongScrollView.SetActive(true);
    }

    private void CloseLoadSongView()
    {
        if (_loadSongScrollView != null)
            _loadSongScrollView.SetActive(false);
    }

    private void OnSongItemClicked(string songName)
    {
        CloseLoadSongView();
        LoadSong(songName, _currentDifficulty);
    }

    private void SetupDiffDropdown()
    {
        if (_diffDropdown == null) return;
        _diffDropdown.ClearOptions();
        _diffDropdown.AddOptions(new List<string>
            { "Easy", "Normal", "Hard", "Expert", "Master" });
        _diffDropdown.onValueChanged.AddListener(OnDiffSelected);
    }

    private void SetupBsdDropdown()
    {
        if (_bsdDropdown == null) return;
        _bsdDropdown.ClearOptions();
        var options = new List<string>();
        for (int i = 0; i < BsdValues.Length; i++)
            options.Add("1/" + BsdValues[i]);
        _bsdDropdown.AddOptions(options);
        _bsdDropdown.value = 3;
        _bsdDropdown.onValueChanged.AddListener(OnBsdChanged);
    }

    private void OnBsdChanged(int index)
    {
        if (_groundTimeline != null) _groundTimeline.SyncShaderParams();
        if (_upperTimeline != null) _upperTimeline.SyncShaderParams();
    }

    private void SetupButtons()
    {
        if (_saveButton != null) _saveButton.onClick.AddListener(OnSave);
        if (_loadButton != null) _loadButton.onClick.AddListener(OnLoad);
        if (_exitButton != null) _exitButton.onClick.AddListener(OnExit);
        if (_testPlayButton != null) _testPlayButton.onClick.AddListener(OnTestPlay);
        if (_effectModeButton != null) _effectModeButton.onClick.AddListener(() => SetEffectMode(true));
        if (_chartModeButton != null) _chartModeButton.onClick.AddListener(() => SetEffectMode(false));
        if (_playButton != null) _playButton.onClick.AddListener(OnPlayPause);
        if (_backButton != null) _backButton.onClick.AddListener(OnBack);
        if (_effectPlayButton != null) _effectPlayButton.onClick.AddListener(OnPlayPause);
        if (_effectBackButton != null) _effectBackButton.onClick.AddListener(OnBack);
    }

    private void SetupBpmInputs()
    {
        if (_bpmInputChart != null)
        {
            _bpmInputChart.contentType = TMP_InputField.ContentType.DecimalNumber;
            _bpmInputChart.onEndEdit.AddListener(OnBpmChanged);
        }
        if (_bpmInputEffect != null)
        {
            _bpmInputEffect.contentType = TMP_InputField.ContentType.DecimalNumber;
            _bpmInputEffect.onEndEdit.AddListener(OnBpmChanged);
        }
    }

    private void SetupEffectDropdowns()
    {
        if (_allSongs == null) return;

        var songNames = new List<string>();
        for (int i = 0; i < _allSongs.Length; i++)
            songNames.Add(_allSongs[i].songName);

        if (_effectsSongDropdown != null)
        {
            _effectsSongDropdown.ClearOptions();
            _effectsSongDropdown.AddOptions(songNames);
        }

        if (_camsSongDropdown != null)
        {
            _camsSongDropdown.ClearOptions();
            _camsSongDropdown.AddOptions(songNames);
        }
    }

    private void SetupNoteTypeButtons()
    {
        if (_tapButton != null)
            _tapButton.onClick.AddListener(() => SelectNoteType(ChartNoteType.Tap));
        if (_holdButton != null)
            _holdButton.onClick.AddListener(() => SelectNoteType(ChartNoteType.Hold));
        if (_dimensionButton != null)
            _dimensionButton.onClick.AddListener(() => SelectNoteType(ChartNoteType.Dimension));
        if (_svButton != null)
            _svButton.onClick.AddListener(ToggleSVMode);
    }

    public void SelectNoteType(ChartNoteType type)
    {
        _currentNoteType = type;
        _svMode = false;
        ApplyNoteTypeToPlacer();
        UpdateNoteTypeVisual();
    }

    private void ToggleSVMode()
    {
        _svMode = !_svMode;
        if (_svMode)
            _currentNoteType = ChartNoteType.Tap;
        ApplyNoteTypeToPlacer();
        UpdateNoteTypeVisual();
    }

    private void ApplyNoteTypeToPlacer()
    {
        if (_groundPlacer != null) _groundPlacer.CurrentNoteType = _currentNoteType;
        if (_upperPlacer != null) _upperPlacer.CurrentNoteType = _currentNoteType;
    }

    private void UpdateNoteTypeVisual()
    {
        SetBtnColor(_tapButton, !_svMode && _currentNoteType == ChartNoteType.Tap);
        SetBtnColor(_holdButton, !_svMode && _currentNoteType == ChartNoteType.Hold);
        SetBtnColor(_dimensionButton, !_svMode && _currentNoteType == ChartNoteType.Dimension);
        SetBtnColor(_svButton, _svMode);
    }

    private void SetBtnColor(Button btn, bool selected)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null)
            img.color = selected ? _noteTypeBtnSelected : _noteTypeBtnDeselected;
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Tab))
            ToggleLiveMapping();

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
            OnSave();

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
            OnUndo();

        SyncTimelineScroll();
    }

    private void SyncTimelineScroll()
    {
        if (_groundTimeline == null || _upperTimeline == null) return;
        if (_groundTimeline.ScrollRectRef == null || _upperTimeline.ScrollRectRef == null) return;

        float gVal = _groundTimeline.ScrollRectRef.verticalNormalizedPosition;
        float uVal = _upperTimeline.ScrollRectRef.verticalNormalizedPosition;

        if (Mathf.Abs(gVal - uVal) > 0.001f)
            _upperTimeline.ScrollRectRef.verticalNormalizedPosition = gVal;
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

        SyncBpmFields();

        if (_songNameText != null)
            _songNameText.text = songName;

        _hasUnsavedChanges = false;
        UpdateSaveIndicator();

        if (_groundTimeline != null) _groundTimeline.SetChart(_currentChart, ChartLaneType.Ground);
        if (_upperTimeline != null) _upperTimeline.SetChart(_currentChart, ChartLaneType.Upper);
    }

    private void SyncBpmFields()
    {
        if (_currentChart == null) return;
        string bpmStr = _currentChart.bpm.ToString("F1");
        if (_bpmInputChart != null) _bpmInputChart.text = bpmStr;
        if (_bpmInputEffect != null) _bpmInputEffect.text = bpmStr;
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

    private void OnDiffSelected(int index)
    {
        if (_diffDropdown == null) return;
        string diff = _diffDropdown.options[index].text;
        if (_hasUnsavedChanges)
            Debug.LogWarning("[ChartEditor] Unsaved changes! Save first.");
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
            SyncBpmFields();
            MarkUnsaved();
            if (_groundTimeline != null) _groundTimeline.SyncShaderParams();
            if (_upperTimeline != null) _upperTimeline.SyncShaderParams();
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
            _audioSource.Pause();
        else
            _audioSource.Play();
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

    private void OnTestPlay()
    {
        Debug.Log("[ChartEditor] TestPlay (not yet implemented)");
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
