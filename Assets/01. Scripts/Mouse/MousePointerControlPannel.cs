using UnityEngine;
using UnityEngine.SceneManagement;

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

    private void ApplyCursorState(bool visible, CursorLockMode lockMode)
    {
        Cursor.visible = visible;
        Cursor.lockState = lockMode;
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
