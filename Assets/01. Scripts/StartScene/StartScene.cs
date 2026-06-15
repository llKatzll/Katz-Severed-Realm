using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartScene : MonoBehaviour
{
    [Header("Title/Particles")]
    [SerializeField] private ParticleSystem _titleParticles;
    [SerializeField] private ParticleSystem _startParticles;

    [Header("Press Any Key TMP")]
    [SerializeField] private TMP_Text _pressAnyKeyText;

    [Header("Time")]
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("FadeOut (Scene Start)")]
    [SerializeField] private RawImage _maskA;
    [SerializeField] private float _fadeOutTime = 1.5f;

    [Header("FadeIn (To MainMenu)")]
    [SerializeField] private float _fadeInTime = 1.0f;
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    [Header("Blink")]
    [SerializeField] private float _blinkPeriod = 1.2f;
    [SerializeField] private float _blinkMinAlpha = 0.25f;

    [Header("Float")]
    [SerializeField] private float _bounceAmplitude = 10f;
    [SerializeField] private float _bouncePeriod = 2.0f;

    [Header("AreYouSure Popup")]
    [SerializeField] private GameObject _areYouSureCanvas;
    [SerializeField] private RawImage _areYouSureBG;
    [SerializeField] private GameObject _yesButton;
    [SerializeField] private GameObject _noButton;
    [SerializeField] private TMP_Text _yesText;
    [SerializeField] private TMP_Text _noText;
    [SerializeField] private float _popupFadeTime = 0.5f;
    [SerializeField] private float _popupBGMaxAlpha = 0.98f;

    private RectTransform _pressRect;
    private Vector2 _pressBaseAnchoredPos;

    private bool _isTransitioning;
    private bool _canPressAnyKey;

    private Coroutine _pressFxCo;
    private Coroutine _fadeCo;
    private Coroutine _popupCo;

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
        _isTransitioning = false;
        _canPressAnyKey = false;

        SetMaskAlpha(1f);
        HidePopupImmediate();
        StartFadeOut();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _fadeCo = null;
        _pressFxCo = null;
        _popupCo = null;
    }

    private void Update()
    {
        if (!_canPressAnyKey) return;
        if (_isTransitioning) return;
        if (IsPopupActive()) return;
        if (ModalStack.Count > 0) return;

        if (IsValidStartInput())
        {
            StartTransitionToMainMenu();
        }
    }

    private bool IsValidStartInput()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;
        }

        return Input.anyKeyDown;
    }

    private void StartFadeOut()
    {
        if (_maskA == null) return;

        if (_fadeCo != null)
            StopCoroutine(_fadeCo);

        _fadeCo = StartCoroutine(CoFadeOut());
    }

    private IEnumerator CoFadeOut()
    {
        float startA = _maskA.color.a;
        float t = 0f;

        while (t < _fadeOutTime)
        {
            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            float k = Mathf.Clamp01(t / _fadeOutTime);
            SetMaskAlpha(Mathf.Lerp(startA, 0f, k));

            yield return null;
        }

        SetMaskAlpha(0f);
        _canPressAnyKey = true;
        StartPressFx();
    }

    private void StartTransitionToMainMenu()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        StopPressFx();

        if (_fadeCo != null)
            StopCoroutine(_fadeCo);

        _fadeCo = StartCoroutine(CoFadeInAndLoadScene());
    }

    private IEnumerator CoFadeInAndLoadScene()
    {
        float t = 0f;

        while (t < _fadeInTime)
        {
            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            float k = Mathf.Clamp01(t / _fadeInTime);
            SetMaskAlpha(k);

            yield return null;
        }

        SetMaskAlpha(1f);

        SceneManager.LoadScene(_mainMenuSceneName);
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

    private IEnumerator CoPressFx()
    {
        float tBlink = 0f;
        float tFloat = 0f;

        while (true)
        {
            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            tBlink += dt;
            tFloat += dt;

            if (_blinkPeriod <= 0.0001f) _blinkPeriod = 0.0001f;
            if (_bouncePeriod <= 0.0001f) _bouncePeriod = 0.0001f;

            float blinkPhase = (tBlink / _blinkPeriod) * Mathf.PI * 2f;
            float blink01 = 0.5f + 0.5f * Mathf.Sin(blinkPhase);
            float alpha = Mathf.Lerp(_blinkMinAlpha, 1f, blink01);

            var col = _pressAnyKeyText.color;
            col.a = alpha;
            _pressAnyKeyText.color = col;

            if (_pressRect != null)
            {
                float floatPhase = (tFloat / _bouncePeriod) * Mathf.PI * 2f;
                float y = Mathf.Sin(floatPhase) * _bounceAmplitude;
                _pressRect.anchoredPosition = _pressBaseAnchoredPos + new Vector2(0f, y);
            }

            yield return null;
        }
    }

    private void SetMaskAlpha(float a01)
    {
        if (_maskA == null) return;

        Color c = _maskA.color;
        c.a = Mathf.Clamp01(a01);
        _maskA.color = c;
    }

    public void OnQuitGameClicked()
    {
        if (_isTransitioning) return;
        ShowPopup();
    }

    public void OnYesClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnNoClicked()
    {
        HidePopup();
    }

    private bool IsPopupActive()
    {
        return _areYouSureCanvas != null && _areYouSureCanvas.activeSelf;
    }

    private void ShowPopup()
    {
        if (_popupCo != null)
            StopCoroutine(_popupCo);

        _popupCo = StartCoroutine(CoShowPopup());
    }

    private void HidePopup()
    {
        if (_popupCo != null)
            StopCoroutine(_popupCo);

        _popupCo = StartCoroutine(CoHidePopup());
    }

    private void HidePopupImmediate()
    {
        if (_areYouSureCanvas != null)
            _areYouSureCanvas.SetActive(false);

        if (_areYouSureBG != null)
            SetImageAlpha(_areYouSureBG, 0f);

        if (_yesText != null)
            SetTextAlpha(_yesText, 0f);

        if (_noText != null)
            SetTextAlpha(_noText, 0f);

        if (_yesButton != null)
            _yesButton.SetActive(false);

        if (_noButton != null)
            _noButton.SetActive(false);
    }

    private IEnumerator CoShowPopup()
    {
        if (_areYouSureCanvas != null)
            _areYouSureCanvas.SetActive(true);

        if (_yesButton != null)
            _yesButton.SetActive(true);

        if (_noButton != null)
            _noButton.SetActive(true);

        float t = 0f;

        while (t < _popupFadeTime)
        {
            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            float k = Mathf.Clamp01(t / _popupFadeTime);

            if (_areYouSureBG != null)
                SetImageAlpha(_areYouSureBG, k * _popupBGMaxAlpha);

            if (_yesText != null)
                SetTextAlpha(_yesText, k);

            if (_noText != null)
                SetTextAlpha(_noText, k);

            yield return null;
        }

        if (_areYouSureBG != null)
            SetImageAlpha(_areYouSureBG, _popupBGMaxAlpha);

        if (_yesText != null)
            SetTextAlpha(_yesText, 1f);

        if (_noText != null)
            SetTextAlpha(_noText, 1f);
    }

    private IEnumerator CoHidePopup()
    {
        float t = 0f;

        while (t < _popupFadeTime)
        {
            float dt = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt;

            float k = 1f - Mathf.Clamp01(t / _popupFadeTime);

            if (_areYouSureBG != null)
                SetImageAlpha(_areYouSureBG, k * _popupBGMaxAlpha);

            if (_yesText != null)
                SetTextAlpha(_yesText, k);

            if (_noText != null)
                SetTextAlpha(_noText, k);

            yield return null;
        }

        HidePopupImmediate();
    }

    private void SetImageAlpha(RawImage img, float a)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = Mathf.Clamp01(a);
        img.color = c;
    }

    private void SetTextAlpha(TMP_Text txt, float a)
    {
        if (txt == null) return;
        Color c = txt.color;
        c.a = Mathf.Clamp01(a);
        txt.color = c;
    }
}