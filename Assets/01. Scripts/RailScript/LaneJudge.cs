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
    [SerializeField] private float _holdJudgeBonusMs = 5f; // Hold is easier than Tap (+5ms)

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

    [Header("Debug Tail")]
    [SerializeField] private bool _debugTail = true;

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

        // IMPORTANT:
        // - No auto "tail success" processing.
        // - But if player keeps holding past tail without KeyUp, it becomes Miss and loop FX must stop.
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

        double rawMs = (AudioSettings.dspTime - _hold.HeadDspTime) * 1000.0 + _userOffsetMs;

        // Hold uses wider window (tap + bonus)
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
            _hold = null;
            return;
        }

        SpawnHoldHeadFx(judge, laneType);

        _hold.StartHold();

        Color c = GetJudgeColor(laneType, judge);
        StartHoldLoopFx(c, laneType);
    }

    private void TryFinishHoldByTail()
    {
        if (_hold == null) return;

        NoteSpawner.NoteType laneType = _hold.NoteType;

        double nowDsp = AudioSettings.dspTime;
        double rawMs = (nowDsp - _hold.TailDspTime) * 1000.0 + _userOffsetMs;

        Debug.LogWarning("TailJudge lane=" + laneType + " nowDsp=" + nowDsp.ToString("F6") + " tailDsp=" + _hold.TailDspTime.ToString("F6") + " rawMs=" + rawMs.ToString("F2"));


        
        if (rawMs < -HoldRuinMs)
        {
            _hold.Fail(_palette, laneType);
            StopHoldLoopFx();
            return;
        }

        JudgeType judge = JudgeFromRawMsHold(rawMs);

        if (judge == JudgeType.Miss)
        {
            _hold.Fail(_palette, laneType);
            StopHoldLoopFx();
            return;
        }

        SpawnHoldTailFx(judge, laneType);

        StopHoldLoopFx();
        _hold.SuccessAndDestroy();
        _hold = null;
    }


    private void AutoFailHoldIfTailIgnored()
    {
        if (_hold == null) return;
        if (_hold.IsFailed) return;
        if (!_hold.IsActive) return;

        if (!Input.GetKey(_key)) return;

        double now = AudioSettings.dspTime;
        double rawTailMs = (now - _hold.TailDspTime) * 1000.0 + _userOffsetMs;

        // tail missed (held too long) => fail + stop FX
        if (rawTailMs > HoldRuinMs)
        {
            NoteSpawner.NoteType laneType = _hold.NoteType;

            _hold.Fail(_palette, laneType);
            StopHoldLoopFx();

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

        double rawMs = (AudioSettings.dspTime - target.ExpectedHitDspTime) * 1000.0 + _userOffsetMs;

        if (rawMs < -_ruinMs)
        {
            SpawnEmptyHit();
            return;
        }

        JudgeType judge = JudgeFromRawMs(rawMs);

        RemoveTap(target);

        if (judge != JudgeType.Miss)
            SpawnTapHitFx(judge, target.NoteType);

        Destroy(target.gameObject);
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
        {
            Debug.LogWarning("PaletteMissing lane=" + laneType + " judge=" + judge);
            return c;
        }

        Color tmp;
        bool ok = _palette.TryGetColor(laneType, judge, out tmp);
        if (!ok)
        {
            Debug.LogWarning("PaletteColorMissing lane=" + laneType + " judge=" + judge);
            return c;
        }

        return tmp;
    }

    private void ApplyFxColor(GameObject fx, Color c)
    {
        if (fx == null) return;

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

            var mats = r.materials;
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m];
                if (mat == null) continue;

                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
                if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", c);
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", c);
                if (mat.HasProperty("_StartColor")) mat.SetColor("_StartColor", c);
            }
        }

        var pss = fx.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < pss.Length; i++)
        {
            var ps = pss[i];
            if (ps == null) continue;

            var main = ps.main;
            main.startColor = c;

            var col = ps.colorOverLifetime;
            if (col.enabled) col.color = new ParticleSystem.MinMaxGradient(c);

            var bySpeed = ps.colorBySpeed;
            if (bySpeed.enabled) bySpeed.color = new ParticleSystem.MinMaxGradient(c);

            var trails = ps.trails;
            if (trails.enabled) trails.colorOverLifetime = new ParticleSystem.MinMaxGradient(c);
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
