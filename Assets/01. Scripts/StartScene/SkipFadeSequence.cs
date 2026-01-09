using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SkipFadeSequence : MonoBehaviour
{
    private enum Phase { None, FadeIn, Hold, FadeOut, Done }

    [Header("Targets")]
    [SerializeField] private RawImage _blackScreen;

    [Header("Timing")]
    [SerializeField] private float _fadeInDuration = 1.0f;
    [SerializeField] private float _holdDuration = 1.0f;
    [SerializeField] private float _fadeOutDuration = 1.0f;

    [Header("Easing")]
    [SerializeField] private bool _easeFadeIn = true;
    [SerializeField] private bool _easeFadeOut = false;

    [Header("Skip")]
    [SerializeField] private bool _allowSkip = true;
    [SerializeField] private bool _skipOnlyDuringHoldAndFadeOut = true;

    [Header("Options")]
    [SerializeField] private bool _playOnStart = true;
    [SerializeField] private bool _useUnscaledTime = true;

    private Coroutine _co;
    private Phase _phase = Phase.None;
    private bool _skipRequested;

    private void Awake()
    {
        SetBlackAlpha(1f);
    }

    private void OnEnable()
    {
        SetBlackAlpha(1f);
    }

    private void Start()
    {
        if (_playOnStart) Play();
    }

    private void Update()
    {
        if (!_allowSkip) return;
        if (_phase == Phase.None || _phase == Phase.Done) return;

        bool canSkipNow = _skipOnlyDuringHoldAndFadeOut
            ? (_phase == Phase.Hold || _phase == Phase.FadeOut)
            : (_phase != Phase.Done && _phase != Phase.None);

        if (canSkipNow && Input.anyKeyDown)
            _skipRequested = true;
    }

    public void Play()
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(CoSequence());
    }

    public void ForceSkip()
    {
        _skipRequested = true;
    }

    private IEnumerator CoSequence()
    {
        _skipRequested = false;

        _phase = Phase.FadeIn;
        SetBlackAlpha(1f);

        yield return FadeBlack(1f, 0f, _fadeInDuration, allowSkipInsideFade: false, ease: _easeFadeIn);

        _phase = Phase.Hold;
        yield return WaitSkippable(_holdDuration);

        _phase = Phase.FadeOut;
        yield return FadeBlack(0f, 1f, _fadeOutDuration, allowSkipInsideFade: true, ease: _easeFadeOut);

        _phase = Phase.Done;
        SetBlackAlpha(1f);
    }

    private IEnumerator WaitSkippable(float sec)
    {
        if (sec <= 0f) yield break;

        float t = 0f;
        while (t < sec)
        {
            if (_skipRequested) yield break;

            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;
            yield return null;
        }
    }

    private IEnumerator FadeBlack(float fromAlpha, float toAlpha, float duration, bool allowSkipInsideFade, bool ease)
    {
        if (duration <= 0f)
        {
            SetBlackAlpha(toAlpha);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            if (allowSkipInsideFade && _skipRequested)
                break;

            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            float u = Mathf.Clamp01(t / duration);
            if (ease) u = u * u * (3f - 2f * u);

            float a = Mathf.Lerp(fromAlpha, toAlpha, u);
            SetBlackAlpha(a);

            yield return null;
        }

        SetBlackAlpha(toAlpha);
    }

    private void SetBlackAlpha(float a01)
    {
        if (_blackScreen == null) return;

        Color c = _blackScreen.color;
        c.a = Mathf.Clamp01(a01);
        _blackScreen.color = c;
    }
}
