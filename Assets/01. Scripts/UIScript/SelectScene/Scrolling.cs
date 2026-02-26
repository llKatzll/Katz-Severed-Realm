using UnityEngine;

public class Scrolling : MonoBehaviour
{
    [Header("Pivot")]
    [SerializeField] private Transform _pivot;

    [Header("Song Bars")]
    [SerializeField] private RectTransform[] _songBars;

    [Header("Rotation Settings")]
    [SerializeField] private float _rotationPerSlot = 40f;
    [SerializeField] private float _rotationSpeed = 8f;

    [Header("Selection Position")]
    [SerializeField] private float _selectedOffsetX = 58f;
    [SerializeField] private float _positionSpeed = 10f;
    [SerializeField] private float _selectionAngleThreshold = 20f;

    [Header("Input")]
    [SerializeField] private bool _invertWheel = false;

    private float _targetRotationZ;
    private float _currentRotationZ;
    private float[] _basePosX;
    private float[] _basePosY;
    private float[] _currentOffsetX;

    private void Start()
    {
        if (_pivot != null)
        {
            _currentRotationZ = _pivot.localEulerAngles.z;
            _targetRotationZ = _currentRotationZ;
        }

        if (_songBars != null)
        {
            _basePosX = new float[_songBars.Length];
            _basePosY = new float[_songBars.Length];
            _currentOffsetX = new float[_songBars.Length];

            for (int i = 0; i < _songBars.Length; i++)
            {
                if (_songBars[i] != null)
                {
                    _basePosX[i] = _songBars[i].anchoredPosition.x;
                    _basePosY[i] = _songBars[i].anchoredPosition.y;
                    _currentOffsetX[i] = 0f;
                }
            }
        }
    }

    private void Update()
    {
        HandleWheelInput();
        ApplyRotation();
        UpdateBarPositions();
    }

    private void HandleWheelInput()
    {
        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            float direction = _invertWheel ? -scroll : scroll;
            _targetRotationZ += direction * _rotationPerSlot;
        }
    }

    private void ApplyRotation()
    {
        if (_pivot == null) return;

        _currentRotationZ = Mathf.Lerp(_currentRotationZ, _targetRotationZ, Time.deltaTime * _rotationSpeed);

        Vector3 euler = _pivot.localEulerAngles;
        euler.z = _currentRotationZ;
        _pivot.localEulerAngles = euler;
    }

    private void UpdateBarPositions()
    {
        if (_songBars == null || _pivot == null) return;

        float pivotZ = _pivot.localEulerAngles.z;

        for (int i = 0; i < _songBars.Length; i++)
        {
            if (_songBars[i] == null) continue;

            float barLocalZ = _songBars[i].localEulerAngles.z;
            float totalAngle = NormalizeAngle(pivotZ + barLocalZ);

            bool isNearHorizontal = Mathf.Abs(totalAngle) <= _selectionAngleThreshold;
            float targetOffset = isNearHorizontal ? _selectedOffsetX : 0f;

            _currentOffsetX[i] = Mathf.Lerp(_currentOffsetX[i], targetOffset, Time.deltaTime * _positionSpeed);

            float barRad = barLocalZ * Mathf.Deg2Rad;
            float offsetX = _currentOffsetX[i] * Mathf.Cos(barRad);
            float offsetY = _currentOffsetX[i] * Mathf.Sin(barRad);

            Vector2 pos;
            pos.x = _basePosX[i] + offsetX;
            pos.y = _basePosY[i] + offsetY;
            _songBars[i].anchoredPosition = pos;
        }
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}