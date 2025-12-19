using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private Note _defaultTapPrefab;
    [SerializeField] private HoldNote _defaultHoldPrefab;

    public enum NoteType { Ground, Upper }
    public enum NoteForm { Tap, Hold }

    [System.Serializable]
    public class NoteLane
    {
        public string _laneName;
        public NoteType _noteType;

        public Transform _space;
        public Transform _spawnPoint;
        public Transform _hitPoint;
        public Transform _despawnPoint;

        public Transform _noteParent;
        public LaneJudge _judge;

        [Header("Optional Per Lane Prefab Override")]
        public Note _tapPrefab;
        public HoldNote _holdPrefab;
    }

    [SerializeField] private NoteLane[] _lanes;

    [Header("Timing")]
    [SerializeField] private float _baseApproachTime = 4.0f;
    [SerializeField] private float _noteSpeed = 1.0f;
    private float CurrentApproachTime => _baseApproachTime / Mathf.Max(0.0001f, _noteSpeed);

    [Header("Test Auto Spawn")]
    [SerializeField] private bool _autoSpawn = true;
    [SerializeField] private float _spawnInterval = 1.0f;
    private float _timer;

    [Header("Spawn Mode")]
    [SerializeField] private bool _spawnRandomSingleLane = true;
    [SerializeField] private bool _avoidSameLaneTwice = true;

    [Header("Lane Filter")]
    [SerializeField] private bool _spawnGroundOnly = false;

    [Header("Form")]
    [SerializeField] private NoteForm _spawnForm = NoteForm.Tap;

    [Header("Hold Test Config")]
    [SerializeField] private float _holdMinSec = 0.6f;
    [SerializeField] private float _holdMaxSec = 1.8f;

    private int _lastPickedLaneIndex = -1;

    private void Update()
    {
        if (!_autoSpawn) return;

        _timer += Time.deltaTime;
        if (_timer < _spawnInterval) return;
        _timer -= _spawnInterval;

        if (_spawnRandomSingleLane)
            SpawnRandomSingle();
    }

    private void SpawnRandomSingle()
    {
        List<int> candidates = BuildCandidateLaneIndices();
        if (candidates.Count == 0) return;

        int pick = PickIndex(candidates);

        if (_avoidSameLaneTwice && candidates.Count >= 2 && pick == _lastPickedLaneIndex)
        {
            int retry = PickIndex(candidates);
            if (retry != pick) pick = retry;
        }

        _lastPickedLaneIndex = pick;
        SpawnOnLane(_lanes[pick]);
    }

    private List<int> BuildCandidateLaneIndices()
    {
        var list = new List<int>(16);

        for (int i = 0; i < _lanes.Length; i++)
        {
            var lane = _lanes[i];
            if (lane == null) continue;
            if (lane._spawnPoint == null || lane._hitPoint == null) continue;
            if (lane._despawnPoint == null) continue; // pass-through always

            if (_spawnGroundOnly && lane._noteType != NoteType.Ground)
                continue;

            list.Add(i);
        }

        return list;
    }

    private int PickIndex(List<int> candidates)
    {
        int r = Random.Range(0, candidates.Count);
        return candidates[r];
    }

    private void SpawnOnLane(NoteLane lane)
    {
        float travelTime = CurrentApproachTime;
        Transform space = lane._space != null ? lane._space : lane._spawnPoint.parent;
        Transform parent = lane._noteParent != null ? lane._noteParent : null;

        LaneJudge judge = lane._judge != null ? lane._judge : lane._hitPoint.GetComponent<LaneJudge>();
        if (judge != null)
            judge.SetLaneType(lane._noteType);

        if (_spawnForm == NoteForm.Tap)
        {
            Note prefab = lane._tapPrefab != null ? lane._tapPrefab : _defaultTapPrefab;
            if (prefab == null) return;

            Note note = Instantiate(prefab, parent);
            note.InitFollow(space, lane._spawnPoint, lane._hitPoint, lane._despawnPoint, travelTime, lane._noteType);

            if (judge != null) judge.RegisterTap(note);
            return;
        }

        // Hold
        HoldNote holdPrefab = lane._holdPrefab != null ? lane._holdPrefab : _defaultHoldPrefab;
        if (holdPrefab == null) return;

        HoldNote hold = Instantiate(holdPrefab, parent);
        hold.InitFollow(space, lane._spawnPoint, lane._hitPoint, lane._despawnPoint, travelTime, lane._noteType);

        float dur = Random.Range(_holdMinSec, _holdMaxSec);
        hold.SetupHoldDuration(dur);

        if (judge != null) judge.RegisterHold(hold);
    }
}
