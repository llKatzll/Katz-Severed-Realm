using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuSelect : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _freePlayButton;
    [SerializeField] private Button _returnToTitleButton;

    [Header("Fade")]
    [SerializeField] private RawImage _blackScreen;
    [SerializeField] private float _fadeInTime = 0.8f;

    [Header("Scene Names")]
    [SerializeField] private string _songSelectSceneName = "SongSelect";
    [SerializeField] private string _startSceneName = "StartScene";

    private bool _isTransitioning;

    private void Start()
    {
        if (_freePlayButton != null)
            _freePlayButton.onClick.AddListener(OnFreePlayClicked);
        if (_returnToTitleButton != null)
            _returnToTitleButton.onClick.AddListener(OnReturnToTitleClicked);
    }

    private void OnFreePlayClicked()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        StartCoroutine(CoFadeAndLoad(_songSelectSceneName));
    }

    private void OnReturnToTitleClicked()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;
        IntroScene.SkipIntroOnce = true;
        StartCoroutine(CoFadeAndLoad(_startSceneName));
    }

    private IEnumerator CoFadeAndLoad(string sceneName)
    {
        MainMenuScene mainMenu = FindAnyObjectByType<MainMenuScene>();
        if (mainMenu != null) mainMenu.CancelFade();

        if (_blackScreen != null)
        {
            _blackScreen.gameObject.SetActive(true);
            float startA = _blackScreen.color.a;
            float t = 0f;

            while (t < _fadeInTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / _fadeInTime);
                Color c = _blackScreen.color;
                c.a = Mathf.Lerp(startA, 1f, k);
                _blackScreen.color = c;
                yield return null;
            }

            Color cc = _blackScreen.color;
            cc.a = 1f;
            _blackScreen.color = cc;
        }

        SceneManager.LoadScene(sceneName);
    }
}
