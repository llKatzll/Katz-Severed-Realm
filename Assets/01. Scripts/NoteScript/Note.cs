using UnityEngine;

public class Note : MonoBehaviour
{
    [Header("Dimension")]
    [SerializeField] private DimensionType _dimension = DimensionType.Dismaller;

    protected float _travelTime;
    protected NoteSpawner.NoteType _noteType;

    protected Transform _space;
    protected Transform _rotateSource;

    // Store Transform references for dynamic rail support
    protected Transform _spawnPointRef;
    protected Transform _hitPointRef;
    protected Transform _despawnPointRef;

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
    private bool _isShowingCorridor;
    private GameObject _noiseEffectInstance;
    [System.NonSerialized] protected Renderer[] _renderers;
    protected Color[] _originalColors;
    protected string[] _originalColorProperties;

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

        // Store Transform references for dynamic updates
        _spawnPointRef = spawnPoint;
        _hitPointRef = hitPoint;
        _despawnPointRef = despawnPoint;

        _useDespawn = (despawnPoint != null);

        _travelTime = Mathf.Max(0.0001f, travelTime);
        _noteType = noteType;

        _yOffsetLocal = yOffsetLocal;

        // Initial calculation
        UpdateLocalPositions();

        // axis (calculated once - direction doesn't change)
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

