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

    public void Populate()
    {
        SongData song = GameManager.I != null ? GameManager.I.SelectedSong : null;

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

        string rank = RankUtility.GetRank(score, totalNotes);

        if (_rankText != null)
        {
            _rankText.text = rank;
            _rankText.color = RankUtility.GetRankColor(rank);
        }

        if (_scoreText != null)
            _scoreText.text = score.ToString("00,000,000");

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
        if (InGameManager.I != null)
            InGameManager.I.OnExitButtonClicked();
    }
}
