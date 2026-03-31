using UnityEngine;

public class Scrolling : MonoBehaviour
{
    [Header("Pivot")]
    [SerializeField] private Transform _pivot;

    [Header("Song Bars")]
    [SerializeField] private SongBar[] _songBars;

    [Header("All Songs")]
    [SerializeField] private string _songResourcePath = "Songs";
    private SongData[] _allSongs;

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

    // --- Recycling ---
    // _songIndexMap[i] = i SongBar
    private int[] _songIndexMap;
    // Pivot ( = * _rotationPerSlot)
    private int _logicalStep = 0;
    private int _prevLogicalStep = 0;

    public int TotalSongCount => _allSongs != null ? _allSongs.Length : 0;

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

        LoadAllSongs();
        InitSongIndexMap();
        StartCoroutine(InitialSelectionCheck());
    }

    private void LoadAllSongs()
    {
        _allSongs = Resources.LoadAll<SongData>(_songResourcePath);
        System.Array.Sort(_allSongs, (a, b) =>
            string.Compare(a.name, b.name, System.StringComparison.Ordinal));
    }

    private void InitSongIndexMap()
    {
        if (_songBars == null) return;

        _songIndexMap = new int[_songBars.Length];

        float pivotInitialZ = _pivot != null ? NormalizeAngle(_pivot.localEulerAngles.z) : 0f;

        for (int i = 0; i < _songBars.Length; i++)
        {
            if (_songBars[i] == null) continue;

            var rt = _songBars[i].GetComponent<RectTransform>();
            float angle = NormalizeAngle(pivotInitialZ + rt.localEulerAngles.z);
            int slot = Mathf.RoundToInt(angle / _rotationPerSlot);

            _songIndexMap[i] = slot;

            if (_allSongs != null && _allSongs.Length > 0)
            {
                _songBars[i].SetSongData(_allSongs[WrapSongIndex(slot)]);
            }
        }
    }

    private int WrapSongIndex(int index)
    {
        if (_allSongs == null || _allSongs.Length == 0) return 0;
        int len = _allSongs.Length;
        return ((index % len) + len) % len;
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
        CheckRecycle();
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
            _logicalStep += direction > 0 ? 1 : -1;
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

    private void CheckRecycle()
    {
        if (_allSongs == null || _allSongs.Length == 0) return;
        if (_songBars == null || _songIndexMap == null) return;
        if (_logicalStep == _prevLogicalStep) return;

        int delta = _logicalStep - _prevLogicalStep;
        int barCount = _songBars.Length;

        if (delta > 0)
        {
            for (int d = 0; d < delta; d++)
            {
                int backBarIndex = FindBackmostBar();
                if (backBarIndex < 0) continue;

                int minSongIdx = int.MaxValue;
                for (int i = 0; i < barCount; i++)
                {
                    if (_songIndexMap[i] < minSongIdx)
                        minSongIdx = _songIndexMap[i];
                }

                int newSongIdx = minSongIdx - 1;
                _songIndexMap[backBarIndex] = newSongIdx;
                _songBars[backBarIndex].SetSongData(_allSongs[WrapSongIndex(newSongIdx)]);
            }
        }
        else if (delta < 0)
        {
            int absDelta = -delta;
            for (int d = 0; d < absDelta; d++)
            {
                int frontBarIndex = FindFrontmostBar();
                if (frontBarIndex < 0) continue;

                int maxSongIdx = int.MinValue;
                for (int i = 0; i < barCount; i++)
                {
                    if (_songIndexMap[i] > maxSongIdx)
                        maxSongIdx = _songIndexMap[i];
                }

                int newSongIdx = maxSongIdx + 1;
                _songIndexMap[frontBarIndex] = newSongIdx;
                _songBars[frontBarIndex].SetSongData(_allSongs[WrapSongIndex(newSongIdx)]);
            }
        }

        _prevLogicalStep = _logicalStep;
    }

    private int FindBackmostBar()
    {
        if (_pivot == null || _songBars == null) return -1;

        float pivotZ = _pivot.localEulerAngles.z;
        int backIndex = -1;
        float backAngle = float.MinValue;

        for (int i = 0; i < _songBars.Length; i++)
        {
            if (_songBars[i] == null) continue;
            var rt = _songBars[i].GetComponent<RectTransform>();
            float totalAngle = NormalizeAngle(pivotZ + rt.localEulerAngles.z);

            if (totalAngle > backAngle)
            {
                backAngle = totalAngle;
                backIndex = i;
            }
        }

        return backIndex;
    }

    private int FindFrontmostBar()
    {
        if (_pivot == null || _songBars == null) return -1;

        float pivotZ = _pivot.localEulerAngles.z;
        int frontIndex = -1;
        float frontAngle = float.MaxValue;

        for (int i = 0; i < _songBars.Length; i++)
        {
            if (_songBars[i] == null) continue;
            var rt = _songBars[i].GetComponent<RectTransform>();
            float totalAngle = NormalizeAngle(pivotZ + rt.localEulerAngles.z);

            if (totalAngle < frontAngle)
            {
                frontAngle = totalAngle;
                frontIndex = i;
            }
        }

        return frontIndex;
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
