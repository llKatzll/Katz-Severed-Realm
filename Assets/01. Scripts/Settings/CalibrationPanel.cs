using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class CalibrationPanel : MonoBehaviour
{
    private enum State { Idle, Running, Done }

    [Header("Buttons")]
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _applyButton;
    [SerializeField] private Button _closeButton;

    [Header("Try Texts (5)")]
    [SerializeField] private TMP_Text[] _tryTexts = new TMP_Text[5];

    [Header("Average")]
    [SerializeField] private TMP_Text _averageText;

    [Header("Tick Sound")]
    [SerializeField] private AudioClip _tickClip;
    [SerializeField] private AudioSource _tickSourceA;
    [SerializeField] private AudioSource _tickSourceB;

    [Header("Keys")]
    [SerializeField] private KeyBindConfig _keyBindConfig;

    [Header("Stage Objects")]
    [SerializeField] private GameObject _spinObject;
    [SerializeField] private float _spinSpeedDeg = 90f;
    [SerializeField] private GameObject _resultGroup;
    [SerializeField] private TMP_Text _startDoneText;

    [Header("Messages")]
    [SerializeField] private string _idleMessage = "Press SPACE or START to begin";
    [SerializeField] private string _doneMessage = "Press APPLY to save, or ESC to discard";

    [Header("Display")]
    [SerializeField] private float _displayBiasMs = 100f;

    [Header("Music Duck")]
    [SerializeField] private float _musicFadeOutSec = 0.3f;
    [SerializeField] private float _musicFadeInSec = 0.6f;

    private static GameObject _fadeRunnerGo;

    [Header("Timing")]
    [SerializeField] private double _tickIntervalSec = 0.5;
    [SerializeField] private double _leadInSec = 1.0;
    [SerializeField] private double _setRestSec = 1.0;
    [SerializeField] private double _earlyWindowSec = 0.25;
    [SerializeField] private double _lateWindowSec = 0.45;

    private const int TICKS_PER_SET = 4;
    private const int TOTAL_SETS = 5;
    private const double SCHEDULE_MARGIN_SEC = 0.15;

    private State _state;
    private int _setIndex;
    private int _scheduledTicks;
    private readonly double[] _tickDsp = new double[TICKS_PER_SET];
    private double _targetDsp;
    private bool _inputCaptured;
    private readonly float[] _samples = new float[TOTAL_SETS];
    private readonly bool[] _sampleValid = new bool[TOTAL_SETS];
    private float _resultMs;
    private bool _hasResult;
    private float _prevTimeScale = 1f;

    private void Awake()
    {
        EnsureSources();

        if (_startButton != null) _startButton.onClick.AddListener(TryStart);
        if (_applyButton != null) _applyButton.onClick.AddListener(Apply);
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        _state = State.Idle;
        ResetUI();
        ApplyStateVisuals();
        StartMusicFade(0f, _musicFadeOutSec);
    }

    private void OnDisable()
    {
        Time.timeScale = _prevTimeScale;
        StopTicks();
        _state = State.Idle;
        StartMusicFade(SettingsConfig.MusicVolume, _musicFadeInSec);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        switch (_state)
        {
            case State.Idle:
                if (Input.GetKeyDown(KeyCode.Space)) TryStart();
                break;
            case State.Running:
                UpdateRunning();
                break;
        }
    }

    private void TryStart()
    {
        if (_state == State.Running) return;
        if (_tickClip == null)
        {
            Debug.LogWarning("[CalibrationPanel] Tick clip is not assigned");
            return;
        }

        ResetUI();
        _setIndex = 0;
        _state = State.Running;
        ApplyStateVisuals();
        BeginSet(AudioSettings.dspTime + _leadInSec);
    }

    private void BeginSet(double firstTickDsp)
    {
        for (int i = 0; i < TICKS_PER_SET; i++)
            _tickDsp[i] = firstTickDsp + i * _tickIntervalSec;

        _targetDsp = _tickDsp[TICKS_PER_SET - 1];
        _inputCaptured = false;

        ScheduleTick(0);
        ScheduleTick(1);
        _scheduledTicks = 2;
    }

    private void UpdateRunning()
    {
        if (_spinObject != null)
            _spinObject.transform.Rotate(0f, 0f, _spinSpeedDeg * Time.unscaledDeltaTime);

        double now = AudioSettings.dspTime;

        if (_scheduledTicks < TICKS_PER_SET &&
            now >= _tickDsp[_scheduledTicks - 2] + SCHEDULE_MARGIN_SEC)
        {
            ScheduleTick(_scheduledTicks);
            _scheduledTicks++;
        }

        if (!_inputCaptured)
        {
            KeyCode key = GetPressedKey();
            if (key != KeyCode.None)
            {
                double diffMs = (now - _targetDsp) * 1000.0;
                if (diffMs >= -_earlyWindowSec * 1000.0 && diffMs <= _lateWindowSec * 1000.0)
                {
                    _inputCaptured = true;
                    _samples[_setIndex] = (float)diffMs;
                    _sampleValid[_setIndex] = true;
                    SetTryText(_setIndex, FormatMs((float)diffMs - _displayBiasMs));
                }
            }
        }

        if (now > _targetDsp + _lateWindowSec)
            FinalizeTry();
    }

    private void FinalizeTry()
    {
        if (!_inputCaptured)
        {
            _sampleValid[_setIndex] = false;
            SetTryText(_setIndex, "MISS");
        }

        _setIndex++;

        if (_setIndex >= TOTAL_SETS)
            FinishAll();
        else
            BeginSet(AudioSettings.dspTime + _setRestSec);
    }

    private void FinishAll()
    {
        _state = State.Done;

        int count = 0;
        float sum = 0f;
        for (int i = 0; i < TOTAL_SETS; i++)
        {
            if (!_sampleValid[i]) continue;
            sum += _samples[i];
            count++;
        }

        _hasResult = count > 0;

        if (_hasResult)
        {
            _resultMs = sum / count;
            if (_averageText != null)
                _averageText.text = FormatMs(_resultMs - _displayBiasMs);
        }
        else
        {
            if (_averageText != null)
                _averageText.text = "NO DATA";
        }

        if (_applyButton != null) _applyButton.interactable = _hasResult;
        ApplyStateVisuals();
    }

    private void ApplyStateVisuals()
    {
        bool idle = _state == State.Idle;
        bool running = _state == State.Running;
        bool done = _state == State.Done;

        if (_startButton != null) _startButton.gameObject.SetActive(idle);
        if (_applyButton != null) _applyButton.gameObject.SetActive(done);
        if (_spinObject != null) _spinObject.SetActive(running);
        if (_resultGroup != null) _resultGroup.SetActive(done);

        if (_startDoneText != null)
        {
            _startDoneText.gameObject.SetActive(!running);
            _startDoneText.text = done ? _doneMessage : _idleMessage;
        }
    }

    private void Apply()
    {
        if (_state != State.Done || !_hasResult) return;

        SettingsConfig.AudioOffsetSec = (_resultMs - _displayBiasMs) / 1000f;
        SettingsConfig.Save();
        Close();
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    private void ScheduleTick(int idx)
    {
        AudioSource src = (idx % 2 == 0) ? _tickSourceA : _tickSourceB;
        if (src == null || _tickClip == null) return;
        src.clip = _tickClip;
        src.PlayScheduled(_tickDsp[idx]);
    }

    private void StopTicks()
    {
        if (_tickSourceA != null) _tickSourceA.Stop();
        if (_tickSourceB != null) _tickSourceB.Stop();
    }

    private KeyCode GetPressedKey()
    {
        for (int i = 0; i < SettingsConfig.LaneCount; i++)
        {
            KeyCode g = _keyBindConfig != null
                ? _keyBindConfig.GetKey(ChartLaneType.Ground, i)
                : SettingsConfig.GetGroundKey(i);
            if (g != KeyCode.None && Input.GetKeyDown(g)) return g;

            KeyCode u = _keyBindConfig != null
                ? _keyBindConfig.GetKey(ChartLaneType.Upper, i)
                : SettingsConfig.GetUpperKey(i);
            if (u != KeyCode.None && Input.GetKeyDown(u)) return u;
        }

        KeyCode d = _keyBindConfig != null
            ? _keyBindConfig.DimensionKey
            : SettingsConfig.DimensionKey;
        if (d != KeyCode.None && Input.GetKeyDown(d)) return d;

        return KeyCode.None;
    }

    private void ResetUI()
    {
        for (int i = 0; i < _tryTexts.Length; i++)
            SetTryText(i, "0ms");

        if (_averageText != null) _averageText.text = "";
        if (_applyButton != null) _applyButton.interactable = false;

        _hasResult = false;
        for (int i = 0; i < TOTAL_SETS; i++) _sampleValid[i] = false;
    }

    private void SetTryText(int idx, string text)
    {
        if (idx < 0 || idx >= _tryTexts.Length) return;
        if (_tryTexts[idx] != null) _tryTexts[idx].text = text;
    }

    private static string FormatMs(float ms)
    {
        int r = Mathf.RoundToInt(ms);
        return (r >= 0 ? "+" : "") + r + "ms";
    }

    private void EnsureSources()
    {
        if (_tickSourceA != null && _tickSourceA == _tickSourceB)
        {
            Debug.LogWarning("[CalibrationPanel] Tick sources must be two different AudioSources. Creating dedicated sources.");
            _tickSourceA = null;
            _tickSourceB = null;
        }

        if (_tickSourceA == null) _tickSourceA = CreateSource("TickSourceA");
        if (_tickSourceB == null) _tickSourceB = CreateSource("TickSourceB");

        var sfxGroup = AudioMixerBinder.GetGroup("SFX");
        if (sfxGroup != null)
        {
            _tickSourceA.outputAudioMixerGroup = sfxGroup;
            _tickSourceB.outputAudioMixerGroup = sfxGroup;
        }
    }

    private AudioSource CreateSource(string sourceName)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        return src;
    }

    private void StartMusicFade(float targetLinear, float duration)
    {
        var mixer = AudioMixerBinder.Mixer;
        if (mixer == null) return;

        if (_fadeRunnerGo != null) Destroy(_fadeRunnerGo);

        _fadeRunnerGo = new GameObject("CalibMusicFade");
        var runner = _fadeRunnerGo.AddComponent<MusicFadeRunner>();
        runner.Run(mixer, targetLinear, duration);
    }

    private class MusicFadeRunner : MonoBehaviour
    {
        private const string PARAM = "MusicVolume";
        private const float MIN_DB = -80f;

        public void Run(AudioMixer mixer, float targetLinear, float duration)
        {
            StartCoroutine(CoFade(mixer, targetLinear, duration));
        }

        private IEnumerator CoFade(AudioMixer mixer, float targetLinear, float duration)
        {
            float currentDb;
            if (!mixer.GetFloat(PARAM, out currentDb)) currentDb = 0f;
            float fromLinear = currentDb <= MIN_DB + 0.01f ? 0f : Mathf.Pow(10f, currentDb / 20f);

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
                SetLinear(mixer, Mathf.Lerp(fromLinear, targetLinear, k));
                yield return null;
            }

            SetLinear(mixer, targetLinear);
            Destroy(gameObject);
        }

        private static void SetLinear(AudioMixer mixer, float v)
        {
            float db = v <= 0.0001f ? MIN_DB : Mathf.Log10(v) * 20f;
            mixer.SetFloat(PARAM, db);
        }
    }
}
