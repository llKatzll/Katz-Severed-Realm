using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultUI : MonoBehaviour
{
    [Header("Song Info")]
    [SerializeField] private TMP_Text _songNameText;
    [SerializeField] private TMP_Text _artistNameText;

    [Header("Score")]
    [SerializeField] private TMP_Text _rankText;
    [SerializeField] private TMP_Text _scoreText;

    [Header("Combo")]
    [SerializeField] private TMP_Text _maxComboNumText;

    [Header("Accuracy")]
    [SerializeField] private TMP_Text _accNumText;

    [Header("Exit")]
    [SerializeField] private Button _exitButton;

    [Header("Paused Mark")]
    [SerializeField] private GameObject _pausedMark;

    [Header("Anomaly Score Color")]
    [SerializeField] private Color _normalScoreColor = Color.white;
    [SerializeField] private Color _noAnomalyScoreColor = new Color(0.6f, 0.8f, 1f);

    public void Populate()
    {
        SongData song = GameManager.I != null ? GameManager.I.SelectedSong : null;
        DifficultyType diff = GameManager.I != null ? GameManager.I.SelectedDifficulty : DifficultyType.Easy;

        if (_songNameText != null)
            _songNameText.text = song != null ? song.songName : "Unknown";

        if (_artistNameText != null)
            _artistNameText.text = song != null ? song.artist : "Unknown";

        int score = 0;
        int maxCombo = 0;
        float accuracy = 0f;
        int totalNotes = 0;

        if (ScoreManager.I != null)
        {
            score = ScoreManager.I.Score;
            maxCombo = ScoreManager.I.MaxCombo;
            accuracy = ScoreManager.I.Accuracy;
            totalNotes = ScoreManager.I.TotalNoteCount;
        }

        bool usedPause = ScoreManager.I != null && ScoreManager.I.UsedPause;
        bool anomaly = GameManager.I != null && GameManager.I.AnomalyEnabled;

        if (usedPause)
            score = 0;

        string rank = usedPause ? "L" : RankUtility.GetRank(score, totalNotes);

        if (song != null && !usedPause)
        {
            ScoreRecord.SaveIfBetter(song.songName, diff, anomaly, score, accuracy, totalNotes);
        }

        if (_pausedMark != null)
            _pausedMark.SetActive(usedPause);

        if (_rankText != null)
        {
            _rankText.text = rank;
            _rankText.color = RankUtility.GetRankColor(rank);
        }

        if (_scoreText != null)
        {
            _scoreText.text = score.ToString("00,000,000");
            _scoreText.color = anomaly ? _normalScoreColor : _noAnomalyScoreColor;
        }

        if (_maxComboNumText != null)
            _maxComboNumText.text = maxCombo.ToString();

        if (_accNumText != null)
            _accNumText.text = accuracy.ToString("F2") + "%";

        if (_exitButton != null)
        {
            _exitButton.onClick.RemoveAllListeners();
            _exitButton.onClick.AddListener(OnExit);
        }
    }

    private void OnExit()
    {
        if (SfxManager.I != null) SfxManager.I.PlayReturn();

        if (InGameManager.I != null)
            InGameManager.I.OnExitButtonClicked();
    }
}
