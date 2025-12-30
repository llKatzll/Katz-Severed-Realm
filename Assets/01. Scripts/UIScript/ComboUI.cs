using System.Collections;
using TMPro;
using UnityEngine;

public class ComboUI : MonoBehaviour
{
    public static ComboUI I { get; private set; }

    [Header("Texts")]
    [SerializeField] private TMP_Text _comboText;
    [SerializeField] private TMP_Text _judgeText;

    [Header("Hold Body Tick")]
    [SerializeField] private float _holdTickMul = 2f;
    [SerializeField] private float _minTickSec = 0.02f;

    [Header("Display Observe")]
    [SerializeField] private bool _useSmoothDisplay = true;
    [SerializeField] private float _displayCountPerSec = 60f; // 화면에서 초당 몇 콤보로 올라가 보이게 할지

    [Header("Debug")]
    [SerializeField] private bool _logHoldTick = false;

    private int _combo;

    private int _displayCombo;
    private float _displayComboF;

    private Coroutine _holdTickCo;
    private bool _holdTickRunning;
    private KeyCode _holdKey;

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
            StopHoldBodyTick();
            return;
        }

        AddCombo(1);
        StartHoldBodyTick(bpm, laneKey);
    }

    public void OnHoldEnd(string judgeLabel, bool breaksCombo)
    {
        SetJudgeText(judgeLabel);

        StopHoldBodyTick();

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
        StopHoldBodyTick();
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
            _comboText.text = shown.ToString();
    }

    private void StartHoldBodyTick(float bpm, KeyCode laneKey)
    {
        StopHoldBodyTick();

        _holdKey = laneKey;

        float tickSec = CalcHoldTickSec(bpm);

        if (_logHoldTick)
            Debug.Log("HoldTick Start bpm=" + bpm.ToString("F2") + " tickSec=" + tickSec.ToString("F4"));

        _holdTickRunning = true;
        _holdTickCo = StartCoroutine(HoldBodyTickCo(tickSec));
    }

    private void StopHoldBodyTick()
    {
        _holdTickRunning = false;

        if (_holdTickCo != null)
        {
            StopCoroutine(_holdTickCo);
            _holdTickCo = null;

            if (_logHoldTick)
                Debug.Log("HoldTick Stop");
        }
    }

    private float CalcHoldTickSec(float bpm)
    {
        float safeBpm = Mathf.Clamp(bpm, 1f, 999f); // bpm 이상치 방어(관측용)
        float ticksPerMin = safeBpm * Mathf.Max(0.01f, _holdTickMul);
        float sec = 60f / ticksPerMin;
        return Mathf.Max(_minTickSec, sec);
    }

    private IEnumerator HoldBodyTickCo(float tickSec)
    {
        _holdTickRunning = true;

        while (_holdTickRunning)
        {
            if (!Input.GetKey(_holdKey))
                yield break;

            AddCombo(1);

            if (_logHoldTick)
                Debug.Log("HoldTick +1 combo=" + _combo);

            yield return new WaitForSeconds(tickSec);
        }
    }
}
