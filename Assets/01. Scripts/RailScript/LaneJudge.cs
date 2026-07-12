using System;
using System.Collections.Generic;
using UnityEngine;

public class LaneJudge : MonoBehaviour
{
    [Header("Lane")]
    [SerializeField] private NoteSpawner.NoteType _laneType = NoteSpawner.NoteType.Ground;
    [SerializeField] private int _laneIndex;

    [Header("Default Key (identifies lane slot)")]
    [SerializeField] private KeyCode _key = KeyCode.A;

    [Header("Timing (ms)")]
    [SerializeField] private float _userOffsetMs = 0f;
    [SerializeField] private float _severanceMs = 35f;
    [SerializeField] private float _cleanMs = 80f;
    [SerializeField] private float _traceMs = 120f;
    [SerializeField] private float _fractureMs = 155f;
    [SerializeField] private float _ruinMs = 200f;

    [Header("Hold Tail Judge Bonus (ms)")]
    [SerializeField] private float _holdTailBonusMs = 10f;

    [Header("Palette")]
    [SerializeField] private HitFxPaletteSO _palette;

    [Header("Tap FX")]
    [SerializeField] private GameObject _tapHitFxPrefab;
    [SerializeField] private float _tapHitFxDestroySec = 0.3f;

    [Header("Hold FX (3 Prefabs)")]
    [SerializeField] private GameObject _holdHeadFxPrefab;
    [SerializeField] private float _holdHeadFxDestroySec = 0.3f;

    [SerializeField] private GameObject _holdTailFxPrefab;
    [SerializeField] private float _holdTailFxDestroySec = 0.3f;

    [SerializeField] private GameObject _holdLoopFxGroundPrefab;
    [SerializeField] private GameObject _holdLoopFxUpperPrefab;
    [SerializeField] private float _holdLoopFxStopDestroySec = 0.2f;

    [Header("Empty Hit")]
    [SerializeField] private GameObject _emptyHitPrefab;
    [SerializeField] private float _emptyDestroySec = 0.2f;

    private GameObject _holdLoopFx;
    private readonly List<Note> _tapNotes = new List<Note>(64);
    private HoldNote _hold;
    private readonly List<HoldNote> _holdQueue = new List<HoldNote>(8);
    private RhythmConductor _rhythm;
    private int _slotIndex;

    private static readonly int IdColor = Shader.PropertyToID("_Color");
    private static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int IdTintColor = Shader.PropertyToID("_TintColor");
    private static readonly int IdEmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int IdColorBlend = Shader.PropertyToID("_ColorBlend");

    private MaterialPropertyBlock _mpb;
    private readonly List<ParticleSystemVertexStream> _streams = new List<ParticleSystemVertexStream>(16);
    private readonly List<ParticleSystem> _pssList = new List<ParticleSystem>(16);
    private readonly Gradient _fxGradient = new Gradient();
    private readonly GradientColorKey[] _fxColorKeys = new GradientColorKey[2];
    private readonly GradientAlphaKey[] _fxAlphaKeys = new GradientAlphaKey[2];


    private KeyCode ActiveKey => _laneType == NoteSpawner.NoteType.Ground
        ? SettingsConfig.GetGroundKey(_slotIndex)
        : SettingsConfig.GetUpperKey(_slotIndex);

    private float EffectiveOffsetMs => _userOffsetMs + SettingsConfig.InputOffsetSec * 1000f;

    public void SetLaneType(NoteSpawner.NoteType t)
    {
        _laneType = t;
        _slotIndex = ResolveSlotIndex();
    }

    private void Awake()
    {
        _rhythm = FindAnyObjectByType<RhythmConductor>();
        _slotIndex = ResolveSlotIndex();
    }

