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

    [Header("Exit Transition")]
    [SerializeField] private RectTransform _transitionRect;
    [SerializeField] private float _transitionSlideTime = 3f;
    [SerializeField] private float _transitionWaitY = 1100f;
    [SerializeField] private float _transitionShowY = 0f;
    [SerializeField] private float _delayBeforeLoad = 0.5f;

    private SongBar _currentSelectedBar;
    private bool _exiting;

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
        if (_exiting) return;
        _exiting = true;
        StartCoroutine(CoExitToMainMenu());
    }

    private System.Collections.IEnumerator CoExitToMainMenu()
    {
        if (_transitionRect != null)
        {
            if (SfxManager.I != null) SfxManager.I.PlayTransition();
            if (_previewPlayer != null) _previewPlayer.FadeOut(_transitionSlideTime);
            SetTransitionY(_transitionWaitY);
            _transitionRect.gameObject.SetActive(true);
            yield return StartCoroutine(SlideTransition(_transitionWaitY, _transitionShowY, _transitionSlideTime));
        }

        yield return new WaitForSeconds(_delayBeforeLoad);

        UnityEngine.SceneManagement.SceneManager.LoadScene(_mainMenuSceneName);
    }

    private System.Collections.IEnumerator SlideTransition(float fromY, float toY, float duration)
    {
        if (_transitionRect == null) yield break;

        float t = 0f;
        Vector2 pos = _transitionRect.anchoredPosition;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            pos.y = Mathf.Lerp(fromY, toY, k);
            _transitionRect.anchoredPosition = pos;
            yield return null;
        }

        pos.y = toY;
        _transitionRect.anchoredPosition = pos;
    }

    private void SetTransitionY(float y)
    {
        if (_transitionRect == null) return;
        Vector2 pos = _transitionRect.anchoredPosition;
        pos.y = y;
        _transitionRect.anchoredPosition = pos;
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