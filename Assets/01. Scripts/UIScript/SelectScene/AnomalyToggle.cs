using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnomalyToggle : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _onButton;
    [SerializeField] private Button _offButton;

    [Header("Images")]
    [SerializeField] private Image _onButtonImage;
    [SerializeField] private Image _offButtonImage;

    [Header("Texts")]
    [SerializeField] private TMP_Text _onButtonText;
    [SerializeField] private TMP_Text _offButtonText;

    [Header("Colors")]
    [SerializeField] private Color _activeBgColor = Color.white;
    [SerializeField] private Color _activeTextColor = Color.black;
    [SerializeField] private Color _inactiveBgColor = Color.black;
    [SerializeField] private Color _inactiveTextColor = Color.white;

    [Header("Flash")]
    [SerializeField] private float _flashDuration = 0.12f;
    [SerializeField] private float _returnDuration = 0.15f;

    private bool _isAnomalyOn = true;
    private Coroutine _flashCo;

    public event System.Action<bool> OnChanged;

    private void Start()
    {
        if (_onButton != null)
            _onButton.onClick.AddListener(OnClickOn);
        if (_offButton != null)
            _offButton.onClick.AddListener(OnClickOff);

        ApplyState(false);
    }

    private void OnClickOn()
    {
        if (_isAnomalyOn) return;
        _isAnomalyOn = true;
        if (SfxManager.I != null) SfxManager.I.PlayAnomaly();
        PlayFlash();
        OnChanged?.Invoke(_isAnomalyOn);
    }

    private void OnClickOff()
    {
        if (!_isAnomalyOn) return;
        _isAnomalyOn = false;
        if (SfxManager.I != null) SfxManager.I.PlayAnomaly();
        PlayFlash();
        OnChanged?.Invoke(_isAnomalyOn);
    }

    private void PlayFlash()
    {
        if (_flashCo != null)
            StopCoroutine(_flashCo);
        _flashCo = StartCoroutine(CoFlash());
    }

    private IEnumerator CoFlash()
    {
        Color onBgFrom = _onButtonImage != null ? _onButtonImage.color : Color.black;
        Color onTxtFrom = _onButtonText != null ? _onButtonText.color : Color.white;
        Color offBgFrom = _offButtonImage != null ? _offButtonImage.color : Color.black;
        Color offTxtFrom = _offButtonText != null ? _offButtonText.color : Color.white;

        Color onBgMid = _activeBgColor;
        Color onTxtMid = _activeTextColor;
        Color offBgMid = _activeBgColor;
        Color offTxtMid = _activeTextColor;

        float t = 0f;
        while (t < _flashDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / _flashDuration);
            SetButtonColors(_onButtonImage, _onButtonText,
                Color.Lerp(onBgFrom, onBgMid, k), Color.Lerp(onTxtFrom, onTxtMid, k));
            SetButtonColors(_offButtonImage, _offButtonText,
                Color.Lerp(offBgFrom, offBgMid, k), Color.Lerp(offTxtFrom, offTxtMid, k));
            yield return null;
        }

        Color onBgTarget = _isAnomalyOn ? _activeBgColor : _inactiveBgColor;
        Color onTxtTarget = _isAnomalyOn ? _activeTextColor : _inactiveTextColor;
        Color offBgTarget = _isAnomalyOn ? _inactiveBgColor : _activeBgColor;
        Color offTxtTarget = _isAnomalyOn ? _inactiveTextColor : _activeTextColor;

        t = 0f;
        while (t < _returnDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / _returnDuration);
            SetButtonColors(_onButtonImage, _onButtonText,
                Color.Lerp(onBgMid, onBgTarget, k), Color.Lerp(onTxtMid, onTxtTarget, k));
            SetButtonColors(_offButtonImage, _offButtonText,
                Color.Lerp(offBgMid, offBgTarget, k), Color.Lerp(offTxtMid, offTxtTarget, k));
            yield return null;
        }

        ApplyState(false);
    }

    private void ApplyState(bool instant)
    {
        Color onBg = _isAnomalyOn ? _activeBgColor : _inactiveBgColor;
        Color onTxt = _isAnomalyOn ? _activeTextColor : _inactiveTextColor;
        Color offBg = _isAnomalyOn ? _inactiveBgColor : _activeBgColor;
        Color offTxt = _isAnomalyOn ? _inactiveTextColor : _activeTextColor;

        SetButtonColors(_onButtonImage, _onButtonText, onBg, onTxt);
        SetButtonColors(_offButtonImage, _offButtonText, offBg, offTxt);
    }

    private void SetButtonColors(Image img, TMP_Text txt, Color bg, Color textColor)
    {
        if (img != null) img.color = bg;
        if (txt != null) txt.color = textColor;
    }

    public bool IsAnomalyOn() => _isAnomalyOn;
}
