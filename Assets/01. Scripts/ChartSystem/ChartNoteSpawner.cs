using System.Collections.Generic;
using UnityEngine;

public class ChartNoteSpawner : MonoBehaviour
{
    [SerializeField] private NoteSpawner _noteSpawner;

    [Header("Approach")]
    [SerializeField, Range(0.1f, 10f)] private float _noteSpeed = 5f;
    private const float APPROACH_BEATS_AT_MAX_SPEED = 4f / 3.5f;
    private const float MAX_SLIDER_VALUE = 10f;

    [Header("Dimension Note Offset")]
    [SerializeField] private float _dimensionYOffsetExtra = 0.2f;

    [Header("Prefabs")]
    [SerializeField] private Note _tapPrefab;
    [SerializeField] private HoldNote _holdPrefab;

    [Header("Dimension Note - Ground")]
    [SerializeField] private Note _dimensionTapPrefabGround;
    [SerializeField] private HoldNote _dimensionHoldPrefabGround;

    [Header("Dimension Note - Upper")]
    [SerializeField] private Note _dimensionTapPrefabUpper;
    [SerializeField] private HoldNote _dimensionHoldPrefabUpper;

    [Header("Sorting Order")]
    [SerializeField] private int _sortGroundNote = 0;
    [SerializeField] private int _sortGroundDN = 1;
    [SerializeField] private int _sortUpperNote = 2;
    [SerializeField] private int _sortUpperDN = 3;

    private List<NoteData> _sortedNotes;
    private int _nextIndex;
    private bool _active;
    private int _groundLaneCount = -1;

    private float ApproachBeats => (APPROACH_BEATS_AT_MAX_SPEED * MAX_SLIDER_VALUE) / Mathf.Max(0.0001f, _noteSpeed);
    private RhythmConductor Conductor => _noteSpawner != null ? _noteSpawner.Conductor : null;
    public bool IsChartLoaded => _active;

    private void Start()
    {
        if (GameManager.I == null) return;
        var song = GameManager.I.SelectedSong;
        if (song == null) return;

        string difficulty = GameManager.I.SelectedDifficulty.ToString();
        LoadChartFromFile(song.songName, difficulty);
    }

    public void LoadChart(ChartData chart)
    {
        _active = false;
        _groundLaneCount = -1;

        if (chart == null || chart.notes == null || chart.notes.Count == 0) return;

        chart.SortAll();
        _sortedNotes = chart.notes;
        _nextIndex = 0;
        _active = true;

        if (ScoreManager.I != null)
        {
            int total = 0;
            for (int i = 0; i < _sortedNotes.Count; i++)
            {
                total += _sortedNotes[i].IsHold ? 2 : 1;
            }
            ScoreManager.I.SetTotalNoteCount(total);
        }
    }

    public void LoadChartFromFile(string songName, string difficulty)
    {
        LoadChart(ChartUtility.LoadFromFile(ChartUtility.GetChartPath(songName, difficulty)));
    }

    public void StopChart()
    {
        _active = false;
        _nextIndex = 0;
    }

    private void Update()
    {
        if (!_active || _sortedNotes == null) return;

        RhythmConductor cond = Conductor;
        if (cond == null || !cond.Started) return;

        NoteSpawner.NoteLane[] lanes = _noteSpawner != null ? _noteSpawner.Lanes : null;
        if (lanes == null || lanes.Length == 0) return;

        double spawnThreshold = cond.CurrentBeat + ApproachBeats;

        int safety = 0;
        while (_nextIndex < _sortedNotes.Count && safety < 512)
        {
            NoteData nd = _sortedNotes[_nextIndex];
            if (nd.beat > spawnThreshold) break;

            SpawnNote(nd, lanes, cond);
            _nextIndex++;
            safety++;
        }
    }

    private void SpawnNote(NoteData nd, NoteSpawner.NoteLane[] lanes, RhythmConductor cond)
    {
        int laneIdx = ResolveLaneIndex(nd, lanes);
        if (laneIdx < 0 || laneIdx >= lanes.Length) return;

        NoteSpawner.NoteLane lane = lanes[laneIdx];
        if (lane == null || lane._spawnPoint == null || lane._hitPoint == null || lane._despawnPoint == null) return;

        double hitDsp = cond.DspTimeAtBeat(nd.beat);
        float travelSec = ApproachBeats * (float)cond.SecPerBeat;

        if (nd.noteType == ChartNoteType.Dimension)
        {
            if (nd.IsHold)
                SpawnDimensionHold(lane, travelSec, hitDsp, nd, cond);
            else
                SpawnDimensionTap(lane, travelSec, hitDsp);
            return;
        }

        if (nd.IsHold)
            SpawnHold(lane, travelSec, hitDsp, nd, cond);
        else
            SpawnTap(lane, travelSec, hitDsp);
    }

