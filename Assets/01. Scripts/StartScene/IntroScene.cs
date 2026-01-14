using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IntroScene : MonoBehaviour
{
    private enum Phase
    {
        None,
        StartDelay,
        Sign,
        Alert,
        Done
    }

    [Header("Root")]
    [SerializeField] private GameObject _canvasRoot;

    [Header("Mask A (fade by alpha)")]
    [SerializeField] private RawImage _maskA;

    [Header("Sign")]
    [SerializeField] private ParticleSystem _signParticles;
    [SerializeField] private Graphic _nameGraphic;

    [Header("Alert")]
    [SerializeField] private RawImage _alertImage;

    [Header("Time")]
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Start Delay")]
    [SerializeField] private float _startDelay = 1.0f;

    [Header("Sign Timing")]
    [SerializeField] private bool _signUseParticleTotalTime = true;
    [SerializeField] private float _signTotalTimeFallback = 2.5f;
    [SerializeField] private float _signFadeIn = 1.0f;
    [SerializeField] private float _signFadeOut = 0.8f;
    [SerializeField] private float _signMinSkipDelay = 1.0f;

    [Header("Alert Timing")]
    [SerializeField] private float _alertFadeIn = 1.5f;
    [SerializeField] private float _alertHold = 2.0f;
    [SerializeField] private float _alertFadeOut = 2.0f;

    [Header("Easing")]
    [SerializeField] private bool _alertEaseOut = true;

    [Header("Options")]
    [SerializeField] private bool _playOnStart = true;
    [SerializeField] private GameObject _nextSceneObject;

    private Coroutine _co;
    private bool _skipRequested;
    private Phase _phase = Phase.None;

    public bool IsDone { get; private set; }

    private void Awake()
    {
        ApplyInitialState();
    }

    private void OnEnable()
    {
        if (_playOnStart && Application.isPlaying)
            Play();
    }

    public void Play()
    {
        IsDone = false;
        _skipRequested = false;

        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoRun());
    }

    public void ForceSkip()
    {
        _skipRequested = true;
    }

    private void ApplyInitialState()
    {
        if (_canvasRoot != null) _canvasRoot.SetActive(true);

        if (_alertImage != null) _alertImage.gameObject.SetActive(false);
        if (_nameGraphic != null) _nameGraphic.gameObject.SetActive(true);

        SetMaskAlpha(1f);

        if (_signParticles != null)
        {
            _signParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _signParticles.gameObject.SetActive(true);
        }

        if (_nextSceneObject != null)
            _nextSceneObject.SetActive(false);
    }

    private IEnumerator CoRun()
    {
        _skipRequested = false;

        _phase = Phase.StartDelay;
        if (_startDelay > 0f)
            yield return WaitNoSkip(_startDelay);

        if (_canvasRoot != null) _canvasRoot.SetActive(true);

        _phase = Phase.Sign;

        if (_alertImage != null) _alertImage.gameObject.SetActive(false);
        if (_nameGraphic != null) _nameGraphic.gameObject.SetActive(true);

        SetMaskAlpha(1f);

        if (_signParticles != null)
        {
            _signParticles.gameObject.SetActive(true);
            _signParticles.Play(true);
        }

        float signTotal = GetSignTotalTime();
        float signFadeIn = Mathf.Max(0f, _signFadeIn);
        float signFadeOut = Mathf.Max(0f, _signFadeOut);
        float signHold = Mathf.Max(0f, signTotal - signFadeIn - signFadeOut);

        yield return FadeMask(1f, 0f, signFadeIn, allowSkipInsideFade: false, easeOut: false);

        yield return WaitSkippable(signHold, _signMinSkipDelay);

        yield return FadeMask(0f, 1f, signFadeOut, allowSkipInsideFade: true, easeOut: false);

        if (_nameGraphic != null) _nameGraphic.gameObject.SetActive(false);

        if (_signParticles != null)
        {
            _signParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _signParticles.gameObject.SetActive(false);
        }

        _skipRequested = false;

        _phase = Phase.Alert;

        if (_alertImage != null) _alertImage.gameObject.SetActive(true);
        SetMaskAlpha(1f);

        yield return FadeMask(1f, 0f, _alertFadeIn, allowSkipInsideFade: false, easeOut: _alertEaseOut);

        yield return WaitSkippable(_alertHold, 0f);

        yield return FadeMask(0f, 1f, _alertFadeOut, allowSkipInsideFade: true, easeOut: _alertEaseOut);

        if (_alertImage != null) _alertImage.gameObject.SetActive(false);

        if (_canvasRoot != null) _canvasRoot.SetActive(false);

        if (_nextSceneObject != null)
            _nextSceneObject.SetActive(true);

        _phase = Phase.Done;
        IsDone = true;
    }

    private float GetSignTotalTime()
    {
        if (!_signUseParticleTotalTime || _signParticles == null)
            return Mathf.Max(0.01f, _signTotalTimeFallback);

        var main = _signParticles.main;

        float dur = Mathf.Max(0f, main.duration);

        float lifeMax = 0f;
        var life = main.startLifetime;
        if (life.mode == ParticleSystemCurveMode.Constant)
        {
            lifeMax = life.constant;
        }
        else if (life.mode == ParticleSystemCurveMode.TwoConstants)
        {
            lifeMax = life.constantMax;
        }
        else
        {
            lifeMax = _signTotalTimeFallback;
        }

        float total = dur + Mathf.Max(0f, lifeMax);
        if (total <= 0.01f) total = Mathf.Max(0.01f, _signTotalTimeFallback);
        return total;
    }

    private IEnumerator WaitNoSkip(float sec)
    {
        if (sec <= 0f) yield break;

        float t = 0f;
        while (t < sec)
        {
            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;
            yield return null;
        }
    }

    private IEnumerator WaitSkippable(float sec, float minSkipDelay)
    {
        if (sec <= 0f) yield break;

        float t = 0f;
        float gate = Mathf.Max(0f, minSkipDelay);

        while (t < sec)
        {
            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            if (t >= gate)
            {
                if (Input.anyKeyDown)
                    _skipRequested = true;

                if (_skipRequested)
                    yield break;
            }

            yield return null;
        }
    }

    private IEnumerator FadeMask(float fromA, float toA, float duration, bool allowSkipInsideFade, bool easeOut)
    {
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f)
        {
            SetMaskAlpha(toA);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            if (allowSkipInsideFade)
            {
                if (Input.anyKeyDown)
                    _skipRequested = true;

                if (_skipRequested)
                    break;
            }

            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            float u = Mathf.Clamp01(t / duration);
            if (easeOut) u = SmoothStep01(u);

            float a = Mathf.Lerp(fromA, toA, u);
            SetMaskAlpha(a);

            yield return null;
        }

        SetMaskAlpha(toA);
    }

    private static float SmoothStep01(float u)
    {
        return u * u * (3f - 2f * u);
    }

    private void SetMaskAlpha(float a01)
    {
        if (_maskA == null) return;

        Color c = _maskA.color;
        c.a = Mathf.Clamp01(a01);
        _maskA.color = c;
    }
}