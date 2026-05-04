using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class EffectConductor : MonoBehaviour
{
    [SerializeField] private RhythmConductor _rhythm;
    [SerializeField] private bool _autoLoadOnStart = true;
    [SerializeField] private string _animatorRootName = "Main Camera";

    private EffectData _data;
    private int _dispatchIdx;
    private bool _armed;

    private PlayableGraph _camGraph;
    private AnimationPlayableOutput _camOutput;
    private AnimationClipPlayable _camClipPlayable;
    private bool _camGraphValid;
    private double _camStopDsp = -1.0;
    private Animator _camAnimator;

    private readonly List<RuntimeParticle> _activeParticles = new List<RuntimeParticle>();

    private struct RuntimeParticle
    {
        public GameObject go;
        public double stopDsp;
    }

    private void Awake()
    {
        if (_rhythm == null) _rhythm = FindObjectOfType<RhythmConductor>();
    }

    private void Start()
    {
        if (_autoLoadOnStart) LoadFromGameManager();
    }

    public void LoadFromGameManager()
    {
        if (GameManager.I == null || GameManager.I.SelectedSong == null)
        {
            Debug.LogWarning("[EffectConductor] No selected song");
            return;
        }
        string songName = GameManager.I.SelectedSong.songName;
        string diff = GameManager.I.SelectedDifficulty.ToString();
        LoadEffectChart(songName, diff);
    }

    public void LoadEffectChart(string songName, string difficulty)
    {
        string path = EffectUtility.GetEffectPath(songName, difficulty);
        var data = EffectUtility.LoadFromFile(path);
        if (data == null)
        {
            Debug.Log("[EffectConductor] No eff file: " + path);
            _data = null;
            _armed = false;
            return;
        }
        data.SortByBeat();
        _data = data;
        _dispatchIdx = 0;
        _armed = true;

        _camAnimator = FindCamAnimator();
    }

    private Animator FindCamAnimator()
    {
        var go = GameObject.Find(_animatorRootName);
        if (go == null) return null;
        var anim = go.GetComponent<Animator>();
        if (anim == null) anim = go.AddComponent<Animator>();
        return anim;
    }

    private void Update()
    {
        if (!_armed || _data == null || _rhythm == null) return;
        if (!_rhythm.IsPlaying) return;

        double songTime = _rhythm.SongTime;
        double secPerBeat = _rhythm.SecPerBeat;

        while (_dispatchIdx < _data.triggers.Count)
        {
            var trig = _data.triggers[_dispatchIdx];
            if (trig == null) { _dispatchIdx++; continue; }
            double trigTime = trig.beat * secPerBeat + _rhythm.AudioOffset;
            if (songTime < trigTime) break;
            DispatchTrigger(trig, secPerBeat);
            _dispatchIdx++;
        }

        UpdateCamGraphStop();
        UpdateActiveParticles();
    }

    private void DispatchTrigger(EffectTrigger trig, double secPerBeat)
    {
        var preset = EffectPresetCache.Get(trig.presetId);
        if (preset == null) return;

        switch (preset.category)
        {
            case EffectCategory.Cam:
            case EffectCategory.Rail:
                DispatchAnimClip(trig, preset, secPerBeat);
                break;
            case EffectCategory.Eff:
                DispatchParticle(trig, preset, secPerBeat);
                break;
            case EffectCategory.Scr:
                break;
        }
    }

    private void DispatchAnimClip(EffectTrigger trig, EffectPresetSO preset, double secPerBeat)
    {
        if (preset.animationClip == null) return;
        if (_camAnimator == null) _camAnimator = FindCamAnimator();
        if (_camAnimator == null) return;

        if (trig.kind == TriggerKind.Off)
        {
            StopCamGraph();
            return;
        }

        double durationSec;
        float speed;
        if (trig.kind == TriggerKind.Sustained && trig.inBeats > 0.0001)
        {
            durationSec = trig.inBeats * secPerBeat;
            speed = (float)(preset.animationClip.length / durationSec);
        }
        else if (trig.kind == TriggerKind.On)
        {
            durationSec = preset.defaultDurationSec > 0f ? preset.defaultDurationSec : preset.animationClip.length;
            speed = preset.animationClip.length > 0f ? (preset.animationClip.length / Mathf.Max(0.001f, (float)durationSec)) : 1f;
        }
        else
        {
            durationSec = preset.animationClip.length;
            speed = 1f;
        }

        BuildCamGraph(_camAnimator, preset.animationClip, speed);
        _camStopDsp = AudioSettings.dspTime + durationSec;
    }

    private void BuildCamGraph(Animator animator, AnimationClip clip, float speed)
    {
        if (_camGraphValid) DestroyCamGraph();

        _camGraph = PlayableGraph.Create("EffectCamGraph");
        _camOutput = AnimationPlayableOutput.Create(_camGraph, "AnimOut", animator);
        _camClipPlayable = AnimationClipPlayable.Create(_camGraph, clip);
        _camClipPlayable.SetSpeed(speed);
        _camOutput.SetSourcePlayable(_camClipPlayable);
        _camGraph.Play();
        _camGraphValid = true;
    }

    private void StopCamGraph()
    {
        DestroyCamGraph();
        _camStopDsp = -1.0;
    }

    private void DestroyCamGraph()
    {
        if (_camGraphValid && _camGraph.IsValid())
        {
            _camGraph.Destroy();
        }
        _camGraphValid = false;
    }

    private void UpdateCamGraphStop()
    {
        if (!_camGraphValid) return;
        if (_camStopDsp < 0.0) return;
        if (AudioSettings.dspTime >= _camStopDsp)
        {
            if (_camClipPlayable.IsValid())
            {
                _camClipPlayable.SetSpeed(0);
            }
            _camStopDsp = -1.0;
        }
    }

    private void DispatchParticle(EffectTrigger trig, EffectPresetSO preset, double secPerBeat)
    {
        if (preset.particlePrefab == null) return;

        Vector3 pos = preset.spawnOffset;
        var go = Instantiate(preset.particlePrefab, pos, Quaternion.identity);

        double durationSec;
        if (trig.kind == TriggerKind.Sustained && trig.inBeats > 0.0001)
            durationSec = trig.inBeats * secPerBeat;
        else
            durationSec = preset.defaultDurationSec > 0f ? preset.defaultDurationSec : 2f;

        _activeParticles.Add(new RuntimeParticle
        {
            go = go,
            stopDsp = AudioSettings.dspTime + durationSec
        });
    }

    private void UpdateActiveParticles()
    {
        double now = AudioSettings.dspTime;
        for (int i = _activeParticles.Count - 1; i >= 0; i--)
        {
            var p = _activeParticles[i];
            if (p.go == null) { _activeParticles.RemoveAt(i); continue; }
            if (now >= p.stopDsp)
            {
                Destroy(p.go);
                _activeParticles.RemoveAt(i);
            }
        }
    }

    private void OnDestroy()
    {
        DestroyCamGraph();
        for (int i = 0; i < _activeParticles.Count; i++)
        {
            if (_activeParticles[i].go != null) Destroy(_activeParticles[i].go);
        }
        _activeParticles.Clear();
    }
}
