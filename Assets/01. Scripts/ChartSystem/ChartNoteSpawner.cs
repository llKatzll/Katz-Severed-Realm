using System.Collections.Generic;
using UnityEngine;

public class ChartNoteSpawner : MonoBehaviour
{
    [SerializeField] private NoteSpawner _noteSpawner;

    [Header("Approach")]
    [SerializeField] private float _baseApproachBeats = 4f;
    [SerializeField] private float _noteSpeedMul = 5f;

    [Header("Prefabs (override, optional)")]
    [SerializeField] private Note _tapPrefab;
    [SerializeField] private HoldNote _holdPrefab;

    [Header("Dimension")]
    [SerializeField] private DimensionType _defaultDimension = DimensionType.Dismaller;

    private ChartData _chart;
    private List<NoteData> _sortedNotes;
    private int _nextIndex;
    private bool _active;

    private float ApproachBeats => Mathf.Max(0.0001f, _baseApproachBeats / Mathf.Max(0.0001f, _noteSpeedMul));

    private RhythmConductor Conductor => _noteSpawner != null ? _noteSpawner.Conductor : null;

    public void LoadChart(ChartData chart)
    {
        _chart = chart;
        if (_chart == null || _chart.notes == null)
        {
            _active = false;
            return;
        }

        _sortedNotes = new List<NoteData>(_chart.notes);
        _sortedNotes.Sort((a, b) => a.beat.CompareTo(b.beat));
        _nextIndex = 0;
        _active = true;
    }

    public void LoadChartFromFile(string songName, string difficulty)
    {
        string path = ChartUtility.GetChartPath(songName, difficulty);
        ChartData data = ChartUtility.LoadFromFile(path);
        LoadChart(data);
    }

    public void StopChart()
    {
        _active = false;
        _nextIndex = 0;
    }

    private void Update()
    {
        if (!_active) return;
        if (_sortedNotes == null) return;
        if (Conductor == null || !Conductor.Started) return;
        if (_noteSpawner == null) return;

        NoteSpawner.NoteLane[] lanes = _noteSpawner.Lanes;
        if (lanes == null || lanes.Length == 0) return;

        double nowBeat = Conductor.CurrentBeat;
        double spawnThreshold = nowBeat + ApproachBeats;

        int safety = 0;
        while (_nextIndex < _sortedNotes.Count && safety < 512)
        {
            NoteData nd = _sortedNotes[_nextIndex];
            if (nd.beat > spawnThreshold) break;

            SpawnFromNoteData(nd, lanes);
            _nextIndex++;
            safety++;
        }
    }

    private void SpawnFromNoteData(NoteData nd, NoteSpawner.NoteLane[] lanes)
    {
        int laneIdx = ResolveLaneIndex(nd, lanes);
        if (laneIdx < 0 || laneIdx >= lanes.Length) return;

        NoteSpawner.NoteLane lane = lanes[laneIdx];
        if (lane == null) return;
        if (lane._spawnPoint == null || lane._hitPoint == null || lane._despawnPoint == null) return;

        double hitDsp = Conductor.DspTimeAtBeat(nd.beat);
        float travelSec = (float)(ApproachBeats * Conductor.SecPerBeat);

        DimensionType dim = nd.noteType == ChartNoteType.Dimension
            ? _defaultDimension : DimensionType.Dismaller;

        if (nd.noteType == ChartNoteType.Hold ||
            (nd.noteType == ChartNoteType.Dimension && nd.holdEndBeat > nd.beat))
        {
            SpawnHold(lane, travelSec, hitDsp, nd, dim);
        }
        else
        {
            SpawnTap(lane, travelSec, hitDsp, dim);
        }
    }

    private int ResolveLaneIndex(NoteData nd, NoteSpawner.NoteLane[] lanes)
    {
        int groundCount = 0;
        int upperStart = 0;
        for (int i = 0; i < lanes.Length; i++)
        {
            if (lanes[i]._noteType == NoteSpawner.NoteType.Ground)
                groundCount++;
        }
        upperStart = groundCount;

        if (nd.laneType == ChartLaneType.Ground)
            return nd.lane;

        return upperStart + nd.lane;
    }

    private void SpawnTap(NoteSpawner.NoteLane lane, float travelSec, double hitDsp, DimensionType dim)
    {
        Note prefab = _tapPrefab != null ? _tapPrefab :
                      (lane._tapPrefab != null ? lane._tapPrefab : null);
        if (prefab == null) return;

        LaneJudge judge = GetJudge(lane);

        Note note = Instantiate(prefab);
        note.SetDimension(dim);
        note.InitFollow(
            lane._hitPoint,
            lane._spawnPoint,
            lane._hitPoint,
            lane._despawnPoint,
            travelSec,
            lane._noteType,
            lane._yOffsetLocal
        );

        if (lane._noteParent != null)
            note.transform.SetParent(lane._noteParent, true);

        note.SetExpectedHitDspTime(hitDsp);

        if (judge != null) judge.RegisterTap(note);
    }

    private void SpawnHold(NoteSpawner.NoteLane lane, float travelSec, double headHitDsp, NoteData nd, DimensionType dim)
    {
        HoldNote prefab = _holdPrefab != null ? _holdPrefab :
                          (lane._holdPrefab != null ? lane._holdPrefab : null);
        if (prefab == null) return;

        LaneJudge judge = GetJudge(lane);

        HoldNote hold = Instantiate(prefab);
        hold.SetDimension(dim);
        hold.InitFollow(
            lane._hitPoint,
            lane._spawnPoint,
            lane._hitPoint,
            lane._despawnPoint,
            travelSec,
            lane._noteType,
            lane._yOffsetLocal
        );

        if (lane._noteParent != null)
            hold.transform.SetParent(lane._noteParent, true);

        hold.SetExpectedHitDspTime(headHitDsp);

        double holdBeats = nd.holdEndBeat - nd.beat;
        if (holdBeats < 0) holdBeats = 0;
        hold.SetupHoldBeats(holdBeats, Conductor.SecPerBeat);

        if (judge != null)
        {
            if (!judge.RegisterHold(hold))
            {
                Destroy(hold.gameObject);
                return;
            }
        }
    }

    private LaneJudge GetJudge(NoteSpawner.NoteLane lane)
    {
        LaneJudge judge = lane._judge != null ? lane._judge
            : lane._hitPoint.GetComponent<LaneJudge>();
        if (judge != null) judge.SetLaneType(lane._noteType);
        return judge;
    }
}
