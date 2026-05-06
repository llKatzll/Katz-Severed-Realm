using UnityEngine;

[ExecuteAlways]
public class SceneFxProxy : MonoBehaviour
{
    [Header("Camera Background")]
    [SerializeField] private Camera _camera;
    [ColorUsage(false, true)] public Color cameraBgColor = Color.black;
    [Range(0f, 1f)] public float cameraUseSolidColor = 0f;

    [Header("Skybox - Tint / Exposure")]
    [SerializeField] private Material _skyboxMaterial;
    [ColorUsage(false, true)] public Color skyboxTint = Color.white;
    public float skyboxExposure = 1f;

    [Header("Skybox - Toggle (A <-> B)")]
    [SerializeField] private Material _skyboxMaterialA;
    [SerializeField] private Material _skyboxMaterialB;
    [Range(0f, 1f)] public float skyboxToggle = 0f;

    private Material _skyboxInstance;

    private static readonly int IdTint = Shader.PropertyToID("_Tint");
    private static readonly int IdExposure = Shader.PropertyToID("_Exposure");

    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;

        if (Application.isPlaying && _skyboxMaterial != null)
        {
            _skyboxInstance = new Material(_skyboxMaterial);
            RenderSettings.skybox = _skyboxInstance;
        }
    }

    private void LateUpdate()
    {
        SyncCameraBg();
        SyncSkybox();
    }

    private void SyncCameraBg()
    {
        if (_camera == null) return;
        _camera.backgroundColor = cameraBgColor;
        _camera.clearFlags = cameraUseSolidColor < 0.5f
            ? CameraClearFlags.Skybox
            : CameraClearFlags.SolidColor;
    }

    private void SyncSkybox()
    {
        if (Application.isPlaying && _skyboxInstance != null)
        {
            if (_skyboxInstance.HasProperty(IdTint))
                _skyboxInstance.SetColor(IdTint, skyboxTint);
            if (_skyboxInstance.HasProperty(IdExposure))
                _skyboxInstance.SetFloat(IdExposure, skyboxExposure);
        }

        if (Application.isPlaying && _skyboxMaterialA != null && _skyboxMaterialB != null)
        {
            Material target = skyboxToggle < 0.5f ? _skyboxMaterialA : _skyboxMaterialB;
            if (RenderSettings.skybox != target)
            {
                RenderSettings.skybox = target;
                DynamicGI.UpdateEnvironment();
            }
        }
    }

    private void OnDestroy()
    {
        if (_skyboxInstance != null)
        {
            if (Application.isPlaying) Destroy(_skyboxInstance);
            else DestroyImmediate(_skyboxInstance);
            _skyboxInstance = null;
        }
    }
}
