using UnityEngine;

public class EditorPlayback : MonoBehaviour
{
    [SerializeField] private EditorTimeline _timeline;
    [SerializeField] private AudioSource _audio;

    private double _bpm = 120.0;
    private double _audioOffset = 0.0;
    private double _startDspTime;
    private double _pausedSongTime;
    private bool _started;
    private bool _paused;
    private float _lastObservedBeat;
    private double _pitch = 1.0;

    public bool IsPlaying => _started && !_paused;
    public bool IsPaused => _started && _paused;
    public double Bpm => _bpm;
    public AudioSource Audio => _audio;
    public double SecPerBeat => _bpm > 0.0 ? (60.0 / _bpm) : 0.5;
    public double Pitch => _pitch;

    public double SongTime
    {
        get
        {
            if (!_started) return 0.0;
            if (_paused) return _pausedSongTime;
            double t = (AudioSettings.dspTime - _startDspTime) * _pitch;
            return t > 0.0 ? t : 0.0;
        }
    }

    public double CurrentBeat => (SongTime - _audioOffset) / SecPerBeat;

    public float Progress01
    {
        get
        {
            if (_audio == null || _audio.clip == null || _audio.clip.length <= 0f) return 0f;
            return Mathf.Clamp01((float)SongTime / _audio.clip.length);
        }
    }

    private void Awake()
    {
        if (_timeline == null) _timeline = GetComponent<EditorTimeline>();
        EnsureAudio();
    }

    private void EnsureAudio()
    {
        if (_audio != null) return;
        var go = new GameObject("EditorAudio");
        go.transform.SetParent(transform);
        _audio = go.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
    }

    public void SetClip(AudioClip clip, double bpm, double offset = 0.0)
    {
        Stop();
        if (_audio != null) _audio.clip = clip;
        if (bpm > 0.0) _bpm = bpm;
        _audioOffset = offset;
    }

    public void SetPitch(float pitch)
    {
        if (pitch <= 0f) return;

        if (_started && !_paused)
        {
            double currentTime = (AudioSettings.dspTime - _startDspTime) * _pitch;
            _pitch = pitch;
            _startDspTime = AudioSettings.dspTime - currentTime / _pitch;
        }
        else
        {
            _pitch = pitch;
        }

        if (_audio != null) _audio.pitch = pitch;
    }

    public void TogglePlay()
    {
        if (!_started) Play();
        else if (_paused) Resume();
        else Pause();
    }

    public void Play()
    {
        double startBeat = _timeline != null ? _timeline.CurrentBeat : 0.0;
        double targetTime = startBeat * SecPerBeat + _audioOffset;
        if (targetTime < 0.0) targetTime = 0.0;

        const double scheduleDelay = 0.1;
        double scheduledDsp = AudioSettings.dspTime + scheduleDelay;
        _startDspTime = scheduledDsp - targetTime / _pitch;
        _started = true;
        _paused = false;

        if (_audio != null && _audio.clip != null)
        {
            _audio.time = Mathf.Clamp((float)targetTime, 0f, _audio.clip.length);
            _audio.pitch = (float)_pitch;
            _audio.PlayScheduled(scheduledDsp);
        }

        _lastObservedBeat = (float)startBeat;
    }

    public void Pause()
    {
        if (!_started || _paused) return;
        _pausedSongTime = (AudioSettings.dspTime - _startDspTime) * _pitch;
        _paused = true;
        if (_audio != null) _audio.Pause();
        if (_timeline != null) _lastObservedBeat = _timeline.CurrentBeat;
    }

    public void Resume()
    {
        if (!_started || !_paused) return;

        const double scheduleDelay = 0.1;
        double scheduledDsp = AudioSettings.dspTime + scheduleDelay;
        _startDspTime = scheduledDsp - _pausedSongTime / _pitch;
        _paused = false;

        if (_audio != null && _audio.clip != null)
        {
            float t = Mathf.Clamp((float)_pausedSongTime, 0f, _audio.clip.length);
            _audio.time = t;
            _audio.pitch = (float)_pitch;
            _audio.PlayScheduled(scheduledDsp);
        }

        if (_timeline != null) _lastObservedBeat = _timeline.CurrentBeat;
    }

    public void Stop()
    {
        _started = false;
        _paused = false;
        _pausedSongTime = 0.0;
        if (_audio != null) _audio.Stop();
    }

    public void RewindToStart()
    {
        Stop();
        if (_timeline != null) _timeline.CurrentBeat = 0f;
    }

    private void Update()
    {
        if (_timeline == null) return;

        if (IsPlaying)
        {
            float beat = (float)CurrentBeat;
            _timeline.CurrentBeat = beat;
            _lastObservedBeat = beat;
        }
        else if (IsPaused)
        {
            float beat = _timeline.CurrentBeat;
            if (!Mathf.Approximately(beat, _lastObservedBeat))
            {
                _pausedSongTime = beat * SecPerBeat + _audioOffset;
                if (_pausedSongTime < 0.0) _pausedSongTime = 0.0;
                _lastObservedBeat = beat;
            }
        }
    }
}
