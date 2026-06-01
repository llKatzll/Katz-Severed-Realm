using UnityEngine;

public static class SettingsConfig
{
    public enum Category { Audio, Offset, Gameplay, Display, Keys }

    public const float DefaultVolume = 1f;
    public const float DefaultOffsetSec = 0f;
    public const float DefaultNoteSpeed = 5f;
    public const int DefaultFpsCap = 144;

    public static readonly KeyCode[] DefaultGroundKeys =
        { KeyCode.A, KeyCode.S, KeyCode.L, KeyCode.Semicolon };
    public static readonly KeyCode[] DefaultUpperKeys =
        { KeyCode.Q, KeyCode.W, KeyCode.O, KeyCode.P };
    public const KeyCode DefaultDimensionKey = KeyCode.Space;

    public const int LaneCount = 4;

    private const string PfxMaster = "Settings.MasterVolume";
    private const string PfxMusic = "Settings.MusicVolume";
    private const string PfxSfx = "Settings.SfxVolume";
    private const string PfxHit = "Settings.HitVolume";
    private const string PfxAudioOffset = "Settings.AudioOffsetSec";
    private const string PfxInputOffset = "Settings.InputOffsetSec";
    private const string PfxNoteSpeed = "Settings.NoteSpeed";
    private const string PfxFpsCap = "Settings.FpsCap";
    private const string PfxGroundKey = "Settings.GroundKey.";
    private const string PfxUpperKey = "Settings.UpperKey.";
    private const string PfxDimensionKey = "Settings.DimensionKey";

    public static event System.Action<Category> OnChanged;

    private static float _masterVolume;
    private static float _musicVolume;
    private static float _sfxVolume;
    private static float _hitVolume;
    private static float _audioOffsetSec;
    private static float _inputOffsetSec;
    private static float _noteSpeed;
    private static int _fpsCap;
    private static readonly KeyCode[] _groundKeys = new KeyCode[LaneCount];
    private static readonly KeyCode[] _upperKeys = new KeyCode[LaneCount];
    private static KeyCode _dimensionKey;

    private static bool _loaded;

    public static float MasterVolume
    {
        get => _masterVolume;
        set
        {
            float v = Mathf.Clamp01(value);
            if (Mathf.Approximately(_masterVolume, v)) return;
            _masterVolume = v;
            PlayerPrefs.SetFloat(PfxMaster, v);
            OnChanged?.Invoke(Category.Audio);
        }
    }

    public static float MusicVolume
    {
        get => _musicVolume;
        set
        {
            float v = Mathf.Clamp01(value);
            if (Mathf.Approximately(_musicVolume, v)) return;
            _musicVolume = v;
            PlayerPrefs.SetFloat(PfxMusic, v);
            OnChanged?.Invoke(Category.Audio);
        }
    }

    public static float SfxVolume
    {
        get => _sfxVolume;
        set
        {
            float v = Mathf.Clamp01(value);
            if (Mathf.Approximately(_sfxVolume, v)) return;
            _sfxVolume = v;
            PlayerPrefs.SetFloat(PfxSfx, v);
            OnChanged?.Invoke(Category.Audio);
        }
    }

    public static float HitVolume
    {
        get => _hitVolume;
        set
        {
            float v = Mathf.Clamp01(value);
            if (Mathf.Approximately(_hitVolume, v)) return;
            _hitVolume = v;
            PlayerPrefs.SetFloat(PfxHit, v);
            OnChanged?.Invoke(Category.Audio);
        }
    }

    public static float AudioOffsetSec
    {
        get => _audioOffsetSec;
        set
        {
            if (Mathf.Approximately(_audioOffsetSec, value)) return;
            _audioOffsetSec = value;
            PlayerPrefs.SetFloat(PfxAudioOffset, value);
            OnChanged?.Invoke(Category.Offset);
        }
    }

    public static float InputOffsetSec
    {
        get => _inputOffsetSec;
        set
        {
            if (Mathf.Approximately(_inputOffsetSec, value)) return;
            _inputOffsetSec = value;
            PlayerPrefs.SetFloat(PfxInputOffset, value);
            OnChanged?.Invoke(Category.Offset);
        }
    }

