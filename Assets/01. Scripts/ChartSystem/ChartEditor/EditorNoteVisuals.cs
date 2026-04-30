using System.Collections.Generic;
using UnityEngine;

public class EditorNoteVisuals : MonoBehaviour
{
    [SerializeField] private EditorChart _chart;
    [SerializeField] private EditorTimeline _timeline;

    [Header("Tap Prefabs")]
    [SerializeField] private GameObject _tapPrefab;
    [SerializeField] private GameObject _dimensionPrefab;

    [Header("Hold Prefabs - Normal")]
    [SerializeField] private GameObject _longNotePrefab;
    [SerializeField] private GameObject _lnHeadPrefab;
    [SerializeField] private GameObject _lnTailPrefab;

    [Header("Hold Prefabs - Dimension")]
    [SerializeField] private GameObject _dimensionLongPrefab;
    [SerializeField] private GameObject _dimensionLnHeadPrefab;
    [SerializeField] private GameObject _dimensionLnTailPrefab;

    [Header("Z Order")]
    [SerializeField] private float _noteZ = -0.6f;
    [SerializeField] private float _dimensionZ = -0.7f;
    [SerializeField] private float _headTailZOffset = -0.01f;

    [Header("Width Ratios (relative to laneWidth)")]
    [SerializeField] private float _normalWidthRatio = 0.9f;
    [SerializeField] private float _normalBodyWidthRatio = 0.7f;
    [SerializeField] private float _dimensionWidthRatio = 0.7f;
    [SerializeField] private float _dimensionBodyWidthRatio = 0.55f;

    private readonly List<GameObject> _spawned = new List<GameObject>();
    private GameObject _previewHead;
    private Transform _parent;

    private void Awake()
    {
        if (_chart == null) _chart = GetComponent<EditorChart>();
        if (_timeline == null) _timeline = GetComponent<EditorTimeline>();
    }

    private Transform GetSpawnParent()
    {
        if (_parent != null) return _parent;
        var bootstrap = _timeline != null ? _timeline.Bootstrap : null;
        if (bootstrap != null && bootstrap.NoteCanvas != null)
        {
            _parent = bootstrap.NoteCanvas.transform;
        }
        else
        {
            _parent = transform;
        }
        return _parent;
    }

    private float GetCanvasScale()
    {
        var bootstrap = _timeline != null ? _timeline.Bootstrap : null;
        return bootstrap != null ? bootstrap.NoteCanvasScale : 1f;
    }

    private float GetLaneWidth()
    {
        var bootstrap = _timeline != null ? _timeline.Bootstrap : null;
        return bootstrap != null ? bootstrap.LaneWidth : 1f;
    }

    private void OnEnable()
    {
        if (_chart != null) _chart.OnChartChanged += Rebuild;
    }

    private void OnDisable()
    {
        if (_chart != null) _chart.OnChartChanged -= Rebuild;
    }

    private void Start()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        ClearAll();
        if (_chart == null || _timeline == null) return;

        foreach (var n in _chart.Chart.notes)
        {
            if (n.noteType == ChartNoteType.Dimension) continue;
            if (n.IsHold) SpawnHold(n);
            else SpawnTap(n);
        }

