using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class StartButtonEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button _button;
    [SerializeField] private Image _buttonImage;
    [SerializeField] private TMP_Text _buttonText;

    [Header("Colors")]
    [SerializeField] private Color _normalBgColor = Color.black;
    [SerializeField] private Color _normalTextColor = Color.white;
    [SerializeField] private Color _flashBgColor = Color.white;
    [SerializeField] private Color _flashTextColor = Color.black;

    [Header("Timing")]
    [SerializeField] private float _flashDuration = 0.15f;
    [SerializeField] private float _returnDuration = 0.2f;
    [SerializeField] private float _delayBeforeLoad = 0.5f;

    [Header("Transition")]
    [SerializeField] private RectTransform _transitionRect;
    [SerializeField] private float _transitionSlideTime = 1f;
    [SerializeField] private float _transitionWaitY = 1100f;
    [SerializeField] private float _transitionShowY = 0f;

    [Header("Scene")]
    [SerializeField] private string _inGameSceneName = "InGame";

    private bool _isTriggered;

    private void Start()
    {
        if (_button != null)
            _button.onClick.AddListener(OnStartClicked);

        ApplyColors(_normalBgColor, _normalTextColor);

        if (_transitionRect != null)
        {
            _transitionRect.gameObject.SetActive(true);
            StartCoroutine(SlideTransition(_transitionShowY, -1100f, _transitionSlideTime, true));
        }
    }

    private void OnStartClicked()
    {
        if (_isTriggered) return;

        if (SongSelectManager.I == null || SongSelectManager.I.GetCurrentSong() == null)
        {
            Debug.LogWarning("[StartButton] No song selected!");
            return;
        }

        _isTriggered = true;
        StartCoroutine(CoFlashAndLoad());
    }

    private IEnumerator CoFlashAndLoad()
    {
        float t = 0f;
        while (t < _flashDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / _flashDuration);
            LerpColors(k, _normalBgColor, _flashBgColor, _normalTextColor, _flashTextColor);
            yield return null;
        }
        ApplyColors(_flashBgColor, _flashTextColor);

        t = 0f;
        while (t < _returnDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / _returnDuration);
            LerpColors(k, _flashBgColor, _normalBgColor, _flashTextColor, _normalTextColor);
            yield return null;
        }
        ApplyColors(_normalBgColor, _normalTextColor);

        if (GameManager.I != null)
        {
            GameManager.I.SetSong(
                SongSelectManager.I.GetCurrentSong(),
                SongSelectManager.I.GetCurrentDifficulty()
            );
        }

        if (_transitionRect != null)
        {
            SetTransitionY(_transitionWaitY);
            _transitionRect.gameObject.SetActive(true);
            yield return StartCoroutine(SlideTransition(_transitionWaitY, _transitionShowY, _transitionSlideTime, false));
        }

        yield return new WaitForSeconds(_delayBeforeLoad);

        SceneManager.LoadScene(_inGameSceneName);
    }

    private void LerpColors(float k, Color fromBg, Color toBg, Color fromText, Color toText)
    {
        if (_buttonImage != null)
            _buttonImage.color = Color.Lerp(fromBg, toBg, k);
        if (_buttonText != null)
            _buttonText.color = Color.Lerp(fromText, toText, k);
    }

    private void ApplyColors(Color bg, Color text)
    {
        if (_buttonImage != null)
            _buttonImage.color = bg;
        if (_buttonText != null)
            _buttonText.color = text;
    }

    private IEnumerator SlideTransition(float fromY, float toY, float duration, bool deactivateOnDone)
    {
        if (_transitionRect == null) yield break;

        float t = 0f;
        Vector2 pos = _transitionRect.anchoredPosition;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            pos.y = Mathf.Lerp(fromY, toY, k);
            _transitionRect.anchoredPosition = pos;
            yield return null;
        }

        pos.y = toY;
        _transitionRect.anchoredPosition = pos;

        if (deactivateOnDone)
            _transitionRect.gameObject.SetActive(false);
    }

    private void SetTransitionY(float y)
    {
        if (_transitionRect == null) return;
        Vector2 pos = _transitionRect.anchoredPosition;
        pos.y = y;
        _transitionRect.anchoredPosition = pos;
    }
}
