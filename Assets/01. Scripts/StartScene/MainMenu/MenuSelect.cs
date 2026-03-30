using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuSelect : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _freePlayButton;

    [Header("Fade")]
    [SerializeField] private RawImage _blackScreen;
    [SerializeField] private float _fadeInTime = 0.8f;

    [Header("Scene Names")]
    [SerializeField] private string _songSelectSceneName = "SongSelect";

    private bool _isTransitioning;

    private void Start()
    {
        if (_freePlayButton != null)
            _freePlayButton.onClick.AddListener(OnFreePlayClicked);
    }

    private void OnFreePlayClicked()
    {
        if (_isTransitioning) return;
        _isTransitioning = true;

        StartCoroutine(CoTransitionToSongSelect());
    }

    private IEnumerator CoTransitionToSongSelect()
    {
        if (_blackScreen != null)
        {
            _blackScreen.gameObject.SetActive(true);
            float t = 0f;

            while (t < _fadeInTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / _fadeInTime);
                Color c = _blackScreen.color;
                c.a = k;
                _blackScreen.color = c;
                yield return null;
            }
        }

        SceneManager.LoadScene(_songSelectSceneName);
    }
}
