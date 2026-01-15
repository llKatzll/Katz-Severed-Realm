using System;
using System.Collections.Generic;
using UnityEngine;

public class LaneJudge : MonoBehaviour
{
    [Header("Lane (fallback only)")]
    [SerializeField] private NoteSpawner.NoteType _laneType = NoteSpawner.NoteType.Ground;

    [Header("Input")]
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

    [Header("Debug")]
    [SerializeField] private bool _enableDimensionDebug = false;

    private GameObject _holdLoopFx;
    private readonly List<Note> _tapNotes = new List<Note>(64);
    private HoldNote _hold;

    private static readonly int IdColor = Shader.PropertyToID("_Color");
    private static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int IdTintColor = Shader.PropertyToID("_TintColor");
    private static readonly int IdEmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int IdColorBlend = Shader.PropertyToID("_ColorBlend");

    private static MaterialPropertyBlock _mpb;
    private static readonly List<ParticleSystemVertexStream> _streams = new List<ParticleSystemVertexStream>(16);


    public void SetLaneType(NoteSpawner.NoteType t) => _laneType = t;

    public void RegisterTap(Note n)
    {
        if (n == null) return;
        _tapNotes.Add(n);
    }

    public bool RegisterHold(HoldNote h)
    {
        if (h == null) return false;
        if (_hold != null) return false;

        _hold = h;
        return true;
    }

    private void Update()
    {
        CleanupDeadTap();
        AutoMissTapNoInput();

        CleanupDeadHold();
        AutoMissHoldNoInput();

        AutoFailHoldIfTailIgnored();

        if (Input.GetKeyDown(_key)) OnKeyDown();
        if (Input.GetKeyUp(_key)) OnKeyUp();
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

        double rawMs = (AudioSettings.dspTime - _hold.HeadDspTime) * 1000.0 + _userOffsetMs;

        if (rawMs < -_ruinMs)
        {
            return false;
        }

        if (!CanJudgeNoteDimension(_hold.Dimension, false))
        {
            if (_enableDimensionDebug)
                Debug.Log("[LaneJudge] Hold start blocked - wrong dimension: " + _hold.Dimension);

            SpawnEmptyHit();
            return true;
        }

        JudgeType judge = JudgeFromRawMs(rawMs);

        if (judge == JudgeType.Miss)
        {
            _hold.Fail(_palette, laneType);
            StopHoldLoopFx();

            if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");

            _hold = null;
            return true;
        }

        SpawnHoldHeadFx(judge, laneType);

        if (ComboUI.I != null)
        {
            float bpm = 120f;
            RhythmConductor rhy = FindObjectOfType<RhythmConductor>(); //ITS WORKING BRO STOP WARNING ABOUT THIS
            if (rhy != null) bpm = (float)rhy.Bpm;

            bool breaks = (judge == JudgeType.Ruin || judge == JudgeType.Miss);
            ComboUI.I.OnHoldStart(judge.ToString(), breaks, bpm, _key);
        }

        _hold.StartHold();

        Color c = GetJudgeColor(laneType, judge);
        StartHoldLoopFx(c, laneType);

        return true;
    }

    private void TryFinishHoldByTail()
    {
        if (_hold == null) return;

        NoteSpawner.NoteType laneType = _hold.NoteType;

        double nowDsp = AudioSettings.dspTime;
        double rawMs = (nowDsp - _hold.TailDspTime) * 1000.0 + _userOffsetMs;

        double tailWindow = _ruinMs + _holdTailBonusMs;

        if (rawMs < -tailWindow)
        {
            _hold.Fail(_palette, laneType);
            StopHoldLoopFx();

            if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");

            _hold = null;
            return;
        }

        JudgeType judge = JudgeFromRawMsTail(rawMs);

        if (judge == JudgeType.Miss)
        {
            _hold.Fail(_palette, laneType);
            StopHoldLoopFx();

            if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");

            _hold = null;
            return;
        }

        SpawnHoldTailFx(judge, laneType);

        if (ComboUI.I != null)
        {
            bool breaks = (judge == JudgeType.Ruin || judge == JudgeType.Miss);
            ComboUI.I.OnHoldEnd(judge.ToString(), breaks);
        }

        StopHoldLoopFx();
        _hold.SuccessAndDestroy();
        _hold = null;
    }

    private void AutoMissHoldNoInput()
    {
        if (_hold == null) return;
        if (_hold.IsFailed) return;
        if (_hold.IsActive) return;

        double now = AudioSettings.dspTime;
        double rawHeadMs = (now - _hold.HeadDspTime) * 1000.0 + _userOffsetMs;

        if (rawHeadMs > _ruinMs)
        {
            NoteSpawner.NoteType laneType = _hold.NoteType;

            _hold.Fail(_palette, laneType);
            StopHoldLoopFx();

            if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");

            _hold = null;
        }
    }

