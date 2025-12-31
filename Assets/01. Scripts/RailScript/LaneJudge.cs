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

    [Header("Hold Judge Bonus (ms)")]
    [SerializeField] private float _holdJudgeBonusMs = 10f;

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
        if (_hold != null && !_hold.IsFailed && !_hold.IsActive)
        {
            TryStartHoldByHead();
            return;
        }

        JudgeTap();
    }

    private void OnKeyUp()
    {
        if (_hold != null && !_hold.IsFailed && _hold.IsActive)
        {
            TryFinishHoldByTail();
        }
    }

    private double HoldSevMs => _severanceMs + _holdJudgeBonusMs;
    private double HoldCleanMs => _cleanMs + _holdJudgeBonusMs;
    private double HoldTraceMs => _traceMs + _holdJudgeBonusMs;
    private double HoldFractureMs => _fractureMs + _holdJudgeBonusMs;
    private double HoldRuinMs => _ruinMs + _holdJudgeBonusMs;

    private void TryStartHoldByHead()
    {
        if (_hold == null) return;

        NoteSpawner.NoteType laneType = _hold.NoteType;

        if (!CanJudgeNoteDimension(_hold.Dimension, false))
        {
            if (_enableDimensionDebug)
                Debug.Log("[LaneJudge] Hold start blocked - wrong dimension: " + _hold.Dimension);

            SpawnEmptyHit();
            return;
        }

        double rawMs = (AudioSettings.dspTime - _hold.HeadDspTime) * 1000.0 + _userOffsetMs;

        if (rawMs < -HoldRuinMs)
        {
            SpawnEmptyHit();
            return;
        }

        JudgeType judge = JudgeFromRawMsHold(rawMs);

        if (judge == JudgeType.Miss)
        {
            _hold.Fail(_palette, laneType);
            StopHoldLoopFx();

            if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");

            _hold = null;
            return;
        }

        SpawnHoldHeadFx(judge, laneType);

        if (ComboUI.I != null)
        {
            float bpm = 120f;
            RhythmConductor rhy = FindObjectOfType<RhythmConductor>();
            if (rhy != null) bpm = (float)rhy.Bpm;

            bool breaks = (judge == JudgeType.Ruin || judge == JudgeType.Miss);
            ComboUI.I.OnHoldStart(judge.ToString(), breaks, bpm, _key);
        }

        _hold.StartHold();

        Color c = GetJudgeColor(laneType, judge);
        StartHoldLoopFx(c, laneType);
    }

    private void TryFinishHoldByTail()
    {
        if (_hold == null) return;

        NoteSpawner.NoteType laneType = _hold.NoteType;

        // Note: Hold in progress - dimension agnostic (always judgeable)
        // CanJudgeNoteDimension(_hold.Dimension, true) would return true
        double nowDsp = AudioSettings.dspTime;
        double rawMs = (nowDsp - _hold.TailDspTime) * 1000.0 + _userOffsetMs;

        if (rawMs < -HoldRuinMs)
        {
            _hold.Fail(_palette, laneType);
            StopHoldLoopFx();

            if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");

            return;
        }

        JudgeType judge = JudgeFromRawMsHold(rawMs);

        if (judge == JudgeType.Miss)
        {
            _hold.Fail(_palette, laneType);
            StopHoldLoopFx();

            if (ComboUI.I != null) ComboUI.I.OnHoldFail("Miss");

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

        if (rawHeadMs > HoldRuinMs)
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

        if (rawTailMs > HoldRuinMs)
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
        if (target == null)
        {
            SpawnEmptyHit();
            return;
        }

        if (!CanJudgeNoteDimension(target.Dimension, false))
        {
            if (_enableDimensionDebug)
                Debug.Log("[LaneJudge] Tap blocked - wrong dimension: " + target.Dimension);

            // Wrong dimension = forced Miss
            if (ComboUI.I != null)
                ComboUI.I.OnTapResult("Miss", true);

            SpawnEmptyHit();
            return;
        }

        double rawMs = (AudioSettings.dspTime - target.ExpectedHitDspTime) * 1000.0 + _userOffsetMs;

        if (rawMs < -_ruinMs)
        {
            SpawnEmptyHit();
            return;
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
    }

    private bool CanJudgeNoteDimension(DimensionType noteDimension, bool isLongNoteInProgress)
    {
        if (DimensionManager.I == null) return true; // No dimension system = always ok
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

    private JudgeType JudgeFromRawMsHold(double rawMs)
    {
        double absMs = Math.Abs(rawMs);

        if (absMs <= HoldSevMs) return JudgeType.Severance;
        if (absMs <= HoldCleanMs) return JudgeType.Clean;
        if (absMs <= HoldTraceMs) return JudgeType.Trace;
        if (absMs <= HoldFractureMs) return JudgeType.Fracture;
        if (absMs <= HoldRuinMs) return JudgeType.Ruin;
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

    private void ApplyFxColor(GameObject fx, Color c)
    {
        if (fx == null) return;

        var pss = fx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in pss)
        {
            if (ps == null) continue;

            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(c);

            var col = ps.colorOverLifetime;
            if (col.enabled)
            {
                Gradient newGrad = new Gradient();
                newGrad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
                );
                col.color = new ParticleSystem.MinMaxGradient(newGrad);
            }

            var colSpeed = ps.colorBySpeed;
            if (colSpeed.enabled)
            {
                colSpeed.color = new ParticleSystem.MinMaxGradient(c);
            }

            var trails = ps.trails;
            if (trails.enabled)
            {
                Gradient trailGrad = new Gradient();
                trailGrad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
                );
                trails.colorOverLifetime = new ParticleSystem.MinMaxGradient(trailGrad);
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