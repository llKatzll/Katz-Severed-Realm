using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EditorNotePlacer : MonoBehaviour
{
    [SerializeField] private EditorTimeline _timeline;
    [SerializeField] private ChartEditorManager _editor;
    [SerializeField] private ChartLaneType _laneType;

    [Header("Note Mode")]
    [SerializeField] private ChartNoteType _currentNoteType = ChartNoteType.Tap;

    [Header("Cursor Prefabs")]
    [SerializeField] private GameObject _tapCursorPrefab;
    [SerializeField] private GameObject _lnHeadCursorPrefab;
    [SerializeField] private GameObject _svCursorPrefab;
    [SerializeField] private GameObject _dimensionCursorPrefab;

    private GameObject _cursorPreview;
    private RectTransform _cursorRect;
    private ChartNoteType _cursorType;

    private bool _hasLnPending;
    private float _pendingLnBeat;
    private int _pendingLnLane;
    private GameObject _pendingLnVisual;

    private bool _cursorInsideZone;
    private bool _cursorInSVZone;
    private float _lastSnapBeat;
    private int _lastSnapLane;

    private const int COLUMN_COUNT = 4;

    public ChartNoteType CurrentNoteType
    {
        get => _currentNoteType;
        set
        {
            if (_currentNoteType != value)
            {
                _currentNoteType = value;
                CancelLnPending();
                RebuildCursorPreview();
            }
        }
    }

    private void Start()
    {
        CreateDragBlocker();
        RebuildCursorPreview();
    }

    private void CreateDragBlocker()
    {
        if (_timeline == null || _timeline.Content == null) return;
        Transform viewport = _timeline.Content.parent;
        if (viewport == null) return;

        GameObject blocker = new GameObject("DragBlocker",
            typeof(RectTransform), typeof(Image), typeof(ViewportDragBlocker));
        blocker.transform.SetParent(viewport, false);

        RectTransform rt = blocker.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = blocker.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = true;

        blocker.transform.SetAsLastSibling();
    }

    private void Update()
    {
        UpdateMouseState();
        UpdateCursorVisual();
        HandleClickInput();
    }

    private void RebuildCursorPreview()
    {
        if (_cursorPreview != null)
        {
            Destroy(_cursorPreview);
            _cursorPreview = null;
            _cursorRect = null;
        }

        GameObject prefab = GetCursorPrefab();
        if (prefab == null) return;

        _cursorPreview = Instantiate(prefab, transform);
        _cursorRect = _cursorPreview.GetComponent<RectTransform>();
        _cursorType = _currentNoteType;

        CanvasGroup cg = _cursorPreview.GetComponent<CanvasGroup>();
        if (cg == null) cg = _cursorPreview.AddComponent<CanvasGroup>();
        cg.alpha = 0.5f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        _cursorPreview.SetActive(false);
    }

    private GameObject GetCursorPrefab()
    {
        if (_editor != null && _editor.SVMode)
            return _svCursorPrefab;

        switch (_currentNoteType)
        {
            case ChartNoteType.Tap: return _tapCursorPrefab;
            case ChartNoteType.Hold: return _lnHeadCursorPrefab;
            case ChartNoteType.Dimension: return _dimensionCursorPrefab;
            default: return _tapCursorPrefab;
        }
    }

    private void UpdateMouseState()
    {
        _cursorInsideZone = false;
        _cursorInSVZone = false;

        if (_timeline == null || _timeline.Content == null) return;

        Vector2 local;
        if (!_timeline.ScreenToContentLocal(Input.mousePosition, out local)) return;
        if (local.y < 0f) return;
        if (local.x < -EditorTimeline.SV_ZONE_WIDTH || local.x > _timeline.Content.rect.width) return;

        _cursorInsideZone = true;
        _lastSnapBeat = _timeline.SnapBeat(_timeline.YToBeat(local.y));

        if (_timeline.IsInSVZone(local.x))
        {
            _cursorInSVZone = true;
            _lastSnapLane = -1;
        }
        else
        {
            _lastSnapLane = _timeline.XToColumn(local.x);
        }
    }

    private void UpdateCursorVisual()
    {
        if (_cursorPreview == null) return;

        if (!_cursorInsideZone)
        {
            _cursorPreview.SetActive(false);
            return;
        }

        _cursorPreview.SetActive(true);

        float contentW = _timeline.Content.rect.width;
        float colWidth = contentW / COLUMN_COUNT;
        float snapY = _timeline.BeatToY(_lastSnapBeat);

        float snapX;
        float cursorW;

        if (_cursorInSVZone)
        {
            snapX = -(EditorTimeline.SV_ZONE_WIDTH * 0.5f);
            cursorW = EditorTimeline.SV_ZONE_WIDTH - 4f;
        }
        else
        {
            snapX = _lastSnapLane * colWidth + colWidth * 0.5f;
            cursorW = colWidth - 2f;
        }

        if (_cursorRect != null)
        {
            _cursorRect.SetParent(_timeline.Content, false);
            _cursorRect.anchorMin = Vector2.zero;
            _cursorRect.anchorMax = Vector2.zero;
            _cursorRect.pivot = new Vector2(0.5f, 0.5f);
            _cursorRect.anchoredPosition = new Vector2(snapX, snapY);
            _cursorRect.sizeDelta = new Vector2(cursorW, _timeline.PixelsPerBeat * 0.15f);
        }
    }

    private void HandleClickInput()
    {
        if (!_cursorInsideZone) return;
        if (_editor == null || _editor.CurrentChart == null) return;

        if (Input.GetMouseButtonDown(1))
        {
            if (_cursorInSVZone)
                RemoveSVAtCursor();
            else
                RemoveNoteAtCursor();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (_cursorInSVZone)
            {
                PlaceSVAtCursor();
                return;
            }

            if (_editor.SVMode) return;

            switch (_currentNoteType)
            {
                case ChartNoteType.Tap:
                case ChartNoteType.Dimension:
                    PlaceTapAtCursor();
                    break;
                case ChartNoteType.Hold:
                    PlaceHoldClick();
                    break;
            }
        }
    }

    private bool HasDuplicateNote(float beat, int lane, ChartNoteType type)
    {
        var notes = _editor.CurrentChart.notes;
        for (int i = 0; i < notes.Count; i++)
        {
            NoteData nd = notes[i];
            if (nd.laneType != _laneType) continue;
            if (nd.lane != lane) continue;
            if (nd.noteType != type) continue;
            if (Mathf.Abs(nd.beat - beat) < 0.01f) return true;
        }
        return false;
    }

    private void PlaceTapAtCursor()
    {
        float beat = _lastSnapBeat;
        int lane = _lastSnapLane;

        if (HasDuplicateNote(beat, lane, _currentNoteType)) return;

        NoteData nd = new NoteData();
        nd.beat = beat;
        nd.lane = lane;
        nd.laneType = _laneType;
        nd.noteType = _currentNoteType;
        nd.holdEndBeat = 0f;

        _editor.CurrentChart.notes.Add(nd);
        SortNotes();
        _editor.MarkUnsaved();
        _timeline.RebuildNotes();
    }

    private void PlaceHoldClick()
    {
        float beat = _lastSnapBeat;
        int lane = _lastSnapLane;

        if (!_hasLnPending)
        {
            _hasLnPending = true;
            _pendingLnBeat = beat;
            _pendingLnLane = lane;
            SpawnPendingVisual(beat, lane);
            return;
        }

        if (lane != _pendingLnLane)
        {
            CancelLnPending();
            return;
        }

        if (Mathf.Abs(beat - _pendingLnBeat) < 0.001f)
        {
            CancelLnPending();
            return;
        }

        float headBeat = Mathf.Min(_pendingLnBeat, beat);
        float tailBeat = Mathf.Max(_pendingLnBeat, beat);

        if (HasDuplicateHold(headBeat, lane))
        {
            CancelLnPending();
            return;
        }

        NoteData nd = new NoteData();
        nd.beat = headBeat;
        nd.lane = lane;
        nd.laneType = _laneType;
        nd.noteType = ChartNoteType.Hold;
        nd.holdEndBeat = tailBeat;

        _editor.CurrentChart.notes.Add(nd);
        SortNotes();
        _editor.MarkUnsaved();

        CancelLnPending();
        _timeline.RebuildNotes();
    }

    private bool HasDuplicateHold(float headBeat, int lane)
    {
        var notes = _editor.CurrentChart.notes;
        for (int i = 0; i < notes.Count; i++)
        {
            NoteData nd = notes[i];
            if (nd.laneType != _laneType) continue;
            if (nd.lane != lane) continue;
            if (nd.noteType != ChartNoteType.Hold) continue;
            if (Mathf.Abs(nd.beat - headBeat) < 0.01f) return true;
        }
        return false;
    }

    private void SpawnPendingVisual(float beat, int lane)
    {
        DestroyPendingVisual();

        if (_lnHeadCursorPrefab == null || _timeline == null || _timeline.Content == null) return;

        float contentW = _timeline.Content.rect.width;
        float colWidth = contentW / COLUMN_COUNT;

        _pendingLnVisual = Instantiate(_lnHeadCursorPrefab, _timeline.Content);
        RectTransform rt = _pendingLnVisual.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(
                lane * colWidth + colWidth * 0.5f,
                _timeline.BeatToY(beat));
            rt.sizeDelta = new Vector2(colWidth - 2f, _timeline.PixelsPerBeat * 0.15f);
        }

        CanvasGroup cg = _pendingLnVisual.GetComponent<CanvasGroup>();
        if (cg == null) cg = _pendingLnVisual.AddComponent<CanvasGroup>();
        cg.alpha = 0.7f;
        cg.blocksRaycasts = false;
    }

    private void DestroyPendingVisual()
    {
        if (_pendingLnVisual != null)
        {
            Destroy(_pendingLnVisual);
            _pendingLnVisual = null;
        }
    }

    private void CancelLnPending()
    {
        _hasLnPending = false;
        DestroyPendingVisual();
    }

    private void RemoveNoteAtCursor()
    {
        float beat = _lastSnapBeat;
        int lane = _lastSnapLane;

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

    private void PlaceSVAtCursor()
    {
        float beat = _lastSnapBeat;
        var svNotes = _editor.CurrentChart.svNotes;
        if (svNotes == null)
        {
            svNotes = new List<SVData>();
            _editor.CurrentChart.svNotes = svNotes;
        }

        for (int i = 0; i < svNotes.Count; i++)
        {
            if (Mathf.Abs(svNotes[i].beat - beat) < 0.01f)
                return;
        }

        SVData sv = new SVData();
        sv.beat = beat;
        sv.amount = 1f;
        svNotes.Add(sv);
        svNotes.Sort((a, b) => a.beat.CompareTo(b.beat));

        _editor.MarkUnsaved();
        _timeline.RebuildNotes();
    }

    private void RemoveSVAtCursor()
    {
        float beat = _lastSnapBeat;
        var svNotes = _editor.CurrentChart.svNotes;
        if (svNotes == null) return;

        for (int i = svNotes.Count - 1; i >= 0; i--)
        {
            if (Mathf.Abs(svNotes[i].beat - beat) < 0.5f)
            {
                svNotes.RemoveAt(i);
                _editor.MarkUnsaved();
                _timeline.RebuildNotes();
                return;
            }
        }
    }

    private void SortNotes()
    {
        _editor.CurrentChart.notes.Sort((a, b) => a.beat.CompareTo(b.beat));
    }
}

public class ViewportDragBlocker : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public void OnBeginDrag(PointerEventData e) { }
    public void OnDrag(PointerEventData e) { }
    public void OnEndDrag(PointerEventData e) { }
}
