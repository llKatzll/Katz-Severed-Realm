using UnityEngine;

public class RhythmConductor : MonoBehaviour
{
    [SerializeField] private double _bpm = 120.0;
    [SerializeField] private AudioSource _audio;

    private double _startDspTime;
    private bool _started;

    public double Bpm => _bpm;
    public double SecPerBeat => (_bpm > 0.0) ? (60.0 / _bpm) : 0.5;
    public double CurrentBeat => SongTime / SecPerBeat;
    public bool Started => _started;

    public double SongTime
    {
        get
        {
            if (!_started) return 0.0;
            double t = AudioSettings.dspTime - _startDspTime;
            if (t < 0.0) t = 0.0;
            return t;
        }
    }

    private void Start()
    {
        StartSong();
    }

    public void StartSong()
    {
        if (_started) return;

        _startDspTime = AudioSettings.dspTime;
        _started = true;

        if (_audio != null)
        {
            _audio.Stop();
            _audio.Play();
        }
    }

    public double DspTimeAtBeat(double beat)
    {
        if (!_started) return AudioSettings.dspTime;
        if (beat < 0.0) beat = 0.0;
        return _startDspTime + (beat * SecPerBeat);
    }

    private void OnValidate()
    {
        if (_bpm <= 0.0) _bpm = 120.0;
    }
}
