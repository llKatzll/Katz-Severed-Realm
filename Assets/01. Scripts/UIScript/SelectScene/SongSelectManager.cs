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

    private SongBar _currentSelectedBar;
    private bool _isFirstSelection = true;

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

    public void OnSongBarSelected(SongBar bar)
    {
        if (bar == null) return;
        if (bar.SongData == null) return;

        if (_currentSelectedBar == bar) return;

        _currentSelectedBar = bar;
        _currentSong = bar.SongData;

        if (_difficultySelector != null)
        {
            _difficultySelector.ShowButtons(true, _isFirstSelection);
            _difficultySelector.SetupForSong(_currentSong);
            _currentDifficulty = _difficultySelector.GetSelectedDifficulty();
        }

        _isFirstSelection = false;

        if (_infoDisplay != null)
        {
            _infoDisplay.DisplaySong(_currentSong, _currentDifficulty);
        }

        if (_previewPlayer != null)
        {
            _previewPlayer.PlayPreview(_currentSong);
        }

        Debug.Log("[SongSelectManager] Song selected: " + _currentSong.songName);
    }

    public void OnSongBarDeselected(SongBar bar)
    {
        if (bar != _currentSelectedBar) return;

        _currentSelectedBar = null;

        if (_difficultySelector != null)
        {
            _difficultySelector.ShowButtons(false);
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

        Debug.Log("[SongSelectManager] Difficulty changed: " + newDifficulty);
    }

    public void OnStartButtonPressed()
    {
        if (_currentSong == null)
        {
            Debug.LogWarning("[SongSelectManager] No song selected!");
            return;
        }

        Debug.Log("[SongSelectManager] Starting: " + _currentSong.songName + " [" + _currentDifficulty + "]");
    }

    public SongData GetCurrentSong() => _currentSong;
    public DifficultyType GetCurrentDifficulty() => _currentDifficulty;
}