    private int ResolveSlotIndex()
    {
        KeyCode[] defaults = _laneType == NoteSpawner.NoteType.Ground
            ? SettingsConfig.DefaultGroundKeys
            : SettingsConfig.DefaultUpperKeys;
        for (int i = 0; i < defaults.Length; i++)
            if (defaults[i] == _key) return i;
        return Mathf.Clamp(_laneIndex, 0, SettingsConfig.LaneCount - 1);
    }

    public void RegisterTap(Note n)
    {
        if (n == null) return;
        _tapNotes.Add(n);
    }

    public bool RegisterHold(HoldNote h)
    {
        if (h == null) return false;
        _holdQueue.Add(h);
        PromoteNextHold();
        return true;
    }

    private void PromoteNextHold()
    {
        for (int i = _holdQueue.Count - 1; i >= 0; i--)
        {
            if (_holdQueue[i] == null) _holdQueue.RemoveAt(i);
        }

        if (_hold != null) return;
        if (_holdQueue.Count == 0) return;

        HoldNote next = null;
        double minTime = double.MaxValue;
        int nextIdx = -1;
        for (int i = 0; i < _holdQueue.Count; i++)
        {
            double t = _holdQueue[i].HeadDspTime;
            if (t < minTime)
            {
                minTime = t;
                next = _holdQueue[i];
                nextIdx = i;
            }
        }

        if (next != null)
        {
            _hold = next;
            _holdQueue.RemoveAt(nextIdx);
        }
    }

    private void Update()
    {
        PromoteNextHold();

        CleanupDeadTap();
        CleanupDeadHold();

        if (_rhythm != null && !_rhythm.IsPlaying) return;

        if (AutoPlay.IsOn)
        {
            AutoPlayUpdate();
            return;
        }

        AutoMissTapNoInput();
        AutoMissHoldNoInput();

        AutoFailHoldIfTailIgnored();

        KeyCode key = ActiveKey;
        if (Input.GetKeyDown(key)) OnKeyDown();
        if (Input.GetKeyUp(key)) OnKeyUp();
    }

    private void AutoPlayUpdate()
    {
        double now = RhythmConductor.Now;

        for (int i = _tapNotes.Count - 1; i >= 0; i--)
        {
            Note n = _tapNotes[i];
            if (n == null) { _tapNotes.RemoveAt(i); continue; }
            if (now >= n.ExpectedHitDspTime)
            {
                _tapNotes.RemoveAt(i);
                AutoJudgeTap(n);
            }
        }

        if (_hold != null && !_hold.IsFailed)
        {
            if (!_hold.IsActive && now >= _hold.HeadDspTime)
            {
                AutoStartHold();
            }
            if (_hold != null && _hold.IsActive && now >= _hold.TailDspTime)
            {
                AutoFinishHold();
            }
        }
    }

    private void AutoJudgeTap(Note target)
    {
        if (target == null) return;

        JudgeType judge = AutoPlay.RollJudge();
        NoteSpawner.NoteType laneType = target.NoteType;

        if (SfxManager.I != null) SfxManager.I.PlayHit();

        if (ComboUI.I != null) ComboUI.I.OnTapResult(judge.ToString(), false);
        if (ScoreManager.I != null) ScoreManager.I.ReportJudge(judge);

        SpawnTapHitFx(judge, laneType);
        Destroy(target.gameObject);
    }

    private void AutoStartHold()
    {
        if (_hold == null) return;

        JudgeType judge = AutoPlay.RollJudge();
        NoteSpawner.NoteType laneType = _hold.NoteType;

        if (SfxManager.I != null) SfxManager.I.PlayHit();
        SpawnHoldHeadFx(judge, laneType);

        if (ComboUI.I != null)
        {
            float bpm = 120f;
            if (_rhythm != null) bpm = (float)_rhythm.Bpm;
            ComboUI.I.OnHoldStart(judge.ToString(), false, bpm, ActiveKey);
        }
        if (ScoreManager.I != null) ScoreManager.I.ReportJudge(judge);

        _hold.StartHold();

        Color c = GetJudgeColor(laneType, judge);
        StartHoldLoopFx(c, laneType);
    }

