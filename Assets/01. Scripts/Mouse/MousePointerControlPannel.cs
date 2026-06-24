using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MousePointerControlPannel : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string[] _excludeSceneNames = { "InGame" };

    [Header("Cursor")]
    [SerializeField] private bool _hideCursorOnEnabledScenes = true;
    [SerializeField] private CursorLockMode _lockModeOnEnabledScenes = CursorLockMode.None;

    [SerializeField] private bool _showCursorOnExcludedScenes = true;
    [SerializeField] private CursorLockMode _lockModeOnExcludedScenes = CursorLockMode.None;

    [Header("Cursor Particle Prefab")]
    [SerializeField] private ParticleSystem _cursorParticlePrefab;

    [Header("Follow")]
    [SerializeField] private float _followDistance = 5f;
    [SerializeField] private float _smooth = 25f;
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Software Cursor")]
    [SerializeField] private bool _useSoftwareCursor = true;
    [SerializeField] private RectTransform _cursorIcon;

    [Header("Cursor Trail")]
    [SerializeField] private bool _useCursorTrail = true;
    [SerializeField] private int _trailCount = 4;
    [SerializeField] private float _trailSmooth = 18f;
    [Range(0f, 1f)]
    [SerializeField] private float _trailStartAlpha = 0.6f;
    [Range(0f, 1f)]
    [SerializeField] private float _trailEndAlpha = 0.05f;

    private RectTransform[] _trailParts;
    private bool _trailBuilt;

    private static MousePointerControlPannel _instance;

    private ParticleSystem _particleInstance;
    private Transform _particleTf;
    private Camera _cam;

    private bool _enabledForThisScene = true;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        EnsureParticle();
        RefreshForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshForScene(scene.name);
    }

    private void LateUpdate()
    {
        UpdateCursorIcon();

        if (!_enabledForThisScene) return;
        if (_particleTf == null) return;

        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        Vector3 mp = Input.mousePosition;
        mp.z = Mathf.Max(0.01f, _followDistance);

        Vector3 target = _cam.ScreenToWorldPoint(mp);

        float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float k = 1f - Mathf.Exp(-_smooth * dt);

        _particleTf.position = Vector3.Lerp(_particleTf.position, target, k);
    }

    private void RefreshForScene(string sceneName)
    {
        _enabledForThisScene = !IsExcludedScene(sceneName);

        if (_enabledForThisScene)
        {
            ApplyCursorState(!_hideCursorOnEnabledScenes, _lockModeOnEnabledScenes);
            EnsureParticle();
            SetParticleActive(true);
            _cam = Camera.main;
        }
        else
        {
            ApplyCursorState(_showCursorOnExcludedScenes, _lockModeOnExcludedScenes);
            SetParticleActive(false);
            _cam = null;
        }
    }

    private bool IsExcludedScene(string sceneName)
    {
        if (_excludeSceneNames == null) return false;

        for (int i = 0; i < _excludeSceneNames.Length; i++)
        {
            string n = _excludeSceneNames[i];
            if (!string.IsNullOrEmpty(n) && n == sceneName)
                return true;
        }

        return false;
    }

    public static void SetVisible(bool visible)
    {
        if (_instance != null) { _instance.SetVisibleInternal(visible); return; }
        Cursor.visible = visible;
    }

    private void SetVisibleInternal(bool visible)
    {
        CursorLockMode lockMode = _enabledForThisScene ? _lockModeOnEnabledScenes : _lockModeOnExcludedScenes;
        ApplyCursorState(visible, lockMode);
    }

    private void ApplyCursorState(bool visible, CursorLockMode lockMode)
    {
        Cursor.lockState = lockMode;

        if (_useSoftwareCursor && _cursorIcon != null)
        {
            Cursor.visible = false;
            _cursorIcon.gameObject.SetActive(visible);
            SetTrailActive(visible);
        }
        else
        {
            Cursor.visible = visible;
            if (_cursorIcon != null) _cursorIcon.gameObject.SetActive(false);
            SetTrailActive(false);
        }
    }

    private void UpdateCursorIcon()
    {
        if (_cursorIcon == null) return;
        if (!_cursorIcon.gameObject.activeSelf) return;

        Vector3 mouse = Input.mousePosition;
        _cursorIcon.position = mouse;

        if (!_useCursorTrail) return;
        EnsureTrail();
        if (_trailParts == null) return;

        float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float k = 1f - Mathf.Exp(-_trailSmooth * dt);

        Vector3 leader = mouse;
        for (int i = 0; i < _trailParts.Length; i++)
        {
            RectTransform part = _trailParts[i];
            if (part == null) continue;
            part.position = Vector3.Lerp(part.position, leader, k);
            leader = part.position;
        }
    }

    private void EnsureTrail()
    {
        if (_trailBuilt) return;
        if (!_useCursorTrail || _cursorIcon == null || _trailCount <= 0) return;

        _trailParts = new RectTransform[_trailCount];
        Transform parent = _cursorIcon.parent;

        for (int i = 0; i < _trailCount; i++)
        {
            GameObject clone = Instantiate(_cursorIcon.gameObject, parent);
            clone.name = "CursorTrail_" + i;

            RectTransform rt = clone.GetComponent<RectTransform>();
            rt.position = _cursorIcon.position;

            Graphic g = clone.GetComponent<Graphic>();
            if (g != null)
            {
                g.raycastTarget = false;
                float t = _trailCount > 1 ? (float)i / (_trailCount - 1) : 0f;
                Color c = g.color;
                c.a = Mathf.Lerp(_trailStartAlpha, _trailEndAlpha, t);
                g.color = c;
            }

            _trailParts[i] = rt;
        }

        _cursorIcon.SetAsLastSibling();
        _trailBuilt = true;
    }

    private void SetTrailActive(bool on)
    {
        if (on) EnsureTrail();
        if (_trailParts == null) return;

        Vector3 mouse = Input.mousePosition;
        for (int i = 0; i < _trailParts.Length; i++)
        {
            RectTransform part = _trailParts[i];
            if (part == null) continue;
            if (on) part.position = mouse;
            if (part.gameObject.activeSelf != on) part.gameObject.SetActive(on);
        }
    }

    private void EnsureParticle()
    {
        if (_cursorParticlePrefab == null) return;
        if (_particleInstance != null) return;

        _particleInstance = Instantiate(_cursorParticlePrefab, transform);
        _particleTf = _particleInstance.transform;

        _particleTf.position = Vector3.zero;
        _particleTf.rotation = Quaternion.identity;
    }

    private void SetParticleActive(bool on)  
    {
        if (_particleInstance == null) return;

        if (on)
        {
            if (!_particleInstance.gameObject.activeSelf)
                _particleInstance.gameObject.SetActive(true);

            if (!_particleInstance.isPlaying)
                _particleInstance.Play(true);
        }
        else
        {
            if (_particleInstance.isPlaying)
                _particleInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (_particleInstance.gameObject.activeSelf)
                _particleInstance.gameObject.SetActive(false);
        }
    }
}
