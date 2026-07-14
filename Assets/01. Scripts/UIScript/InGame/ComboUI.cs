using TMPro;
using UnityEngine;

public class ComboUI : MonoBehaviour
{
    public static ComboUI I { get; private set; }

    [Header("Texts")]
    [SerializeField] private TMP_Text _comboText;
    [SerializeField] private TMP_Text _judgeText;

    [Header("Display Observe")]
    [SerializeField] private bool _useSmoothDisplay = true;
    [SerializeField] private float _displayCountPerSec = 60f;

    private int _combo;

    private int _displayCombo;
    private float _displayComboF;

    private void Awake()
    {
        if (I != null && I != this)
        {
            bool replace =
                (I._comboText == null && _comboText != null) ||
                (I._judgeText == null && _judgeText != null);

            if (replace)
            {
                Destroy(I.gameObject);
                I = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            I = this;
        }

        SetCombo(0);
        SetJudgeText("");
    }

    private void Update()
    {
        if (!_useSmoothDisplay) return;

        float target = _combo;
        float spd = Mathf.Max(1f, _displayCountPerSec);

        _displayComboF = Mathf.MoveTowards(_displayComboF, target, spd * Time.deltaTime);

        int newDisp = Mathf.FloorToInt(_displayComboF + 0.0001f);
        if (newDisp != _displayCombo)
        {
            _displayCombo = newDisp;
            RefreshComboText();
        }
    }

    public void SetJudgeText(string text)
    {
        if (_judgeText == null) return;
        _judgeText.text = text ?? "";
    }

    public void OnTapResult(string judgeLabel, bool breaksCombo)
    {
        SetJudgeText(judgeLabel);

        if (breaksCombo)
        {
            BreakCombo();
            return;
        }

        AddCombo(1);
    }

    public void OnHoldStart(string judgeLabel, bool breaksCombo, float bpm, KeyCode laneKey)
    {
        SetJudgeText(judgeLabel);

        if (breaksCombo)
        {
            BreakCombo();
            return;
        }

        AddCombo(1);
    }

    public void OnHoldEnd(string judgeLabel, bool breaksCombo)
    {
        SetJudgeText(judgeLabel);

        if (breaksCombo)
        {
            BreakCombo();
            return;
        }

        AddCombo(1);
    }

    public void OnHoldFail(string judgeLabel)
    {
        SetJudgeText(judgeLabel);
        BreakCombo();
    }

    private void AddCombo(int add)
    {
        if (add <= 0) return;
        _combo += add;

        if (!_useSmoothDisplay)
            RefreshComboText();
    }

    private void BreakCombo()
    {
        _combo = 0;

        if (_useSmoothDisplay)
        {
            _displayComboF = 0f;
            _displayCombo = 0;
        }

        RefreshComboText();
    }

    private void SetCombo(int value)
    {
        _combo = Mathf.Max(0, value);

        if (_useSmoothDisplay)
        {
            _displayComboF = _combo;
            _displayCombo = _combo;
        }

        RefreshComboText();
    }

    private void RefreshComboText()
    {
        if (_comboText == null) return;

        int shown = _useSmoothDisplay ? _displayCombo : _combo;

        if (shown <= 0)
            _comboText.text = "";
        else
            _comboText.SetText("{0}", shown);
    }
}
