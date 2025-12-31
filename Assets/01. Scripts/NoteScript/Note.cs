using UnityEngine;

public class Note : MonoBehaviour
{
    [Header("Dimension")]
    [SerializeField] private DimensionType _dimension = DimensionType.Dismaller;

    protected float _travelTime;
    protected NoteSpawner.NoteType _noteType;

    protected Transform _space;
    protected Transform _rotateSource;

    protected Vector3 _spawnLocal;
    protected Vector3 _hitLocal;
    protected Vector3 _despawnLocal;

    protected bool _useDespawn;
    protected float _postTime;

    protected float _yOffsetLocal;

    protected double _spawnDspTime;
    public double ExpectedHitDspTime { get; protected set; }

    // projection axis (spawn -> hit)
    protected Vector3 _axisLocal;
    protected float _spawnS;
    protected float _hitS;
    protected float _despawnS;
    protected float _moveSignS;

    public NoteSpawner.NoteType NoteType => _noteType;
    public DimensionType Dimension => _dimension;

    // Wrong dimension visual state
    private bool _isShowingWrongDimension;
    private GameObject _noiseEffectInstance;
    [System.NonSerialized] protected Renderer[] _renderers;
    private Color[] _originalColors;

    public void SetDimension(DimensionType dim)
    {
        _dimension = dim;
    }

    public void InitFollow(
        Transform space,
        Transform spawnPoint,
        Transform hitPoint,
        Transform despawnPoint,
        float travelTime,
        NoteSpawner.NoteType noteType,
        float yOffsetLocal = 0f
    )
    {
        _space = space != null ? space : hitPoint;
        _rotateSource = hitPoint != null ? hitPoint : _space;

        _useDespawn = (despawnPoint != null);

        _travelTime = Mathf.Max(0.0001f, travelTime);
        _noteType = noteType;

        _yOffsetLocal = yOffsetLocal;

        _spawnLocal = _space.InverseTransformPoint(spawnPoint.position);
        _hitLocal = _space.InverseTransformPoint(hitPoint.position);

        if (_useDespawn)
            _despawnLocal = _space.InverseTransformPoint(despawnPoint.position);
        else
            _despawnLocal = _hitLocal;

        // axis
        Vector3 axis = _hitLocal - _spawnLocal;
        if (axis.sqrMagnitude < 0.000001f) axis = Vector3.forward;
        _axisLocal = axis.normalized;

        _spawnS = Vector3.Dot(_spawnLocal, _axisLocal);
        _hitS = Vector3.Dot(_hitLocal, _axisLocal);
        _despawnS = Vector3.Dot(_despawnLocal, _axisLocal);

        _moveSignS = Mathf.Sign(_hitS - _spawnS);
        if (_moveSignS == 0f) _moveSignS = 1f;

        // post time based on world distance (stable)
        float distA = Vector3.Distance(_spawnLocal, _hitLocal);
        float speed = distA / _travelTime;

        if (_useDespawn)
        {
            float distB = Vector3.Distance(_hitLocal, _despawnLocal);
            _postTime = distB / Mathf.Max(0.0001f, speed);
        }
        else
        {
            _postTime = 0f;
        }

        _spawnDspTime = AudioSettings.dspTime;
        ExpectedHitDspTime = _spawnDspTime + _travelTime;

        Vector3 local0 = _spawnLocal;
        local0.y += _yOffsetLocal;
        transform.position = _space.TransformPoint(local0);

        if (_rotateSource != null)
            transform.rotation = _rotateSource.rotation;

        // Cache renderers for dimension effects
        CacheRenderers();

        // Subscribe to dimension changes
        if (DimensionManager.I != null)
        {
            DimensionManager.I.OnDimensionChanged += OnDimensionChanged;
            UpdateWrongDimensionVisual();
        }
    }

