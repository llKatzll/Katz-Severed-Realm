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
        public AnimationLayerMixerPlayable mixer;
        public List<ActiveClip> active = new List<ActiveClip>();
    }

    private class ActiveClip
    {
        public AnimationClipPlayable clip;
        public int port;
        public string presetId;
        public double stopDsp;
        public bool stopped;
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

    private bool _wasPlaying = true;

    private void Update()
    {
        if (_rhythm == null) return;

        bool isPlaying = _rhythm.IsPlaying;
        if (isPlaying != _wasPlaying)
        {
            _wasPlaying = isPlaying;
            UpdateGraphsPlayState(isPlaying);
        }

        if (!_armed || _data == null) return;
        if (!isPlaying) return;

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

        UpdateAllClipStops();
        UpdateActiveParticles();
    }

    private void UpdateGraphsPlayState(bool play)
    {
        foreach (var kv in _graphs)
        {
            var state = kv.Value;
            if (state == null || !state.graph.IsValid()) continue;
            if (play) state.graph.Play();
            else state.graph.Stop();
        }

        for (int i = 0; i < _activeParticles.Count; i++)
        {
            var p = _activeParticles[i];
            if (p.go == null) continue;
            var pss = p.go.GetComponentsInChildren<ParticleSystem>(true);
            for (int j = 0; j < pss.Length; j++)
            {
                if (pss[j] == null) continue;
                if (play) pss[j].Play(false);
                else pss[j].Pause(false);
            }
        }
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
            StopActiveByPresetId(key, preset.presetId);
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

        var state = GetOrBuildGraph(key, animator);
        AddClipToMixer(state, preset.animationClip, speed, preset.presetId, durationSec);
    }

    private GraphState GetOrBuildGraph(string key, Animator animator)
    {
        GraphState existing;
        if (_graphs.TryGetValue(key, out existing) && existing != null && existing.graph.IsValid())
            return existing;

        var state = new GraphState();
        state.animator = animator;
        state.graph = PlayableGraph.Create("EffectGraph_" + key);
        state.output = AnimationPlayableOutput.Create(state.graph, "AnimOut", animator);
        state.mixer = AnimationLayerMixerPlayable.Create(state.graph, 0);
        state.output.SetSourcePlayable(state.mixer);
        state.graph.Play();
        _graphs[key] = state;
        return state;
    }

    private void AddClipToMixer(GraphState state, AnimationClip clip, float speed, string presetId, double durationSec)
    {
        var clipPlayable = AnimationClipPlayable.Create(state.graph, clip);
        clipPlayable.SetSpeed(speed);

        int port = state.mixer.AddInput(clipPlayable, 0, 1f);

        var ac = new ActiveClip
        {
            clip = clipPlayable,
            port = port,
            presetId = presetId,
            stopDsp = RhythmConductor.Now + durationSec,
            stopped = false
        };
        state.active.Add(ac);
    }

    private void StopActiveByPresetId(string key, string presetId)
    {
        GraphState state;
        if (!_graphs.TryGetValue(key, out state) || state == null) return;

        for (int i = state.active.Count - 1; i >= 0; i--)
        {
            var ac = state.active[i];
            if (ac.presetId != presetId) continue;

            if (ac.port >= 0 && state.mixer.IsValid())
                state.mixer.DisconnectInput(ac.port);
            if (ac.clip.IsValid()) ac.clip.Destroy();
            state.active.RemoveAt(i);
        }
    }

    private void UpdateAllClipStops()
    {
        double now = RhythmConductor.Now;
        foreach (var kv in _graphs)
        {
            var state = kv.Value;
            if (state == null) continue;

            bool cleanupHappened = false;
            for (int i = state.active.Count - 1; i >= 0; i--)
            {
                if (i >= state.active.Count) continue;
                var ac = state.active[i];
                if (ac.stopped) continue;
                if (now < ac.stopDsp) continue;

                bool isOut = ac.presetId != null && ac.presetId.StartsWith("OUT_", System.StringComparison.OrdinalIgnoreCase);

                if (isOut)
                {
                    CleanupPairedClips(state, ac.presetId);
                    cleanupHappened = true;
                    break;
                }
                else
                {
                    if (ac.clip.IsValid())
                    {
                        var clipAsset = ac.clip.GetAnimationClip();
                        if (clipAsset != null) ac.clip.SetTime(clipAsset.length);
                        ac.clip.SetSpeed(0);
                    }
                    ac.stopped = true;
                }
            }
            if (cleanupHappened) continue;
        }
    }

    private void CleanupPairedClips(GraphState state, string outPresetId)
    {
        string suffix = outPresetId.Length > 4 ? outPresetId.Substring(4) : "";

        for (int i = state.active.Count - 1; i >= 0; i--)
        {
            var ac = state.active[i];
            if (ac.presetId == null) continue;

            bool isPairedIn = ac.presetId.StartsWith("IN_", System.StringComparison.OrdinalIgnoreCase) &&
                              ac.presetId.Length > 3 &&
                              ac.presetId.Substring(3).Equals(suffix, System.StringComparison.OrdinalIgnoreCase);
            bool isSelf = ac.presetId.Equals(outPresetId, System.StringComparison.OrdinalIgnoreCase);

            if (!isPairedIn && !isSelf) continue;

            if (ac.port >= 0 && state.mixer.IsValid())
                state.mixer.DisconnectInput(ac.port);
            if (ac.clip.IsValid()) ac.clip.Destroy();
            state.active.RemoveAt(i);
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
            stopDsp = RhythmConductor.Now + durationSec
        });
    }

    private void UpdateActiveParticles()
    {
        double now = RhythmConductor.Now;
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
            var state = kv.Value;
            if (state == null) continue;
            for (int i = 0; i < state.active.Count; i++)
            {
                if (state.active[i].clip.IsValid()) state.active[i].clip.Destroy();
            }
            if (state.graph.IsValid()) state.graph.Destroy();
        }
        _graphs.Clear();

        for (int i = 0; i < _activeParticles.Count; i++)
        {
            if (_activeParticles[i].go != null) Destroy(_activeParticles[i].go);
        }
        _activeParticles.Clear();
    }
}
