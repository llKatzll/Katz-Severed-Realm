using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseController : MonoBehaviour
{
    [Header("Conductor")]
    [SerializeField] private RhythmConductor _conductor;

    [Header("Pause Icon")]
    [SerializeField] private Image _pauseIcon;
    [SerializeField] private Sprite _pauseSpriteDefault;
    [SerializeField] private Sprite _pauseSpriteArmed;
    [SerializeField] private float _doubleTapWindowSec = 2f;

    [Header("Pause Menu Panel")]
    [SerializeField] private GameObject _pauseMenuPanel;

    [Header("Paused Indicator (persists after pause used)")]
    [SerializeField] private GameObject _pausedIndicator;

    [Header("Countdown")]
    [SerializeField] private GameObject _countdownGroup;
    [SerializeField] private TMP_Text _countdownText;
    [SerializeField] private float _countdownIntervalSec = 1f;

    [Header("Menu Buttons")]
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _musicSelectButton;
    [SerializeField] private Button _mainMenuButton;
    [SerializeField] private Button _closeButton;

    [Header("Scenes")]
    [SerializeField] private string _inGameScene = "InGame";
    [SerializeField] private string _songSelectScene = "SongSelect";
    [SerializeField] private string _mainMenuScene = "MainMenu";

    private bool _armed;
    private float _armedTime;
    private bool _paused;
    private Coroutine _countdownCoroutine;

    private void Start()
    {
        if (_conductor == null) _conductor = FindAnyObjectByType<RhythmConductor>();

        if (_pauseMenuPanel != null) _pauseMenuPanel.SetActive(false);
        if (_countdownGroup != null) _countdownGroup.SetActive(false);
        if (_pausedIndicator != null) _pausedIndicator.SetActive(false);
        SetIcon(_pauseSpriteDefault);

        if (_resumeButton != null) _resumeButton.onClick.AddListener(OnResume);
        if (_restartButton != null) _restartButton.onClick.AddListener(OnRestart);
        if (_musicSelectButton != null) _musicSelectButton.onClick.AddListener(OnMusicSelect);
        if (_mainMenuButton != null) _mainMenuButton.onClick.AddListener(OnMainMenu);
        if (_closeButton != null) _closeButton.onClick.AddListener(OnResume);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            HandleEsc();

        if (_armed && Time.unscaledTime - _armedTime > _doubleTapWindowSec)
            Disarm();
    }

    private void HandleEsc()
    {
        if (_countdownCoroutine != null)
        {
            CancelCountdown();
            ShowPauseMenu();
            return;
        }

        if (_paused) { OnResume(); return; }

        if (!_armed) { Arm(); return; }

        Pause();
    }

    private void Arm()
    {
        _armed = true;
        _armedTime = Time.unscaledTime;
        SetIcon(_pauseSpriteArmed);
    }

    private void Disarm()
    {
        _armed = false;
        SetIcon(_pauseSpriteDefault);
    }

    private void Pause()
    {
        _armed = false;
        _paused = true;
        if (_conductor != null) _conductor.Pause();
        Time.timeScale = 0f;
        ShowPauseMenu();
        if (_pausedIndicator != null) _pausedIndicator.SetActive(true);
        if (ScoreManager.I != null) ScoreManager.I.MarkPaused();
    }

    private void ShowPauseMenu()
    {
        _paused = true;
        if (_pauseMenuPanel != null) _pauseMenuPanel.SetActive(true);
    }

    private void HidePauseMenu()
    {
        if (_pauseMenuPanel != null) _pauseMenuPanel.SetActive(false);
    }

    private void OnResume()
    {
        HidePauseMenu();
        _countdownCoroutine = StartCoroutine(CountdownThenResume());
    }

    private IEnumerator CountdownThenResume()
    {
        if (_countdownGroup != null) _countdownGroup.SetActive(true);

        for (int n = 3; n >= 1; n--)
        {
            if (_countdownText != null) _countdownText.text = n.ToString();
            yield return new WaitForSecondsRealtime(_countdownIntervalSec);
        }

        if (_countdownGroup != null) _countdownGroup.SetActive(false);

        Time.timeScale = 1f;
        if (_conductor != null) _conductor.Resume();

        _paused = false;
        SetIcon(_pauseSpriteDefault);
        _countdownCoroutine = null;
    }

    private void CancelCountdown()
    {
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }
        if (_countdownGroup != null) _countdownGroup.SetActive(false);
    }

    private void OnRestart()
    {
        Cleanup();
        if (ScoreManager.I != null) ScoreManager.I.ClearPaused();
        SceneManager.LoadScene(_inGameScene);
    }

    private void OnMusicSelect()
    {
        CleanupForExit();
        if (InGameManager.I != null)
            InGameManager.I.ExitToScene(_songSelectScene, true);
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(_songSelectScene);
        }
    }

    private void OnMainMenu()
    {
        CleanupForExit();
        if (InGameManager.I != null)
            InGameManager.I.ExitToScene(_mainMenuScene, true);
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(_mainMenuScene);
        }
    }

    private void Cleanup()
    {
        Time.timeScale = 1f;
        if (_conductor != null && _conductor.Paused) _conductor.Resume();
        CancelCountdown();
        HidePauseMenu();
        _paused = false;
    }

    private void CleanupForExit()
    {
        CancelCountdown();
        HidePauseMenu();
        _paused = false;
    }

    private void SetIcon(Sprite s)
    {
        if (_pauseIcon != null && s != null) _pauseIcon.sprite = s;
    }
}
