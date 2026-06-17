using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BgmManager : MonoBehaviour
{
    public static BgmManager I { get; private set; }

    [SerializeField] private AudioSource _source;
    [SerializeField] private AudioClip _menuBgm;

    [SerializeField] private string[] _playScenes = { "StartScene", "MainMenu" };
    [SerializeField] private string _manualPlayScene = "StartScene";

    [SerializeField] private float _maxVolume = 1f;
    [SerializeField] private float _fadeInTime = 1f;
    [SerializeField] private float _fadeOutTime = 1f;

    private const string MusicGroupName = "Music";

    private Coroutine _fadeCo;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);

        if (_source == null) _source = GetComponent<AudioSource>();
        if (_source != null)
        {
            _source.playOnAwake = false;
            _source.loop = true;
            _source.clip = _menuBgm;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (I == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        EnsureOutput();
        HandleScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HandleScene(scene.name);
    }

    private void HandleScene(string sceneName)
    {
        if (!IsPlayScene(sceneName))
        {
            StopIfPlaying();
            return;
        }
        if (sceneName == _manualPlayScene) return;
        PlayIfStopped();
    }

    private bool IsPlayScene(string sceneName)
    {
        if (_playScenes == null) return false;
        for (int i = 0; i < _playScenes.Length; i++)
        {
            if (_playScenes[i] == sceneName) return true;
        }
        return false;
    }

    public void PlayMenuBgm()
    {
        PlayIfStopped();
    }

    private void PlayIfStopped()
    {
        if (_source == null || _menuBgm == null) return;

        EnsureOutput();

        if (_source.isPlaying)
        {
            if (_fadeCo != null) StartFade(_maxVolume, _fadeInTime, false);
            return;
        }

        _source.clip = _menuBgm;
        _source.volume = 0f;
        _source.Play();
        StartFade(_maxVolume, _fadeInTime, false);
    }

    private void StopIfPlaying()
    {
        if (_source == null) return;
        if (!_source.isPlaying) return;

        StartFade(0f, _fadeOutTime, true);
    }

    private void StartFade(float target, float duration, bool stopAtEnd)
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CoFade(target, duration, stopAtEnd));
    }

    private IEnumerator CoFade(float target, float duration, bool stopAtEnd)
    {
        float start = _source.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
            _source.volume = Mathf.Lerp(start, target, k);
            yield return null;
        }

        _source.volume = target;
        if (stopAtEnd) _source.Stop();
        _fadeCo = null;
    }

    private void EnsureOutput()
    {
        if (_source == null) return;
        if (_source.outputAudioMixerGroup != null) return;

        var group = AudioMixerBinder.GetGroup(MusicGroupName);
        if (group != null) _source.outputAudioMixerGroup = group;
    }
}
