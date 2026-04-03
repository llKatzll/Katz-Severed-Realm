using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditorTimeline : MonoBehaviour
{
    private const int COLUMN_COUNT = 4;

    [Header("References")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _content;
    [SerializeField] private ChartEditorManager _editor;
    [SerializeField] private AudioSource _audioSource;

    [Header("Settings")]
    [SerializeField] private float _pixelsPerBeat = 80f;
    [SerializeField] private float _minPixelsPerBeat = 20f;
    [SerializeField] private float _maxPixelsPerBeat = 400f;
    [SerializeField] private float _zoomStep = 20f;

    [Header("Colors")]
    [SerializeField] private Color _bgColor = new Color(0.05f, 0.05f, 0.05f, 1f);
    [SerializeField] private Color _columnLineColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color _beatLineColor = Color.white;
    [SerializeField] private Color _halfBeatLineColor = new Color(0f, 0.5f, 1f, 0.7f);
    [SerializeField] private Color _subBeatLineColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
    [SerializeField] private Color _tapNoteColor = Color.white;
    [SerializeField] private Color _holdNoteColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color _dnNoteColor = new Color(0.8f, 0.2f, 0.8f, 1f);
    [SerializeField] private Color _playheadColor = new Color(0f, 1f, 0f, 0.8f);

    private ChartData _chart;
    private ChartLaneType _laneType;
    private float _totalBeats;
    private float _contentHeight;
    private bool _isScrolling;

    private readonly List<GameObject> _noteObjects = new List<GameObject>(256);
    private readonly List<GameObject> _gridLines = new List<GameObject>(128);
    private GameObject _playhead;

    private float _viewportHeight;

    public float PixelsPerBeat => _pixelsPerBeat;
    public RectTransform Content => _content;

    public void SetChart(ChartData chart, ChartLaneType laneType)
    {
        _chart = chart;
        _laneType = laneType;

        if (_chart == null) return;

        CalculateTotalBeats();
        RebuildContent();
        RebuildNotes();
    }

    private void CalculateTotalBeats()
    {
        if (_audioSource != null && _audioSource.clip != null && _chart.bpm > 0)
        {
            float totalTime = _audioSource.clip.length;
            _totalBeats = (totalTime - _chart.audioOffset) / (60f / _chart.bpm);
        }
        else
        {
            _totalBeats = 200f;
        }
        _totalBeats = Mathf.Max(_totalBeats, 16f);
    }

    private void RebuildContent()
    {
        if (_content == null) return;

        _contentHeight = _totalBeats * _pixelsPerBeat;
        _content.sizeDelta = new Vector2(_content.sizeDelta.x, _contentHeight);

        RebuildGrid();
    }

    private void RebuildGrid()
    {
        for (int i = 0; i < _gridLines.Count; i++)
        {
            if (_gridLines[i] != null) Destroy(_gridLines[i]);
        }
        _gridLines.Clear();

        if (_content == null || _chart == null) return;

        float contentW = _content.rect.width;
        float colWidth = contentW / COLUMN_COUNT;

        for (int c = 1; c < COLUMN_COUNT; c++)
        {
            float x = c * colWidth;
            CreateLine(_content, x, 0f, x, _contentHeight, _columnLineColor, 1f);
        }

        int bsd = _editor != null ? _editor.CurrentBsd : 4;
        float subdivisionBeats = 1f / bsd;

        int totalSubdivisions = Mathf.CeilToInt(_totalBeats / subdivisionBeats);
        for (int i = 0; i <= totalSubdivisions; i++)
        {
            float beat = i * subdivisionBeats;
            float y = BeatToY(beat);

            Color lineColor;
            float lineWidth;

            float beatMod = beat % 1f;
            if (beatMod < 0.001f || beatMod > 0.999f)
            {
                lineColor = _beatLineColor;
                lineWidth = 2f;
            }
            else if (Mathf.Abs(beatMod - 0.5f) < 0.001f)
            {
                lineColor = _halfBeatLineColor;
                lineWidth = 1.5f;
            }
            else
            {
                lineColor = _subBeatLineColor;
                lineWidth = 1f;
            }

            CreateLine(_content, 0f, y, contentW, y, lineColor, lineWidth);
        }
    }

    private GameObject CreateLine(RectTransform parent, float x1, float y1, float x2, float y2, Color color, float width)
    {
        GameObject go = new GameObject("Line", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        Image img = go.GetComponent<Image>();
        img.color = color;

        bool isHorizontal = Mathf.Abs(y2 - y1) < 0.01f;
        if (isHorizontal)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x1, y1);
            rt.sizeDelta = new Vector2(x2 - x1, width);
        }
        else
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(x1, 0f);
            rt.sizeDelta = new Vector2(width, y2 > y1 ? y2 - y1 : _contentHeight);
        }

        _gridLines.Add(go);
        return go;
    }

    public void RebuildNotes()
    {
        for (int i = 0; i < _noteObjects.Count; i++)
        {
            if (_noteObjects[i] != null) Destroy(_noteObjects[i]);
        }
        _noteObjects.Clear();

        if (_chart == null || _chart.notes == null || _content == null) return;

        float contentW = _content.rect.width;
        float colWidth = contentW / COLUMN_COUNT;
        float noteHeight = _pixelsPerBeat * 0.15f;

        for (int i = 0; i < _chart.notes.Count; i++)
        {
            NoteData nd = _chart.notes[i];
            if (nd.laneType != _laneType) continue;
            if (nd.lane < 0 || nd.lane >= COLUMN_COUNT) continue;

            float y = BeatToY(nd.beat);
            float x = nd.lane * colWidth;

            Color noteColor;
            if (nd.noteType == ChartNoteType.Dimension)
                noteColor = _dnNoteColor;
            else if (nd.noteType == ChartNoteType.Hold)
                noteColor = _holdNoteColor;
            else
                noteColor = _tapNoteColor;

            if (nd.noteType == ChartNoteType.Hold ||
                (nd.noteType == ChartNoteType.Dimension && nd.holdEndBeat > nd.beat))
            {
                float endY = BeatToY(nd.holdEndBeat);
                float holdHeight = endY - y;
                CreateNoteRect(x, y, colWidth - 2f, holdHeight, noteColor, 0.6f);
                CreateNoteRect(x, y, colWidth - 2f, noteHeight, noteColor, 1f);
                CreateNoteRect(x, endY - noteHeight, colWidth - 2f, noteHeight, noteColor, 1f);
            }
            else
            {
                CreateNoteRect(x, y - noteHeight * 0.5f, colWidth - 2f, noteHeight, noteColor, 1f);
            }
        }
    }

    private GameObject CreateNoteRect(float x, float y, float w, float h, Color color, float alpha)
    {
        GameObject go = new GameObject("Note", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(_content, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(x + 1f, y);
        rt.sizeDelta = new Vector2(w, h);

        Image img = go.GetComponent<Image>();
        Color c = color;
        c.a = alpha;
        img.color = c;

        _noteObjects.Add(go);
        return go;
    }

    private void LateUpdate()
    {
        if (_audioSource != null && _audioSource.isPlaying && _chart != null)
        {
            float beat = _editor != null ? _editor.CurrentBeat : 0f;
            ScrollToBeat(beat);
        }

        UpdatePlayhead();
        HandleZoomInput();
    }

    private void HandleZoomInput()
    {
        if (_scrollRect == null) return;

        RectTransform scrollRt = _scrollRect.GetComponent<RectTransform>();
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            scrollRt, Input.mousePosition, null, out localPoint))
            return;

        if (!scrollRt.rect.Contains(localPoint)) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) < 0.001f) return;

        if (Input.GetKey(KeyCode.LeftControl))
        {
            float oldPpb = _pixelsPerBeat;
            _pixelsPerBeat += scroll > 0 ? _zoomStep : -_zoomStep;
            _pixelsPerBeat = Mathf.Clamp(_pixelsPerBeat, _minPixelsPerBeat, _maxPixelsPerBeat);

            if (Mathf.Abs(oldPpb - _pixelsPerBeat) > 0.01f)
            {
                RebuildContent();
                RebuildNotes();
            }
        }
        else
        {
            float seekAmount = Input.GetKey(KeyCode.LeftShift) ? 4f : 1f;
            float beatDelta = scroll > 0 ? -seekAmount : seekAmount;
            SeekByBeats(beatDelta);
        }
    }

    public void ScrollToBeat(float beat)
    {
        if (_scrollRect == null || _content == null) return;
        if (_contentHeight <= 0f) return;

        _viewportHeight = _scrollRect.GetComponent<RectTransform>().rect.height;
        float y = BeatToY(beat);
        float normalizedY = (y - _viewportHeight * 0.5f) / (_contentHeight - _viewportHeight);
        normalizedY = Mathf.Clamp01(normalizedY);
        _scrollRect.verticalNormalizedPosition = normalizedY;
    }

    private void SeekByBeats(float beatDelta)
    {
        if (_audioSource == null || _editor == null) return;
        float currentBeat = _editor.CurrentBeat;
        float newBeat = currentBeat + beatDelta;
        newBeat = Mathf.Max(0f, newBeat);

        float newTime = _editor.BeatToTime(newBeat);
        if (_audioSource.clip != null)
            newTime = Mathf.Clamp(newTime, 0f, _audioSource.clip.length);

        _audioSource.time = newTime;

        if (!_audioSource.isPlaying)
            ScrollToBeat(newBeat);
    }

    private void UpdatePlayhead()
    {
        if (_content == null || _editor == null) return;

        float beat = _editor.CurrentBeat;
        float y = BeatToY(beat);

        if (_playhead == null)
        {
            _playhead = new GameObject("Playhead", typeof(RectTransform), typeof(Image));
            _playhead.transform.SetParent(_content, false);
            Image img = _playhead.GetComponent<Image>();
            img.color = _playheadColor;
            img.raycastTarget = false;
        }

        RectTransform rt = _playhead.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(_content.rect.width, 2f);
        _playhead.transform.SetAsLastSibling();
    }

    public float BeatToY(float beat)
    {
        return beat * _pixelsPerBeat;
    }

    public float YToBeat(float y)
    {
        if (_pixelsPerBeat <= 0f) return 0f;
        return y / _pixelsPerBeat;
    }

    public int XToColumn(float localX)
    {
        if (_content == null) return -1;
        float colWidth = _content.rect.width / COLUMN_COUNT;
        int col = Mathf.FloorToInt(localX / colWidth);
        return Mathf.Clamp(col, 0, COLUMN_COUNT - 1);
    }

    public float SnapBeat(float beat)
    {
        int bsd = _editor != null ? _editor.CurrentBsd : 4;
        float snap = 1f / bsd;
        return Mathf.Round(beat / snap) * snap;
    }
}