    public static float NoteSpeed
    {
        get => _noteSpeed;
        set
        {
            float v = Mathf.Clamp(value, 0.1f, 10f);
            if (Mathf.Approximately(_noteSpeed, v)) return;
            _noteSpeed = v;
            PlayerPrefs.SetFloat(PfxNoteSpeed, v);
            OnChanged?.Invoke(Category.Gameplay);
        }
    }

    public static int FpsCap
    {
        get => _fpsCap;
        set
        {
            int v = Mathf.Max(0, value);
            if (_fpsCap == v) return;
            _fpsCap = v;
            PlayerPrefs.SetInt(PfxFpsCap, v);
            OnChanged?.Invoke(Category.Display);
        }
    }

    public static KeyCode GetGroundKey(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= LaneCount) return KeyCode.None;
        return _groundKeys[laneIndex];
    }

    public static KeyCode GetUpperKey(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= LaneCount) return KeyCode.None;
        return _upperKeys[laneIndex];
    }

    public static KeyCode DimensionKey
    {
        get => _dimensionKey;
        set
        {
            if (_dimensionKey == value) return;
            _dimensionKey = value;
            PlayerPrefs.SetInt(PfxDimensionKey, (int)value);
            OnChanged?.Invoke(Category.Keys);
        }
    }

    public static void SetGroundKey(int laneIndex, KeyCode key)
    {
        if (laneIndex < 0 || laneIndex >= LaneCount) return;
        if (_groundKeys[laneIndex] == key) return;
        _groundKeys[laneIndex] = key;
        PlayerPrefs.SetInt(PfxGroundKey + laneIndex, (int)key);
        OnChanged?.Invoke(Category.Keys);
    }

    public static void SetUpperKey(int laneIndex, KeyCode key)
    {
        if (laneIndex < 0 || laneIndex >= LaneCount) return;
        if (_upperKeys[laneIndex] == key) return;
        _upperKeys[laneIndex] = key;
        PlayerPrefs.SetInt(PfxUpperKey + laneIndex, (int)key);
        OnChanged?.Invoke(Category.Keys);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        LoadAll();
    }

    public static void LoadAll()
    {
        _masterVolume = PlayerPrefs.GetFloat(PfxMaster, DefaultVolume);
        _musicVolume = PlayerPrefs.GetFloat(PfxMusic, DefaultVolume);
        _sfxVolume = PlayerPrefs.GetFloat(PfxSfx, DefaultVolume);
        _hitVolume = PlayerPrefs.GetFloat(PfxHit, DefaultVolume);

        _audioOffsetSec = PlayerPrefs.GetFloat(PfxAudioOffset, DefaultOffsetSec);
        _inputOffsetSec = PlayerPrefs.GetFloat(PfxInputOffset, DefaultOffsetSec);

        _noteSpeed = PlayerPrefs.GetFloat(PfxNoteSpeed, DefaultNoteSpeed);
        _fpsCap = PlayerPrefs.GetInt(PfxFpsCap, DefaultFpsCap);

        for (int i = 0; i < LaneCount; i++)
        {
            _groundKeys[i] = (KeyCode)PlayerPrefs.GetInt(PfxGroundKey + i, (int)DefaultGroundKeys[i]);
            _upperKeys[i] = (KeyCode)PlayerPrefs.GetInt(PfxUpperKey + i, (int)DefaultUpperKeys[i]);
        }
        _dimensionKey = (KeyCode)PlayerPrefs.GetInt(PfxDimensionKey, (int)DefaultDimensionKey);

        _loaded = true;
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    public static void ResetToDefaults()
    {
        MasterVolume = DefaultVolume;
        MusicVolume = DefaultVolume;
        SfxVolume = DefaultVolume;
        HitVolume = DefaultVolume;

        AudioOffsetSec = DefaultOffsetSec;
        InputOffsetSec = DefaultOffsetSec;

        NoteSpeed = DefaultNoteSpeed;
        FpsCap = DefaultFpsCap;

        for (int i = 0; i < LaneCount; i++)
        {
            SetGroundKey(i, DefaultGroundKeys[i]);
            SetUpperKey(i, DefaultUpperKeys[i]);
        }
        DimensionKey = DefaultDimensionKey;

        Save();
    }

    public static bool IsLoaded => _loaded;
}