    private int ResolveLaneIndex(NoteData nd, NoteSpawner.NoteLane[] lanes)
    {
        if (_groundLaneCount < 0)
        {
            _groundLaneCount = 0;
            for (int i = 0; i < lanes.Length; i++)
            {
                if (lanes[i]._noteType == NoteSpawner.NoteType.Ground)
                    _groundLaneCount++;
            }
        }

        return nd.laneType == ChartLaneType.Ground ? nd.lane : _groundLaneCount + nd.lane;
    }

    private void SpawnTap(NoteSpawner.NoteLane lane, float travelSec, double hitDsp)
    {
        Note prefab = _tapPrefab != null ? _tapPrefab : lane._tapPrefab;
        if (prefab == null) return;

        Note note = Instantiate(prefab);
        note.InitFollow(lane._hitPoint, lane._spawnPoint, lane._hitPoint, lane._despawnPoint,
            travelSec, lane._noteType, lane._yOffsetLocal);

        if (lane._noteParent != null) note.transform.SetParent(lane._noteParent, true);
        note.SetExpectedHitDspTime(hitDsp);
        note.SetSortingOrder(lane._noteType == NoteSpawner.NoteType.Ground ? _sortGroundNote : _sortUpperNote);

        LaneJudge judge = GetJudge(lane);
        if (judge != null) judge.RegisterTap(note);
    }

    private void SpawnHold(NoteSpawner.NoteLane lane, float travelSec, double hitDsp, NoteData nd, RhythmConductor cond)
    {
        HoldNote prefab = _holdPrefab != null ? _holdPrefab : lane._holdPrefab;
        if (prefab == null) return;

        LaneJudge judge = GetJudge(lane);

        HoldNote hold = Instantiate(prefab);
        hold.InitFollow(lane._hitPoint, lane._spawnPoint, lane._hitPoint, lane._despawnPoint,
            travelSec, lane._noteType, lane._yOffsetLocal);

        if (lane._noteParent != null) hold.transform.SetParent(lane._noteParent, true);
        hold.SetExpectedHitDspTime(hitDsp);

        double holdBeats = Mathf.Max(0f, nd.holdEndBeat - nd.beat);
        hold.SetupHoldBeats(holdBeats, cond.SecPerBeat);
        hold.SetSortingOrder(lane._noteType == NoteSpawner.NoteType.Ground ? _sortGroundNote : _sortUpperNote);

        if (judge != null && !judge.RegisterHold(hold))
        {
            Destroy(hold.gameObject);
        }
    }

    private void SpawnDimensionTap(NoteSpawner.NoteLane lane, float travelSec, double hitDsp)
    {
        bool isGround = lane._noteType == NoteSpawner.NoteType.Ground;
        Note prefab = isGround
            ? (_dimensionTapPrefabGround != null ? _dimensionTapPrefabGround : _tapPrefab)
            : (_dimensionTapPrefabUpper != null ? _dimensionTapPrefabUpper : _tapPrefab);
        if (prefab == null) return;

        Note note = Instantiate(prefab);
        note.MarkAsDimensionNote();
        note.InitFollow(lane._hitPoint, lane._spawnPoint, lane._hitPoint, lane._despawnPoint,
            travelSec, lane._noteType, lane._yOffsetLocal + _dimensionYOffsetExtra);

        if (lane._noteParent != null) note.transform.SetParent(lane._noteParent, true);
        note.SetExpectedHitDspTime(hitDsp);
        note.SetSortingOrder(isGround ? _sortGroundDN : _sortUpperDN);

        if (DimensionNoteJudge.I != null) DimensionNoteJudge.I.RegisterTap(note);
    }

    private void SpawnDimensionHold(NoteSpawner.NoteLane lane, float travelSec, double hitDsp, NoteData nd, RhythmConductor cond)
    {
        bool isGround = lane._noteType == NoteSpawner.NoteType.Ground;
        HoldNote prefab = isGround
            ? (_dimensionHoldPrefabGround != null ? _dimensionHoldPrefabGround : _holdPrefab)
            : (_dimensionHoldPrefabUpper != null ? _dimensionHoldPrefabUpper : _holdPrefab);
        if (prefab == null) return;

        HoldNote hold = Instantiate(prefab);
        hold.MarkAsDimensionNote();
        hold.InitFollow(lane._hitPoint, lane._spawnPoint, lane._hitPoint, lane._despawnPoint,
            travelSec, lane._noteType, lane._yOffsetLocal + _dimensionYOffsetExtra);

        if (lane._noteParent != null) hold.transform.SetParent(lane._noteParent, true);
        hold.SetExpectedHitDspTime(hitDsp);

        double holdBeats = Mathf.Max(0f, nd.holdEndBeat - nd.beat);
        hold.SetupHoldBeats(holdBeats, cond.SecPerBeat);
        hold.SetSortingOrder(isGround ? _sortGroundDN : _sortUpperDN);

        if (DimensionNoteJudge.I != null) DimensionNoteJudge.I.RegisterHold(hold);
    }

    private LaneJudge GetJudge(NoteSpawner.NoteLane lane)
    {
        LaneJudge judge = lane._judge != null ? lane._judge : lane._hitPoint.GetComponent<LaneJudge>();
        if (judge != null) judge.SetLaneType(lane._noteType);
        return judge;
    }
}
