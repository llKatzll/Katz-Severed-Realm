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
    [SerializeField] private float _traceMs = 120f;
    [SerializeField] private float _fractureMs = 150f;
    [SerializeField] private float _ruinMs = 200f;

    [Header("FX")]
    [SerializeField] private HitFxPaletteSO _palette;
    [SerializeField] private GameObject _emptyHitPrefab;
    [SerializeField] private float _emptyDestroySec = 0.2f;

    [Header("Hold Loop FX")]
    [SerializeField] private GameObject _holdLoopFxGroundPrefab;
    [SerializeField] private GameObject _holdLoopFxUpperPrefab;
    [SerializeField] private float _holdLoopFxDestroySec = 0.2f;

    private GameObject _holdLoopFx;

    private readonly List<Note> _tapNotes = new List<Note>(64);

    private HoldNote _hold; // 한 레일엔 1개만

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
        AutoFailHoldRules();

        if (Input.GetKeyDown(_key)) OnKeyDown();
        if (Input.GetKeyUp(_key)) OnKeyUp();
    }

    private void OnKeyDown()
    {
        // hold가 있고 아직 활성 전이면 head 판정 시도
        if (_hold != null && !_hold.IsFailed && !_hold.IsActive)
        {
            TryStartHoldByHead();
            return;
        }

        // 그 외는 탭 판정
        JudgeTap();
    }

    private void OnKeyUp()
    {
        // hold 활성 중이면 tail 판정
        if (_hold != null && !_hold.IsFailed && _hold.IsActive)
        {
            TryFinishHoldByTail();
        }
    }

    private void TryStartHoldByHead()
    {
        double rawMs = (AudioSettings.dspTime - _hold.HeadDspTime) * 1000.0 + _userOffsetMs;

        // 너무 이른 샷건 방지
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

        // head 성공
        SpawnHitFx(judge);
        _hold.StartHold();
        StartHoldLoopFx();
    }

    private void TryFinishHoldByTail()
    {
        double rawMs = (AudioSettings.dspTime - _hold.TailDspTime) * 1000.0 + _userOffsetMs;

        // 너무 이른 키업은 실패(네 룰: tail 시점에 keyup)
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

        // tail 성공
        SpawnHitFx(judge);
        StopHoldLoopFx();
        _hold.SuccessAndDestroy();
        _hold = null;
    }

    private void AutoFailHoldRules()
    {
        if (_hold == null) return;
        if (_hold.IsFailed) return;

        double now = AudioSettings.dspTime;

        // head 미입력으로 지나가면 fail
        if (!_hold.IsActive)
        {
            double rawHeadMs = (now - _hold.HeadDspTime) * 1000.0 + _userOffsetMs;
            if (rawHeadMs > _ruinMs)
            {
                _hold.Fail();
                StopHoldLoopFx();
            }
            return;
        }

        // 활성 홀드 중에 키를 놓고 있으면 즉시 fail
        if (!Input.GetKey(_key))
        {
            _hold.Fail();
            StopHoldLoopFx();
            return;
        }

        // tail 지나쳤는데도 아직 잡고 있으면 fail (네 룰)
        double rawTailMs = (now - _hold.TailDspTime) * 1000.0 + _userOffsetMs;
        if (rawTailMs > _ruinMs)
        {
            _hold.Fail();
            StopHoldLoopFx();
        }
    }

    private void CleanupDeadHold()
    {
        if (_hold == null) return;
        if (_hold.gameObject == null) _hold = null;
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

    private void StartHoldLoopFx()
    {
        if (_holdLoopFx != null) return;

        GameObject prefab = null;
        if (_laneType == NoteSpawner.NoteType.Ground) prefab = _holdLoopFxGroundPrefab;
        else if (_laneType == NoteSpawner.NoteType.Upper) prefab = _holdLoopFxUpperPrefab;

        if (prefab == null) return;

        _holdLoopFx = Instantiate(prefab, transform.position, transform.rotation);
    }

    private void StopHoldLoopFx()
    {
        if (_holdLoopFx == null) return;

        if (_holdLoopFxDestroySec > 0f) Destroy(_holdLoopFx, _holdLoopFxDestroySec);
        else Destroy(_holdLoopFx);

        _holdLoopFx = null;
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

            var pss = fx.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < pss.Length; i++)
            {
                var ps = pss[i];
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
    }
}
