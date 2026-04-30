using UnityEngine;

public class EditorChart : MonoBehaviour
{
    private enum RemoveFilter { All, DimensionOnly, NonDimensionOnly }

    private ChartData _chart = new ChartData();

    public ChartData Chart => _chart;
    public event System.Action OnChartChanged;

    public void NewChart(string songName, string difficulty, float bpm)
    {
        _chart = new ChartData
        {
            songName = songName,
            difficulty = difficulty,
            bpm = bpm
        };
        OnChartChanged?.Invoke();
    }

    public void LoadChart(ChartData data)
    {
        _chart = data ?? new ChartData();
        OnChartChanged?.Invoke();
    }

    public void AddTap(float beat, int lane, ChartLaneType laneType, ChartNoteType noteType)
    {
        bool isDim = noteType == ChartNoteType.Dimension;
        var filter = isDim ? RemoveFilter.DimensionOnly : RemoveFilter.NonDimensionOnly;
        InternalRemoveInRange(beat, beat, lane, laneType, filter, 0.0001f);
        _chart.notes.Add(new NoteData(beat, lane, laneType, noteType));
        OnChartChanged?.Invoke();
    }

    public void AddHold(float startBeat, float endBeat, int lane, ChartLaneType laneType, bool isDimension)
    {
        if (endBeat <= startBeat) return;
        var filter = isDimension ? RemoveFilter.DimensionOnly : RemoveFilter.NonDimensionOnly;
        InternalRemoveInRange(startBeat, endBeat, lane, laneType, filter, 0.0001f);
        var noteType = isDimension ? ChartNoteType.Dimension : ChartNoteType.Hold;
        _chart.notes.Add(new NoteData(startBeat, lane, laneType, noteType, endBeat));
        OnChartChanged?.Invoke();
    }

    public bool RemoveAt(float beat, int lane, ChartLaneType laneType, float tolerance = 0.0001f)
    {
        bool removed = InternalRemoveInRange(beat, beat, lane, laneType, RemoveFilter.DimensionOnly, tolerance);
        if (!removed)
        {
            removed = InternalRemoveInRange(beat, beat, lane, laneType, RemoveFilter.NonDimensionOnly, tolerance);
        }
        if (removed) OnChartChanged?.Invoke();
        return removed;
    }

    private bool InternalRemoveInRange(float from, float to, int lane, ChartLaneType laneType, RemoveFilter filter, float tol)
    {
        bool removed = false;
        for (int i = _chart.notes.Count - 1; i >= 0; i--)
        {
            var n = _chart.notes[i];
            if (n.lane != lane || n.laneType != laneType) continue;

            bool isDim = n.noteType == ChartNoteType.Dimension;
            if (filter == RemoveFilter.DimensionOnly && !isDim) continue;
            if (filter == RemoveFilter.NonDimensionOnly && isDim) continue;

            float noteFrom = n.beat;
            float noteTo = n.IsHold ? n.holdEndBeat : n.beat;
            bool overlaps = !(noteTo < from - tol || noteFrom > to + tol);

            if (overlaps)
            {
                _chart.notes.RemoveAt(i);
                removed = true;
            }
        }
        return removed;
    }

    public NoteData FindNoteAt(float beat, int lane, ChartLaneType laneType)
    {
        const float tol = 0.0001f;
        foreach (var n in _chart.notes)
        {
            if (n.lane != lane || n.laneType != laneType) continue;
            if (n.IsHold)
            {
                if (beat >= n.beat - tol && beat <= n.holdEndBeat + tol) return n;
            }
            else
            {
                if (Mathf.Abs(n.beat - beat) < tol) return n;
            }
        }
        return null;
    }
}
