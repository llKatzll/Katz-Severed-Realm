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

    [Header("Scene")]
    [SerializeField] private string _inGameSceneName = "InGame";

    private bool _isTriggered;

    private void Start()
    {
        if (_button != null)
            _button.onClick.AddListener(OnStartClicked);

        ApplyColors(_normalBgColor, _normalTextColor);
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
}
