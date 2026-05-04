using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EditorUI : MonoBehaviour
{
    [SerializeField] private EditorInput _input;
    [SerializeField] private EditorPlayback _playback;
    [SerializeField] private EditorSaveLoad _saveLoad;
    [SerializeField] private EditorLoadSong _loadSong;
    [SerializeField] private EditorTimeline _timeline;

    [Header("Mode Buttons")]
    [SerializeField] private Button _tapButton;
    [SerializeField] private Button _lnButton;
    [SerializeField] private Button _dnButton;
    [SerializeField] private Button _dlnButton;

    [Header("Action Buttons")]
    [SerializeField] private Button _loadSongButton;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _loadButton;
    [SerializeField] private Button _exitButton;
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _rewindButton;
    [SerializeField] private Button _selectDiffButton;
    [SerializeField] private Button _selectDiffButtonEffect;

    [Header("BSD")]
    [SerializeField] private TMP_Dropdown _bsdDropdown;

    [Header("SongName UI")]
    [SerializeField] private TextMeshProUGUI _songNameText;

    [Header("Difficulty UI")]
    [SerializeField] private TextMeshProUGUI _difficultyText;

    [Header("Progress UI")]
    [SerializeField] private Image _progressFillImage;

    [Header("Speed Slider (4 steps: 25/50/75/100%)")]
    [SerializeField] private Slider _speedSlider;

    [Header("SelectDiff Panel")]
    [SerializeField] private GameObject _selectDiffPanel;
    [SerializeField] private Button _easyButton;
    [SerializeField] private Button _mediumButton;
    [SerializeField] private Button _hardButton;
    [SerializeField] private Button _insaneButton;
    [SerializeField] private Button _masterButton;
    [SerializeField] private Button _delButton;
    [SerializeField] private GameObject _selectDiffBlocker;

    [Header("Exit Scene")]
    [SerializeField] private string _exitSceneName = "SongSelect";

    private void Awake()
    {
        if (_input == null) _input = GetComponent<EditorInput>();
        if (_playback == null) _playback = GetComponent<EditorPlayback>();
        if (_saveLoad == null) _saveLoad = GetComponent<EditorSaveLoad>();
        if (_loadSong == null) _loadSong = GetComponent<EditorLoadSong>();
        if (_timeline == null) _timeline = GetComponent<EditorTimeline>();
    }

    private void Start()
    {
        BindModeButtons();
        BindActionButtons();
        BindBsd();
        BindSelectDiff();
        BindSpeedSlider();
        UpdateSongName();
        UpdateDifficultyText();
        if (_selectDiffPanel != null) _selectDiffPanel.SetActive(false);
    }

    private void BindSpeedSlider()
    {
        if (_speedSlider == null) return;
        _speedSlider.minValue = 1f;
        _speedSlider.maxValue = 4f;
        _speedSlider.wholeNumbers = true;
        _speedSlider.value = 4f;
        _speedSlider.onValueChanged.AddListener(OnSpeedSliderChanged);
        OnSpeedSliderChanged(_speedSlider.value);
    }

    private void OnSpeedSliderChanged(float value)
    {
        int step = Mathf.Clamp((int)value, 1, 4);
        float pitch = step * 0.25f;
        if (_playback != null) _playback.SetPitch(pitch);
    }

    private void Update()
    {
        if (_progressFillImage != null && _playback != null)
        {
            _progressFillImage.fillAmount = _playback.Progress01;
        }
    }

    private void BindModeButtons()
    {
        if (_tapButton != null) _tapButton.onClick.AddListener(() => SetMode(EditorPlaceMode.Tap));
        if (_lnButton != null) _lnButton.onClick.AddListener(() => SetMode(EditorPlaceMode.LongNote));
        if (_dnButton != null) _dnButton.onClick.AddListener(() => SetMode(EditorPlaceMode.DimensionTap));
        if (_dlnButton != null) _dlnButton.onClick.AddListener(() => SetMode(EditorPlaceMode.DimensionLongNote));
    }

    private void BindActionButtons()
    {
        if (_loadSongButton != null) _loadSongButton.onClick.AddListener(OnLoadSongClicked);
        if (_saveButton != null) _saveButton.onClick.AddListener(OnSaveClicked);
        if (_loadButton != null) _loadButton.onClick.AddListener(OnLoadClicked);
        if (_exitButton != null) _exitButton.onClick.AddListener(OnExitClicked);
        if (_playButton != null) _playButton.onClick.AddListener(OnPlayClicked);
        if (_rewindButton != null) _rewindButton.onClick.AddListener(OnRewindClicked);
    }

    private void BindBsd()
    {
        if (_bsdDropdown == null) return;
        _bsdDropdown.value = 0;
        _bsdDropdown.onValueChanged.AddListener(OnBsdChanged);
        OnBsdChanged(0);
    }

    private void BindSelectDiff()
    {
        if (_selectDiffButton != null) _selectDiffButton.onClick.AddListener(ToggleSelectDiff);
        if (_selectDiffButtonEffect != null) _selectDiffButtonEffect.onClick.AddListener(ToggleSelectDiff);
        if (_selectDiffBlocker != null)
        {
            var btn = _selectDiffBlocker.GetComponent<Button>();
            if (btn == null) btn = _selectDiffBlocker.AddComponent<Button>();
            btn.onClick.AddListener(CloseSelectDiff);
        }
        if (_easyButton != null) _easyButton.onClick.AddListener(() => SelectDifficulty(DifficultyType.Easy));
        if (_mediumButton != null) _mediumButton.onClick.AddListener(() => SelectDifficulty(DifficultyType.Medium));
        if (_hardButton != null) _hardButton.onClick.AddListener(() => SelectDifficulty(DifficultyType.Hard));
        if (_insaneButton != null) _insaneButton.onClick.AddListener(() => SelectDifficulty(DifficultyType.Insane));
        if (_masterButton != null) _masterButton.onClick.AddListener(() => SelectDifficulty(DifficultyType.Master));
        if (_delButton != null) _delButton.onClick.AddListener(() => SelectDifficulty(DifficultyType.Del));
    }

    private void OnBsdChanged(int idx)
    {
        if (_timeline == null || _bsdDropdown == null) return;
        if (idx < 0 || idx >= _bsdDropdown.options.Count) return;
        string text = _bsdDropdown.options[idx].text;
        _timeline.Bsd = ParseBsdText(text);
    }

    private int ParseBsdText(string text)
    {
        if (string.IsNullOrEmpty(text)) return 4;
        int slashIdx = text.IndexOf('/');
        if (slashIdx < 0)
        {
            if (int.TryParse(text, out int direct) && direct > 0) return direct;
            return 4;
        }
        string numStr = text.Substring(slashIdx + 1).Trim();
        if (int.TryParse(numStr, out int n) && n > 0) return n;
        return 4;
    }

    private void SetMode(EditorPlaceMode mode)
    {
        if (_input != null) _input.Mode = mode;
    }

    private void OnLoadSongClicked()
    {
        if (_loadSong != null) _loadSong.OpenLoadDialog();
        UpdateSongName();
    }

    private void OnSaveClicked()
    {
        if (_saveLoad != null) _saveLoad.Save();
    }

    private void OnLoadClicked()
    {
        if (_saveLoad != null) _saveLoad.OpenLoadDialog();
        UpdateSongName();
        UpdateDifficultyText();
    }

    private void OnExitClicked()
    {
        if (string.IsNullOrEmpty(_exitSceneName)) return;
        UnityEngine.SceneManagement.SceneManager.LoadScene(_exitSceneName);
    }

    private void OnPlayClicked()
    {
        if (_playback != null) _playback.TogglePlay();
    }

    private void OnRewindClicked()
    {
        if (_playback != null) _playback.RewindToStart();
    }

    private void ToggleSelectDiff()
    {
        if (_selectDiffPanel == null) return;
        _selectDiffPanel.SetActive(!_selectDiffPanel.activeSelf);
    }

    private void CloseSelectDiff()
    {
        if (_selectDiffPanel != null) _selectDiffPanel.SetActive(false);
    }

    private void SelectDifficulty(DifficultyType diff)
    {
        if (_loadSong != null)
        {
            if (_loadSong.CurrentDifficulty == diff)
            {
                CloseSelectDiff();
                return;
            }
            _loadSong.CurrentDifficulty = diff;
            if (_loadSong.CurrentSong != null) _loadSong.ReloadChartForCurrentSongDifficulty();
        }
        CloseSelectDiff();
        UpdateSongName();
        UpdateDifficultyText();
    }

    private void UpdateDifficultyText()
    {
        if (_difficultyText == null) return;
        if (_loadSong != null)
        {
            _difficultyText.text = _loadSong.CurrentDifficulty.ToString();
        }
        else
        {
            _difficultyText.text = "Easy";
        }
    }

    private void UpdateSongName()
    {
        if (_songNameText == null) return;
        if (_loadSong != null && _loadSong.CurrentSong != null)
        {
            _songNameText.text = _loadSong.CurrentSong.songName;
        }
        else
        {
            _songNameText.text = "SampleSongName";
        }
    }
}
