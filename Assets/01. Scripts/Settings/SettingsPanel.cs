using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour, IModalPanel
{
    [Header("Audio Sliders")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _hitSlider;

    [Header("Audio Value Labels (optional)")]
    [SerializeField] private TMP_Text _masterValueText;
    [SerializeField] private TMP_Text _musicValueText;
    [SerializeField] private TMP_Text _sfxValueText;
    [SerializeField] private TMP_Text _hitValueText;

    [Header("Offset Sliders (seconds)")]
    [SerializeField] private Slider _audioOffsetSlider;
    [SerializeField] private Slider _inputOffsetSlider;

    [Header("Offset Value Labels (optional)")]
    [SerializeField] private TMP_Text _audioOffsetValueText;
    [SerializeField] private TMP_Text _inputOffsetValueText;

    [Header("Gameplay")]
    [SerializeField] private Slider _noteSpeedSlider;
    [SerializeField] private TMP_Text _noteSpeedValueText;

    [Header("Display")]
    [SerializeField] private TMP_Dropdown _fpsDropdown;

    [Header("Buttons")]
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _keyBindButton;
    [SerializeField] private GameObject _keyBindPanel;

    [Header("Modal Behavior")]
    [SerializeField] private bool _freezeTimeWhileOpen = false;
    [SerializeField] private bool _registerToModalStack = true;

    private float _prevTimeScale = 1f;

    private static readonly int[] FpsOptions = { 60, 120, 144, 0 };

    private void Awake()
    {
        InitFpsDropdown();
    }

    private void OnEnable()
    {
        RefreshFromConfig();
        BindUI();
        SettingsConfig.OnChanged += OnConfigChanged;
        if (_registerToModalStack) ModalStack.Push(this);
        if (_freezeTimeWhileOpen)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    private void OnDisable()
    {
        SettingsConfig.OnChanged -= OnConfigChanged;
        UnbindUI();
        SettingsConfig.Save();
        if (_registerToModalStack) ModalStack.Remove(this);
        if (_freezeTimeWhileOpen)
        {
            Time.timeScale = _prevTimeScale;
        }
    }

    public void OnEscape()
    {
        gameObject.SetActive(false);
    }

    private void InitFpsDropdown()
    {
        if (_fpsDropdown == null) return;
        _fpsDropdown.ClearOptions();
        var opts = new System.Collections.Generic.List<string>();
        for (int i = 0; i < FpsOptions.Length; i++)
        {
            opts.Add(FpsOptions[i] == 0 ? "Unlimited" : FpsOptions[i].ToString());
        }
        _fpsDropdown.AddOptions(opts);
    }

    private void RefreshFromConfig()
    {
        SetSlider(_masterSlider, SettingsConfig.MasterVolume * 100f);
        SetSlider(_musicSlider, SettingsConfig.MusicVolume * 100f);
        SetSlider(_sfxSlider, SettingsConfig.SfxVolume * 100f);
        SetSlider(_hitSlider, SettingsConfig.HitVolume * 100f);

        SetSlider(_audioOffsetSlider, SettingsConfig.AudioOffsetSec * 1000f);
        SetSlider(_inputOffsetSlider, SettingsConfig.InputOffsetSec * 1000f);

        SetSlider(_noteSpeedSlider, SettingsConfig.NoteSpeed);

        UpdateAudioLabels();
        UpdateOffsetLabels();
        UpdateNoteSpeedLabel();

        if (_fpsDropdown != null)
        {
            int idx = FindFpsIndex(SettingsConfig.FpsCap);
            _fpsDropdown.SetValueWithoutNotify(idx);
            _fpsDropdown.RefreshShownValue();
        }
    }

    private static void SetSlider(Slider s, float v)
    {
        if (s == null) return;
        s.SetValueWithoutNotify(v);
    }

    private int FindFpsIndex(int cap)
    {
        for (int i = 0; i < FpsOptions.Length; i++)
            if (FpsOptions[i] == cap) return i;
        return 0;
    }

    private void BindUI()
    {
        if (_masterSlider != null) _masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (_musicSlider != null) _musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (_hitSlider != null) _hitSlider.onValueChanged.AddListener(OnHitChanged);

        if (_audioOffsetSlider != null) _audioOffsetSlider.onValueChanged.AddListener(OnAudioOffsetChanged);
        if (_inputOffsetSlider != null) _inputOffsetSlider.onValueChanged.AddListener(OnInputOffsetChanged);

        if (_noteSpeedSlider != null) _noteSpeedSlider.onValueChanged.AddListener(OnNoteSpeedChanged);

        if (_fpsDropdown != null) _fpsDropdown.onValueChanged.AddListener(OnFpsChanged);

        if (_resetButton != null) _resetButton.onClick.AddListener(OnResetClicked);
        if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClicked);
        if (_keyBindButton != null) _keyBindButton.onClick.AddListener(OnKeyBindClicked);
    }

    private void UnbindUI()
    {
        if (_masterSlider != null) _masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        if (_musicSlider != null) _musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        if (_sfxSlider != null) _sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        if (_hitSlider != null) _hitSlider.onValueChanged.RemoveListener(OnHitChanged);

        if (_audioOffsetSlider != null) _audioOffsetSlider.onValueChanged.RemoveListener(OnAudioOffsetChanged);
        if (_inputOffsetSlider != null) _inputOffsetSlider.onValueChanged.RemoveListener(OnInputOffsetChanged);

        if (_noteSpeedSlider != null) _noteSpeedSlider.onValueChanged.RemoveListener(OnNoteSpeedChanged);

        if (_fpsDropdown != null) _fpsDropdown.onValueChanged.RemoveListener(OnFpsChanged);

        if (_resetButton != null) _resetButton.onClick.RemoveListener(OnResetClicked);
        if (_closeButton != null) _closeButton.onClick.RemoveListener(OnCloseClicked);
        if (_keyBindButton != null) _keyBindButton.onClick.RemoveListener(OnKeyBindClicked);
    }

    private void OnMasterChanged(float v) { SettingsConfig.MasterVolume = v / 100f; UpdateAudioLabels(); }
    private void OnMusicChanged(float v) { SettingsConfig.MusicVolume = v / 100f; UpdateAudioLabels(); }
    private void OnSfxChanged(float v) { SettingsConfig.SfxVolume = v / 100f; UpdateAudioLabels(); }
    private void OnHitChanged(float v) { SettingsConfig.HitVolume = v / 100f; UpdateAudioLabels(); }

    private void OnAudioOffsetChanged(float v) { SettingsConfig.AudioOffsetSec = v / 1000f; UpdateOffsetLabels(); }
    private void OnInputOffsetChanged(float v) { SettingsConfig.InputOffsetSec = v / 1000f; UpdateOffsetLabels(); }

    private void OnNoteSpeedChanged(float v) { SettingsConfig.NoteSpeed = v; UpdateNoteSpeedLabel(); }

    private void OnConfigChanged(SettingsConfig.Category cat)
    {
        if (cat != SettingsConfig.Category.Offset) return;
        SetSlider(_audioOffsetSlider, SettingsConfig.AudioOffsetSec * 1000f);
        SetSlider(_inputOffsetSlider, SettingsConfig.InputOffsetSec * 1000f);
        UpdateOffsetLabels();
    }

    private void OnFpsChanged(int idx)
    {
        if (idx < 0 || idx >= FpsOptions.Length) return;
        SettingsConfig.FpsCap = FpsOptions[idx];
    }

    private void OnResetClicked()
    {
        SettingsConfig.ResetToDefaults();
        RefreshFromConfig();
    }

    private void OnCloseClicked()
    {
        gameObject.SetActive(false);
    }

    private void OnKeyBindClicked()
    {
        if (_keyBindPanel != null) _keyBindPanel.SetActive(true);
    }

    private void UpdateAudioLabels()
    {
        SetPercentLabel(_masterValueText, SettingsConfig.MasterVolume);
        SetPercentLabel(_musicValueText, SettingsConfig.MusicVolume);
        SetPercentLabel(_sfxValueText, SettingsConfig.SfxVolume);
        SetPercentLabel(_hitValueText, SettingsConfig.HitVolume);
    }

    private void UpdateOffsetLabels()
    {
        SetMsLabel(_audioOffsetValueText, SettingsConfig.AudioOffsetSec);
        SetMsLabel(_inputOffsetValueText, SettingsConfig.InputOffsetSec);
    }

    private void UpdateNoteSpeedLabel()
    {
        if (_noteSpeedValueText == null) return;
        _noteSpeedValueText.text = SettingsConfig.NoteSpeed.ToString("F1");
    }

    private static void SetPercentLabel(TMP_Text t, float v01)
    {
        if (t == null) return;
        t.text = Mathf.RoundToInt(v01 * 100f) + "%";
    }

    private static void SetMsLabel(TMP_Text t, float sec)
    {
        if (t == null) return;
        int ms = Mathf.RoundToInt(sec * 1000f);
        t.text = (ms >= 0 ? "+" : "") + ms + " ms";
    }
}
