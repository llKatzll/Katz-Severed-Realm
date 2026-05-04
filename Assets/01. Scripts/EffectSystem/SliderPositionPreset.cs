using UnityEngine;

public class SliderPositionPreset : MonoBehaviour
{
    [SerializeField] private RectTransform _slider;
    [SerializeField] private float _chartModeX = -700f;
    [SerializeField] private float _effectModeX = 370f;

    public void ApplyChartMode()
    {
        if (_slider == null) return;
        var p = _slider.anchoredPosition;
        p.x = _chartModeX;
        _slider.anchoredPosition = p;
    }

    public void ApplyEffectMode()
    {
        if (_slider == null) return;
        var p = _slider.anchoredPosition;
        p.x = _effectModeX;
        _slider.anchoredPosition = p;
    }
}
