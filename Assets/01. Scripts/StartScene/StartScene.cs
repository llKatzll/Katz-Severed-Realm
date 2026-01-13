using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class StartScene : MonoBehaviour
{
    [Header("Title/Particles")]
    [SerializeField] private ParticleSystem _titleParticles;
    [SerializeField] private ParticleSystem _startParticles;

    [Header("Press Any Key TMP")]
    [SerializeField] private TMP_Text _pressAnyKeyText;

    [Header("Blink")]
    [SerializeField] private float _blinkPeriod = 1.2f;
    [SerializeField] private float _blinkMinAlpha = 0.25f;
    [SerializeField] private bool _blinkUseUnscaledTime = true;

    [Header("Float")]
    [SerializeField] private float _bounceAmplitude = 10f;
    [SerializeField] private float _bouncePeriod = 2.0f;
    [SerializeField] private bool _floatUseUnscaledTime = true;

    [Header("Flow")]
    [SerializeField] private bool _playTitleParticlesOnEnable = true;

    private RectTransform _pressRect;
    private Vector2 _pressBaseAnchoredPos;
    private Coroutine _pressFxCo;

    private void Awake()
    {
        if (_pressAnyKeyText != null)
        {
            _pressRect = _pressAnyKeyText.rectTransform;
            _pressBaseAnchoredPos = _pressRect.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        if (_playTitleParticlesOnEnable && _titleParticles != null)
        {
            _titleParticles.Play(true);
        }

        StartPressFx();
    }

    private void OnDisable()
    {
        StopPressFx();
        ResetPressVisual();
    }

    public void StartParticlesPlay()
    {
        if (_startParticles == null) return;
        _startParticles.Play(true);
    }

    private void StartPressFx()
    {
        if (_pressAnyKeyText == null) return;

        StopPressFx();
        _pressFxCo = StartCoroutine(CoPressFx());
    }

    private void StopPressFx()
    {
        if (_pressFxCo != null)
        {
            StopCoroutine(_pressFxCo);
            _pressFxCo = null;
        }
    }

    private void ResetPressVisual()
    {
        if (_pressAnyKeyText != null)
        {
            var c = _pressAnyKeyText.color;
            c.a = 1f;
            _pressAnyKeyText.color = c;
        }

        if (_pressRect != null)
        {
            _pressRect.anchoredPosition = _pressBaseAnchoredPos;
        }
    }

    private IEnumerator CoPressFx()
    {
        float tBlink = 0f;
        float tFloat = 0f;

        while (true)
        {
            float dtBlink = _blinkUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float dtFloat = _floatUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            tBlink += dtBlink;
            tFloat += dtFloat;

            if (_blinkPeriod <= 0.0001f) _blinkPeriod = 0.0001f;
            if (_bouncePeriod <= 0.0001f) _bouncePeriod = 0.0001f;

            float blinkPhase = (tBlink / _blinkPeriod) * 6.2831853f;
            float blink01 = 0.5f + 0.5f * Mathf.Sin(blinkPhase);
            float alpha = Mathf.Lerp(_blinkMinAlpha, 1f, blink01);

            var col = _pressAnyKeyText.color;
            col.a = alpha;
            _pressAnyKeyText.color = col;

            if (_pressRect != null)
            {
                float floatPhase = (tFloat / _bouncePeriod) * 6.2831853f;
                float y = Mathf.Sin(floatPhase) * _bounceAmplitude;
                _pressRect.anchoredPosition = _pressBaseAnchoredPos + new Vector2(0f, y);
            }

            yield return null;
        }
    }
}