    private void AutoFinishHold()
    {
        if (_hold == null) return;

        JudgeType judge = AutoPlay.RollJudge();
        NoteSpawner.NoteType laneType = _hold.NoteType;

        SpawnHoldTailFx(judge, laneType);

        if (ComboUI.I != null) ComboUI.I.OnHoldEnd(judge.ToString(), false);
        if (ScoreManager.I != null) ScoreManager.I.ReportJudge(judge);

        StopHoldLoopFx();
        _hold.SuccessAndDestroy();
        _hold = null;
    }

    private void OnKeyDown()
    {
        Note earliestTap = PickEarliestTap();

        bool hasValidHold = _hold != null && !_hold.IsFailed && !_hold.IsActive;
        bool hasValidTap = earliestTap != null;

        if (!hasValidHold && !hasValidTap)
        {
            SpawnEmptyHit();
            return;
        }

        if (hasValidHold && hasValidTap)
        {
            double holdHeadTime = _hold.HeadDspTime;
            double tapTime = earliestTap.ExpectedHitDspTime;

            if (holdHeadTime <= tapTime)
            {
                if (TryStartHoldByHead())
                    return;
                JudgeTap();
            }
            else
            {
                if (JudgeTapInternal(earliestTap))
                    return;
                TryStartHoldByHead();
            }
        }
        else if (hasValidHold)
        {
            if (!TryStartHoldByHead())
            {
                SpawnEmptyHit();
            }
        }
        else
        {
            JudgeTap();
        }
    }

    private void OnKeyUp()
    {
        if (_hold != null && !_hold.IsFailed && _hold.IsActive)
        {
            TryFinishHoldByTail();
        }
    }

    private bool TryStartHoldByHead()
    {
        if (_hold == null) return false;

        NoteSpawner.NoteType laneType = _hold.NoteType;

        double rawMs = (RhythmConductor.Now - _hold.HeadDspTime) * 1000.0 + EffectiveOffsetMs;

        if (rawMs < -_ruinMs)
        {
            return false;
        }

        JudgeType judge = JudgeFromRawMs(rawMs);

        if (judge == JudgeType.Miss)
        {
            _hold.Fail(_palette, laneType);
            StopHoldLoopFx();

            if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");
            if (ScoreManager.I != null) ScoreManager.I.ReportJudge(JudgeType.Miss);

            _hold = null;
            return true;
        }

        if (SfxManager.I != null) SfxManager.I.PlayHit();
        SpawnHoldHeadFx(judge, laneType);

        if (ComboUI.I != null)
        {
            float bpm = 120f;
            if (_rhythm != null) bpm = (float)_rhythm.Bpm;

            bool breaks = (judge == JudgeType.Ruin || judge == JudgeType.Miss);
            ComboUI.I.OnHoldStart(judge.ToString(), breaks, bpm, ActiveKey);
        }

        if (ScoreManager.I != null) ScoreManager.I.ReportJudge(judge);

        _hold.StartHold();

        Color c = GetJudgeColor(laneType, judge);
        StartHoldLoopFx(c, laneType);

        return true;
    }

