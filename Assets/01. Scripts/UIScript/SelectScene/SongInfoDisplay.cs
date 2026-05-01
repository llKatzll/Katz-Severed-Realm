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

    private SongData _currentSong;
    private DifficultyType _currentDifficulty;

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

        if (_charterText != null)
            _charterText.text = "Charter : " + song.charter;

        if (_mapperText != null)
            _mapperText.text = "Effecter : " + song.mapper;

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

        if (_scoreText != null)
            _scoreText.text = diffData.highScore.ToString("N0");

        if (_rankText != null)
        {
            string rank = RankUtility.GetRank(diffData.highScore, _maxNotesDefault);
            _rankText.text = rank;
            _rankText.color = RankUtility.GetRankColor(rank);
        }

        if (_accuracyText != null)
            _accuracyText.text = diffData.accuracy.ToString("F2") + "%";
    }
}