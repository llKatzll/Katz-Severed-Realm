using System.Collections.Generic;
using UnityEngine;

public class EditorBootstrap : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private Color _panelColor = Color.black;

    [Header("Note Canvas")]
    [SerializeField] private float _noteCanvasScale = 0.01f;

    [Header("Layout")]
    [SerializeField] private float _orthoSize = 5f;
    [SerializeField] private float _beatHeight = 3f;
    [SerializeField] private int _laneCount = 4;
    [SerializeField] private float _laneWidth = 1f;
    [SerializeField] private float _panelGap = 2f;
    [SerializeField] private int _totalBeats = 1000;

    [Header("Judge Line")]
    [SerializeField] private float _judgeLineScreenRatio = 0.25f;
    [SerializeField] private Color _judgeLineColor = new Color(0.2f, 1f, 0.2f, 1f);
    [SerializeField] private float _judgeLineThickness = 0.05f;

    [Header("Column Lines")]
    [SerializeField] private Color _columnLineColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
    [SerializeField] private float _columnLineThickness = 0.05f;
    [SerializeField] private float _columnLineMarginScale = 8f;

    private readonly List<Transform> _columnLines = new List<Transform>();

    private Camera _editorCamera;
    private Transform _groundPanel;
    private Transform _upperPanel;
    private Transform _judgeLine;
    private Canvas _noteCanvas;

    public Camera EditorCamera => _editorCamera;
    public Transform GroundPanel => _groundPanel;
    public Transform UpperPanel => _upperPanel;
    public Transform JudgeLine => _judgeLine;
    public Canvas NoteCanvas => _noteCanvas;
    public float OrthoSize => _orthoSize;
    public float BeatHeight => _beatHeight;
    public int LaneCount => _laneCount;
    public float LaneWidth => _laneWidth;
    public float PanelGap => _panelGap;
    public float JudgeLineScreenRatio => _judgeLineScreenRatio;
    public float NoteCanvasScale => _noteCanvasScale;

    private void Awake()
    {
        SetupCamera();

        float halfPanelWidth = _laneCount * _laneWidth * 0.5f;
        float centerOffset = halfPanelWidth + _panelGap * 0.5f;
        _groundPanel = SetupPanel("GroundPanel", -centerOffset);
        _upperPanel = SetupPanel("UpperPanel", centerOffset);

        SetupJudgeLine();
        SetupColumnLines();
        SetupNoteCanvas();
    }

    private void SetupNoteCanvas()
    {
        var go = new GameObject("NoteCanvas");
        go.transform.SetParent(transform);

        var rt = go.AddComponent<RectTransform>();
        rt.position = Vector3.zero;
        rt.sizeDelta = new Vector2(1f, 1f);
        rt.localScale = new Vector3(_noteCanvasScale, _noteCanvasScale, 1f);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = _editorCamera;
        canvas.sortingOrder = 10;

        go.AddComponent<UnityEngine.UI.CanvasScaler>();

        _noteCanvas = canvas;
    }

    private void SetupCamera()
    {
        var camGo = new GameObject("EditorCamera");
        camGo.transform.SetParent(transform);

        float cameraY = _orthoSize * (1f - 2f * _judgeLineScreenRatio);
        camGo.transform.position = new Vector3(0f, cameraY, -10f);

        _editorCamera = camGo.AddComponent<Camera>();
        _editorCamera.orthographic = true;
        _editorCamera.orthographicSize = _orthoSize;
        _editorCamera.backgroundColor = Color.black;
        _editorCamera.clearFlags = CameraClearFlags.SolidColor;
        _editorCamera.nearClipPlane = 0.1f;
        _editorCamera.farClipPlane = 100f;

        camGo.AddComponent<AudioListener>();
    }

    private Transform SetupPanel(string panelName, float centerX)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = panelName;
        go.transform.SetParent(transform);

        var meshCollider = go.GetComponent<MeshCollider>();
        if (meshCollider != null) Destroy(meshCollider);
        go.AddComponent<BoxCollider>();

        float width = _laneCount * _laneWidth;
        float height = _totalBeats * _beatHeight;
        go.transform.position = new Vector3(centerX, height * 0.5f, 0f);
        go.transform.localScale = new Vector3(width, height, 1f);

        var rend = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = _panelColor;
        rend.sharedMaterial = mat;

        return go.transform;
    }

    private void SetupJudgeLine()
    {
        var parentGo = new GameObject("JudgeLine");
        parentGo.transform.SetParent(transform);
        parentGo.transform.position = new Vector3(0f, 0f, -1f);

        float halfPanel = _laneCount * _laneWidth * 0.5f;
        float centerOffset = halfPanel + _panelGap * 0.5f;
        float panelWidth = _laneCount * _laneWidth;

        CreateJudgeQuad(parentGo.transform, -centerOffset, panelWidth);
        CreateJudgeQuad(parentGo.transform, centerOffset, panelWidth);

        _judgeLine = parentGo.transform;
    }

    private void CreateJudgeQuad(Transform parent, float localX, float width)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "JudgeQuad";
        go.transform.SetParent(parent, false);

        var meshCollider = go.GetComponent<MeshCollider>();
        if (meshCollider != null) Destroy(meshCollider);

        go.transform.localPosition = new Vector3(localX, 0f, 0f);
        go.transform.localScale = new Vector3(width, _judgeLineThickness, 1f);

        var rend = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = _judgeLineColor;
        rend.sharedMaterial = mat;
    }

    private void SetupColumnLines()
    {
        float halfPanel = _laneCount * _laneWidth * 0.5f;
        float centerOffset = halfPanel + _panelGap * 0.5f;
        float columnHeight = _orthoSize * _columnLineMarginScale;

        for (int i = 0; i <= _laneCount; i++)
        {
            float xGround = -centerOffset - halfPanel + i * _laneWidth;
            float xUpper = centerOffset - halfPanel + i * _laneWidth;
            CreateColumnLine(xGround, columnHeight);
            CreateColumnLine(xUpper, columnHeight);
        }
    }

    private void CreateColumnLine(float x, float height)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "ColLine";
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

    private void LateUpdate()
    {
        if (_editorCamera == null) return;
        float cameraY = _editorCamera.transform.position.y;
        for (int i = 0; i < _columnLines.Count; i++)
        {
            var t = _columnLines[i];
            if (t == null) continue;
            var p = t.position;
            p.y = cameraY;
            t.position = p;
        }
    }
}
