using System.Collections.Generic;
using UnityEngine;

public class NoteHitLine : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode _key = KeyCode.A;

    [Header("Timing (ms)")]
    [SerializeField] private float _userOffsetMs = 0f;
    [SerializeField] private float _severanceMs = 35f;
    [SerializeField] private float _cleanMs = 75f;
    [SerializeField] private float _traceMs = 120f;
    [SerializeField] private float _fractureMs = 170f;
    [SerializeField] private float _ruinMs = 220f;

    [Header("FX Palette")]
    [SerializeField] private HitFxPaletteSO _palette;

    [Header("Empty Hit FX")]
    [SerializeField] private GameObject _emptyHitPrefab;
    [SerializeField] private float _emptyFxDestroySec = 0.2f;

    private readonly List<Note> _notes = new List<Note>(64);

    private NoteSpawner.NoteType _laneType;
    public void SetLane(NoteSpawner.NoteType laneType) => _laneType = laneType;

    private void Update()
    {
        CleanupNulls();
        AutoMissNoInput();

        if (Input.GetKeyDown(_key))
            JudgeByKeyPress();
    }

    private void JudgeByKeyPress()
    {
        if (_notes.Count == 0)
        {
            SpawnEmptyHitEffect();
            return;
        }

        int best = -1;
        double bestExpected = double.MaxValue;

        for (int i = 0; i < _notes.Count; i++)
        {
            Note n = _notes[i];
            if (n == null) continue;

            double expected = n.ExpectedHitDspTime;
            if (expected < bestExpected)
            {
                bestExpected = expected;
                best = i;
            }
        }

        if (best < 0)
        {
            SpawnEmptyHitEffect();
            return;
        }

        Note target = _notes[best];

        double now = AudioSettings.dspTime;
        double rawMs = (now - target.ExpectedHitDspTime) * 1000.0 + _userOffsetMs;

        // early shotgun -> empty hit, do NOT consume note
        if (rawMs < -_ruinMs)
        {
            SpawnEmptyHitEffect();
            return;
        }

        double absMs = System.Math.Abs(rawMs);
        JudgeType judge = GetJudge(absMs);

        //for log (__)/
        string earlyLate = rawMs < 0 ? "Early" : "Late";
        Debug.Log("[" + _laneType + "] [" + judge + "] " + earlyLate + " " +
                  System.Math.Abs(rawMs).ToString("0.0") + "ms (raw " +
                  rawMs.ToString("0.0") + "ms)");

        _notes.RemoveAt(best);

        if (judge != JudgeType.Miss)
            SpawnJudgeEffect(judge);

        if (target != null)
            Destroy(target.gameObject);
    }

    private void AutoMissNoInput()
    {
        if (_notes.Count == 0) return;

        double now = AudioSettings.dspTime;

        for (int i = _notes.Count - 1; i >= 0; i--)
        {
            Note n = _notes[i];
            if (n == null)
            {
                _notes.RemoveAt(i);
                continue;
            }

            double rawMs = (now - n.ExpectedHitDspTime) * 1000.0 + _userOffsetMs;

            // no-input miss: remove from list only, note keeps moving and despawns later
            if (rawMs > _ruinMs)
            {
                _notes.RemoveAt(i);
                Debug.Log("[" + _laneType + "] [Miss] (no input) Late " + rawMs.ToString("0.0") + "ms");
            }
        }
    }

    private JudgeType GetJudge(double absMs)
    {
        if (absMs <= _severanceMs) return JudgeType.Severance;
        if (absMs <= _cleanMs) return JudgeType.Clean;
        if (absMs <= _traceMs) return JudgeType.Trace;
        if (absMs <= _fractureMs) return JudgeType.Fracture;
        if (absMs <= _ruinMs) return JudgeType.Ruin;
        return JudgeType.Miss;
    }

    private void SpawnEmptyHitEffect()
    {
        if (_emptyHitPrefab == null) return;

        GameObject fx = Instantiate(_emptyHitPrefab, transform.position, transform.rotation);
        if (_emptyFxDestroySec > 0f) Destroy(fx, _emptyFxDestroySec);
    }

    private void SpawnJudgeEffect(JudgeType judge)
    {
        if (_palette == null) return;
        if (_palette.hitFxPrefab == null) return;

        // Miss is filtered earlier but keep safe
        if (judge == JudgeType.Miss) return;

        GameObject fx = Instantiate(_palette.hitFxPrefab, transform.position, transform.rotation);

        Color c;
        bool overrideColor;
        if (_palette.TryGetColor(_laneType, judge, out c, out overrideColor) && overrideColor)
        {
            ApplyColorToEffect(fx, c);
        }

        if (_palette.fxDestroySec > 0f)
            Destroy(fx, _palette.fxDestroySec);
    }

    private void ApplyColorToEffect(GameObject fxRoot, Color c)
    {
        ParticleSystem[] pss = fxRoot.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < pss.Length; i++)
        {
            ParticleSystem ps = pss[i];
            if (ps == null) continue;

            var main = ps.main;
            main.startColor = c;
        }

        Renderer[] renderers = fxRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
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

    private void CleanupNulls()
    {
        for (int i = _notes.Count - 1; i >= 0; i--)
            if (_notes[i] == null) _notes.RemoveAt(i);
    }

    private void OnTriggerEnter(Collider other)
    {
        Note note = other.GetComponent<Note>();
        if (note == null) note = other.GetComponentInParent<Note>();
        if (note == null) return;

        if (!_notes.Contains(note))
            _notes.Add(note);
    }

    private void OnTriggerExit(Collider other)
    {
        // do nothing (late stability + pass-through)
    }
}
