using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuScene : MonoBehaviour
{
    [Header("BlackScreen")]
    [SerializeField] private RawImage _blackScreen;

    [Header("Fade Settings")]
    [SerializeField] private float _fadeOutTime = 1.5f;
    [SerializeField] private bool _useUnscaledTime = true;

    private Coroutine _fadeCo;

    private void OnEnable()
    {
        SetAlpha(1f);
        StartFadeOut();
    }

    private void OnDisable()
    {
        if (_fadeCo != null)
        {
            StopCoroutine(_fadeCo);
            _fadeCo = null;
        }
    }

    private void StartFadeOut()
    {
        if (_blackScreen == null) return;

        if (_fadeCo != null)
            StopCoroutine(_fadeCo);

        _fadeCo = StartCoroutine(CoFadeOut());
    }

    private IEnumerator CoFadeOut()
    {
        float t = 0f;

        while (t < _fadeOutTime)
        {
            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            float k = Mathf.Clamp01(t / _fadeOutTime);
            SetAlpha(1f - k);

            yield return null;
        }

        SetAlpha(0f);

        if (_blackScreen != null)
            _blackScreen.gameObject.SetActive(false);
    }

    private void SetAlpha(float a)
    {
        if (_blackScreen == null) return;

        Color c = _blackScreen.color;
        c.a = Mathf.Clamp01(a);
        _blackScreen.color = c;
    }
}