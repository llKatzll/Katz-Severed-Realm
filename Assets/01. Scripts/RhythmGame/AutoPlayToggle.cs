using UnityEngine;
using TMPro;

public class AutoPlayToggle : MonoBehaviour
{
    [SerializeField] private KeyCode _toggleKey = KeyCode.F1;
    [SerializeField] private bool _startOn = false;
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private string _onText = "AUTO PLAY: ON";
    [SerializeField] private string _offText = "AUTO PLAY: OFF";
    [SerializeField] private Color _onColor = new Color(1f, 0.7f, 0.2f, 1f);
    [SerializeField] private Color _offColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);

    private void Awake()
    {
        AutoPlay.IsOn = _startOn;
        Refresh();
    }

    private void Update()
    {
        if (ModalStack.Count > 0) return;

        if (Input.GetKeyDown(_toggleKey))
        {
            AutoPlay.IsOn = !AutoPlay.IsOn;
            Refresh();
        }
    }

    private void Refresh()
    {
        if (_statusText == null) return;
        _statusText.text = AutoPlay.IsOn ? _onText : _offText;
        _statusText.color = AutoPlay.IsOn ? _onColor : _offColor;
    }
}
