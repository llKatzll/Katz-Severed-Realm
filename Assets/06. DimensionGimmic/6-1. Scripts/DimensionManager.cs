using System;
using UnityEngine;
using UnityEngine.UI;

public class DimensionManager : MonoBehaviour
{
    public static DimensionManager I { get; private set; }

    public enum SwitchMode
    {
        Hold,   // Space pressed = Separation, released = Dismaller
        Toggle  // Space toggles between dimensions
    }

    [Header("Current State")]
    [SerializeField] private DimensionType _currentDimension = DimensionType.Dismaller;
    public DimensionType CurrentDimension => _currentDimension;

    [Header("Switch Settings")]
    [SerializeField] private SwitchMode _switchMode = SwitchMode.Hold;
    [SerializeField] private KeyCode _switchKey = KeyCode.Space;

    [Header("Indicator Settings")]
    [SerializeField] private float _indicatorLeadTimeMs = 700f;
    public float IndicatorLeadTimeSec => _indicatorLeadTimeMs / 1000f;

    [Header("Corridor Settings")]
    [SerializeField] private float _corridorDurationMs = 700f;
    public float CorridorDurationSec => _corridorDurationMs / 1000f;
    [SerializeField] private CorridorColorsSO _corridorColors;

    [Header("Corridor Effects")]
    [SerializeField] private GameObject _corridorBgEffectPrefab;
    [SerializeField] private GameObject _corridorTapHitFxPrefab;
    [SerializeField] private GameObject _corridorHoldHeadFxPrefab;
    [SerializeField] private GameObject _corridorHoldTailFxPrefab;
    [SerializeField] private GameObject _corridorHoldLoopFxPrefab;

    [Header("Background Effects")]
    [SerializeField] private GameObject _dismallerBgEffectPrefab;
    [SerializeField] private GameObject _separationBgEffectPrefab;
    [SerializeField] private Canvas _bgEffectCanvas;  // UI용 (Screen Space - Overlay)
    [SerializeField] private bool _attachToCamera = true;  // 파티클용: 카메라에 붙이기
    [SerializeField] private Camera _mainCamera;

    [Header("Indicator Effects")]
    [SerializeField] private GameObject _dismallerIndicatorPrefab;
    [SerializeField] private GameObject _separationIndicatorPrefab;
    [SerializeField] private Transform _indicatorSpawnPoint;

    [Header("Wrong Dimension Note Effects")]
    [SerializeField] private GameObject _dismallerNoteNoisePrefab;      // Tap/Head/Tail
    [SerializeField] private GameObject _separationNoteNoisePrefab;     // Tap/Head/Tail
    [SerializeField] private GameObject _dismallerBodyNoisePrefab;      // Long note body
    [SerializeField] private GameObject _separationBodyNoisePrefab;     // Long note body

    [Header("Wrong Dimension Note Colors")]
    [SerializeField][ColorUsage(true, true)] private Color _dismallerWrongColor = Color.black;
    [SerializeField][ColorUsage(true, true)] private Color _separationWrongColor = Color.white;

    [Header("Debug")]
    [SerializeField] private bool _enableDebugLog = true;
    [SerializeField] private KeyCode _debugTriggerSeparationKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode _debugTriggerDismallerKey = KeyCode.Alpha2;

    // Events
    public event Action<DimensionType> OnDimensionChanged;
    public event Action<DimensionType> OnIndicatorTriggered;
    public event Action OnCorridorStarted;
    public event Action OnCorridorEnded;

    // Background effect instances
    private GameObject _dismallerBgEffectInstance;
    private GameObject _separationBgEffectInstance;
    private GameObject _corridorBgEffectInstance;

    // Indicator state
    private bool _indicatorActive;
    private DimensionType _indicatedDimension;
    private float _indicatorStartTime;

