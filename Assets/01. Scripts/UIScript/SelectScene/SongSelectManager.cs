using UnityEngine;
using UnityEngine.UI;

public class SongSelectManager : MonoBehaviour
{
    public static SongSelectManager I { get; private set; }

    [Header("References")]
    [SerializeField] private Scrolling _scrolling;
    [SerializeField] private SongInfoDisplay _infoDisplay;
    [SerializeField] private DifficultySelector _difficultySelector;
    [SerializeField] private SongPreviewPlayer _previewPlayer;

    [Header("Song Bars")]
    [SerializeField] private SongBar[] _songBars;

    [Header("Current Selection")]
    [SerializeField] private SongData _currentSong;
    [SerializeField] private DifficultyType _currentDifficulty;

    [Header("Exit")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    private SongBar _currentSelectedBar;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
    }

    private void Start()
    {
        if (_difficultySelector != null)
        {
            _difficultySelector.OnDifficultySelected += OnDifficultyChanged;
        }
    }

    private void OnDestroy()
    {
        if (_difficultySelector != null)
        {
            _difficultySelector.OnDifficultySelected -= OnDifficultyChanged;
        }
    }

    public void ExitToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(_mainMenuSceneName);
    }

    public void OnSongBarSelected(SongBar bar)
    {
        if (bar == null) return;
        if (bar.SongData == null) return;

        if (_currentSelectedBar == bar) return;

        _currentSelectedBar = bar;
        _currentSong = bar.SongData;

        if (_difficultySelector != null)
        {
            _difficultySelector.SetupForSong(_currentSong);
            _currentDifficulty = _difficultySelector.GetSelectedDifficulty();

            float fadeTime = _previewPlayer != null ? _previewPlayer.FadeInTime : 0.5f;
            _difficultySelector.ShowWithFade(fadeTime);
        }

        if (_infoDisplay != null)
        {
            _infoDisplay.DisplaySong(_currentSong, _currentDifficulty);
        }

        if (_previewPlayer != null)
        {
            _previewPlayer.PlayPreview(_currentSong);
        }

    }

    public void OnSongBarDeselected(SongBar bar)
    {
        if (bar != _currentSelectedBar) return;

        _currentSelectedBar = null;

        if (_difficultySelector != null)
        {
            _difficultySelector.HideInstant();
        }

        if (_previewPlayer != null)
        {
            _previewPlayer.StopPreview();
        }
    }

    private void OnDifficultyChanged(DifficultyType newDifficulty)
    {
        _currentDifficulty = newDifficulty;

        if (_infoDisplay != null)
        {
            _infoDisplay.ChangeDifficulty(newDifficulty);
        }

    }

    public void OnStartButtonPressed()
    {
        if (_currentSong == null)
        {
            Debug.LogWarning("[SongSelectManager] No song selected!");
            return;
        }

    }

    public SongData GetCurrentSong() => _currentSong;
    public DifficultyType GetCurrentDifficulty() => _currentDifficulty;
}