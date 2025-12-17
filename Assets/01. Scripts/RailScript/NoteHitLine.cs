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

    private readonly List<Note> _notes = new List<Note>(64);

    private NoteSpawner.NoteType _laneType;
    public void SetLane(NoteSpawner.NoteType laneType) => _laneType = laneType;

    private void Update()
    {
        CleanupNulls();

        // ★ 아무 것도 안 누르면: ruinMs 지난 뒤 Miss만 찍고 리스트에서만 제거
        AutoMissNoInput();

        // ★ 키 입력 시: 가장 먼저 도착할 노트 1개만 판정(큐처럼)
        if (Input.GetKeyDown(_key))
            JudgeByKeyPress();
    }

    private void JudgeByKeyPress()
    {
        if (_notes.Count == 0) return;

        double now = AudioSettings.dspTime;

        // "가장 먼저 도착할 노트" 선택(안정적)
        int best = -1;
        double bestExpected = double.MaxValue;

        for (int i = 0; i < _notes.Count; i++)
        {
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

        //샷건 치지마!
        if (rawMs < -_ruinMs)
        {
            Debug.Log($"[{_laneType}] [Miss] Early {System.Math.Abs(rawMs):0.0}ms (raw {rawMs:0.0}ms)");

            _notes.RemoveAt(best);
            if (target != null) Destroy(target.gameObject);
            return;
        }

        double absMs = System.Math.Abs(rawMs);

        bool isMiss = absMs > _ruinMs;
        string judge = isMiss ? "Miss" : JudgeName(absMs);
        string earlyLate = rawMs < 0 ? "Early" : "Late";

        Debug.Log($"[{_laneType}] [{judge}] {earlyLate} {System.Math.Abs(rawMs):0.0}ms (raw {rawMs:0.0}ms)");

        // ★ 입력으로 판정한 노트는 Miss든 뭐든 즉시 삭제
        _notes.RemoveAt(best);
        if (target != null) Destroy(target.gameObject);
    }

    private void AutoMissNoInput()
    {
        if (_notes.Count == 0) return;

        double now = AudioSettings.dspTime;

        for (int i = _notes.Count - 1; i >= 0; i--)
        {
            Note n = _notes[i];
            double rawMs = (now - n.ExpectedHitDspTime) * 1000.0 + _userOffsetMs;

            // 무입력 Miss: 노트는 통과해서 계속 가고, 판정만 Miss 처리
            if (rawMs > _ruinMs)
            {
                _notes.RemoveAt(i);
                Debug.Log($"[{_laneType}] [Miss] (no input) Late {rawMs:0.0}ms");
            }
        }
    }

    private string JudgeName(double absMs)
    {
        if (absMs <= _severanceMs) return "Severance";
        if (absMs <= _cleanMs) return "Clean";
        if (absMs <= _traceMs) return "Trace";
        if (absMs <= _fractureMs) return "Fracture";
        if (absMs <= _ruinMs) return "Ruin";
        return "Miss";
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
        // ★ Exit에서 제거하지 않는다 (Late 판정 안정 + 통과 연출)
    }
}