    // Corridor state
    private bool _isCorridorActive;
    public bool IsCorridorActive => _isCorridorActive;
    private double _corridorEndDspTime;
    private DimensionType _dimensionAfterCorridor;
    private DimensionType _queuedDimension;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        InitializeBackgroundEffects();
        ApplyDimensionVisuals();
    }

    private void InitializeBackgroundEffects()
    {
        if (_attachToCamera)
        {
            // 파티클 시스템용: 카메라에 직접 붙이기
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_mainCamera == null)
            {
                Debug.LogWarning("[DimensionManager] Main Camera not found!");
                return;
            }

            if (_dismallerBgEffectPrefab != null)
            {
                _dismallerBgEffectInstance = Instantiate(_dismallerBgEffectPrefab, _mainCamera.transform);
                _dismallerBgEffectInstance.transform.localPosition = new Vector3(0, 0, 20.5f); // 카메라 앞
                _dismallerBgEffectInstance.transform.localRotation = Quaternion.identity;
            }

            if (_separationBgEffectPrefab != null)
            {
                _separationBgEffectInstance = Instantiate(_separationBgEffectPrefab, _mainCamera.transform);
                _separationBgEffectInstance.transform.localPosition = new Vector3(0, 0, 20.5f);
                _separationBgEffectInstance.transform.localRotation = Quaternion.identity;
            }

            if (_corridorBgEffectPrefab != null)
            {
                _corridorBgEffectInstance = Instantiate(_corridorBgEffectPrefab, _mainCamera.transform);
                _corridorBgEffectInstance.transform.localPosition = new Vector3(0, 0, 20.5f);
                _corridorBgEffectInstance.transform.localRotation = Quaternion.identity;
            }
        }
        else
        {
            // UI용: Canvas에 붙이기
            if (_bgEffectCanvas == null)
            {
                GameObject canvasObj = new GameObject("DimensionBgCanvas");
                _bgEffectCanvas = canvasObj.AddComponent<Canvas>();
                _bgEffectCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _bgEffectCanvas.sortingOrder = -100;
                canvasObj.AddComponent<CanvasScaler>();
            }

            if (_dismallerBgEffectPrefab != null)
            {
                _dismallerBgEffectInstance = Instantiate(_dismallerBgEffectPrefab, _bgEffectCanvas.transform);
                SetupAsFullscreenUI(_dismallerBgEffectInstance);
            }

            if (_separationBgEffectPrefab != null)
            {
                _separationBgEffectInstance = Instantiate(_separationBgEffectPrefab, _bgEffectCanvas.transform);
                SetupAsFullscreenUI(_separationBgEffectInstance);
            }

            if (_corridorBgEffectPrefab != null)
            {
                _corridorBgEffectInstance = Instantiate(_corridorBgEffectPrefab, _bgEffectCanvas.transform);
                SetupAsFullscreenUI(_corridorBgEffectInstance);
            }
        }
    }

    private void SetupAsFullscreenUI(GameObject obj)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null)
            rect = obj.AddComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        HandleDimensionSwitch();
        HandleDebugTriggers();
        UpdateCorridorTimer();
    }

    private void UpdateCorridorTimer()
    {
        if (!_isCorridorActive) return;

        double now = AudioSettings.dspTime;
        if (now >= _corridorEndDspTime)
        {
            EndCorridor();
        }
    }

    private void HandleDimensionSwitch()
    {
        if (_switchMode == SwitchMode.Hold)
        {
            if (Input.GetKeyDown(_switchKey))
            {
                if (_isCorridorActive)
                {
                    _queuedDimension = DimensionType.Separation;
                    ApplyQueuedDimensionVisuals();
                }
                else
                    SetDimension(DimensionType.Separation);
            }
            else if (Input.GetKeyUp(_switchKey))
            {
                if (_isCorridorActive)
                {
                    _queuedDimension = DimensionType.Dismaller;
                    ApplyQueuedDimensionVisuals();
                }
                else
                    SetDimension(DimensionType.Dismaller);
            }
        }
        else
        {
            if (Input.GetKeyDown(_switchKey))
            {
                if (_isCorridorActive)
                {
                    _queuedDimension = (_queuedDimension == DimensionType.Dismaller)
                        ? DimensionType.Separation
                        : DimensionType.Dismaller;
                    ApplyQueuedDimensionVisuals();
                }
                else
                {
                    DimensionType newDim = (_currentDimension == DimensionType.Dismaller)
                        ? DimensionType.Separation
                        : DimensionType.Dismaller;
                    SetDimension(newDim);
                }
            }
        }
    }

    private void ApplyQueuedDimensionVisuals()
    {
        bool isDismaller = (_queuedDimension == DimensionType.Dismaller);

        if (_dismallerBgEffectInstance != null)
            _dismallerBgEffectInstance.SetActive(isDismaller);

        if (_separationBgEffectInstance != null)
            _separationBgEffectInstance.SetActive(!isDismaller);

        if (_enableDebugLog)
        {
            Debug.Log("[DimensionManager] Queued dimension visual: " + _queuedDimension);
        }
    }

    private void HandleDebugTriggers()
    {
        if (Input.GetKeyDown(_debugTriggerSeparationKey))
        {
            TriggerIndicator(DimensionType.Separation);
        }

        if (Input.GetKeyDown(_debugTriggerDismallerKey))
        {
            TriggerIndicator(DimensionType.Dismaller);
        }
    }

    public void SetDimension(DimensionType newDimension)
    {
        if (_currentDimension == newDimension) return;

        DimensionType oldDimension = _currentDimension;
        _currentDimension = newDimension;

        if (_enableDebugLog)
        {
            Debug.Log("[DimensionManager] Dimension changed: " + oldDimension + " -> " + newDimension);
        }

        ApplyDimensionVisuals();
        OnDimensionChanged?.Invoke(_currentDimension);
    }

    private void ApplyDimensionVisuals()
    {
        bool isDismaller = (_currentDimension == DimensionType.Dismaller);

        if (_dismallerBgEffectInstance != null)
            _dismallerBgEffectInstance.SetActive(isDismaller);

        if (_separationBgEffectInstance != null)
            _separationBgEffectInstance.SetActive(!isDismaller);
    }

    /// <summary>
    /// Trigger indicator for upcoming dimension notes.
    /// Called by chart/editor system to signal "notes of this dimension coming in 700ms"
    /// This also starts the Corridor period.
    /// </summary>
    public void TriggerIndicator(DimensionType upcomingDimension)
    {
        _indicatorActive = true;
        _indicatedDimension = upcomingDimension;
        _indicatorStartTime = Time.time;

        if (_enableDebugLog)
        {
            Debug.Log("[DimensionManager] Indicator triggered: " + upcomingDimension + " notes coming in " + _indicatorLeadTimeMs + "ms");
        }

        SpawnIndicatorEffect(upcomingDimension);
        OnIndicatorTriggered?.Invoke(upcomingDimension);

        // Start corridor
        StartCorridor(upcomingDimension);
    }

    private void StartCorridor(DimensionType dimensionAfter)
    {
        _isCorridorActive = true;
        _dimensionAfterCorridor = dimensionAfter;
        _corridorEndDspTime = AudioSettings.dspTime + CorridorDurationSec;

        // Initialize queued dimension to current (player can change it during corridor)
        _queuedDimension = _currentDimension;

        // For Hold mode: check if Space is currently held
        if (_switchMode == SwitchMode.Hold && Input.GetKey(_switchKey))
        {
            _queuedDimension = DimensionType.Separation;
        }

        if (_enableDebugLog)
        {
            Debug.Log("[DimensionManager] Corridor started. Duration: " + _corridorDurationMs + "ms. After: " + dimensionAfter);
        }

        ApplyCorridorVisuals(true);
        OnCorridorStarted?.Invoke();
    }

    private void EndCorridor()
    {
        _isCorridorActive = false;
        _indicatorActive = false;

        // For Hold mode: check current Space state at corridor end
        if (_switchMode == SwitchMode.Hold)
        {
            _queuedDimension = Input.GetKey(_switchKey)
                ? DimensionType.Separation
                : DimensionType.Dismaller;
        }

        if (_enableDebugLog)
        {
            Debug.Log("[DimensionManager] Corridor ended. Queued dimension: " + _queuedDimension);
        }

        ApplyCorridorVisuals(false);

        // Apply queued dimension
        SetDimension(_queuedDimension);

        OnCorridorEnded?.Invoke();
    }

    private void ApplyCorridorVisuals(bool corridorActive)
    {
        // Corridor effect on top of current dimension
        if (_corridorBgEffectInstance != null)
            _corridorBgEffectInstance.SetActive(corridorActive);
    }

    private void SpawnIndicatorEffect(DimensionType dimension)
    {
        GameObject prefab = (dimension == DimensionType.Dismaller)
            ? _dismallerIndicatorPrefab
            : _separationIndicatorPrefab;

        if (prefab == null) return;

        Transform spawnPoint = _indicatorSpawnPoint != null ? _indicatorSpawnPoint : transform;
        GameObject fx = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        // Auto destroy after lead time + buffer
        Destroy(fx, IndicatorLeadTimeSec + 1f);
    }

    /// <summary>
    /// Check if a note can be judged in current dimension.
    /// Long notes in progress are always judgeable.
    /// During corridor, all notes are judgeable.
    /// </summary>
    public bool CanJudgeNote(DimensionType noteDimension, bool isLongNoteInProgress = false)
    {
        if (isLongNoteInProgress) return true;
        if (_isCorridorActive) return true;
        return noteDimension == _currentDimension;
    }

    /// <summary>
    /// Get the color to apply when note is in wrong dimension
    /// </summary>
    public Color GetWrongDimensionColor(DimensionType noteDimension)
    {
        // Note is Dismaller but we're in Separation -> show as dark
        // Note is Separation but we're in Dismaller -> show as bright
        return (noteDimension == DimensionType.Dismaller)
            ? _dismallerWrongColor
            : _separationWrongColor;
    }

    /// <summary>
    /// Get noise effect prefab for wrong dimension note
    /// </summary>
    public GameObject GetWrongDimensionNoisePrefab(DimensionType noteDimension, bool isBody = false)
    {
        if (isBody)
        {
            return (noteDimension == DimensionType.Dismaller)
                ? _dismallerBodyNoisePrefab
                : _separationBodyNoisePrefab;
        }
        else
        {
            return (noteDimension == DimensionType.Dismaller)
                ? _dismallerNoteNoisePrefab
                : _separationNoteNoisePrefab;
        }
    }

    /// <summary>
    /// Check if note is in wrong dimension (should show noise effect)
    /// During corridor, no notes are "wrong"
    /// </summary>
    public bool IsNoteInWrongDimension(DimensionType noteDimension)
    {
        if (_isCorridorActive) return false;
        return noteDimension != _currentDimension;
    }

    /// <summary>
    /// Get corridor color for note based on note type
    /// </summary>
    public Color GetCorridorNoteColor(NoteSpawner.NoteType noteType, bool isLongNote = false)
    {
        if (_corridorColors == null)
            return new Color(0f, 0.5f, 1f, 1f);

        return _corridorColors.GetNoteColor(noteType, isLongNote);
    }

    /// <summary>
    /// Get corridor HitFX color for note type
    /// </summary>
    public Color GetCorridorHitFxColor(NoteSpawner.NoteType noteType)
    {
        if (_corridorColors == null)
            return new Color(0f, 0.7f, 1f, 1f);

        return _corridorColors.GetHitFxColor(noteType);
    }

    /// <summary>
    /// Get corridor HitFX prefab for tap notes
    /// </summary>
    public GameObject GetCorridorTapHitFxPrefab()
    {
        return _corridorTapHitFxPrefab;
    }

    /// <summary>
    /// Get corridor HitFX prefab for hold notes
    /// </summary>
    public GameObject GetCorridorHoldFxPrefab(string part)
    {
        switch (part.ToLower())
        {
            case "head": return _corridorHoldHeadFxPrefab;
            case "tail": return _corridorHoldTailFxPrefab;
            case "loop": return _corridorHoldLoopFxPrefab;
            default: return _corridorHoldHeadFxPrefab;
        }
    }

    /// <summary>
    /// Get the CorridorColorsSO asset
    /// </summary>
    public CorridorColorsSO GetCorridorColors() => _corridorColors;

    // Public getters for settings
    public SwitchMode GetSwitchMode() => _switchMode;
    public void SetSwitchMode(SwitchMode mode) => _switchMode = mode;
    public KeyCode GetSwitchKey() => _switchKey;
    public void SetSwitchKey(KeyCode key) => _switchKey = key;
    public DimensionType QueuedDimension => _queuedDimension;
}