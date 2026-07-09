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

    private double _score;
    private int _combo;
    private int _maxCombo;

    private int _judgedCount;
    private double _accuracySum;

    public int Score => (int)Math.Round(_score);
    public int MaxCombo => _maxCombo;
    public int TotalNoteCount => _totalNoteCount;
    public bool UsedPause { get; private set; }

    public void MarkPaused() => UsedPause = true;
    public void ClearPaused() => UsedPause = false;

    public float Accuracy => _judgedCount <= 0 ? 0f : (float)(_accuracySum / _judgedCount) * 100f;

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

    public void SetTotalNoteCount(int count) => _totalNoteCount = Mathf.Max(0, count);

    public void Reset()
    {
        _score = 0.0;
        _combo = 0;
        _maxCombo = 0;
        _judgedCount = 0;
        _accuracySum = 0.0;
        UsedPause = false;
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
        double weight = GetAccuracyWeight(judge);
        double sevBonus = (judge == JudgeType.Severance) ? 1.0 : 0.0;

        return basePerNote * weight + sevBonus;
    }

    private double GetAccuracyWeight(JudgeType judge) => judge switch
    {
        JudgeType.Severance => 1.0,
        JudgeType.Clean     => 1.0,
        JudgeType.Trace     => 200.0 / 300.0,
        JudgeType.Fracture  => 100.0 / 300.0,
        JudgeType.Ruin      => 50.0 / 300.0,
        _                   => 0.0,
    };

    private void RefreshUI()
    {
        if (_scoreText != null)
            _scoreText.text = Score.ToString("00,000,000");

        if (_accuracyText != null)
            _accuracyText.text = Accuracy.ToString("F2") + "%";
    }
}
