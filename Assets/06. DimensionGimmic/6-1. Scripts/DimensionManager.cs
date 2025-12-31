using System;
using UnityEngine;

public class DimensionManager : MonoBehaviour
{
    public static DimensionManager I { get; private set; }

    public enum SwitchMode
    {
        Hold,   // 홀드 시 세퍼레이션, 비홀드 시 디스멀러
        Toggle  // 토글방식
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

    [Header("Background Effects")]
    [SerializeField] private GameObject _dismallerBgEffectPrefab;
    [SerializeField] private GameObject _separationBgEffectPrefab;

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
    [SerializeField] private Color _dismallerWrongColor = Color.black;
    [SerializeField] private Color _separationWrongColor = Color.white;

    [Header("Debug")]
    [SerializeField] private bool _enableDebugLog = true;
    [SerializeField] private KeyCode _debugTriggerSeparationKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode _debugTriggerDismallerKey = KeyCode.Alpha2;

    // Events
    public event Action<DimensionType> OnDimensionChanged;
    public event Action<DimensionType> OnIndicatorTriggered;

    // Background effect instances
    private GameObject _dismallerBgEffectInstance;
    private GameObject _separationBgEffectInstance;

    // Indicator state
    private bool _indicatorActive;
    private DimensionType _indicatedDimension;
    private float _indicatorStartTime;

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
        if (_dismallerBgEffectPrefab != null)
        {
            _dismallerBgEffectInstance = Instantiate(_dismallerBgEffectPrefab, transform);
        }

        if (_separationBgEffectPrefab != null)
        {
            _separationBgEffectInstance = Instantiate(_separationBgEffectPrefab, transform);
        }
    }

    private void Update()
    {
        HandleDimensionSwitch();
        HandleDebugTriggers();
    }

    private void HandleDimensionSwitch()
    {
        if (_switchMode == SwitchMode.Hold)
        {
            if (Input.GetKeyDown(_switchKey))
            {
                SetDimension(DimensionType.Separation);
            }
            else if (Input.GetKeyUp(_switchKey))
            {
                SetDimension(DimensionType.Dismaller);
            }
        }
        else
        {
            if (Input.GetKeyDown(_switchKey))
            {
                DimensionType newDim = (_currentDimension == DimensionType.Dismaller)
                    ? DimensionType.Separation
                    : DimensionType.Dismaller;
                SetDimension(newDim);
            }
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


    // 실험용 인디케이터 트리거 일으키기
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

    // 롱노트 도중 차원이 바뀌어도 현재 치고 있는 롱노트까지는 허락.

    public bool CanJudgeNote(DimensionType noteDimension, bool isLongNoteInProgress = false)
    {
        if (isLongNoteInProgress) return true;
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
        return noteDimension != _currentDimension;
    }

    public SwitchMode GetSwitchMode() => _switchMode;
    public void SetSwitchMode(SwitchMode mode) => _switchMode = mode;
    public KeyCode GetSwitchKey() => _switchKey;
    public void SetSwitchKey(KeyCode key) => _switchKey = key;
}