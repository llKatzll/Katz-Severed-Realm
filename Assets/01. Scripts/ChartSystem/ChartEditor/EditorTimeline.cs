using UnityEngine;

public class EditorTimeline : MonoBehaviour
{
    [SerializeField] private EditorBootstrap _bootstrap;

    [Header("Scroll")]
    [SerializeField] private float _scrollSpeed = 1f;

    [Header("Snap")]
    [SerializeField] private int _bsd = 4;

    private float _currentBeat;
    private Camera _cam;

    public float CurrentBeat
    {
        get => _currentBeat;
        set
        {
            _currentBeat = Mathf.Max(0f, value);
            UpdateCameraAndJudgeLine();
        }
    }

    public int Bsd
    {
        get => _bsd;
        set => _bsd = Mathf.Max(1, value);
    }

    public EditorBootstrap Bootstrap => _bootstrap;

    private void Awake()
    {
        if (_bootstrap == null) _bootstrap = GetComponent<EditorBootstrap>();
    }

    private void Start()
    {
        if (_bootstrap == null) return;
        _cam = _bootstrap.EditorCamera;
        UpdateCameraAndJudgeLine();
    }

    private void Update()
    {
        if (_cam == null) return;
        HandleInput();
    }

    private void HandleInput()
    {
        float wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) < 0.001f) return;
        CurrentBeat += wheel * _scrollSpeed;
    }

    private void UpdateCameraAndJudgeLine()
    {
        if (_cam == null || _bootstrap == null) return;

        float beatHeight = _bootstrap.BeatHeight;
        float ratio = _bootstrap.JudgeLineScreenRatio;
        float ortho = _cam.orthographicSize;

        float judgeWorldY = _currentBeat * beatHeight;
        float cameraWorldY = judgeWorldY + ortho * (1f - 2f * ratio);

        var camPos = _cam.transform.position;
        camPos.y = cameraWorldY;
        _cam.transform.position = camPos;

        var judge = _bootstrap.JudgeLine;
        if (judge != null)
        {
            var jp = judge.position;
            jp.y = judgeWorldY;
            judge.position = jp;
        }
    }

    public float BeatToWorldY(float beat)
    {
        return beat * _bootstrap.BeatHeight;
    }

    public float WorldYToBeat(float y)
    {
        return y / _bootstrap.BeatHeight;
    }

    public float SnapBeat(float beat)
    {
        return Mathf.Round(beat * _bsd) / _bsd;
    }

    public float LaneToWorldX(int lane, ChartLaneType type)
    {
        float laneCount = _bootstrap.LaneCount;
        float laneWidth = _bootstrap.LaneWidth;
        float gap = _bootstrap.PanelGap;
        float halfPanel = laneCount * laneWidth * 0.5f;
        float centerOffset = halfPanel + gap * 0.5f;
        float panelCenterX = (type == ChartLaneType.Ground) ? -centerOffset : centerOffset;
        float laneOffset = (lane - (laneCount - 1) * 0.5f) * laneWidth;
        return panelCenterX + laneOffset;
    }

    public bool WorldXToLane(float worldX, out int lane, out ChartLaneType type)
    {
        lane = -1;
        type = ChartLaneType.Ground;

        float laneCount = _bootstrap.LaneCount;
        float laneWidth = _bootstrap.LaneWidth;
        float gap = _bootstrap.PanelGap;
        float halfPanel = laneCount * laneWidth * 0.5f;
        float centerOffset = halfPanel + gap * 0.5f;

        float groundLeft = -centerOffset - halfPanel;
        float groundRight = -centerOffset + halfPanel;
        float upperLeft = centerOffset - halfPanel;
        float upperRight = centerOffset + halfPanel;

        if (worldX >= groundLeft && worldX < groundRight)
        {
            lane = Mathf.FloorToInt((worldX - groundLeft) / laneWidth);
            lane = Mathf.Clamp(lane, 0, (int)laneCount - 1);
            type = ChartLaneType.Ground;
            return true;
        }

        if (worldX >= upperLeft && worldX < upperRight)
        {
            lane = Mathf.FloorToInt((worldX - upperLeft) / laneWidth);
            lane = Mathf.Clamp(lane, 0, (int)laneCount - 1);
            type = ChartLaneType.Upper;
            return true;
        }

        return false;
    }
}
