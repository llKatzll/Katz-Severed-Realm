using UnityEngine;
using UnityEngine.EventSystems;

public enum EditorPlaceMode
{
    Tap,
    LongNote,
    DimensionTap,
    DimensionLongNote
}

public class EditorInput : MonoBehaviour
{
    [SerializeField] private EditorBootstrap _bootstrap;
    [SerializeField] private EditorTimeline _timeline;
    [SerializeField] private EditorChart _chart;
    [SerializeField] private EditorNoteVisuals _visuals;

    [Header("Mode")]
    [SerializeField] private EditorPlaceMode _mode = EditorPlaceMode.Tap;

    private bool _holdPending;
    private float _holdStartBeat;
    private int _holdStartLane;
    private ChartLaneType _holdStartLaneType;

    public EditorPlaceMode Mode
    {
        get => _mode;
        set
        {
            if (_mode == value) return;
            _mode = value;
            CancelPendingHold();
        }
    }

    private void Awake()
    {
        if (_bootstrap == null) _bootstrap = GetComponent<EditorBootstrap>();
        if (_timeline == null) _timeline = GetComponent<EditorTimeline>();
        if (_chart == null) _chart = GetComponent<EditorChart>();
        if (_visuals == null) _visuals = GetComponent<EditorNoteVisuals>();
    }

    private void Update()
    {
        if (IsPointerOverUI()) return;

        if (Input.GetMouseButtonDown(0)) HandleLeftClick();
        else if (Input.GetMouseButtonDown(1)) HandleRightClick();
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private bool TryGetLaneBeat(out int lane, out ChartLaneType type, out float beat)
    {
        lane = -1;
        type = ChartLaneType.Ground;
        beat = 0f;

        var cam = _bootstrap != null ? _bootstrap.EditorCamera : null;
        if (cam == null || _timeline == null) return false;

        Vector3 mp = Input.mousePosition;
        mp.z = -cam.transform.position.z;
        Vector3 world = cam.ScreenToWorldPoint(mp);

        if (!_timeline.WorldXToLane(world.x, out lane, out type)) return false;

        float rawBeat = _timeline.WorldYToBeat(world.y);
        Debug.Log($"[Click Raw] worldY={world.y:F4} rawBeat={rawBeat:F4} bsd={_timeline.Bsd}");
        if (rawBeat < 0f) return false;

        beat = _timeline.SnapBeat(rawBeat);
        Debug.Log($"[Click Snap] snappedBeat={beat:F4} lane={lane} type={type}");
        if (beat < 0f) return false;
        return true;
    }

    private void HandleLeftClick()
    {
        if (!TryGetLaneBeat(out int lane, out var type, out float beat)) return;
        if (_chart == null) return;

        switch (_mode)
        {
            case EditorPlaceMode.Tap:
                _chart.AddTap(beat, lane, type, ChartNoteType.Tap);
                break;

            case EditorPlaceMode.DimensionTap:
                _chart.AddTap(beat, lane, type, ChartNoteType.Dimension);
                break;

            case EditorPlaceMode.LongNote:
                HandleHoldClick(beat, lane, type, false);
                break;

            case EditorPlaceMode.DimensionLongNote:
                HandleHoldClick(beat, lane, type, true);
                break;
        }
    }

    private void HandleHoldClick(float beat, int lane, ChartLaneType type, bool isDimension)
    {
        if (!_holdPending)
        {
            _holdPending = true;
            _holdStartBeat = beat;
            _holdStartLane = lane;
            _holdStartLaneType = type;
            _visuals?.ShowPreviewHead(beat, lane, type, isDimension);
            return;
        }

        if (lane != _holdStartLane || type != _holdStartLaneType)
        {
            _holdStartBeat = beat;
            _holdStartLane = lane;
            _holdStartLaneType = type;
            _visuals?.ShowPreviewHead(beat, lane, type, isDimension);
            return;
        }

        if (Mathf.Approximately(beat, _holdStartBeat))
        {
            CancelPendingHold();
            return;
        }

        float a = Mathf.Min(_holdStartBeat, beat);
        float b = Mathf.Max(_holdStartBeat, beat);
        _chart.AddHold(a, b, lane, type, isDimension);
        CancelPendingHold();
    }

    private void HandleRightClick()
    {
        if (!TryGetLaneBeat(out int lane, out var type, out float beat)) return;
        if (_chart == null) return;

        int bsd = _timeline != null ? _timeline.Bsd : 4;
        float tol = Mathf.Max(0.25f, 1f / Mathf.Max(1, bsd));
        _chart.RemoveAt(beat, lane, type, tol);
        CancelPendingHold();
    }

    private void CancelPendingHold()
    {
        _holdPending = false;
        _visuals?.ClearPreview();
    }
}