    private void TryFinishHoldByTail()
    {
        if (_hold == null) return;

        NoteSpawner.NoteType laneType = _hold.NoteType;

        double nowDsp = RhythmConductor.Now;
        double rawMs = (nowDsp - _hold.TailDspTime) * 1000.0 + EffectiveOffsetMs;

        double tailWindow = _ruinMs + _holdTailBonusMs;

        if (rawMs < -tailWindow)
        {
            _hold.Fail(_palette, laneType);
            StopHoldLoopFx();

            if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");
            if (ScoreManager.I != null) ScoreManager.I.ReportJudge(JudgeType.Miss);

            _hold = null;
            return;
        }

        JudgeType judge = JudgeFromRawMsTail(rawMs);

        if (judge == JudgeType.Miss)
        {
            _hold.Fail(_palette, laneType);
            StopHoldLoopFx();

            if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");
            if (ScoreManager.I != null) ScoreManager.I.ReportJudge(JudgeType.Miss);

            _hold = null;
            return;
        }

        SpawnHoldTailFx(judge, laneType);

        if (ComboUI.I != null)
        {
            bool breaks = (judge == JudgeType.Ruin || judge == JudgeType.Miss);
            ComboUI.I.OnHoldEnd(judge.ToString(), breaks);
        }

        if (ScoreManager.I != null) ScoreManager.I.ReportJudge(judge);

        StopHoldLoopFx();
        _hold.SuccessAndDestroy();
        _hold = null;
    }

    private void AutoMissHoldNoInput()
    {
        if (_hold == null) return;
        if (_hold.IsFailed) return;
        if (_hold.IsActive) return;

        double now = RhythmConductor.Now;
        double rawHeadMs = (now - _hold.HeadDspTime) * 1000.0 + EffectiveOffsetMs;

        if (rawHeadMs > _ruinMs)
        {
            NoteSpawner.NoteType laneType = _hold.NoteType;

            _hold.Fail(_palette, laneType);
            StopHoldLoopFx();

            if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");
            if (ScoreManager.I != null) ScoreManager.I.ReportJudge(JudgeType.Miss);

            _hold = null;
        }
    }

    private void AutoFailHoldIfTailIgnored()
    {
        if (_hold == null) return;
        if (_hold.IsFailed) return;
        if (!_hold.IsActive) return;

        if (!Input.GetKey(ActiveKey)) return;

        double now = RhythmConductor.Now;
        double rawTailMs = (now - _hold.TailDspTime) * 1000.0 + EffectiveOffsetMs;

        double tailWindow = _ruinMs + _holdTailBonusMs;

        if (rawTailMs > tailWindow)
        {
            NoteSpawner.NoteType laneType = _hold.NoteType;

            _hold.Fail(_palette, laneType);
            StopHoldLoopFx();

            if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");
            if (ScoreManager.I != null) ScoreManager.I.ReportJudge(JudgeType.Miss);

            _hold = null;
        }
    }

    private void CleanupDeadHold()
    {
        if (_hold == null) return;

        if (_hold.gameObject == null)
        {
            _hold = null;
        }
    }

    private void JudgeTap()
    {
        Note target = PickEarliestTap();
        JudgeTapInternal(target);
    }

    private bool JudgeTapInternal(Note target)
    {
        if (target == null)
        {
            SpawnEmptyHit();
            return false;
        }

        double rawMs = (RhythmConductor.Now - target.ExpectedHitDspTime) * 1000.0 + EffectiveOffsetMs;

        if (rawMs < -_ruinMs)
        {
            SpawnEmptyHit();
            return false;
        }

        JudgeType judge = JudgeFromRawMs(rawMs);

        if (ComboUI.I != null)
        {
            bool breaks = (judge == JudgeType.Ruin || judge == JudgeType.Miss);
            ComboUI.I.OnTapResult(judge.ToString(), breaks);
        }

        if (ScoreManager.I != null) ScoreManager.I.ReportJudge(judge);

        RemoveTap(target);

        if (judge != JudgeType.Miss)
        {
            if (SfxManager.I != null) SfxManager.I.PlayHit();
            SpawnTapHitFx(judge, target.NoteType);
        }

        Destroy(target.gameObject);
        return true;
    }

    private JudgeType JudgeFromRawMs(double rawMs)
    {
        double absMs = Math.Abs(rawMs);

        if (absMs <= _severanceMs) return JudgeType.Severance;
        if (absMs <= _cleanMs) return JudgeType.Clean;
        if (absMs <= _traceMs) return JudgeType.Trace;
        if (absMs <= _fractureMs) return JudgeType.Fracture;
        if (absMs <= _ruinMs) return JudgeType.Ruin;
        return JudgeType.Miss;
    }

