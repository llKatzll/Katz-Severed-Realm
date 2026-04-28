using System;
using System.Collections.Generic;
using UnityEngine;

public class DimensionNoteJudge : MonoBehaviour
{
    public static DimensionNoteJudge I { get; private set; }

    [Header("Key")]
    [SerializeField] private KeyBindConfig _keyBindConfig;
    [SerializeField] private KeyCode _fallbackKey = KeyCode.Space;

    [Header("Timing (ms)")]
    [SerializeField] private float _userOffsetMs = 0f;
    [SerializeField] private float _severanceMs = 35f;
    [SerializeField] private float _cleanMs = 80f;
    [SerializeField] private float _traceMs = 120f;
    [SerializeField] private float _fractureMs = 155f;
    [SerializeField] private float _ruinMs = 200f;

    [Header("Hold Tail Bonus (ms)")]
    [SerializeField] private float _holdTailBonusMs = 10f;

    [Header("Palette")]
    [SerializeField] private HitFxPaletteSO _palette;

    [Header("Tap FX")]
    [SerializeField] private GameObject _tapHitFxPrefab;
    [SerializeField] private float _tapHitFxDestroySec = 0.3f;

    [Header("Hold FX")]
    [SerializeField] private GameObject _holdHeadFxPrefab;
    [SerializeField] private float _holdHeadFxDestroySec = 0.3f;
    [SerializeField] private GameObject _holdTailFxPrefab;
    [SerializeField] private float _holdTailFxDestroySec = 0.3f;

    [Header("Empty Hit")]
    [SerializeField] private GameObject _emptyHitPrefab;
    [SerializeField] private float _emptyDestroySec = 0.2f;

    private readonly List<Note> _tapNotes = new List<Note>(32);
    private readonly List<HoldNote> _holds = new List<HoldNote>(8);
    private readonly List<HoldNote> _activeHolds = new List<HoldNote>(8);

    private static MaterialPropertyBlock _mpb;
    private static readonly int IdColor = Shader.PropertyToID("_Color");
    private static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int IdTintColor = Shader.PropertyToID("_TintColor");
    private static readonly int IdEmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int IdColorBlend = Shader.PropertyToID("_ColorBlend");
    private static readonly List<ParticleSystemVertexStream> _streams = new List<ParticleSystemVertexStream>(16);

    private const double GROUP_TOLERANCE_MS = 2.0;

