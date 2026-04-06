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
    [SerializeField] private RawImage _gridRawImage;

    [Header("Note Prefabs")]
    [SerializeField] private GameObject _tapPrefab;
    [SerializeField] private GameObject _lnHeadPrefab;
    [SerializeField] private GameObject _lnBodyPrefab;
    [SerializeField] private GameObject _lnTailPrefab;
    [SerializeField] private GameObject _dimensionPrefab;

    [Header("Settings")]
    [SerializeField] private float _pixelsPerBeat = 80f;
    [SerializeField] private float _minPixelsPerBeat = 20f;
    [SerializeField] private float _maxPixelsPerBeat = 400f;
    [SerializeField] private float _zoomStep = 20f;

    [Header("Colors")]
    [SerializeField] private Color _playheadColor = new Color(0f, 1f, 0f, 0.8f);

    private ChartData _chart;
    private ChartLaneType _laneType;
    private float _totalBeats;
    private float _contentHeight;

    private readonly List<GameObject> _noteObjects = new List<GameObject>(256);
    private GameObject _playhead;

    private float _viewportHeight;

    public float PixelsPerBeat => _pixelsPerBeat;
    public RectTransform Content => _content;
    public ScrollRect ScrollRectRef => _scrollRect;

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

        SyncShaderParams();
    }

    public void SyncShaderParams()
    {
        if (_gridRawImage == null) return;
        Material mat = _gridRawImage.material;
        if (mat == null) return;

        int bsd = _editor != null ? _editor.CurrentBsd : 4;

        mat.SetFloat("_TotalBeats", _totalBeats);
        mat.SetFloat("_BSD", bsd);
        mat.SetFloat("_Columns", COLUMN_COUNT);
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

        for (int i = 0; i < _chart.notes.Count; i++)
        {
            NoteData nd = _chart.notes[i];
            if (nd.laneType != _laneType) continue;
            if (nd.lane < 0 || nd.lane >= COLUMN_COUNT) continue;

            float y = BeatToY(nd.beat);
            float x = nd.lane * colWidth + 1f;
            float w = colWidth - 2f;

            bool isHold = nd.noteType == ChartNoteType.Hold
                || (nd.noteType == ChartNoteType.Dimension && nd.holdEndBeat > nd.beat);

            if (isHold)
            {
                SpawnHoldVisual(nd, x, y, w);
            }
            else
            {
                GameObject prefab = nd.noteType == ChartNoteType.Dimension
                    ? _dimensionPrefab : _tapPrefab;
                SpawnSingleNote(prefab, x, y, w);
            }
        }
    }

    private void SpawnHoldVisual(NoteData nd, float x, float y, float w)
    {
        float endY = BeatToY(nd.holdEndBeat);
        float holdHeight = endY - y;

        if (_lnHeadPrefab != null)
        {
            GameObject head = Instantiate(_lnHeadPrefab, _content);
            SetNoteRect(head, x, y, w);
            _noteObjects.Add(head);
        }

        if (_lnBodyPrefab != null)
        {
            GameObject body = Instantiate(_lnBodyPrefab, _content);
            RectTransform rt = body.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, holdHeight);
            _noteObjects.Add(body);
        }

        if (_lnTailPrefab != null)
        {
            GameObject tail = Instantiate(_lnTailPrefab, _content);
            SetNoteRect(tail, x, endY, w);
            _noteObjects.Add(tail);
        }
    }

    private void SpawnSingleNote(GameObject prefab, float x, float y, float w)
    {
        if (prefab == null) return;
        GameObject go = Instantiate(prefab, _content);
        SetNoteRect(go, x, y - _pixelsPerBeat * 0.075f, w);
        _noteObjects.Add(go);
    }

    private void SetNoteRect(GameObject go, float x, float y, float w)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) return;

        float h = _pixelsPerBeat * 0.15f;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
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
