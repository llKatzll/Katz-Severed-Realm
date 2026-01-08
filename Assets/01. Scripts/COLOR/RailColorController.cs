using UnityEngine;

[ExecuteAlways]
public class RailColorController : MonoBehaviour
{
    [Header("Rail Type")]
    [SerializeField] private NoteSpawner.NoteType _railType = NoteSpawner.NoteType.Ground;
    public NoteSpawner.NoteType RailType => _railType;

    [Header("Target Renderers")]
    [SerializeField] private Renderer[] _targetRenderers;

    [Header("Auto Find (Optional)")]
    [SerializeField] private bool _autoFindChildren = false;

    [Header("Preview (Editor Only)")]
    [SerializeField] private bool _previewInEditor = false;
    [SerializeField] private DimensionType _previewDimension = DimensionType.Dismaller;

    private DimensionType _lastPreviewDimension;
    private bool _lastPreviewState;

    private MaterialPropertyBlock _mpb; //mpb
    private bool _initialized;

    private static readonly int IdColor = Shader.PropertyToID("_Color");
    private static readonly int IdColor2 = Shader.PropertyToID("Color");
    private static readonly int IdEmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");
    

    private void OnEnable()
    {
        Initialize();

        if (Application.isPlaying)
        {
            if (DimensionManager.I != null)
            {
                DimensionManager.I.OnDimensionChanged += OnDimensionChanged;
                DimensionManager.I.OnCorridorStarted += OnCorridorStarted;
                DimensionManager.I.OnCorridorEnded += OnCorridorEnded;
            }

            if (RuntimeColorPalette.I != null)
            {
                RuntimeColorPalette.I.OnColorsChanged += OnColorsChanged;
            }

            ApplyCurrentColor();
        }
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            if (DimensionManager.I != null)
            {
                DimensionManager.I.OnDimensionChanged -= OnDimensionChanged;
                DimensionManager.I.OnCorridorStarted -= OnCorridorStarted;
                DimensionManager.I.OnCorridorEnded -= OnCorridorEnded;
            }

            if (RuntimeColorPalette.I != null)
            {
                RuntimeColorPalette.I.OnColorsChanged -= OnColorsChanged;
            }
        }
    }

    private void Initialize()
    {
        if (_initialized) return;

        if (_mpb == null)
            _mpb = new MaterialPropertyBlock();

        if (_autoFindChildren || _targetRenderers == null || _targetRenderers.Length == 0)
        {
            _targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        _initialized = true;
    }

    private void OnDimensionChanged(DimensionType newDimension)
    {
        ApplyCurrentColor();
    }

    private void OnColorsChanged()
    {
        ApplyCurrentColor();
    }

    private void OnCorridorStarted()
    {
        ApplyCurrentColor();
    }

    private void OnCorridorEnded()
    {
        ApplyCurrentColor();
    }

    public void ApplyCurrentColor()
    {
        Color color;

        if (RuntimeColorPalette.I != null)
        {
            color = RuntimeColorPalette.I.GetRailColor(_railType);
        }
        else if (DimensionManager.I != null)
        {
            color = DimensionManager.I.GetRailColor(_railType);
        }
        else
        {
            color = (_railType == NoteSpawner.NoteType.Ground)
                ? new Color(0f, 2f, 4f, 1f)
                : new Color(0.5f, 2.5f, 4f, 1f);
        }

        //Debug is Color workin
        //Debug.Log($"RailColorController [{name}] railType={_railType} " +
        //  $"RuntimePalette={(RuntimeColorPalette.I != null)} " +
        //  $"DimensionMgr={(DimensionManager.I != null)} " +
        //  $"color={color}");

        ApplyColor(color);
    }

    public void ApplyDimensionColor(DimensionType dimension)
    {
        ApplyCurrentColor();
    }

    public void ApplyTypeColor()
    {
        ApplyCurrentColor();
    }

    public void ApplyCorridorColor()
    {
        ApplyCurrentColor();
    }

    public void ApplyColor(Color color)
    {
        if (_targetRenderers == null) return;
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        for (int i = 0; i < _targetRenderers.Length; i++)
        {
            if (_targetRenderers[i] == null) continue;

            var rend = _targetRenderers[i];
            var mat = rend.sharedMaterial;

            if (mat == null) continue;

            _mpb.Clear();
            rend.GetPropertyBlock(_mpb);

            if (mat.HasProperty(IdColor)) _mpb.SetColor(IdColor, color);

            if (mat.HasProperty(IdColor2)) _mpb.SetColor(IdColor2, color);

            if (mat.HasProperty(IdEmissionColor)) _mpb.SetColor(IdEmissionColor, color);

            if (mat.HasProperty(IdBaseColor)) _mpb.SetColor(IdBaseColor, color);


            rend.SetPropertyBlock(_mpb);
        }
    }

    public void ClearPropertyBlock()
    {
        if (_targetRenderers == null) return;

        for (int i = 0; i < _targetRenderers.Length; i++)
        {
            if (_targetRenderers[i] != null)
            {
                _targetRenderers[i].SetPropertyBlock(null);
            }
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!Application.isPlaying)
        {
            HandleEditorPreview();
        }
    }

    private void HandleEditorPreview()
    {
        if (_previewInEditor != _lastPreviewState ||
            (_previewInEditor && _previewDimension != _lastPreviewDimension))
        {
            _lastPreviewState = _previewInEditor;
            _lastPreviewDimension = _previewDimension;

            if (_previewInEditor)
            {
                Initialize();

                Color previewColor = GetEditorPreviewColor(_previewDimension);
                ApplyColor(previewColor);
            }
            else
            {
                ClearPropertyBlock();
            }
        }
    }

    private Color GetEditorPreviewColor(DimensionType dimension)
    {
        if (RuntimeColorPalette.I != null)
        {
            return RuntimeColorPalette.I.GetRailColor(_railType);
        }

        return _railType == NoteSpawner.NoteType.Ground
            ? new Color(0f, 2f, 4f, 1f)
            : new Color(0.5f, 2.5f, 4f, 1f);
    }

    private void OnValidate()
    {
        _initialized = false;
    }
#endif
}