    private void AutoFailHoldIfTailIgnored()
    {
        if (_hold == null) return;
        if (_hold.IsFailed) return;
        if (!_hold.IsActive) return;

        if (!Input.GetKey(_key)) return;

        double now = AudioSettings.dspTime;
        double rawTailMs = (now - _hold.TailDspTime) * 1000.0 + _userOffsetMs;

        double tailWindow = _ruinMs + _holdTailBonusMs;

        if (rawTailMs > tailWindow)
        {
            NoteSpawner.NoteType laneType = _hold.NoteType;

            _hold.Fail(_palette, laneType);
            StopHoldLoopFx();

            if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");

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

        double rawMs = (AudioSettings.dspTime - target.ExpectedHitDspTime) * 1000.0 + _userOffsetMs;

        if (rawMs < -_ruinMs)
        {
            SpawnEmptyHit();
            return false;
        }

        if (!CanJudgeNoteDimension(target.Dimension, false))
        {
            if (_enableDimensionDebug)
                Debug.Log("[LaneJudge] Tap blocked - wrong dimension: " + target.Dimension);

            if (ComboUI.I != null)
                ComboUI.I.OnTapResult("Miss", true);

            SpawnEmptyHit();
            return true;
        }

        JudgeType judge = JudgeFromRawMs(rawMs);

        if (ComboUI.I != null)
        {
            bool breaks = (judge == JudgeType.Ruin || judge == JudgeType.Miss);
            ComboUI.I.OnTapResult(judge.ToString(), breaks);
        }

        RemoveTap(target);

        if (judge != JudgeType.Miss)
            SpawnTapHitFx(judge, target.NoteType);

        Destroy(target.gameObject);
        return true;
    }

    private bool CanJudgeNoteDimension(DimensionType noteDimension, bool isLongNoteInProgress)
    {
        if (DimensionManager.I == null) return true;
        return DimensionManager.I.CanJudgeNote(noteDimension, isLongNoteInProgress);
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

        if (DimensionManager.I != null && DimensionManager.I.IsCorridorActive)
        {
            if (RuntimeColorPalette.I != null)
                return RuntimeColorPalette.I.GetCorridorColor(laneType);
            return DimensionManager.I.GetCorridorHitFxColor(laneType);
        }

        if (judge == JudgeType.Severance)
        {
            if (RuntimeColorPalette.I != null)
                return RuntimeColorPalette.I.GetSevHitFxColor(laneType);
        }

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
            {
                mat.EnableKeyword("_EMISSION");
            }
        }
    }


    private void SpawnTapHitFx(JudgeType judge, NoteSpawner.NoteType laneType)
    {
        if (judge == JudgeType.Miss) return;

        GameObject prefab = _tapHitFxPrefab != null ? _tapHitFxPrefab : (_palette != null ? _palette.hitFxPrefab : null);

        if (prefab == null) return;

        GameObject fx = Instantiate(prefab, transform.position, transform.rotation);

        Color c = GetJudgeColor(laneType, judge);
        ApplyFxColor(fx, c);

        float life = _tapHitFxDestroySec > 0f ? _tapHitFxDestroySec : (_palette != null ? _palette.fxDestroySec : 0.3f);
        if (life > 0f) Destroy(fx, life);
    }

    private void SpawnHoldHeadFx(JudgeType judge, NoteSpawner.NoteType laneType)
    {
        if (judge == JudgeType.Miss) return;

        if (_holdHeadFxPrefab == null) return;

        GameObject fx = Instantiate(_holdHeadFxPrefab, transform.position, transform.rotation);

        Color c = GetJudgeColor(laneType, judge);
        ApplyFxColor(fx, c);

        if (_holdHeadFxDestroySec > 0f) Destroy(fx, _holdHeadFxDestroySec);
    }

    private void SpawnHoldTailFx(JudgeType judge, NoteSpawner.NoteType laneType)
    {
        if (judge == JudgeType.Miss) return;

        if (_holdTailFxPrefab == null) return;

        GameObject fx = Instantiate(_holdTailFxPrefab, transform.position, transform.rotation);

        Color c = GetJudgeColor(laneType, judge);
        ApplyFxColor(fx, c);

        if (_holdTailFxDestroySec > 0f) Destroy(fx, _holdTailFxDestroySec);
    }

    private void StartHoldLoopFx(Color c, NoteSpawner.NoteType laneType)
    {
        if (_holdLoopFx != null) return;

        GameObject prefab = null;
        if (laneType == NoteSpawner.NoteType.Ground) prefab = _holdLoopFxGroundPrefab;
        else if (laneType == NoteSpawner.NoteType.Upper) prefab = _holdLoopFxUpperPrefab;

        if (prefab == null) return;

        _holdLoopFx = Instantiate(prefab, transform.position, transform.rotation);
        ApplyFxColor(_holdLoopFx, c);
    }

    private void StopHoldLoopFx()
    {
        if (_holdLoopFx == null) return;

        if (_holdLoopFxStopDestroySec > 0f) Destroy(_holdLoopFx, _holdLoopFxStopDestroySec);
        else Destroy(_holdLoopFx);

        _holdLoopFx = null;
    }

    private void SpawnEmptyHit()
    {
        if (_emptyHitPrefab == null) return;

        GameObject fx = Instantiate(_emptyHitPrefab, transform.position, transform.rotation);
        if (_emptyDestroySec > 0f) Destroy(fx, _emptyDestroySec);
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
        double now = AudioSettings.dspTime;

        for (int i = _tapNotes.Count - 1; i >= 0; i--)
        {
            Note n = _tapNotes[i];
            if (n == null) { _tapNotes.RemoveAt(i); continue; }

            double rawMs = (now - n.ExpectedHitDspTime) * 1000.0 + _userOffsetMs;
            if (rawMs > _ruinMs)
            {
                _tapNotes.RemoveAt(i);

                if (ComboUI.I != null)
                    ComboUI.I.OnTapResult("Miss", true);
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