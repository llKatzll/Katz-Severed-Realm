using System.Collections.Generic;
using UnityEngine;

public class ChartNoteSpawner : MonoBehaviour
{
    [SerializeField] private NoteSpawner _noteSpawner;

    [Header("Approach")]
    [SerializeField] private float _baseApproachBeats = 4f;
    [SerializeField] private float _noteSpeedMul = 5f;

    [Header("Prefabs")]
    [SerializeField] private Note _tapPrefab;
    [SerializeField] private HoldNote _holdPrefab;

    [Header("Dimension")]
    [SerializeField] private DimensionType _defaultDimension = DimensionType.Dismaller;

    private List<NoteData> _sortedNotes;
    private int _nextIndex;
    private bool _active;
    private int _groundLaneCount = -1;

    private float ApproachBeats => Mathf.Max(0.0001f, _baseApproachBeats / Mathf.Max(0.0001f, _noteSpeedMul));
    private RhythmConductor Conductor => _noteSpawner != null ? _noteSpawner.Conductor : null;

    public void LoadChart(ChartData chart)
    {
        _active = false;
        _groundLaneCount = -1;

        if (chart == null || chart.notes == null || chart.notes.Count == 0) return;

        chart.SortAll();
        _sortedNotes = chart.notes;
        _nextIndex = 0;
        _active = true;
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
        DimensionType dim = nd.noteType == ChartNoteType.Dimension ? _defaultDimension : DimensionType.Dismaller;

        if (nd.IsHold)
            SpawnHold(lane, travelSec, hitDsp, nd, dim, cond);
        else
            SpawnTap(lane, travelSec, hitDsp, dim);
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

    private void SpawnTap(NoteSpawner.NoteLane lane, float travelSec, double hitDsp, DimensionType dim)
    {
        Note prefab = _tapPrefab != null ? _tapPrefab : lane._tapPrefab;
        if (prefab == null) return;

        Note note = Instantiate(prefab);
        note.SetDimension(dim);
        note.InitFollow(lane._hitPoint, lane._spawnPoint, lane._hitPoint, lane._despawnPoint,
            travelSec, lane._noteType, lane._yOffsetLocal);

        if (lane._noteParent != null) note.transform.SetParent(lane._noteParent, true);
        note.SetExpectedHitDspTime(hitDsp);

        LaneJudge judge = GetJudge(lane);
        if (judge != null) judge.RegisterTap(note);
    }

    private void SpawnHold(NoteSpawner.NoteLane lane, float travelSec, double hitDsp, NoteData nd, DimensionType dim, RhythmConductor cond)
    {
        HoldNote prefab = _holdPrefab != null ? _holdPrefab : lane._holdPrefab;
        if (prefab == null) return;

        LaneJudge judge = GetJudge(lane);

        HoldNote hold = Instantiate(prefab);
        hold.SetDimension(dim);
        hold.InitFollow(lane._hitPoint, lane._spawnPoint, lane._hitPoint, lane._despawnPoint,
            travelSec, lane._noteType, lane._yOffsetLocal);

        if (lane._noteParent != null) hold.transform.SetParent(lane._noteParent, true);
        hold.SetExpectedHitDspTime(hitDsp);

        double holdBeats = Mathf.Max(0f, nd.holdEndBeat - nd.beat);
        hold.SetupHoldBeats(holdBeats, cond.SecPerBeat);

        if (judge != null && !judge.RegisterHold(hold))
        {
            Destroy(hold.gameObject);
        }
    }

    private LaneJudge GetJudge(NoteSpawner.NoteLane lane)
    {
        LaneJudge judge = lane._judge != null ? lane._judge : lane._hitPoint.GetComponent<LaneJudge>();
        if (judge != null) judge.SetLaneType(lane._noteType);
        return judge;
    }
}
