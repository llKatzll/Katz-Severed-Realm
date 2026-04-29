using System.Collections.Generic;
using UnityEngine;

public class EditorBeatGrid : MonoBehaviour
{
    [SerializeField] private EditorBootstrap _bootstrap;
    [SerializeField] private EditorTimeline _timeline;

    [Header("Line Settings")]
    [SerializeField] private float _lineThickness = 0.03f;
    [SerializeField] private float _lineMarginBeats = 5f;
    [SerializeField] private Color _lineColor = new Color(0.55f, 0.55f, 0.55f, 0.7f);
    [SerializeField] private Color _wholeBeatColor = new Color(0.95f, 0.3f, 0.3f, 0.85f);

    private readonly List<Transform> _pool = new List<Transform>();
    private readonly List<MeshRenderer> _renderers = new List<MeshRenderer>();

    private void Awake()
    {
        if (_bootstrap == null) _bootstrap = GetComponent<EditorBootstrap>();
        if (_timeline == null) _timeline = GetComponent<EditorTimeline>();
    }

    private void LateUpdate()
    {
        UpdateLines();
    }

    private void UpdateLines()
    {
        if (_timeline == null || _bootstrap == null) return;
        var cam = _bootstrap.EditorCamera;
        if (cam == null) return;

        float ortho = cam.orthographicSize;
        float beatHeight = _bootstrap.BeatHeight;
        float currentBeat = _timeline.CurrentBeat;
        float beatRange = ortho / beatHeight + _lineMarginBeats;
        float minBeat = Mathf.Max(0f, currentBeat - beatRange);
        float maxBeat = currentBeat + beatRange;

        int bsd = Mathf.Max(1, _timeline.Bsd);
        float step = 1f / bsd;
        float startBeat = Mathf.Ceil(minBeat * bsd) / bsd;

        float panelWidth = _bootstrap.LaneCount * _bootstrap.LaneWidth;
        float halfPanel = panelWidth * 0.5f;
        float centerOffset = halfPanel + _bootstrap.PanelGap * 0.5f;

        int idx = 0;
        for (float b = startBeat; b <= maxBeat + 0.0001f; b += step)
        {
            bool isWholeBeat = Mathf.Abs(b - Mathf.Round(b)) < 0.0001f;
            Color color = isWholeBeat ? _wholeBeatColor : _lineColor;
            float worldY = b * beatHeight;

            PlaceLine(idx, -centerOffset, worldY, panelWidth, color);
            idx++;
            PlaceLine(idx, centerOffset, worldY, panelWidth, color);
            idx++;
        }

        for (int i = idx; i < _pool.Count; i++)
        {
            _pool[i].gameObject.SetActive(false);
        }
    }

    private void PlaceLine(int idx, float worldX, float worldY, float width, Color color)
    {
        EnsureLine(idx);
        var t = _pool[idx];
        t.gameObject.SetActive(true);
        t.position = new Vector3(worldX, worldY, -0.3f);
        t.localScale = new Vector3(width, _lineThickness, 1f);
        _renderers[idx].sharedMaterial.color = color;
    }

    private void EnsureLine(int idx)
    {
        while (_pool.Count <= idx)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "BeatLine_" + _pool.Count;
            go.transform.SetParent(transform);

            var col = go.GetComponent<MeshCollider>();
            if (col != null) Destroy(col);

            var rend = go.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = _lineColor;
            rend.sharedMaterial = mat;

            _pool.Add(go.transform);
            _renderers.Add(rend);
        }
    }
}