    private JudgeType JudgeFromRawMsTail(double rawMs)
    {
        double absMs = Math.Abs(rawMs);
        double bonus = _holdTailBonusMs;

        if (absMs <= _severanceMs + bonus) return JudgeType.Severance;
        if (absMs <= _cleanMs + bonus) return JudgeType.Clean;
        if (absMs <= _traceMs + bonus) return JudgeType.Trace;
        if (absMs <= _fractureMs + bonus) return JudgeType.Fracture;
        if (absMs <= _ruinMs + bonus) return JudgeType.Ruin;
        return JudgeType.Miss;
    }

    private Color GetJudgeColor(NoteSpawner.NoteType laneType, JudgeType judge)
    {
        Color c = Color.white;

        if (_palette == null)
            return c;

        Color tmp;
        bool ok = _palette.TryGetColor(laneType, judge, out tmp);

        if (!ok)
            return c;

        return tmp;
    }

    private static Color FixAlpha(Color c)
    {
        if (c.a <= 0.0001f) c.a = 1f;
        return c;
    }

    private Gradient BuildFadeGradient(Color c)
    {
        _fxColorKeys[0] = new GradientColorKey(c, 0f);
        _fxColorKeys[1] = new GradientColorKey(c, 1f);
        _fxAlphaKeys[0] = new GradientAlphaKey(c.a, 0f);
        _fxAlphaKeys[1] = new GradientAlphaKey(0f, 1f);
        _fxGradient.SetKeys(_fxColorKeys, _fxAlphaKeys);
        return _fxGradient;
    }

    private void ApplyFxColor(GameObject fx, Color c)
    {
        if (fx == null) return;

        c = FixAlpha(c);

        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        fx.GetComponentsInChildren(true, _pssList);
        for (int i = 0; i < _pssList.Count; i++)
        {
            var ps = _pssList[i];
            if (ps == null) continue;

            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(c);

            var col = ps.colorOverLifetime;
            if (col.enabled)
                col.color = new ParticleSystem.MinMaxGradient(BuildFadeGradient(c));

            var colSpeed = ps.colorBySpeed;
            if (colSpeed.enabled)
                colSpeed.color = new ParticleSystem.MinMaxGradient(c);

            var trails = ps.trails;
            if (trails.enabled)
                trails.colorOverLifetime = new ParticleSystem.MinMaxGradient(BuildFadeGradient(c));

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
            {
                mat.EnableKeyword("_EMISSION");
            }
        }
    }


    private GameObject SpawnPooled(GameObject prefab, Vector3 pos, Quaternion rot, float life)
    {
        if (prefab == null) return null;

        if (FxPoolManager.I != null)
            return FxPoolManager.I.Spawn(prefab, pos, rot, life);

        GameObject fx = Instantiate(prefab, pos, rot);
        if (life > 0f) Destroy(fx, life);
        return fx;
    }

    private void SpawnTapHitFx(JudgeType judge, NoteSpawner.NoteType laneType)
    {
        if (judge == JudgeType.Miss) return;

        GameObject prefab = _tapHitFxPrefab != null ? _tapHitFxPrefab : (_palette != null ? _palette.hitFxPrefab : null);
        if (prefab == null) return;

        float life = _tapHitFxDestroySec > 0f ? _tapHitFxDestroySec : (_palette != null ? _palette.fxDestroySec : 0.3f);
        GameObject fx = SpawnPooled(prefab, transform.position, transform.rotation, life);
        if (fx == null) return;

        Color c = GetJudgeColor(laneType, judge);
        ApplyFxColor(fx, c);
    }

