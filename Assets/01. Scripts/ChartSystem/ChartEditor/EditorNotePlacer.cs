using UnityEngine;
using UnityEngine.EventSystems;

public class EditorNotePlacer : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private EditorTimeline _timeline;
    [SerializeField] private ChartEditorManager _editor;
    [SerializeField] private ChartLaneType _laneType;

    [Header("Note Mode")]
    [SerializeField] private ChartNoteType _currentNoteType = ChartNoteType.Tap;

    private bool _isDragging;
    private float _dragStartBeat;
    private int _dragStartLane;

    public ChartNoteType CurrentNoteType
    {
        get => _currentNoteType;
        set => _currentNoteType = value;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isDragging) return;
        if (_editor == null || _editor.CurrentChart == null) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            RemoveNoteAt(eventData);
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (_currentNoteType == ChartNoteType.Tap || _currentNoteType == ChartNoteType.Dimension)
            {
                PlaceTapAt(eventData);
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (_currentNoteType != ChartNoteType.Hold && _currentNoteType != ChartNoteType.Dimension) return;

        Vector2 local;
        if (!GetLocalPosition(eventData, out local)) return;

        _dragStartBeat = _timeline.SnapBeat(_timeline.YToBeat(local.y));
        _dragStartLane = _timeline.XToColumn(local.x);
        _isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (_editor == null || _editor.CurrentChart == null) return;

        Vector2 local;
        if (!GetLocalPosition(eventData, out local)) return;

        float endBeat = _timeline.SnapBeat(_timeline.YToBeat(local.y));
        int endLane = _timeline.XToColumn(local.x);

        if (endLane != _dragStartLane) return;

        float startBeat = Mathf.Min(_dragStartBeat, endBeat);
        float holdEndBeat = Mathf.Max(_dragStartBeat, endBeat);

        if (holdEndBeat - startBeat < 0.01f) return;

        RemoveOverlappingNotes(startBeat, holdEndBeat, _dragStartLane);

        NoteData nd = new NoteData();
        nd.beat = startBeat;
        nd.lane = _dragStartLane;
        nd.laneType = _laneType;
        nd.noteType = _currentNoteType == ChartNoteType.Dimension
            ? ChartNoteType.Dimension : ChartNoteType.Hold;
        nd.holdEndBeat = holdEndBeat;

        _editor.CurrentChart.notes.Add(nd);
        _editor.MarkUnsaved();
        _timeline.RebuildNotes();
    }

    private void PlaceTapAt(PointerEventData eventData)
    {
        Vector2 local;
        if (!GetLocalPosition(eventData, out local)) return;

        float beat = _timeline.SnapBeat(_timeline.YToBeat(local.y));
        int lane = _timeline.XToColumn(local.x);

        if (HasNoteAt(beat, lane)) return;

        NoteData nd = new NoteData();
        nd.beat = beat;
        nd.lane = lane;
        nd.laneType = _laneType;
        nd.noteType = _currentNoteType;
        nd.holdEndBeat = 0f;

        _editor.CurrentChart.notes.Add(nd);
        _editor.MarkUnsaved();
        _timeline.RebuildNotes();
    }

    private void RemoveNoteAt(PointerEventData eventData)
    {
        Vector2 local;
        if (!GetLocalPosition(eventData, out local)) return;

        float beat = _timeline.YToBeat(local.y);
        int lane = _timeline.XToColumn(local.x);

        var notes = _editor.CurrentChart.notes;
        for (int i = notes.Count - 1; i >= 0; i--)
        {
            NoteData nd = notes[i];
            if (nd.laneType != _laneType) continue;
            if (nd.lane != lane) continue;

            if (nd.noteType == ChartNoteType.Hold ||
                (nd.noteType == ChartNoteType.Dimension && nd.holdEndBeat > nd.beat))
            {
                if (beat >= nd.beat - 0.1f && beat <= nd.holdEndBeat + 0.1f)
                {
                    notes.RemoveAt(i);
                    _editor.MarkUnsaved();
                    _timeline.RebuildNotes();
                    return;
                }
            }
            else
            {
                if (Mathf.Abs(nd.beat - beat) < 0.5f)
                {
                    notes.RemoveAt(i);
                    _editor.MarkUnsaved();
                    _timeline.RebuildNotes();
                    return;
                }
            }
        }
    }

    private void RemoveOverlappingNotes(float startBeat, float endBeat, int lane)
    {
        var notes = _editor.CurrentChart.notes;
        for (int i = notes.Count - 1; i >= 0; i--)
        {
            NoteData nd = notes[i];
            if (nd.laneType != _laneType) continue;
            if (nd.lane != lane) continue;

            if (nd.noteType == ChartNoteType.Tap || nd.noteType == ChartNoteType.Dimension)
            {
                if (nd.holdEndBeat <= nd.beat)
                {
                    if (nd.beat >= startBeat && nd.beat <= endBeat)
                    {
                        notes.RemoveAt(i);
                    }
                }
            }
        }
    }

    private bool HasNoteAt(float beat, int lane)
    {
        var notes = _editor.CurrentChart.notes;
        for (int i = 0; i < notes.Count; i++)
        {
            NoteData nd = notes[i];
            if (nd.laneType != _laneType) continue;
            if (nd.lane != lane) continue;
            if (Mathf.Abs(nd.beat - beat) < 0.01f) return true;
        }
        return false;
    }

    private bool GetLocalPosition(PointerEventData eventData, out Vector2 local)
    {
        local = Vector2.zero;
        if (_timeline == null || _timeline.Content == null) return false;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _timeline.Content, eventData.position, eventData.pressEventCamera, out local);
    }
}
