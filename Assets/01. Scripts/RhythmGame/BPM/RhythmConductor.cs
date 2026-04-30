using UnityEngine;

public class RhythmConductor : MonoBehaviour
{
    [SerializeField] private double _bpm = 120.0;
    [SerializeField] private AudioSource _audio;
    [SerializeField] private double _audioOffset;
    [SerializeField] private bool _autoStart = false;

    private double _startDspTime;
    private double _pausedSongTime;
    private bool _started;
    private bool _paused;

    public double Bpm => _bpm;
    public double SecPerBeat => (_bpm > 0.0) ? (60.0 / _bpm) : 0.5;
    public double CurrentBeat => (SongTime - _audioOffset) / SecPerBeat;
    public double AudioOffset => _audioOffset;
    public bool Started => _started;
    public bool Paused => _paused;
    public bool IsPlaying => _started && !_paused;
    public AudioSource Audio => _audio;

    public double SongTime
    {
        get
        {
            if (!_started) return 0.0;
            if (_paused) return _pausedSongTime;
            double t = AudioSettings.dspTime - _startDspTime;
            return t > 0.0 ? t : 0.0;
        }
    }

    private void Start()
    {
        if (_autoStart) StartSong();
    }

    public void SetBpm(double bpm)
    {
        if (bpm <= 0.0) return;
        _bpm = bpm;
    }

    public void SetAudioOffset(double offset)
    {
        _audioOffset = offset;
    }

    public void StartSong()
    {
        const double scheduleDelay = 0.1;
        double scheduledDsp = AudioSettings.dspTime + scheduleDelay;

        _startDspTime = scheduledDsp;
        _pausedSongTime = 0.0;
        _started = true;
        _paused = false;

        if (_audio != null)
        {
            _audio.Stop();
            _audio.time = 0f;
            _audio.PlayScheduled(scheduledDsp);
        }
    }

    public void Pause()
    {
        if (!_started || _paused) return;

        _pausedSongTime = AudioSettings.dspTime - _startDspTime;
        _paused = true;

        if (_audio != null) _audio.Pause();
    }

    public void Resume()
    {
        if (!_started || !_paused) return;

        _startDspTime = AudioSettings.dspTime - _pausedSongTime;
        _paused = false;

        if (_audio != null) _audio.UnPause();
    }

    public void TogglePlayPause()
    {
        if (!_started) { StartSong(); return; }
        if (_paused) Resume(); else Pause();
    }

    public void Stop()
    {
        _started = false;
        _paused = false;
        _pausedSongTime = 0.0;

        if (_audio != null) _audio.Stop();
    }

    public void SeekToBeat(double beat)
    {
        if (beat < 0.0) beat = 0.0;
        double targetTime = beat * SecPerBeat + _audioOffset;
        if (targetTime < 0.0) targetTime = 0.0;

        if (_paused || !_started)
        {
            _pausedSongTime = targetTime;
            if (!_started) { _started = true; _paused = true; }
        }
        else
        {
            _startDspTime = AudioSettings.dspTime - targetTime;
        }

        if (_audio != null && _audio.clip != null)
        {
            float clampedTime = Mathf.Clamp((float)targetTime, 0f, _audio.clip.length);
            _audio.time = clampedTime;
        }
    }

    public double DspTimeAtBeat(double beat)
    {
        if (!_started) return AudioSettings.dspTime;
        if (beat < 0.0) beat = 0.0;
        return _startDspTime + _audioOffset + (beat * SecPerBeat);
    }

    private void OnValidate()
    {
        if (_bpm <= 0.0) _bpm = 120.0;
    }
}
