using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager I { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _accuracyText;

    [Header("Note Count")]
    [SerializeField] private int _totalNoteCount = 0;

    private int _score;
    private int _combo;
    private int _maxCombo;

    private int _judgedCount;
    private double _accuracySum;

    public int Score => _score;
    public int MaxCombo => _maxCombo;
    public int TotalNoteCount => _totalNoteCount;

    public float Accuracy
    {
        get
        {
            if (_judgedCount <= 0) return 0f;
            return (float)(_accuracySum / _judgedCount) * 100f;
        }
    }

    public string Rank => RankUtility.GetRank(_score, _totalNoteCount);

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        Reset();
    }

    public void SetTotalNoteCount(int count)
    {
        _totalNoteCount = Mathf.Max(0, count);
    }

    public void Reset()
    {
        _score = 0;
        _combo = 0;
        _maxCombo = 0;
        _judgedCount = 0;
        _accuracySum = 0.0;
        RefreshUI();
    }

    public void ReportJudge(JudgeType judge)
    {
        _judgedCount++;

        double weight = GetAccuracyWeight(judge);
        _accuracySum += weight;

        int noteScore = GetNoteScore(judge);
        _score += noteScore;

        if (judge == JudgeType.Ruin || judge == JudgeType.Miss)
        {
            _combo = 0;
        }
        else
        {
            _combo++;
            if (_combo > _maxCombo) _maxCombo = _combo;
        }

        RefreshUI();
    }

    public void ReportHoldTick()
    {
        _combo++;
        if (_combo > _maxCombo) _maxCombo = _combo;
    }

    private int GetNoteScore(JudgeType judge)
    {
        if (_totalNoteCount <= 0) return 0;

        int basePerNote = 10000000 / Mathf.Max(1, _totalNoteCount);

        switch (judge)
        {
            case JudgeType.Severance: return basePerNote + 1;
            case JudgeType.Clean:     return basePerNote;
            case JudgeType.Trace:     return (int)(basePerNote * 0.8f);
            case JudgeType.Fracture:  return (int)(basePerNote * 0.5f);
            case JudgeType.Ruin:      return (int)(basePerNote * 0.2f);
            default:                  return 0;
        }
    }

    private double GetAccuracyWeight(JudgeType judge)
    {
        switch (judge)
        {
            case JudgeType.Severance: return 1.0;
            case JudgeType.Clean:     return 0.95;
            case JudgeType.Trace:     return 0.8;
            case JudgeType.Fracture:  return 0.5;
            case JudgeType.Ruin:      return 0.2;
            default:                  return 0.0;
        }
    }

    private void RefreshUI()
    {
        if (_scoreText != null)
            _scoreText.text = _score.ToString("N0");

        if (_accuracyText != null)
            _accuracyText.text = Accuracy.ToString("F2") + "%";
    }
}
