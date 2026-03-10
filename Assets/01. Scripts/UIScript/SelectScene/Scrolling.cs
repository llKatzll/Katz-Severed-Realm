using UnityEngine;

public class Scrolling : MonoBehaviour
{
    [Header("Pivot")]
    [SerializeField] private Transform _pivot;

    [Header("Song Bars")]
    [SerializeField] private SongBar[] _songBars;

    [Header("Rotation Settings")]
    [SerializeField] private float _rotationPerSlot = 40f;
    [SerializeField] private float _rotationSpeed = 8f;

    [Header("Selection")]
    [SerializeField] private float _selectedOffsetX = 58f;
    [SerializeField] private float _positionSpeed = 10f;
    [SerializeField] private float _selectionAngleThreshold = 20f;

    [Header("Input")]
    [SerializeField] private bool _invertWheel = false;

    [Header("Key Hold Settings")]
    [SerializeField] private float _initialHoldInterval = 0.5f;
    [SerializeField] private float _minHoldInterval = 0.05f;
    [SerializeField] private float _holdAcceleration = 0.85f;

    private float _targetRotationZ;
    private float _currentRotationZ;
    private float[] _basePosX;
    private float[] _basePosY;
    private float[] _currentOffsetX;
    private int _currentSelectedIndex = -1;

    private float _holdTimer = 0f;
    private float _currentHoldInterval;
    private int _holdDirection = 0;

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
                    var rt = _songBars[i].GetComponent<RectTransform>();
                    _basePosX[i] = rt.anchoredPosition.x;
                    _basePosY[i] = rt.anchoredPosition.y;
                    _currentOffsetX[i] = 0f;
                }
            }
        }

        _currentHoldInterval = _initialHoldInterval;

        StartCoroutine(InitialSelectionCheck());
    }

    private System.Collections.IEnumerator InitialSelectionCheck()
    {
        yield return null;
        UpdateBarPositions();
    }

    private void Update()
    {
        HandleInput();
        ApplyRotation();
        UpdateBarPositions();
    }

    private void HandleInput()
    {
        float scroll = Input.mouseScrollDelta.y;

        if (Input.GetKeyDown(KeyCode.W))
        {
            scroll = 1f;
            _holdDirection = 1;
            _holdTimer = 0f;
            _currentHoldInterval = _initialHoldInterval;
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            scroll = -1f;
            _holdDirection = -1;
            _holdTimer = 0f;
            _currentHoldInterval = _initialHoldInterval;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            _holdDirection = 1;
            _holdTimer += Time.deltaTime;

            if (_holdTimer >= _currentHoldInterval)
            {
                scroll = 1f;
                _holdTimer = 0f;
                _currentHoldInterval = Mathf.Max(_minHoldInterval, _currentHoldInterval * _holdAcceleration);
            }
        }
        else if (Input.GetKey(KeyCode.S))
        {
            _holdDirection = -1;
            _holdTimer += Time.deltaTime;

            if (_holdTimer >= _currentHoldInterval)
            {
                scroll = -1f;
                _holdTimer = 0f;
                _currentHoldInterval = Mathf.Max(_minHoldInterval, _currentHoldInterval * _holdAcceleration);
            }
        }
        else
        {
            _holdDirection = 0;
            _holdTimer = 0f;
            _currentHoldInterval = _initialHoldInterval;
        }

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
        int newSelectedIndex = -1;
        float closestAngle = float.MaxValue;

        for (int i = 0; i < _songBars.Length; i++)
        {
            if (_songBars[i] == null) continue;

            var rt = _songBars[i].GetComponent<RectTransform>();
            if (rt == null) continue;

            float barLocalZ = rt.localEulerAngles.z;
            float totalAngle = NormalizeAngle(pivotZ + barLocalZ);
            float absAngle = Mathf.Abs(totalAngle);

            bool isNearHorizontal = absAngle <= _selectionAngleThreshold;
            float targetOffset = isNearHorizontal ? _selectedOffsetX : 0f;

            _currentOffsetX[i] = Mathf.Lerp(_currentOffsetX[i], targetOffset, Time.deltaTime * _positionSpeed);

            float barRad = barLocalZ * Mathf.Deg2Rad;
            float offsetX = _currentOffsetX[i] * Mathf.Cos(barRad);
            float offsetY = _currentOffsetX[i] * Mathf.Sin(barRad);

            Vector2 pos;
            pos.x = _basePosX[i] + offsetX;
            pos.y = _basePosY[i] + offsetY;
            rt.anchoredPosition = pos;

            if (isNearHorizontal && absAngle < closestAngle)
            {
                closestAngle = absAngle;
                newSelectedIndex = i;
            }
        }

        if (newSelectedIndex != _currentSelectedIndex)
        {
            if (_currentSelectedIndex >= 0 && _currentSelectedIndex < _songBars.Length)
            {
                OnBarDeselected(_songBars[_currentSelectedIndex]);
            }

            _currentSelectedIndex = newSelectedIndex;

            if (_currentSelectedIndex >= 0)
            {
                OnBarSelected(_songBars[_currentSelectedIndex]);
            }
        }
    }

    private void OnBarSelected(SongBar bar)
    {
        if (SongSelectManager.I != null)
        {
            SongSelectManager.I.OnSongBarSelected(bar);
        }
    }

    private void OnBarDeselected(SongBar bar)
    {
        if (SongSelectManager.I != null)
        {
            SongSelectManager.I.OnSongBarDeselected(bar);
        }
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}