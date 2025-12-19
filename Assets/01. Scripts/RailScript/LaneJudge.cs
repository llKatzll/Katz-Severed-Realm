using System.Collections.Generic;
using UnityEngine;

public class LaneJudge : MonoBehaviour
{
    [Header("Lane")]
    [SerializeField] private NoteSpawner.NoteType _laneType = NoteSpawner.NoteType.Ground;

    [Header("Input")]
    [SerializeField] private KeyCode _key = KeyCode.A;

    [Header("Timing (ms)")]
    [SerializeField] private float _userOffsetMs = 0f;
    [SerializeField] private float _severanceMs = 30f;
    [SerializeField] private float _cleanMs = 70f;
    [SerializeField] private float _traceMs = 120f;
    [SerializeField] private float _fractureMs = 250f;
    [SerializeField] private float _ruinMs = 350f;

    [Header("FX")]
    [SerializeField] private HitFxPaletteSO _palette;
    [SerializeField] private GameObject _emptyHitPrefab;
    [SerializeField] private float _emptyDestroySec = 0.2f;

    private readonly List<Note> _tapNotes = new List<Note>(64);
    private HoldNote _hold;

    public void SetLaneType(NoteSpawner.NoteType t) => _laneType = t;

    public void RegisterTap(Note n)
    {
        if (n == null) return;
        _tapNotes.Add(n);
    }

    public void RegisterHold(HoldNote h)
    {
        if (h == null) return;
        _hold = h;
    }

    private void Update()
    {
        CleanupDeadTap();
        AutoMissTapNoInput();

        // Hold upkeep checks
        if (_hold != null && _hold.IsActive && !_hold.IsFailed)
        {
            if (!Input.GetKey(_key))
            {
                _hold.Fail();
            }
            else
            {
                double rawTailLateMs = (AudioSettings.dspTime - _hold.TailDspTime) * 1000.0 + _userOffsetMs;
                if (rawTailLateMs > _ruinMs)
                {
                    _hold.Fail();
                }
            }
        }

        if (Input.GetKeyDown(_key))
            OnKeyDown();

        if (Input.GetKeyUp(_key))
            OnKeyUp();
    }

    private void OnKeyDown()
    {
        // If hold exists and not started yet, head judgement first
        if (_hold != null && !_hold.IsActive && !_hold.IsFailed)
        {
            JudgeHoldHead();
            return;
        }

        JudgeTap();
    }

    private void OnKeyUp()
    {
        // If holding, tail judgement
        if (_hold != null && _hold.IsActive && !_hold.IsFailed)
        {
            JudgeHoldTail();
            return;
        }

        SpawnEmptyHit();
    }

    private void JudgeTap()
    {
        Note target = PickEarliestTap();
        if (target == null)
        {
            SpawnEmptyHit();
            return;
        }

        double rawMs = (AudioSettings.dspTime - target.ExpectedHitDspTime) * 1000.0 + _userOffsetMs;

        // DO NOT SHOTGUN
        if (rawMs < -_ruinMs)
        {
            SpawnEmptyHit();
            return;
        }

        JudgeType judge = JudgeFromRawMs(rawMs);

        RemoveTap(target);

        if (judge != JudgeType.Miss)
            SpawnHitFx(judge);

        Destroy(target.gameObject);
    }

    private void JudgeHoldHead()
    {
        double rawMs = (AudioSettings.dspTime - _hold.HeadDspTime) * 1000.0 + _userOffsetMs;

        if (rawMs < -_ruinMs)
        {
            SpawnEmptyHit();
            return;
        }

        JudgeType judge = JudgeFromRawMs(rawMs);

        if (judge == JudgeType.Miss)
        {
            _hold.Fail();
            return;
        }

        // Head success (+1 combo)
        SpawnHitFx(judge);
        _hold.StartHold();
    }

    private void JudgeHoldTail()
    {
        double rawMs = (AudioSettings.dspTime - _hold.TailDspTime) * 1000.0 + _userOffsetMs;

        if (rawMs < -_ruinMs)
        {
            _hold.Fail();
            return;
        }

        JudgeType judge = JudgeFromRawMs(rawMs);

        if (judge == JudgeType.Miss)
        {
            _hold.Fail();
            return;
        }

        // Tail success (+1 combo)
        SpawnHitFx(judge);
        _hold.SuccessAndDestroy();
    }

    private JudgeType JudgeFromRawMs(double rawMs)
    {
        double absMs = System.Math.Abs(rawMs);

        if (absMs <= _severanceMs) return JudgeType.Severance;
        if (absMs <= _cleanMs) return JudgeType.Clean;
        if (absMs <= _traceMs) return JudgeType.Trace;
        if (absMs <= _fractureMs) return JudgeType.Fracture;
        if (absMs <= _ruinMs) return JudgeType.Ruin;
        return JudgeType.Miss;
    }

    private void SpawnHitFx(JudgeType judge)
    {
        if (_palette == null) return;
        if (_palette.hitFxPrefab == null) return;
        if (judge == JudgeType.Miss) return;

        GameObject fx = Instantiate(_palette.hitFxPrefab, transform.position, transform.rotation);


        bool doOverride = !(_laneType == NoteSpawner.NoteType.Upper && judge == JudgeType.Severance);

        if (doOverride)
        {
            Color c;
            bool overrideColor;
            bool ok = _palette.TryGetColor(_laneType, judge, out c, out overrideColor);

            if (ok)
            {

                var renderers = fx.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    var r = renderers[i];
                    if (r == null) continue;

                    var mpb = new MaterialPropertyBlock();
                    r.GetPropertyBlock(mpb);

                    mpb.SetColor("_BaseColor", c);
                    mpb.SetColor("_Color", c);
                    mpb.SetColor("_TintColor", c);
                    mpb.SetColor("_EmissionColor", c);

                    r.SetPropertyBlock(mpb);
                }
            }
        }

        if (_palette.fxDestroySec > 0f)
            Destroy(fx, _palette.fxDestroySec);
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
