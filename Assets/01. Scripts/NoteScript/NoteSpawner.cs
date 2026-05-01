using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public enum NoteType { Ground, Upper }
    public enum SpawnForm { Tap, Hold, Mixed, Dimension, DimensionHold, All }

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

    public NoteLane[] Lanes => _lanes;
    public RhythmConductor Conductor => _conductor;

    [SerializeField] private RhythmConductor _conductor;

    [SerializeField] private Note _defaultTapPrefab;
    [SerializeField] private HoldNote _defaultHoldPrefab;

    [Header("Dimension Note Prefabs")]
    [SerializeField] private Note _dimensionTapPrefabGround;
    [SerializeField] private HoldNote _dimensionHoldPrefabGround;
    [SerializeField] private Note _dimensionTapPrefabUpper;
    [SerializeField] private HoldNote _dimensionHoldPrefabUpper;

    [Header("Sorting Order")]
    [SerializeField] private int _sortGroundNote = 0;
    [SerializeField] private int _sortGroundDN = 1;
    [SerializeField] private int _sortUpperNote = 2;
    [SerializeField] private int _sortUpperDN = 3;

    [SerializeField] private NoteLane[] _lanes;

    [Header("Approach")]
    [SerializeField, Range(0.1f, 10f)] private float _noteSpeed = 5f;
    private const float APPROACH_BEATS_AT_MAX_SPEED = 4f / 3.5f;
    private const float MAX_SLIDER_VALUE = 10f;
    private float ApproachBeats => (APPROACH_BEATS_AT_MAX_SPEED * MAX_SLIDER_VALUE) / Mathf.Max(0.0001f, _noteSpeed);

    [Header("Auto Spawn Test")]
    [SerializeField] private bool _autoSpawn = true;
    [SerializeField] private float _spawnIntervalBeats = 0.25f;

    [Header("Spawn Mode")]
    [SerializeField] private bool _spawnRandomSingleLane = true;
    [SerializeField] private bool _avoidSameLaneTwice = true;

    [Header("Form")]
    [SerializeField] private SpawnForm _spawnForm = SpawnForm.Mixed;

    [Header("Hold")]
    [SerializeField] private bool _preventHoldOverlapOnSameLane = true;
    [SerializeField] private float _holdBeats = 1f;

    private double _nextHitBeat;
    private bool _primed;
    private int _lastPickedLaneIndex = -1;

    private readonly Dictionary<int, HoldNote> _aliveHoldByLane = new Dictionary<int, HoldNote>(16);

    private void OnValidate()
    {
        if (_spawnIntervalBeats <= 0f) _spawnIntervalBeats = 0.0001f;
        if (_noteSpeed <= 0f) _noteSpeed = 0.0001f;
        if (_holdBeats < 0f) _holdBeats = 0f;
    }

    private void Start()
    {
        PrimeNextBeat();
    }

    private void OnEnable()
    {
        _primed = false;
        PrimeNextBeat();
    }

    private void PrimeNextBeat()
    {
        if (_primed) return;
        if (_conductor == null) return;
        if (!_conductor.Started) return;

        _nextHitBeat = _conductor.CurrentBeat + ApproachBeats;
        _primed = true;
    }

    private void CleanupHoldDict()
    {
        List<int> deadKeys = null;

        foreach (var kv in _aliveHoldByLane)
        {
            if (kv.Value == null)
            {
                if (deadKeys == null) deadKeys = new List<int>(8);
                deadKeys.Add(kv.Key);
            }
        }

        if (deadKeys != null)
        {
            for (int i = 0; i < deadKeys.Count; i++)
                _aliveHoldByLane.Remove(deadKeys[i]);
        }
    }

    private void Update()
    {
        CleanupHoldDict();

        if (!_autoSpawn) return;
        if (_conductor == null) return;
        if (!_conductor.Started) return;
        if (_lanes == null || _lanes.Length == 0) return;

        PrimeNextBeat();

        double nowBeat = _conductor.CurrentBeat;
        double interval = Mathf.Max(0.0001f, _spawnIntervalBeats);

        int safety = 0;
        while (nowBeat >= (_nextHitBeat - ApproachBeats))
        {
            SpawnOneAtBeat(_nextHitBeat);
            _nextHitBeat += interval;

            safety++;
            if (safety > 256) break;
        }
    }

    private void SpawnOneAtBeat(double hitBeat)
    {
        int laneIndex = PickLaneIndex();
        if (laneIndex < 0) return;

        var lane = _lanes[laneIndex];
        if (lane == null) return;
        if (lane._spawnPoint == null || lane._hitPoint == null || lane._despawnPoint == null) return;

        double headHitDsp = _conductor.DspTimeAtBeat(hitBeat);
        float travelSec = (float)(ApproachBeats * _conductor.SecPerBeat);

        if (_spawnForm == SpawnForm.Tap)
        {
            SpawnTapAtBeat(laneIndex, lane, travelSec, headHitDsp);
            return;
        }

        if (_spawnForm == SpawnForm.Hold)
        {
            SpawnHoldAtBeat(laneIndex, lane, travelSec, headHitDsp);
            return;
        }

        if (_spawnForm == SpawnForm.Dimension)
        {
            SpawnDimensionTapAtBeat(laneIndex, lane, travelSec, headHitDsp);
            return;
        }

        if (_spawnForm == SpawnForm.DimensionHold)
        {
            SpawnDimensionHoldAtBeat(laneIndex, lane, travelSec, headHitDsp);
            return;
        }

        if (_spawnForm == SpawnForm.Mixed)
        {
            if (Random.value < 0.5f) SpawnTapAtBeat(laneIndex, lane, travelSec, headHitDsp);
            else SpawnHoldAtBeat(laneIndex, lane, travelSec, headHitDsp);
            return;
        }

        float roll = Random.value;
        if (roll < 0.35f) SpawnTapAtBeat(laneIndex, lane, travelSec, headHitDsp);
        else if (roll < 0.65f) SpawnHoldAtBeat(laneIndex, lane, travelSec, headHitDsp);
        else if (roll < 0.85f) SpawnDimensionTapAtBeat(laneIndex, lane, travelSec, headHitDsp);
        else SpawnDimensionHoldAtBeat(laneIndex, lane, travelSec, headHitDsp);
    }

    private int PickLaneIndex()
    {
        if (_spawnRandomSingleLane)
        {
            int pick = Random.Range(0, _lanes.Length);

            if (_avoidSameLaneTwice && _lanes.Length >= 2 && pick == _lastPickedLaneIndex)
            {
                int retry = Random.Range(0, _lanes.Length);
                if (retry != pick) pick = retry;
            }

            _lastPickedLaneIndex = pick;
            return pick;
        }

        return 0;
    }

    private LaneJudge GetJudge(NoteLane lane)
    {
        LaneJudge judge = lane._judge != null ? lane._judge : lane._hitPoint.GetComponent<LaneJudge>();
        if (judge != null) judge.SetLaneType(lane._noteType);
        return judge;
    }

    private void SpawnTapAtBeat(int laneIndex, NoteLane lane, float travelSec, double hitDsp)
    {
        Note prefab = lane._tapPrefab != null ? lane._tapPrefab : _defaultTapPrefab;
        if (prefab == null) return;

        var judge = GetJudge(lane);

        Note note = Instantiate(prefab);
        note.InitFollow(
            lane._hitPoint,
            lane._spawnPoint,
            lane._hitPoint,
            lane._despawnPoint,
            travelSec,
            lane._noteType,
            lane._yOffsetLocal
        );

        if (lane._noteParent != null) note.transform.SetParent(lane._noteParent, true);

        note.SetExpectedHitDspTime(hitDsp);
        note.SetSortingOrder(lane._noteType == NoteType.Ground ? _sortGroundNote : _sortUpperNote);

        if (judge != null) judge.RegisterTap(note);
    }

    private void SpawnHoldAtBeat(int laneIndex, NoteLane lane, float travelSec, double headHitDsp)
    {
        if (_preventHoldOverlapOnSameLane)
        {
            if (_aliveHoldByLane.ContainsKey(laneIndex) && _aliveHoldByLane[laneIndex] != null)
                return;
        }

        HoldNote prefab = lane._holdPrefab != null ? lane._holdPrefab : _defaultHoldPrefab;
        if (prefab == null) return;

        var judge = GetJudge(lane);

        HoldNote hold = Instantiate(prefab);
        hold.InitFollow(
            lane._hitPoint,
            lane._spawnPoint,
            lane._hitPoint,
            lane._despawnPoint,
            travelSec,
            lane._noteType,
            lane._yOffsetLocal
        );

        if (lane._noteParent != null) hold.transform.SetParent(lane._noteParent, true);

        hold.SetExpectedHitDspTime(headHitDsp);
        hold.SetupHoldBeats(_holdBeats, _conductor.SecPerBeat);
        hold.SetSortingOrder(lane._noteType == NoteType.Ground ? _sortGroundNote : _sortUpperNote);

        if (judge != null)
        {
            if (!judge.RegisterHold(hold))
            {
                Destroy(hold.gameObject);
                return;
            }
        }

        _aliveHoldByLane[laneIndex] = hold;
    }

    private void SpawnDimensionTapAtBeat(int laneIndex, NoteLane lane, float travelSec, double hitDsp)
    {
        bool isGround = lane._noteType == NoteType.Ground;
        Note prefab = isGround
            ? (_dimensionTapPrefabGround != null ? _dimensionTapPrefabGround : _defaultTapPrefab)
            : (_dimensionTapPrefabUpper != null ? _dimensionTapPrefabUpper : _defaultTapPrefab);
        if (prefab == null) return;

        Note note = Instantiate(prefab);
        note.MarkAsDimensionNote();
        note.InitFollow(
            lane._hitPoint,
            lane._spawnPoint,
            lane._hitPoint,
            lane._despawnPoint,
            travelSec,
            lane._noteType,
            lane._yOffsetLocal
        );

        if (lane._noteParent != null) note.transform.SetParent(lane._noteParent, true);
        note.SetExpectedHitDspTime(hitDsp);
        note.SetSortingOrder(isGround ? _sortGroundDN : _sortUpperDN);

        if (DimensionNoteJudge.I != null) DimensionNoteJudge.I.RegisterTap(note);
    }

    private void SpawnDimensionHoldAtBeat(int laneIndex, NoteLane lane, float travelSec, double headHitDsp)
    {
        bool isGround = lane._noteType == NoteType.Ground;
        HoldNote prefab = isGround
            ? (_dimensionHoldPrefabGround != null ? _dimensionHoldPrefabGround : _defaultHoldPrefab)
            : (_dimensionHoldPrefabUpper != null ? _dimensionHoldPrefabUpper : _defaultHoldPrefab);
        if (prefab == null) return;

        HoldNote hold = Instantiate(prefab);
        hold.MarkAsDimensionNote();
        hold.InitFollow(
            lane._hitPoint,
            lane._spawnPoint,
            lane._hitPoint,
            lane._despawnPoint,
            travelSec,
            lane._noteType,
            lane._yOffsetLocal
        );

        if (lane._noteParent != null) hold.transform.SetParent(lane._noteParent, true);
        hold.SetExpectedHitDspTime(headHitDsp);
        hold.SetupHoldBeats(_holdBeats, _conductor.SecPerBeat);
        hold.SetSortingOrder(isGround ? _sortGroundDN : _sortUpperDN);

        if (DimensionNoteJudge.I != null) DimensionNoteJudge.I.RegisterHold(hold);
    }
}