        // Subscribe to dimension and corridor changes
        if (DimensionManager.I != null)
        {
            DimensionManager.I.OnDimensionChanged += OnDimensionChanged;
            DimensionManager.I.OnCorridorStarted += OnCorridorStarted;
            DimensionManager.I.OnCorridorEnded += OnCorridorEnded;
            UpdateNoteVisual();
        }
    }

    /// <summary>
    /// Update local positions from current Transform positions (for dynamic rails)
    /// </summary>
    protected void UpdateLocalPositions()
    {
        if (_space == null) return;

        if (_spawnPointRef != null)
            _spawnLocal = _space.InverseTransformPoint(_spawnPointRef.position);

        if (_hitPointRef != null)
            _hitLocal = _space.InverseTransformPoint(_hitPointRef.position);

        if (_useDespawn && _despawnPointRef != null)
            _despawnLocal = _space.InverseTransformPoint(_despawnPointRef.position);
        else
            _despawnLocal = _hitLocal;
    }

    private void CacheRenderers()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _originalColors = new Color[_renderers.Length];
        _originalColorProperties = new string[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && _renderers[i].material != null)
            {
                var mat = _renderers[i].material;

                // Check EmissionColor first, but only if it has meaningful value
                if (mat.HasProperty("_EmissionColor"))
                {
                    Color emissionCol = mat.GetColor("_EmissionColor");
                    // If emission has meaningful color (not black), use it
                    if (emissionCol.r > 0.01f || emissionCol.g > 0.01f || emissionCol.b > 0.01f)
                    {
                        _originalColors[i] = emissionCol;
                        _originalColorProperties[i] = "_EmissionColor";
                        continue;
                    }
                }

                // Otherwise use _Color or _BaseColor
                if (mat.HasProperty("_Color"))
                {
                    _originalColors[i] = mat.GetColor("_Color");
                    _originalColorProperties[i] = "_Color";
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    _originalColors[i] = mat.GetColor("_BaseColor");
                    _originalColorProperties[i] = "_BaseColor";
                }
                else
                {
                    _originalColors[i] = mat.color;
                    _originalColorProperties[i] = "_Color";
                }
            }
        }
    }

    private void OnDimensionChanged(DimensionType newDimension)
    {
        UpdateNoteVisual();
    }

    private void OnCorridorStarted()
    {
        UpdateNoteVisual();
    }

    private void OnCorridorEnded()
    {
        UpdateNoteVisual();
    }

    protected virtual void UpdateNoteVisual()
    {
        if (DimensionManager.I == null) return;

        bool inCorridor = DimensionManager.I.IsCorridorActive;
        bool inWrongDimension = DimensionManager.I.IsNoteInWrongDimension(_dimension);

        // Priority: Corridor > Wrong Dimension > Normal
        if (inCorridor)
        {
            // Show corridor visual
            if (!_isShowingCorridor)
            {
                DestroyNoiseEffect();
                ApplyCorridorColor();
                _isShowingCorridor = true;
                _isShowingWrongDimension = false;
            }
        }
        else if (inWrongDimension)
        {
            // Show wrong dimension effect
            if (!_isShowingWrongDimension || _isShowingCorridor)
            {
                ApplyWrongDimensionColor();
                SpawnNoiseEffect();
                _isShowingWrongDimension = true;
                _isShowingCorridor = false;
            }
        }
        else
        {
            // Restore normal appearance
            if (_isShowingWrongDimension || _isShowingCorridor)
            {
                RestoreOriginalColor();
                DestroyNoiseEffect();
                _isShowingWrongDimension = false;
                _isShowingCorridor = false;
            }
        }
    }

    protected virtual void UpdateWrongDimensionVisual()
    {
        UpdateNoteVisual();
    }

    protected virtual void ApplyCorridorColor()
    {
        if (DimensionManager.I == null) return;

        Color corridorColor = DimensionManager.I.GetCorridorNoteColor(_noteType, false);

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && _renderers[i].material != null)
            {
                var mat = _renderers[i].material;

                // ShaderGraph Override method
                if (mat.HasProperty("_UseColorOverride"))
                {
                    mat.SetFloat("_UseColorOverride", 1f);
                    mat.SetColor("_OverrideColor", corridorColor);
                }
                // Fallback: direct property set
                else if (_originalColorProperties != null && i < _originalColorProperties.Length)
                {
                    string prop = _originalColorProperties[i];
                    if (!string.IsNullOrEmpty(prop) && mat.HasProperty(prop))
                    {
                        if (prop == "_EmissionColor")
                            mat.EnableKeyword("_EMISSION");
                        mat.SetColor(prop, corridorColor);
                    }
                }
            }
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
                var mat = _renderers[i].material;

                // ShaderGraph Override method
                if (mat.HasProperty("_UseColorOverride"))
                {
                    mat.SetFloat("_UseColorOverride", 1f);
                    mat.SetColor("_OverrideColor", wrongColor);
                }
                // Fallback: direct property set
                else if (_originalColorProperties != null && i < _originalColorProperties.Length)
                {
                    string prop = _originalColorProperties[i];
                    if (!string.IsNullOrEmpty(prop) && mat.HasProperty(prop))
                    {
                        if (prop == "_EmissionColor")
                            mat.EnableKeyword("_EMISSION");
                        mat.SetColor(prop, wrongColor);
                    }
                }
            }
        }
    }

    private void RestoreOriginalColor()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && _renderers[i].material != null)
            {
                var mat = _renderers[i].material;

                // ShaderGraph Override method - disable override
                if (mat.HasProperty("_UseColorOverride"))
                {
                    mat.SetFloat("_UseColorOverride", 0f);
                }
                // Fallback: restore original color
                else if (_originalColors != null && i < _originalColors.Length &&
                         _originalColorProperties != null && i < _originalColorProperties.Length)
                {
                    Color original = _originalColors[i];
                    string prop = _originalColorProperties[i];

                    if (!string.IsNullOrEmpty(prop) && mat.HasProperty(prop))
                    {
                        mat.SetColor(prop, original);
                    }
                }
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

        // Update local positions for dynamic rails
        UpdateLocalPositions();

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
        // Unsubscribe from dimension and corridor changes
        if (DimensionManager.I != null)
        {
            DimensionManager.I.OnDimensionChanged -= OnDimensionChanged;
            DimensionManager.I.OnCorridorStarted -= OnCorridorStarted;
            DimensionManager.I.OnCorridorEnded -= OnCorridorEnded;
        }

        DestroyNoiseEffect();
    }
}