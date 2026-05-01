using System;
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

    [Header("Score Share")]
    [SerializeField, Range(0f, 1f)] private float _accuracyShare = 0.7f;
    [SerializeField, Range(0f, 1f)] private float _comboShare = 0.3f;

    private double _score;
    private int _combo;
    private int _maxCombo;

    private int _judgedCount;
    private double _accuracySum;

    public int Score => (int)Math.Round(_score);
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

    public string Rank => RankUtility.GetRank(Score, _totalNoteCount);

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
        _score = 0.0;
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

        double noteScore = GetNoteScore(judge);
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

    private double GetNoteScore(JudgeType judge)
    {
        if (_totalNoteCount <= 0) return 0.0;

        double basePerNote = 10_000_000.0 / _totalNoteCount;
        double judgmentWeight = GetJudgmentWeight(judge);
        bool comboSurvived = (judge != JudgeType.Ruin && judge != JudgeType.Miss);

        double accuracyScore = basePerNote * judgmentWeight * _accuracyShare;
        double comboScore = comboSurvived ? basePerNote * _comboShare : 0.0;
        double sevBonus = (judge == JudgeType.Severance) ? 1.0 : 0.0;

        return accuracyScore + comboScore + sevBonus;
    }

    private double GetJudgmentWeight(JudgeType judge)
    {
        switch (judge)
        {
            case JudgeType.Severance: return 1.0;
            case JudgeType.Clean:     return 1.0;
            case JudgeType.Trace:     return 0.8;
            case JudgeType.Fracture:  return 0.5;
            case JudgeType.Ruin:      return 0.2;
            default:                  return 0.0;
        }
    }

    private double GetAccuracyWeight(JudgeType judge)
    {
        switch (judge)
        {
            case JudgeType.Severance: return 1.0;
            case JudgeType.Clean:     return 1.0;
            case JudgeType.Trace:     return 200.0 / 300.0;
            case JudgeType.Fracture:  return 100.0 / 300.0;
            case JudgeType.Ruin:      return 50.0 / 300.0;
            default:                  return 0.0;
        }
    }

    private void RefreshUI()
    {
        if (_scoreText != null)
            _scoreText.text = Score.ToString("00,000,000");

        if (_accuracyText != null)
            _accuracyText.text = Accuracy.ToString("F2") + "%";
    }
}