        foreach (var n in _chart.Chart.notes)
        {
            if (n.noteType != ChartNoteType.Dimension) continue;
            if (n.IsHold) SpawnHold(n);
            else SpawnTap(n);
        }
    }

    private void ClearAll()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null) Destroy(_spawned[i]);
        }
        _spawned.Clear();
    }

    private void SpawnTap(NoteData n)
    {
        bool dim = n.noteType == ChartNoteType.Dimension;
        var prefab = dim ? _dimensionPrefab : _tapPrefab;
        if (prefab == null) return;

        float x = _timeline.LaneToWorldX(n.lane, n.laneType);
        float y = _timeline.BeatToWorldY(n.beat);
        float z = dim ? _dimensionZ : _noteZ;

        var go = Instantiate(prefab, new Vector3(x, y, z), Quaternion.identity, GetSpawnParent());
        ApplyWidth(go, dim);
        _spawned.Add(go);
    }

    private void SpawnHold(NoteData n)
    {
        bool dim = n.noteType == ChartNoteType.Dimension;
        var bodyPrefab = dim ? _dimensionLongPrefab : _longNotePrefab;
        var headPrefab = dim ? _dimensionLnHeadPrefab : _lnHeadPrefab;
        var tailPrefab = dim ? _dimensionLnTailPrefab : _lnTailPrefab;

        float x = _timeline.LaneToWorldX(n.lane, n.laneType);
        float startY = _timeline.BeatToWorldY(n.beat);
        float endY = _timeline.BeatToWorldY(n.holdEndBeat);
        float bodyHeight = endY - startY;
        float baseZ = dim ? _dimensionZ : _noteZ;

        if (bodyPrefab != null && bodyHeight > 0f)
        {
            var body = Instantiate(bodyPrefab, new Vector3(x, (startY + endY) * 0.5f, baseZ), Quaternion.identity, GetSpawnParent());
            ApplyWidth(body, dim, true);
            StretchBodyHeight(body, bodyHeight);
            _spawned.Add(body);
        }

        if (headPrefab != null)
        {
            var head = Instantiate(headPrefab, new Vector3(x, startY, baseZ + _headTailZOffset), Quaternion.identity, GetSpawnParent());
            ApplyWidth(head, dim);
            _spawned.Add(head);
        }

        if (tailPrefab != null)
        {
            var tail = Instantiate(tailPrefab, new Vector3(x, endY, baseZ + _headTailZOffset), Quaternion.identity, GetSpawnParent());
            ApplyWidth(tail, dim);
            _spawned.Add(tail);
        }
    }

    private void StretchBodyHeight(GameObject body, float worldHeight)
    {
        var rt = body.GetComponent<RectTransform>();
        if (rt == null) return;

        float canvasScale = GetCanvasScale();
        if (canvasScale <= 0f) canvasScale = 1f;

        float origSizeY = rt.sizeDelta.y;
        if (origSizeY <= 0f) origSizeY = 1f;

        var s = body.transform.localScale;
        s.y = worldHeight / (origSizeY * canvasScale);
        body.transform.localScale = s;
    }

    private void ApplyWidth(GameObject go, bool isDimension, bool isBody = false)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;

        float canvasScale = GetCanvasScale();
        if (canvasScale <= 0f) canvasScale = 1f;

        float origSizeX = rt.sizeDelta.x;
        if (origSizeX <= 0f) origSizeX = 1f;

        float ratio;
        if (isDimension) ratio = isBody ? _dimensionBodyWidthRatio : _dimensionWidthRatio;
        else ratio = isBody ? _normalBodyWidthRatio : _normalWidthRatio;

        float worldWidth = GetLaneWidth() * ratio;

        var s = go.transform.localScale;
        s.x = worldWidth / (origSizeX * canvasScale);
        go.transform.localScale = s;
    }

    public void ShowPreviewHead(float beat, int lane, ChartLaneType type, bool isDimension)
    {
        ClearPreview();
        var prefab = isDimension ? _dimensionLnHeadPrefab : _lnHeadPrefab;
        if (prefab == null || _timeline == null) return;

        float x = _timeline.LaneToWorldX(lane, type);
        float y = _timeline.BeatToWorldY(beat);
        float z = isDimension ? _dimensionZ : _noteZ;

        _previewHead = Instantiate(prefab, new Vector3(x, y, z + _headTailZOffset), Quaternion.identity, GetSpawnParent());
        ApplyWidth(_previewHead, isDimension);
    }

    public void ClearPreview()
    {
        if (_previewHead != null) Destroy(_previewHead);
        _previewHead = null;
    }
}
