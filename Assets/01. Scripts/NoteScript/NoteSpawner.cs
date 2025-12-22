using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private Note _defaultTapPrefab;

    public enum NoteType { Ground, Upper }

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

        [Header("Optional Per Lane Override")]
        public Note _tapPrefab;

        [Header("Visual Offset (Local Y)")]
        public float _yOffsetLocal; //Ground 0.05, Upper 0
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
    [SerializeField] private bool _spawnRandomSingleLane = true;
    [SerializeField] private bool _avoidSameLaneTwice = true;

    [Header("Lane Filter")]
    [SerializeField] private bool _spawnGroundOnly = false;

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

            if (lane._spawnPoint == null || lane._hitPoint == null || lane._despawnPoint == null)
                continue;

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

        Transform parent = lane._noteParent;

        LaneJudge judge = lane._judge != null ? lane._judge : lane._hitPoint.GetComponent<LaneJudge>();
        if (judge != null) judge.SetLaneType(lane._noteType);

        Note prefab = lane._tapPrefab != null ? lane._tapPrefab : _defaultTapPrefab;
        if (prefab == null) return;

        //월드에 먼저 소환 (부모 상속으로 꼬이는걸 줄이기)
        Note note = Instantiate(prefab);

        //판정선 기준 로컬로 경로 세팅
        //space = hitPoint (판정선)
        note.InitFollow(
            lane._hitPoint,
            lane._spawnPoint,
            lane._hitPoint,
            lane._despawnPoint,
            travelTime,
            lane._noteType,
            lane._yOffsetLocal
        );

        //나중에 부모 붙이기 (월드 위치 유지)
        if (parent != null) note.transform.SetParent(parent, true);

        if (judge != null) judge.RegisterTap(note);
    }
}
