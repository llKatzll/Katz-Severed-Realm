using System;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class DimensionManager : MonoBehaviour
{
    public static DimensionManager I { get; private set; }

    public enum SwitchMode
    {
        Hold,
        Toggle
    }

    [Header("Current State")]
    [SerializeField] private DimensionType _currentDimension = DimensionType.Dismaller;
    public DimensionType CurrentDimension => _currentDimension;

    [Header("Switch Settings")]
    [SerializeField] private SwitchMode _switchMode = SwitchMode.Hold;
    [SerializeField] private KeyCode _switchKey = KeyCode.Space;

    //Lifetime set later.
    [Header("Indicator Settings")]
    [SerializeField] private float _indicatorLeadTimeMs;
    public float IndicatorLeadTimeSec => _indicatorLeadTimeMs;

    //Lifetime set later.
    [Header("Corridor Settings")]
    [SerializeField] private float _corridorDurationMs;
    public float CorridorDurationSec => _corridorDurationMs;

    [Header("Corridor Colors (HDR)")]
    [SerializeField][ColorUsage(true, true)] private Color _corridorGroundColor;
    [SerializeField][ColorUsage(true, true)] private Color _corridorUpperColor;

    [Header("Background Effects")]
    [SerializeField] private GameObject _dismallerBgEffectPrefab;
    [SerializeField] private GameObject _separationBgEffectPrefab;
    [SerializeField] private GameObject _corridorBgEffectPrefab;
    [SerializeField] private bool _attachToCamera = true;
    [SerializeField] private Camera _mainCamera;

    [Header("Indicator Effects")]
    [SerializeField] private GameObject _dismallerIndicatorPrefab;
    [SerializeField] private GameObject _separationIndicatorPrefab;
    [SerializeField] private Transform _indicatorSpawnPoint;

    [Header("Wrong Dimension Note Effects")]
    [SerializeField] private GameObject _dismallerNoteNoisePrefab;
    [SerializeField] private GameObject _separationNoteNoisePrefab;
    [SerializeField] private GameObject _dismallerBodyNoisePrefab;
    [SerializeField] private GameObject _separationBodyNoisePrefab;

    [Header("Wrong Dimension Note Colors (HDR - Unlit style)")]
    [SerializeField][ColorUsage(true, true)] private Color _dismallerWrongColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField][ColorUsage(true, true)] private Color _separationWrongColor = new Color(4f, 0.5f, 0.5f, 1f);

    [Header("Rail Color Source")]
    [SerializeField] private HitFxPaletteSO _railColorPalette;
    [SerializeField][ColorUsage(true, true)] private Color _corridorRailColor = new Color(0f, 2f, 4f, 1f);

    [Header("Debug")]
    [SerializeField] private bool _enableDebugLog = true;
    [SerializeField] private KeyCode _debugTriggerSeparationKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode _debugTriggerDismallerKey = KeyCode.Alpha2;

    public event Action<DimensionType> OnDimensionChanged;
    public event Action<DimensionType> OnIndicatorTriggered;
    public event Action OnCorridorStarted;
    public event Action OnCorridorEnded;

    private GameObject _dismallerBgEffectInstance;
    private GameObject _separationBgEffectInstance;
    private GameObject _corridorBgEffectInstance;

    private bool _indicatorActive;
    private DimensionType _indicatedDimension;

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
                _dismallerBgEffectInstance.transform.localPosition = new Vector3(0, 0, 20.5f);
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

    public void TriggerIndicator(DimensionType upcomingDimension)
    {
        _indicatorActive = true;
        _indicatedDimension = upcomingDimension;

        if (_enableDebugLog)
        {
            Debug.Log("[DimensionManager] Indicator triggered: " + upcomingDimension + " notes coming in " + _indicatorLeadTimeMs + "ms");
        }

        SpawnIndicatorEffect(upcomingDimension);
        OnIndicatorTriggered?.Invoke(upcomingDimension);

        StartCorridor(upcomingDimension);
    }

    private void StartCorridor(DimensionType dimensionAfter)
    {
        _isCorridorActive = true;
        _dimensionAfterCorridor = dimensionAfter;
        _corridorEndDspTime = AudioSettings.dspTime + CorridorDurationSec;

        _queuedDimension = _currentDimension;

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

        SetDimension(_queuedDimension);

        OnCorridorEnded?.Invoke();
    }

    private void ApplyCorridorVisuals(bool corridorActive)
    {
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

        Destroy(fx, IndicatorLeadTimeSec + 1f);
    }

    public bool CanJudgeNote(DimensionType noteDimension, bool isLongNoteInProgress = false)
    {
        if (isLongNoteInProgress) return true;
        if (_isCorridorActive) return true;
        return noteDimension == _currentDimension;
    }

    public Color GetWrongDimensionColor(DimensionType noteDimension)
    {
        return (noteDimension == DimensionType.Dismaller)
            ? _dismallerWrongColor
            : _separationWrongColor;
    }

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

    public bool IsNoteInWrongDimension(DimensionType noteDimension)
    {
        if (_isCorridorActive) return false;
        return noteDimension != _currentDimension;
    }

    public Color GetCorridorNoteColor(NoteSpawner.NoteType noteType)
    {
        return (noteType == NoteSpawner.NoteType.Ground)
            ? _corridorGroundColor
            : _corridorUpperColor;
    }

    public Color GetCorridorHitFxColor(NoteSpawner.NoteType noteType)
    {
        return (noteType == NoteSpawner.NoteType.Ground)
            ? _corridorGroundColor
            : _corridorUpperColor;
    }

    public Color GetRailColor(NoteSpawner.NoteType railType)
    {
        if (_railColorPalette == null)
        {
            return (railType == NoteSpawner.NoteType.Ground)
                ? new Color(2f, 1f, 0f, 1f)
                : new Color(4f, 4f, 4f, 1f);
        }

        return (railType == NoteSpawner.NoteType.Ground)
            ? _railColorPalette.ground_Sev
            : _railColorPalette.upper_Sev;
    }

    public Color GetRailCorridorColor()
    {
        return _corridorRailColor;
    }

    public SwitchMode GetSwitchMode() => _switchMode;
    public void SetSwitchMode(SwitchMode mode) => _switchMode = mode;
    public KeyCode GetSwitchKey() => _switchKey;
    public void SetSwitchKey(KeyCode key) => _switchKey = key;
    public DimensionType QueuedDimension => _queuedDimension;
}