    private void SpawnHoldHeadFx(JudgeType judge, NoteSpawner.NoteType laneType)
    {
        if (judge == JudgeType.Miss) return;
        if (_holdHeadFxPrefab == null) return;

        GameObject fx = SpawnPooled(_holdHeadFxPrefab, transform.position, transform.rotation, _holdHeadFxDestroySec);
        if (fx == null) return;

        Color c = GetJudgeColor(laneType, judge);
        ApplyFxColor(fx, c);
    }

    private void SpawnHoldTailFx(JudgeType judge, NoteSpawner.NoteType laneType)
    {
        if (judge == JudgeType.Miss) return;
        if (_holdTailFxPrefab == null) return;

        GameObject fx = SpawnPooled(_holdTailFxPrefab, transform.position, transform.rotation, _holdTailFxDestroySec);
        if (fx == null) return;

        Color c = GetJudgeColor(laneType, judge);
        ApplyFxColor(fx, c);
    }

    private void StartHoldLoopFx(Color c, NoteSpawner.NoteType laneType)
    {
        if (_holdLoopFx != null) return;

        GameObject prefab = null;
        if (laneType == NoteSpawner.NoteType.Ground) prefab = _holdLoopFxGroundPrefab;
        else if (laneType == NoteSpawner.NoteType.Upper) prefab = _holdLoopFxUpperPrefab;

        if (prefab == null) return;

        _holdLoopFx = SpawnPooled(prefab, transform.position, transform.rotation, 0f);
        if (_holdLoopFx == null) return;

        ApplyFxColor(_holdLoopFx, c);
    }

    private void StopHoldLoopFx()
    {
        if (_holdLoopFx == null) return;

        if (FxPoolManager.I != null)
        {
            FxPoolManager.I.ReturnDelayed(_holdLoopFx, _holdLoopFxStopDestroySec);
        }
        else
        {
            if (_holdLoopFxStopDestroySec > 0f) Destroy(_holdLoopFx, _holdLoopFxStopDestroySec);
            else Destroy(_holdLoopFx);
        }

        _holdLoopFx = null;
    }

    private void SpawnEmptyHit()
    {
        if (_emptyHitPrefab == null) return;

        GameObject fx = SpawnPooled(_emptyHitPrefab, transform.position, transform.rotation, _emptyDestroySec);
        if (fx == null) return;

        if (RuntimeColorPalette.I != null)
        {
            Color c = RuntimeColorPalette.I.GetRailColor(_laneType);
            ApplyFxColor(fx, c);
        }
    }

    private Note PickEarliestTap()
    {
        Note best = null;
        double bestTime = double.MaxValue;

        for (int i = 0; i < _tapNotes.Count; i++)
        {
            Note n = _tapNotes[i];
            if (n == null) continue;

            double t = n.ExpectedHitDspTime;
            if (t < bestTime)
            {
                bestTime = t;
                best = n;
            }
        }

        return best;
    }

    private void RemoveTap(Note n)
    {
        for (int i = _tapNotes.Count - 1; i >= 0; i--)
        {
            if (_tapNotes[i] == n) _tapNotes.RemoveAt(i);
        }
    }

    private void AutoMissTapNoInput()
    {
        double now = RhythmConductor.Now;

        for (int i = _tapNotes.Count - 1; i >= 0; i--)
        {
            Note n = _tapNotes[i];
            if (n == null) { _tapNotes.RemoveAt(i); continue; }

            double rawMs = (now - n.ExpectedHitDspTime) * 1000.0 + EffectiveOffsetMs;
            if (rawMs > _ruinMs)
            {
                _tapNotes.RemoveAt(i);

                if (ComboUI.I != null)
                    ComboUI.I.OnTapResult("Miss", true);
                if (ScoreManager.I != null)
                    ScoreManager.I.ReportJudge(JudgeType.Miss);
            }
        }
    }

    private void CleanupDeadTap()
    {
        for (int i = _tapNotes.Count - 1; i >= 0; i--)
        {
            if (_tapNotes[i] == null) _tapNotes.RemoveAt(i);
        }
    }
}