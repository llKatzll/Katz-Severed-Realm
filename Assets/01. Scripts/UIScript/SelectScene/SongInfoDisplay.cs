using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SongInfoDisplay : MonoBehaviour
{
    [Header("Song Info")]
    [SerializeField] private TMP_Text _songNameText;
    [SerializeField] private TMP_Text _artistText;
    [SerializeField] private TMP_Text _bpmText;
    [SerializeField] private TMP_Text _tempoShiftText;
    [SerializeField] private TMP_Text _durationText;
    [SerializeField] private TMP_Text _charterText;
    [SerializeField] private TMP_Text _mapperText;
    [SerializeField] private Image _songImage;

    [Header("Difficulty Info")]
    [SerializeField] private TMP_Text _difficultyLevelText;
    [SerializeField] private TMP_Text _difficultyConstantText;
    [SerializeField] private TMP_Text _difficultyNameText;

    [Header("Record Info")]
    [SerializeField] private TMP_Text _scoreText;
    [SerializeField] private TMP_Text _rankText;
    [SerializeField] private TMP_Text _accuracyText;

    [Header("Settings")]
    [SerializeField] private int _maxNotesDefault = 1000;

    [Header("Anomaly Score Color")]
    [SerializeField] private Color _normalScoreColor = Color.white;
    [SerializeField] private Color _noAnomalyScoreColor = new Color(0.6f, 0.8f, 1f);

    private SongData _currentSong;
    private DifficultyType _currentDifficulty;
    private bool _anomalyOn = true;

    public void DisplaySong(SongData song, DifficultyType difficulty)
    {
        if (song == null) return;

        _currentSong = song;
        _currentDifficulty = difficulty;

        UpdateBasicInfo(song);
        UpdateDifficultyInfo(song, difficulty);
    }

    public void ChangeDifficulty(DifficultyType newDifficulty)
    {
        if (_currentSong == null) return;
        if (!_currentSong.HasDifficulty(newDifficulty)) return;

        _currentDifficulty = newDifficulty;
        UpdateDifficultyInfo(_currentSong, newDifficulty);
    }

    public void SetAnomaly(bool on)
    {
        if (_anomalyOn == on) return;
        _anomalyOn = on;
        if (_currentSong != null)
            UpdateDifficultyInfo(_currentSong, _currentDifficulty);
    }

    private void UpdateBasicInfo(SongData song)
    {
        if (_songNameText != null)
            _songNameText.text = song.songName;

        if (_artistText != null)
            _artistText.text = song.artist;

        if (_bpmText != null)
            _bpmText.text = "BPM : " + song.bpm.ToString("F0");

        if (_tempoShiftText != null)
        {
            _tempoShiftText.gameObject.SetActive(song.hasTempoShift);
            if (song.hasTempoShift)
                _tempoShiftText.text = "[TempoShift]";
        }

        if (_durationText != null)
            _durationText.text = song.GetFormattedDuration();

        if (_songImage != null && song.songImage != null)
            _songImage.sprite = song.songImage;
    }

    private void UpdateDifficultyInfo(SongData song, DifficultyType difficulty)
    {
        var diffData = song.GetDifficulty(difficulty);
        if (diffData == null) return;

        Color diffColor = DifficultyUtility.GetDifficultyColor(difficulty);

        if (_difficultyLevelText != null)
        {
            _difficultyLevelText.text = DifficultyUtility.FormatLevel(diffData.level, diffData.constant);
            _difficultyLevelText.color = diffColor;
        }

        if (_difficultyConstantText != null)
        {
            _difficultyConstantText.text = "(" + diffData.constant.ToString("F1") + ")";
            _difficultyConstantText.color = diffColor;
        }

        if (_difficultyNameText != null)
        {
            _difficultyNameText.text = DifficultyUtility.GetDifficultyName(difficulty);
            _difficultyNameText.color = diffColor;
        }

        string charter = !string.IsNullOrEmpty(diffData.charter) ? diffData.charter : song.charter;
        string mapper = !string.IsNullOrEmpty(diffData.mapper) ? diffData.mapper : song.mapper;

        if (_charterText != null)
            _charterText.text = "Charter : " + charter;

        if (_mapperText != null)
            _mapperText.text = "Effecter : " + mapper;

        int recordScore = ScoreRecord.GetHighScore(song.songName, difficulty, _anomalyOn);
        float recordAcc = ScoreRecord.GetAccuracy(song.songName, difficulty, _anomalyOn);
        int recordTotal = ScoreRecord.GetTotalNoteCount(song.songName, difficulty, _anomalyOn);
        int rankNoteCount = recordTotal > 0 ? recordTotal : _maxNotesDefault;

        if (_scoreText != null)
        {
            _scoreText.text = recordScore.ToString("00,000,000");
            _scoreText.color = _anomalyOn ? _normalScoreColor : _noAnomalyScoreColor;
        }

        if (_rankText != null)
        {
            string rank = RankUtility.GetRank(recordScore, rankNoteCount);
            _rankText.text = rank;
            _rankText.color = RankUtility.GetRankColor(rank);
        }

        if (_accuracyText != null)
            _accuracyText.text = recordAcc.ToString("F2") + "%";
    }
}