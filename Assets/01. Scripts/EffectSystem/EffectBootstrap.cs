using System.Collections.Generic;
using UnityEngine;

public class EffectBootstrap : MonoBehaviour
{
    [SerializeField] private EditorBootstrap _editorBootstrap;

    [Header("Panel")]
    [SerializeField] private float _panelCenterX = -7f;
    [SerializeField] private Color _panelColor = Color.black;

    [Header("Layout")]
    [SerializeField] private int _laneCount = 6;
    [SerializeField] private float _laneWidth = 0.4f;
    [SerializeField] private int _totalBeats = 1000;

    [Header("Column Lines")]
    [SerializeField] private Color _columnLineColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
    [SerializeField] private float _columnLineThickness = 0.03f;
    [SerializeField] private float _columnLineMarginScale = 8f;

    [Header("Note Canvas")]
    [SerializeField] private float _noteCanvasScale = 0.01f;

    private Transform _effectPanel;
    private Canvas _noteCanvas;
    private readonly List<Transform> _columnLines = new List<Transform>();

    public Transform EffectPanel => _effectPanel;
    public Canvas NoteCanvas => _noteCanvas;
    public int LaneCount => _laneCount;
    public float LaneWidth => _laneWidth;
    public float PanelCenterX => _panelCenterX;
    public EditorBootstrap EditorBootstrap => _editorBootstrap;
    public float BeatHeight => _editorBootstrap != null ? _editorBootstrap.BeatHeight : 3f;
    public float NoteCanvasScale => _noteCanvasScale;

    private void Awake()
    {
        SetupPanel();
        SetupColumnLines();
        SetupNoteCanvas();
    }

    private void SetupPanel()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "EffectPanel";
        go.transform.SetParent(transform);

        var meshCollider = go.GetComponent<MeshCollider>();
        if (meshCollider != null) Destroy(meshCollider);
        go.AddComponent<BoxCollider>();

        float width = _laneCount * _laneWidth;
        float height = _totalBeats * BeatHeight;
        go.transform.position = new Vector3(_panelCenterX, height * 0.5f, 0f);
        go.transform.localScale = new Vector3(width, height, 1f);

        var rend = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = _panelColor;
        rend.sharedMaterial = mat;

        _effectPanel = go.transform;
    }

    private void SetupColumnLines()
    {
        float halfPanel = _laneCount * _laneWidth * 0.5f;
        float ortho = _editorBootstrap != null ? _editorBootstrap.OrthoSize : 5f;
        float columnHeight = ortho * _columnLineMarginScale;

        for (int i = 0; i <= _laneCount; i++)
        {
            float x = _panelCenterX - halfPanel + i * _laneWidth;
            CreateColumnLine(x, columnHeight);
        }
    }

    private void CreateColumnLine(float x, float height)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "EffectColLine";
        go.transform.SetParent(transform);

        var meshCollider = go.GetComponent<MeshCollider>();
        if (meshCollider != null) Destroy(meshCollider);

        go.transform.position = new Vector3(x, 0f, -0.5f);
        go.transform.localScale = new Vector3(_columnLineThickness, height, 1f);

        var rend = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = _columnLineColor;
        mat.renderQueue = 3001;
        rend.sharedMaterial = mat;

        _columnLines.Add(go.transform);
    }

    private void SetupNoteCanvas()
    {
        var go = new GameObject("EffectNoteCanvas");
        go.transform.SetParent(transform);

        var rt = go.AddComponent<RectTransform>();
        rt.position = Vector3.zero;
        rt.sizeDelta = new Vector2(1f, 1f);
        rt.localScale = new Vector3(_noteCanvasScale, _noteCanvasScale, 1f);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        if (_editorBootstrap != null && _editorBootstrap.EditorCamera != null)
            canvas.worldCamera = _editorBootstrap.EditorCamera;
        canvas.sortingOrder = 11;

        go.AddComponent<UnityEngine.UI.CanvasScaler>();

        _noteCanvas = canvas;
    }

    private void LateUpdate()
    {
        if (_editorBootstrap == null) return;
        var cam = _editorBootstrap.EditorCamera;
        if (cam == null) return;

        float cameraY = cam.transform.position.y;
        for (int i = 0; i < _columnLines.Count; i++)
        {
            var t = _columnLines[i];
            if (t == null) continue;
            var p = t.position;
            p.y = cameraY;
            t.position = p;
        }
    }

    public float LaneToWorldX(int lane)
    {
        float laneOffset = (lane - (_laneCount - 1) * 0.5f) * _laneWidth;
        return _panelCenterX + laneOffset;
    }

    public bool WorldXToLane(float worldX, out int lane)
    {
        lane = -1;
        float halfPanel = _laneCount * _laneWidth * 0.5f;
        float left = _panelCenterX - halfPanel;
        float right = _panelCenterX + halfPanel;

        if (worldX < left || worldX >= right) return false;

        lane = Mathf.FloorToInt((worldX - left) / _laneWidth);
        lane = Mathf.Clamp(lane, 0, _laneCount - 1);
        return true;
    }
}
