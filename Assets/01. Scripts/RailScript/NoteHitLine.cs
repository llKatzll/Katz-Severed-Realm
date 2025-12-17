using System.Collections.Generic;
using UnityEngine;

public class NoteHitLine : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode _key = KeyCode.A;

    [Header("Timing (ms)")]
    [SerializeField] private float _userOffsetMs = 0f;
    [SerializeField] private float _severanceMs = 45f;
    [SerializeField] private float _cleanMs = 80f;
    [SerializeField] private float _traceMs = 150f;
    [SerializeField] private float _fractureMs = 250f;
    [SerializeField] private float _ruinMs = 350f;

    [Header("Hit Effect")]
    [SerializeField] private GameObject _hitFxPrefab;
    [SerializeField] private float _fxDestroySec = 2.0f;

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
            return;

        double now = AudioSettings.dspTime;

        int best = -1;
        double bestExpected = double.MaxValue;

        for (int i = 0; i < _notes.Count; i++)
        {
            if (_notes[i] == null) continue;

            double expected = _notes[i].ExpectedHitDspTime;
            if (expected < bestExpected)
            {
                bestExpected = expected;
                best = i;
            }
        }

        if (best < 0) return;

        Note target = _notes[best];
        double rawMs = (now - target.ExpectedHitDspTime) * 1000.0 + _userOffsetMs;

        // Early shotgun: do not delete future note. Just treat as empty miss.
        if (rawMs < -_ruinMs)
        {
            Debug.Log("[" + _laneType + "] [Miss] (empty) Early " +
                      System.Math.Abs(rawMs).ToString("0.0") + "ms (raw " +
                      rawMs.ToString("0.0") + "ms)");
            return;
        }

        double absMs = System.Math.Abs(rawMs);
        bool isMiss = absMs > _ruinMs;

        string judge = isMiss ? "Miss" : JudgeName(absMs);
        string earlyLate = rawMs < 0 ? "Early" : "Late";

        Debug.Log("[" + _laneType + "] [" + judge + "] " + earlyLate + " " +
                  System.Math.Abs(rawMs).ToString("0.0") + "ms (raw " +
                  rawMs.ToString("0.0") + "ms)");

        _notes.RemoveAt(best);

        if (!isMiss)
            SpawnHitEffect(judge);

        if (target != null) Destroy(target.gameObject);
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

            // No input miss: log only, do not destroy the note (it should keep going)
            if (rawMs > _ruinMs)
            {
                _notes.RemoveAt(i);
                Debug.Log("[" + _laneType + "] [Miss] (no input) Late " + rawMs.ToString("0.0") + "ms");
            }
        }
    }

    private string JudgeName(double absMs)
    {
        if (absMs <= _severanceMs) return "Severance"; //ÆÛÆåÆ®++
        if (absMs <= _cleanMs) return "Clean"; //ÆÛÆåÆ®
        if (absMs <= _traceMs) return "Trace"; //±Â
        if (absMs <= _fractureMs) return "Fracture"; //¹èµå
        if (absMs <= _ruinMs) return "Ruin"; //ÁøÂ¥ ¹èµå
        return "Miss"; //¹Ì½º
    }

    private void SpawnHitEffect(string judge)
    {
        if (_hitFxPrefab == null) return;
        if (judge == "Miss") return;

        GameObject fx = Instantiate(_hitFxPrefab, transform.position, transform.rotation);

        bool overrideColor = !(_laneType == NoteSpawner.NoteType.Upper && judge == "Severance");
        if (overrideColor)
        {
            Color c = GetJudgeColor(_laneType, judge);
            ApplyColorToEffect(fx, c);
        }

        if (_fxDestroySec > 0f)
            Destroy(fx, _fxDestroySec);
    }

    private Color GetJudgeColor(NoteSpawner.NoteType laneType, string judge)
    {
        if (judge == "Ruin") judge = "Fracture";

        if (laneType == NoteSpawner.NoteType.Ground)
        {
            if (judge == "Severance") return Color.white;
            if (judge == "Clean") return new Color(1.0f, 0.85f, 0.25f);
            if (judge == "Trace") return new Color(0.25f, 1.0f, 0.45f);
            if (judge == "Fracture") return new Color(0.75f, 0.25f, 1.0f);
        }
        else if (laneType == NoteSpawner.NoteType.Upper)
        {
            if (judge == "Clean") return new Color(1.0f, 0.95f, 0.35f);
            if (judge == "Trace") return new Color(0.30f, 1.0f, 0.55f);
            if (judge == "Fracture") return new Color(0.75f, 0.25f, 1.0f);
        }

        return Color.white;
    }

    private void ApplyColorToEffect(GameObject fxRoot, Color c)
    {
        var pss = fxRoot.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < pss.Length; i++)
        {
            var ps = pss[i];
            if (ps == null) continue;

            var main = ps.main;
            main.startColor = c;
        }

        var renderers = fxRoot.GetComponentsInChildren<Renderer>(true);
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

    private void CleanupNulls()
    {
        for (int i = _notes.Count - 1; i >= 0; i--)
            if (_notes[i] == null) _notes.RemoveAt(i);
    }

    private void OnTriggerEnter(Collider other)
    {
        Note note = other.GetComponent<Note>() ?? other.GetComponentInParent<Note>();
        if (note == null) return;

        if (!_notes.Contains(note))
            _notes.Add(note);
    }

    private void OnTriggerExit(Collider other)
    {
        // Intentionally do nothing.
    }
}
