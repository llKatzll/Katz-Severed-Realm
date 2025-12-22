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
    [SerializeField] private float _severanceMs = 35f;
    [SerializeField] private float _cleanMs = 80f;
    [SerializeField] private float _traceMs = 135f;
    [SerializeField] private float _fractureMs = 200f;
    [SerializeField] private float _ruinMs = 300f;

    [Header("FX")]
    [SerializeField] private HitFxPaletteSO _palette;
    [SerializeField] private GameObject _emptyHitPrefab;
    [SerializeField] private float _emptyDestroySec = 0.2f;

    [Header("Hold Loop FX")]
    [SerializeField] private GameObject _holdLoopGroundPrefab;
    [SerializeField] private GameObject _holdLoopUpperPrefab;
    [SerializeField] private Transform _holdLoopAnchor;
    [SerializeField] private float _holdLoopStopDelaySec = 0.15f;

    private GameObject _holdLoopInstance;

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

        AutoFailHoldNoInputOrLate();

        if (Input.GetKeyDown(_key))
            OnKeyDown();

        if (Input.GetKeyUp(_key))
            OnKeyUp();
    }

    private void AutoFailHoldNoInputOrLate()
    {
        if (_hold == null) return;
        if (_hold.IsFailed) return;

        if (!_hold.IsActive)
        {
            double rawHeadLateMs = (AudioSettings.dspTime - _hold.HeadDspTime) * 1000.0 + _userOffsetMs;
            if (rawHeadLateMs > _ruinMs)
            {
                _hold.Fail();
                StopHoldLoopFx();
            }
            return;
        }

        if (!Input.GetKey(_key))
        {
            _hold.Fail();
            StopHoldLoopFx();
            return;
        }

        double rawTailLateMs = (AudioSettings.dspTime - _hold.TailDspTime) * 1000.0 + _userOffsetMs;
        if (rawTailLateMs > _ruinMs)
        {
            _hold.Fail();
            StopHoldLoopFx();
        }
    }

    private void OnKeyDown()
    {
        if (_hold != null && !_hold.IsActive && !_hold.IsFailed)
        {
            JudgeHoldHead();
            return;
        }

        JudgeTap();
    }

    private void OnKeyUp()
    {
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
            StopHoldLoopFx();
            return;
        }

        SpawnHitFx(judge);
        _hold.StartHold();
        StartHoldLoopFx();
    }

    private void JudgeHoldTail()
    {
        double rawMs = (AudioSettings.dspTime - _hold.TailDspTime) * 1000.0 + _userOffsetMs;

        if (rawMs < -_ruinMs)
        {
            _hold.Fail();
            StopHoldLoopFx();
            return;
        }

        JudgeType judge = JudgeFromRawMs(rawMs);

        if (judge == JudgeType.Miss)
        {
            _hold.Fail();
            StopHoldLoopFx();
            return;
        }

        SpawnHitFx(judge);
        StopHoldLoopFx();
        _hold.SuccessAndDestroy();
        _hold = null;
    }

    private void StartHoldLoopFx()
    {
        if (_holdLoopInstance != null) return;

        GameObject prefab = null;
        if (_laneType == NoteSpawner.NoteType.Ground) prefab = _holdLoopGroundPrefab;
        if (_laneType == NoteSpawner.NoteType.Upper) prefab = _holdLoopUpperPrefab;
        if (prefab == null) return;

        Transform anchor = _holdLoopAnchor != null ? _holdLoopAnchor : transform;

        _holdLoopInstance = Instantiate(prefab, anchor.position, anchor.rotation);
        _holdLoopInstance.transform.SetParent(anchor, true);
    }

    private void StopHoldLoopFx()
    {
        if (_holdLoopInstance == null) return;

        var pss = _holdLoopInstance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < pss.Length; i++)
        {
            var ps = pss[i];
            if (ps == null) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        Destroy(_holdLoopInstance, Mathf.Max(0f, _holdLoopStopDelaySec));
        _holdLoopInstance = null;
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

        Color c;
        bool ok = _palette.TryGetColor(_laneType, judge, out c);

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
                mpb.SetColor("_StartColor", c);

                r.SetPropertyBlock(mpb);
            }

            var pss2 = fx.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < pss2.Length; i++)
            {
                var ps = pss2[i];
                if (ps == null) continue;

                var main = ps.main;
                main.startColor = c;
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

        if (_holdLoopInstance != null && _hold == null)
        {
            StopHoldLoopFx();
        }
    }
}