    private KeyCode DimensionKey
    {
        get
        {
            if (_keyBindConfig != null) return _keyBindConfig.DimensionKey;
            return _fallbackKey;
        }
    }

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
    }

    public void RegisterTap(Note n)
    {
        if (n == null) return;
        _tapNotes.Add(n);
    }

    public void RegisterHold(HoldNote h)
    {
        if (h == null) return;
        _holds.Add(h);
    }

    private void Update()
    {
        CleanupDead();
        AutoMissTaps();
        AutoMissHolds();
        AutoFailActiveHolds();

        if (Input.GetKeyDown(DimensionKey)) OnKeyDown();
        if (Input.GetKeyUp(DimensionKey)) OnKeyUp();
    }

    private void OnKeyDown()
    {
        bool judgedAnything = false;

        judgedAnything |= JudgeAllTapsInWindow();
        judgedAnything |= StartAllHoldsInWindow();

        if (!judgedAnything)
            SpawnEmptyHit(transform.position);
    }

    private void OnKeyUp()
    {
        for (int i = _activeHolds.Count - 1; i >= 0; i--)
        {
            HoldNote h = _activeHolds[i];
            if (h == null || h.IsFailed)
            {
                _activeHolds.RemoveAt(i);
                continue;
            }
            TryFinishHold(h);
            _activeHolds.RemoveAt(i);
        }
    }

    private bool JudgeAllTapsInWindow()
    {
        bool any = false;
        double now = AudioSettings.dspTime;

        double earliestHit = double.MaxValue;
        for (int i = 0; i < _tapNotes.Count; i++)
        {
            Note n = _tapNotes[i];
            if (n == null) continue;
            double rawMs = (now - n.ExpectedHitDspTime) * 1000.0 + _userOffsetMs;
            if (rawMs < -_ruinMs) continue;
            if (n.ExpectedHitDspTime < earliestHit)
                earliestHit = n.ExpectedHitDspTime;
        }

        if (earliestHit == double.MaxValue) return false;

        for (int i = _tapNotes.Count - 1; i >= 0; i--)
        {
            Note n = _tapNotes[i];
            if (n == null) { _tapNotes.RemoveAt(i); continue; }

            double diffFromEarliest = Math.Abs(n.ExpectedHitDspTime - earliestHit) * 1000.0;
            if (diffFromEarliest > GROUP_TOLERANCE_MS) continue;

            double rawMs = (now - n.ExpectedHitDspTime) * 1000.0 + _userOffsetMs;
            if (rawMs < -_ruinMs) continue;

            JudgeType judge = JudgeFromRawMs(rawMs);
            NoteSpawner.NoteType laneType = n.NoteType;
            Transform hitRef = n.HitPointRef;
            _tapNotes.RemoveAt(i);

            if (ComboUI.I != null)
            {
                bool breaks = (judge == JudgeType.Ruin || judge == JudgeType.Miss);
                ComboUI.I.OnTapResult(judge.ToString(), breaks);
            }

            if (judge != JudgeType.Miss)
                SpawnJudgedFx(_tapHitFxPrefab, hitRef, laneType, judge, _tapHitFxDestroySec);

            Destroy(n.gameObject);
            any = true;
        }

        return any;
    }

    private bool StartAllHoldsInWindow()
    {
        bool any = false;
        double now = AudioSettings.dspTime;

        double earliestHead = double.MaxValue;
        for (int i = 0; i < _holds.Count; i++)
        {
            HoldNote h = _holds[i];
            if (h == null || h.IsFailed) continue;
            double rawMs = (now - h.HeadDspTime) * 1000.0 + _userOffsetMs;
            if (rawMs < -_ruinMs) continue;
            if (h.HeadDspTime < earliestHead)
                earliestHead = h.HeadDspTime;
        }

        if (earliestHead == double.MaxValue) return false;

        for (int i = _holds.Count - 1; i >= 0; i--)
        {
            HoldNote h = _holds[i];
            if (h == null || h.IsFailed) { _holds.RemoveAt(i); continue; }

            double diffFromEarliest = Math.Abs(h.HeadDspTime - earliestHead) * 1000.0;
            if (diffFromEarliest > GROUP_TOLERANCE_MS) continue;

            double rawMs = (now - h.HeadDspTime) * 1000.0 + _userOffsetMs;
            if (rawMs < -_ruinMs) continue;

            JudgeType judge = JudgeFromRawMs(rawMs);
            NoteSpawner.NoteType laneType = h.NoteType;
            Transform hitRef = h.HitPointRef;
            _holds.RemoveAt(i);

            if (judge == JudgeType.Miss)
            {
                h.Fail(_palette, laneType);
                if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");
                continue;
            }

            if (ComboUI.I != null)
            {
                float bpm = 120f;
                RhythmConductor rhy = FindObjectOfType<RhythmConductor>();
                if (rhy != null) bpm = (float)rhy.Bpm;

                bool breaks = (judge == JudgeType.Ruin || judge == JudgeType.Miss);
                ComboUI.I.OnHoldStart(judge.ToString(), breaks, bpm, DimensionKey);
            }

            SpawnJudgedFx(_holdHeadFxPrefab, hitRef, laneType, judge, _holdHeadFxDestroySec);
            h.StartHold();
            _activeHolds.Add(h);
            any = true;
        }

        return any;
    }

    private void TryFinishHold(HoldNote h)
    {
        if (h == null || h.IsFailed) return;
        if (!h.IsActive) return;

        NoteSpawner.NoteType laneType = h.NoteType;
        Transform hitRef = h.HitPointRef;

        double rawMs = (AudioSettings.dspTime - h.TailDspTime) * 1000.0 + _userOffsetMs;
        double tailWindow = _ruinMs + _holdTailBonusMs;

        if (rawMs < -tailWindow)
        {
            h.Fail(_palette, laneType);
            if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");
            return;
        }

        JudgeType judge = JudgeFromRawMsTail(rawMs);

        if (judge == JudgeType.Miss)
        {
            h.Fail(_palette, laneType);
            if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");
            return;
        }

        SpawnJudgedFx(_holdTailFxPrefab, hitRef, laneType, judge, _holdTailFxDestroySec);

        if (ComboUI.I != null)
        {
            bool breaks = (judge == JudgeType.Ruin || judge == JudgeType.Miss);
            ComboUI.I.OnHoldEnd(judge.ToString(), breaks);
        }

        h.SuccessAndDestroy();
    }

    private void SpawnJudgedFx(GameObject prefab, Transform hitRef, NoteSpawner.NoteType laneType, JudgeType judge, float life)
    {
        if (prefab == null) return;
        if (judge == JudgeType.Miss) return;

        Vector3 pos = hitRef != null ? hitRef.position : transform.position;
        Quaternion rot = hitRef != null ? hitRef.rotation : Quaternion.identity;

        GameObject fx = Instantiate(prefab, pos, rot);

        Color c = GetJudgeColor(laneType, judge);
        ApplyFxColor(fx, c);

        if (life > 0f) Destroy(fx, life);
    }

    private Color GetJudgeColor(NoteSpawner.NoteType laneType, JudgeType judge)
    {
        if (_palette == null) return Color.white;

        Color c;
        if (_palette.TryGetColor(laneType, judge, out c))
            return c;

        return Color.white;
    }

    private static Color FixAlpha(Color c)
    {
        if (c.a <= 0.0001f) c.a = 1f;
        return c;
    }

    private void ApplyFxColor(GameObject fx, Color c)
    {
        if (fx == null) return;

        c = FixAlpha(c);

        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        var pss = fx.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < pss.Length; i++)
        {
            var ps = pss[i];
            if (ps == null) continue;

            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(c);

            var col = ps.colorOverLifetime;
            if (col.enabled)
            {
                Gradient g = new Gradient();
                g.SetKeys(
                    new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                    new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(0f, 1f) }
                );
                col.color = new ParticleSystem.MinMaxGradient(g);
            }

            var colSpeed = ps.colorBySpeed;
            if (colSpeed.enabled)
                colSpeed.color = new ParticleSystem.MinMaxGradient(c);

            var trails = ps.trails;
            if (trails.enabled)
            {
                Gradient tg = new Gradient();
                tg.SetKeys(
                    new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                    new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(0f, 1f) }
                );
                trails.colorOverLifetime = new ParticleSystem.MinMaxGradient(tg);
            }

            var r = ps.GetComponent<ParticleSystemRenderer>();
            if (r == null) continue;

            _streams.Clear();
            r.GetActiveVertexStreams(_streams);

            bool changed = false;
            if (!_streams.Contains(ParticleSystemVertexStream.Color))
            {
                _streams.Add(ParticleSystemVertexStream.Color);
                changed = true;
            }
            if (!_streams.Contains(ParticleSystemVertexStream.Custom1XYZW))
            {
                _streams.Add(ParticleSystemVertexStream.Custom1XYZW);
                changed = true;
            }
            if (changed)
                r.SetActiveVertexStreams(_streams);

            r.GetPropertyBlock(_mpb);

            _mpb.SetColor(IdColor, c);
            _mpb.SetColor(IdBaseColor, c);
            _mpb.SetColor(IdTintColor, c);
            _mpb.SetColor(IdEmissionColor, c);
            _mpb.SetFloat(IdColorBlend, 1f);

            r.SetPropertyBlock(_mpb);

            var mat = r.sharedMaterial;
            if (mat != null && mat.HasProperty(IdEmissionColor))
                mat.EnableKeyword("_EMISSION");
        }
    }

    private void AutoMissTaps()
    {
        double now = AudioSettings.dspTime;
        for (int i = _tapNotes.Count - 1; i >= 0; i--)
        {
            Note n = _tapNotes[i];
            if (n == null) { _tapNotes.RemoveAt(i); continue; }

            double rawMs = (now - n.ExpectedHitDspTime) * 1000.0 + _userOffsetMs;
            if (rawMs > _ruinMs)
            {
                _tapNotes.RemoveAt(i);
                if (ComboUI.I != null) ComboUI.I.OnTapResult("Miss", true);
            }
        }
    }

    private void AutoMissHolds()
    {
        double now = AudioSettings.dspTime;
        for (int i = _holds.Count - 1; i >= 0; i--)
        {
            HoldNote h = _holds[i];
            if (h == null || h.IsFailed) { _holds.RemoveAt(i); continue; }

            double rawMs = (now - h.HeadDspTime) * 1000.0 + _userOffsetMs;
            if (rawMs > _ruinMs)
            {
                _holds.RemoveAt(i);
                h.Fail(_palette, h.NoteType);
                if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");
            }
        }
    }

    private void AutoFailActiveHolds()
    {
        if (!Input.GetKey(DimensionKey)) return;

        double now = AudioSettings.dspTime;
        for (int i = _activeHolds.Count - 1; i >= 0; i--)
        {
            HoldNote h = _activeHolds[i];
            if (h == null || h.IsFailed) { _activeHolds.RemoveAt(i); continue; }

            double rawMs = (now - h.TailDspTime) * 1000.0 + _userOffsetMs;
            if (rawMs > _ruinMs + _holdTailBonusMs)
            {
                h.Fail(_palette, h.NoteType);
                if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");
                _activeHolds.RemoveAt(i);
            }
        }
    }

    private void CleanupDead()
    {
        for (int i = _tapNotes.Count - 1; i >= 0; i--)
        {
            if (_tapNotes[i] == null) _tapNotes.RemoveAt(i);
        }
        for (int i = _holds.Count - 1; i >= 0; i--)
        {
            if (_holds[i] == null || _holds[i].gameObject == null) _holds.RemoveAt(i);
        }
        for (int i = _activeHolds.Count - 1; i >= 0; i--)
        {
            if (_activeHolds[i] == null || _activeHolds[i].gameObject == null) _activeHolds.RemoveAt(i);
        }
    }

    private JudgeType JudgeFromRawMs(double rawMs)
    {
        double abs = Math.Abs(rawMs);
        if (abs <= _severanceMs) return JudgeType.Severance;
        if (abs <= _cleanMs) return JudgeType.Clean;
        if (abs <= _traceMs) return JudgeType.Trace;
        if (abs <= _fractureMs) return JudgeType.Fracture;
        if (abs <= _ruinMs) return JudgeType.Ruin;
        return JudgeType.Miss;
    }

    private JudgeType JudgeFromRawMsTail(double rawMs)
    {
        double abs = Math.Abs(rawMs);
        double b = _holdTailBonusMs;
        if (abs <= _severanceMs + b) return JudgeType.Severance;
        if (abs <= _cleanMs + b) return JudgeType.Clean;
        if (abs <= _traceMs + b) return JudgeType.Trace;
        if (abs <= _fractureMs + b) return JudgeType.Fracture;
        if (abs <= _ruinMs + b) return JudgeType.Ruin;
        return JudgeType.Miss;
    }

    private void SpawnEmptyHit(Vector3 pos)
    {
        if (_emptyHitPrefab == null) return;
        GameObject fx = Instantiate(_emptyHitPrefab, pos, Quaternion.identity);
        if (_emptyDestroySec > 0f) Destroy(fx, _emptyDestroySec);
    }
}
