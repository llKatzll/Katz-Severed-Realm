using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class EffectConductor : MonoBehaviour
{
    [SerializeField] private RhythmConductor _rhythm;
    [SerializeField] private bool _autoLoadOnStart = true;

    private EffectData _data;
    private int _dispatchIdx;
    private bool _armed;

    private class GraphState
    {
        public Animator animator;
        public PlayableGraph graph;
        public AnimationPlayableOutput output;
        public AnimationClipPlayable clip;
        public double stopDsp;
        public bool valid;
    }

    private readonly Dictionary<string, GraphState> _graphs = new Dictionary<string, GraphState>();
    private readonly Dictionary<string, Animator> _animatorCache = new Dictionary<string, Animator>();

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
    }

    private Animator FindAnimator(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        Animator cached;
        if (_animatorCache.TryGetValue(path, out cached) && cached != null) return cached;

        var go = GameObject.Find(path);
        if (go == null) return null;
        var anim = go.GetComponent<Animator>();
        if (anim == null) anim = go.AddComponent<Animator>();
        _animatorCache[path] = anim;
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

        UpdateAllGraphStops();
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
            case EffectCategory.Scr:
                DispatchAnimClip(trig, preset, secPerBeat);
                break;
            case EffectCategory.Eff:
                DispatchParticle(trig, preset, secPerBeat);
                break;
        }
    }

    private void DispatchAnimClip(EffectTrigger trig, EffectPresetSO preset, double secPerBeat)
    {
        if (preset.animationClip == null) return;
        if (string.IsNullOrEmpty(preset.targetAnimatorPath)) return;

        Animator animator = FindAnimator(preset.targetAnimatorPath);
        if (animator == null) return;

        string key = preset.targetAnimatorPath;

        if (trig.kind == TriggerKind.Off)
        {
            StopGraph(key);
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

        BuildGraph(key, animator, preset.animationClip, speed);
        var state = _graphs[key];
        state.stopDsp = AudioSettings.dspTime + durationSec;
    }

    private void BuildGraph(string key, Animator animator, AnimationClip clip, float speed)
    {
        GraphState existing;
        if (_graphs.TryGetValue(key, out existing) && existing.valid)
        {
            DestroyGraph(existing);
        }

        var state = new GraphState();
        state.animator = animator;
        state.graph = PlayableGraph.Create("EffectGraph_" + key);
        state.output = AnimationPlayableOutput.Create(state.graph, "AnimOut", animator);
        state.clip = AnimationClipPlayable.Create(state.graph, clip);
        state.clip.SetSpeed(speed);
        state.output.SetSourcePlayable(state.clip);
        state.graph.Play();
        state.valid = true;
        state.stopDsp = -1.0;

        _graphs[key] = state;
    }

    private void StopGraph(string key)
    {
        GraphState state;
        if (!_graphs.TryGetValue(key, out state)) return;
        DestroyGraph(state);
        _graphs.Remove(key);
    }

    private void DestroyGraph(GraphState state)
    {
        if (state == null) return;
        if (state.valid && state.graph.IsValid())
        {
            state.graph.Destroy();
        }
        state.valid = false;
    }

    private void UpdateAllGraphStops()
    {
        double now = AudioSettings.dspTime;
        foreach (var kv in _graphs)
        {
            var state = kv.Value;
            if (state == null || !state.valid) continue;
            if (state.stopDsp < 0.0) continue;
            if (now >= state.stopDsp)
            {
                if (state.clip.IsValid())
                {
                    state.clip.SetSpeed(0);
                }
                state.stopDsp = -1.0;
            }
        }
    }

    private void DispatchParticle(EffectTrigger trig, EffectPresetSO preset, double secPerBeat)
    {
        if (preset.particlePrefab == null) return;

        var go = Instantiate(preset.particlePrefab);
        go.transform.position += preset.spawnOffset;

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
        foreach (var kv in _graphs)
        {
            DestroyGraph(kv.Value);
        }
        _graphs.Clear();

        for (int i = 0; i < _activeParticles.Count; i++)
        {
            if (_activeParticles[i].go != null) Destroy(_activeParticles[i].go);
        }
        _activeParticles.Clear();
    }
}