    private void CacheRenderers()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _originalColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && _renderers[i].material != null)
            {
                _originalColors[i] = _renderers[i].material.color;
            }
        }
    }

    private void OnDimensionChanged(DimensionType newDimension)
    {
        UpdateWrongDimensionVisual();
    }

    protected virtual void UpdateWrongDimensionVisual()
    {
        if (DimensionManager.I == null) return;

        bool shouldShowWrong = DimensionManager.I.IsNoteInWrongDimension(_dimension);

        if (shouldShowWrong && !_isShowingWrongDimension)
        {
            // Show wrong dimension effect
            ApplyWrongDimensionColor();
            SpawnNoiseEffect();
            _isShowingWrongDimension = true;
        }
        else if (!shouldShowWrong && _isShowingWrongDimension)
        {
            // Restore normal appearance
            RestoreOriginalColor();
            DestroyNoiseEffect();
            _isShowingWrongDimension = false;
        }
    }

    private void ApplyWrongDimensionColor()
    {
        if (DimensionManager.I == null) return;

        Color wrongColor = DimensionManager.I.GetWrongDimensionColor(_dimension);

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && _renderers[i].material != null)
            {
                _renderers[i].material.color = wrongColor;
            }
        }
    }

    private void RestoreOriginalColor()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && _renderers[i].material != null && i < _originalColors.Length)
            {
                _renderers[i].material.color = _originalColors[i];
            }
        }
    }

    private void SpawnNoiseEffect()
    {
        if (DimensionManager.I == null) return;
        if (_noiseEffectInstance != null) return;

        GameObject noisePrefab = DimensionManager.I.GetWrongDimensionNoisePrefab(_dimension, false);
        if (noisePrefab != null)
        {
            _noiseEffectInstance = Instantiate(noisePrefab, transform);
            _noiseEffectInstance.transform.localPosition = Vector3.zero;
        }
    }

    private void DestroyNoiseEffect()
    {
        if (_noiseEffectInstance != null)
        {
            Destroy(_noiseEffectInstance);
            _noiseEffectInstance = null;
        }
    }

    public void SetExpectedHitDspTime(double hitDspTime)
    {
        ExpectedHitDspTime = hitDspTime;
        _spawnDspTime = ExpectedHitDspTime - _travelTime;
    }

    protected virtual void Update()
    {
        if (_space == null) return;

        float elapsed = (float)(AudioSettings.dspTime - _spawnDspTime);
        if (elapsed < 0f) elapsed = 0f;

        Vector3 localPos;
        bool finished;
        EvaluateLocal(elapsed, out localPos, out finished);

        if (finished)
        {
            Destroy(gameObject);
            return;
        }

        localPos.y += _yOffsetLocal;
        transform.position = _space.TransformPoint(localPos);

        if (_rotateSource != null)
            transform.rotation = _rotateSource.rotation;
    }

    protected void EvaluateLocal(float elapsed, out Vector3 localPos, out bool finished)
    {
        finished = false;

        if (!_useDespawn)
        {
            float t = Mathf.Clamp01(elapsed / _travelTime);
            localPos = Vector3.Lerp(_spawnLocal, _hitLocal, t);
            return;
        }

        if (elapsed <= _travelTime)
        {
            float t = Mathf.Clamp01(elapsed / _travelTime);
            localPos = Vector3.Lerp(_spawnLocal, _hitLocal, t);
            return;
        }

        float e2 = elapsed - _travelTime;
        float t2 = Mathf.Clamp01(e2 / Mathf.Max(0.0001f, _postTime));
        localPos = Vector3.Lerp(_hitLocal, _despawnLocal, t2);

        if (t2 >= 1f) finished = true;
    }

    public float GetSpeedLocal()
    {
        float distA = Vector3.Distance(_spawnLocal, _hitLocal);
        return distA / Mathf.Max(0.0001f, _travelTime);
    }

    protected virtual void OnDestroy()
    {
        // Unsubscribe from dimension changes
        if (DimensionManager.I != null)
        {
            DimensionManager.I.OnDimensionChanged -= OnDimensionChanged;
        }

        DestroyNoiseEffect();
    }
}