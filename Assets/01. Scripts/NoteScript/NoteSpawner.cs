using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private Note _defaultTapPrefab;
    [SerializeField] private HoldNote _defaultHoldPrefab;

    public enum NoteType { Ground, Upper }
    public enum SpawnMode { TapOnly, HoldOnly, Mixed }

    [System.Serializable]
    public class NoteLane
    {
        public string _laneName;
        public NoteType _noteType;

        public Transform _spawnPoint;
        public Transform _hitPoint;
        public Transform _despawnPoint;

        public Transform _noteParent;
        public LaneJudge _judge;

        public Note _tapPrefab;
        public HoldNote _holdPrefab;

        public float _yOffsetLocal;
    }

    [SerializeField] private NoteLane[] _lanes;

    [Header("Timing")]
    [SerializeField] private float _baseApproachTime = 2.5f;
    [SerializeField] private float _noteSpeed = 5f;
    private float CurrentApproachTime => _baseApproachTime / Mathf.Max(0.0001f, _noteSpeed);

    [Header("Auto Spawn")]
    [SerializeField] private bool _autoSpawn = true;
    [SerializeField] private float _spawnInterval = 0.15f;
    private float _timer;

    [Header("Spawn Mode")]
    [SerializeField] private SpawnMode _spawnMode = SpawnMode.TapOnly;
    [SerializeField] private bool _spawnRandomSingleLane = true;
    [SerializeField] private bool _avoidSameLaneTwice = true;

    [Header("Lane Filter")]
    [SerializeField] private bool _spawnGroundOnly = false;

    [Header("Hold Config")]
    [SerializeField] private float _holdSeconds = 1.0f;
    [SerializeField] private bool _preventHoldOverlapOnSameLane = true;

    private int _lastPickedLaneIndex = -1;

    private readonly Dictionary<int, HoldNote> _aliveHoldByLane = new Dictionary<int, HoldNote>(32);

    private void Update()
    {
        CleanupHoldLocks();

        if (!_autoSpawn) return;

        _timer += Time.deltaTime;
        if (_timer < _spawnInterval) return;
        _timer -= _spawnInterval;

        if (_spawnRandomSingleLane)
            SpawnRandomSingle();
    }

    private void CleanupHoldLocks()
    {
        if (_aliveHoldByLane.Count == 0) return;

        var keys = new List<int>(_aliveHoldByLane.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            int laneIndex = keys[i];
            HoldNote h = _aliveHoldByLane[laneIndex];
            if (h == null)
                _aliveHoldByLane.Remove(laneIndex);
        }
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
        SpawnOnLane(pick, _lanes[pick]);
    }

    private List<int> BuildCandidateLaneIndices()
    {
        var list = new List<int>(16);

        for (int i = 0; i < _lanes.Length; i++)
        {
            var lane = _lanes[i];
            if (lane == null) continue;

            if (lane._spawnPoint == null || lane._hitPoint == null || lane._despawnPoint == null)
                continue;

            if (_spawnGroundOnly && lane._noteType != NoteType.Ground)
                continue;

            if (_spawnMode == SpawnMode.HoldOnly && _preventHoldOverlapOnSameLane)
            {
                if (_aliveHoldByLane.ContainsKey(i))
                    continue;
            }

            list.Add(i);
        }

        return list;
    }

    private int PickIndex(List<int> candidates)
    {
        int r = Random.Range(0, candidates.Count);
        return candidates[r];
    }

    private void SpawnOnLane(int laneIndex, NoteLane lane)
    {
        float travelTime = CurrentApproachTime;

        Transform parent = lane._noteParent;

        LaneJudge judge = lane._judge != null ? lane._judge : lane._hitPoint.GetComponent<LaneJudge>();
        if (judge != null) judge.SetLaneType(lane._noteType);

        if (_spawnMode == SpawnMode.TapOnly)
        {
            SpawnTap(lane, parent, judge, travelTime);
            return;
        }

        if (_spawnMode == SpawnMode.HoldOnly)
        {
            SpawnHold(laneIndex, lane, parent, judge, travelTime);
            return;
        }

        if (Random.value < 0.5f)
            SpawnTap(lane, parent, judge, travelTime);
        else
            SpawnHold(laneIndex, lane, parent, judge, travelTime);
    }

    private void SpawnTap(NoteLane lane, Transform parent, LaneJudge judge, float travelTime)
    {
        Note prefab = lane._tapPrefab != null ? lane._tapPrefab : _defaultTapPrefab;
        if (prefab == null) return;

        Note note = Instantiate(prefab);

        note.InitFollow(
            lane._hitPoint,
            lane._spawnPoint,
            lane._hitPoint,
            lane._despawnPoint,
            travelTime,
            lane._noteType,
            lane._yOffsetLocal
        );

        if (parent != null) note.transform.SetParent(parent, true);

        if (judge != null) judge.RegisterTap(note);
    }

    private void SpawnHold(int laneIndex, NoteLane lane, Transform parent, LaneJudge judge, float travelTime)
    {
        if (_preventHoldOverlapOnSameLane && _aliveHoldByLane.ContainsKey(laneIndex))
            return;

        HoldNote prefab = lane._holdPrefab != null ? lane._holdPrefab : _defaultHoldPrefab;
        if (prefab == null) return;

        HoldNote hold = Instantiate(prefab);

        hold.InitFollow(
            lane._hitPoint,
            lane._spawnPoint,
            lane._hitPoint,
            lane._despawnPoint,
            travelTime,
            lane._noteType,
            lane._yOffsetLocal
        );

        if (parent != null) hold.transform.SetParent(parent, true);

        hold.SetupHoldSeconds(_holdSeconds);

        bool registered = true;
        if (judge != null)
            registered = judge.RegisterHold(hold);

        if (!registered)
        {
            Destroy(hold.gameObject);
            return;
        }

        _aliveHoldByLane[laneIndex] = hold;
    }
}
