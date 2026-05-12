using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class InGameManager : MonoBehaviour
{
    public static InGameManager I { get; private set; }

    [Header("Conductor")]
    [SerializeField] private RhythmConductor _conductor;

    [Header("Transition")]
    [SerializeField] private RectTransform _transitionRect;
    [SerializeField] private float _transitionSlideTime = 1f;
    [SerializeField] private float _transitionHideY = -1100f;
    [SerializeField] private float _transitionShowY = 0f;
    [SerializeField] private float _transitionWaitY = 1100f;
    [SerializeField] private float _exitTransitionSec = 5f;
    [SerializeField] private float _postLoadingPauseSec = 0.1f;

    [Header("Effect Warmup")]
    [SerializeField] private EffectWarmup _effectWarmup;

    [Header("Song Reveal")]
    [SerializeField] private CanvasGroup _songRevealGroup;
    [SerializeField] private TMP_Text _revealSongName;
    [SerializeField] private TMP_Text _revealArtistName;
    [SerializeField] private TMP_Text _readyText;
    [SerializeField] private TMP_Text _goText;

    [Header("Song Reveal Timing")]
    [SerializeField] private float _revealFadeInSec = 0.2f;
    [SerializeField] private float _artistShowSec = 0.5f;
    [SerializeField] private float _songNameShowSec = 0.5f;
    [SerializeField] private float _readySec = 0.4f;
    [SerializeField] private float _goSec = 0.3f;
    [SerializeField] private float _goFadeOutSec = 0.15f;

    [Header("Gauge")]
    [SerializeField] private Image _gaugeImage;

    [Header("Result")]
    [SerializeField] private CanvasGroup _resultGroup;
    [SerializeField] private float _resultDelaySec = 2f;
    [SerializeField] private float _resultFadeInSec = 0.5f;

    [Header("Scene")]
    [SerializeField] private string _songSelectScene = "SongSelect";

    private float _songDuration;
    private bool _songStarted;
    private bool _songFinished;
    private bool _resultShown;
    private bool _exiting;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        Application.runInBackground = true;
    }

    private void Start()
    {
        if (_songRevealGroup != null)
        {
            _songRevealGroup.alpha = 0f;
            _songRevealGroup.gameObject.SetActive(true);
        }

        if (_resultGroup != null)
        {
            _resultGroup.alpha = 0f;
            _resultGroup.gameObject.SetActive(false);
        }

        HideRevealTexts();

        SongData song = GameManager.I != null ? GameManager.I.SelectedSong : null;
        if (song != null)
        {
            _songDuration = song.durationSeconds;
            if (_revealSongName != null) _revealSongName.text = song.songName;
            if (_revealArtistName != null) _revealArtistName.text = song.artist;
        }
        else
        {
            _songDuration = _conductor != null && _conductor.Audio != null && _conductor.Audio.clip != null
                ? _conductor.Audio.clip.length
                : 120f;
        }

        if (_gaugeImage != null) _gaugeImage.fillAmount = 1f;

        StartCoroutine(IntroSequence());
    }

    private void Update()
    {
        if (!_songStarted || _songFinished) return;

        UpdateGauge();
        CheckSongEnd();
    }

    private void UpdateGauge()
    {
        if (_gaugeImage == null || _conductor == null) return;
        if (_songDuration <= 0f) return;

        float elapsed = (float)_conductor.SongTime;
        float remain = Mathf.Clamp01(1f - elapsed / _songDuration);
        _gaugeImage.fillAmount = remain;
    }

    private void CheckSongEnd()
    {
        if (_conductor == null) return;

        if ((float)_conductor.SongTime >= _songDuration)
        {
            _songFinished = true;
            StartCoroutine(OutroSequence());
        }
    }

    private IEnumerator IntroSequence()
    {
        if (_transitionRect != null)
        {
            SetTransitionY(_transitionShowY);
            _transitionRect.gameObject.SetActive(true);
        }

        yield return StartCoroutine(WaitForLoading());

        if (_postLoadingPauseSec > 0f)
            yield return new WaitForSeconds(_postLoadingPauseSec);

        if (_transitionRect != null)
        {
            yield return StartCoroutine(SlideTransition(_transitionShowY, _transitionHideY, _transitionSlideTime));
            _transitionRect.gameObject.SetActive(false);
        }

        yield return StartCoroutine(FadeCanvasGroup(_songRevealGroup, 0f, 1f, _revealFadeInSec));

        if (_revealArtistName != null)
        {
            _revealArtistName.gameObject.SetActive(true);
            yield return StartCoroutine(FadeTMP(_revealArtistName, 0f, 1f, 0.4f));
        }

        yield return new WaitForSeconds(_artistShowSec);

        if (_revealArtistName != null)
            yield return StartCoroutine(FadeTMP(_revealArtistName, 1f, 0f, 0.3f));

        if (_revealSongName != null)
        {
            _revealSongName.gameObject.SetActive(true);
            yield return StartCoroutine(FadeTMP(_revealSongName, 0f, 1f, 0.4f));
        }

        yield return new WaitForSeconds(_songNameShowSec);

        if (_revealSongName != null)
            yield return StartCoroutine(FadeTMP(_revealSongName, 1f, 0f, 0.3f));

        if (_readyText != null)
        {
            _readyText.gameObject.SetActive(true);
            yield return StartCoroutine(FadeTMP(_readyText, 0f, 1f, 0.2f));
            yield return new WaitForSeconds(_readySec);
            yield return StartCoroutine(FadeTMP(_readyText, 1f, 0f, 0.2f));
            _readyText.gameObject.SetActive(false);
        }

        if (_goText != null)
        {
            _goText.gameObject.SetActive(true);
            yield return StartCoroutine(FadeTMP(_goText, 0f, 1f, 0.15f));
            yield return new WaitForSeconds(_goSec);
            yield return StartCoroutine(FadeTMP(_goText, 1f, 0f, _goFadeOutSec));
            _goText.gameObject.SetActive(false);
        }

        yield return StartCoroutine(FadeCanvasGroup(_songRevealGroup, 1f, 0f, _goFadeOutSec));
        _songRevealGroup.gameObject.SetActive(false);

        SongData song = GameManager.I != null ? GameManager.I.SelectedSong : null;
        if (_conductor != null && song != null)
        {
            _conductor.SetBpm(song.bpm);
            _conductor.SetAudioOffset(song.audioOffsetSec);
            if (song.fullClip != null && _conductor.Audio != null)
                _conductor.Audio.clip = song.fullClip;
        }

        if (_conductor != null) _conductor.StartSong();
        _songStarted = true;
    }

    private IEnumerator OutroSequence()
    {
        yield return new WaitForSeconds(_resultDelaySec);

        if (_resultGroup != null)
        {
            ResultUI resultUI = _resultGroup.GetComponent<ResultUI>();
            if (resultUI != null) resultUI.Populate();

            _resultGroup.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(_resultGroup, 0f, 1f, _resultFadeInSec));
            _resultShown = true;
        }
    }

    public void OnExitButtonClicked()
    {
        if (_exiting) return;
        _exiting = true;
        StartCoroutine(ExitSequence());
    }

    private IEnumerator ExitSequence()
    {
        if (_transitionRect != null)
        {
            SetTransitionY(_transitionWaitY);
            _transitionRect.gameObject.SetActive(true);
            yield return StartCoroutine(SlideTransition(_transitionWaitY, _transitionShowY, _transitionSlideTime));
        }

        yield return new WaitForSeconds(_exitTransitionSec);

        SceneManager.LoadScene(_songSelectScene);
    }

    private IEnumerator WaitForLoading()
    {
        SongData song = GameManager.I != null ? GameManager.I.SelectedSong : null;
        if (song != null && song.fullClip != null)
        {
            AudioClip clip = song.fullClip;
            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                clip.LoadAudioData();
                while (clip.loadState == AudioDataLoadState.Loading)
                {
                    yield return null;
                }
            }
        }

        ChartNoteSpawner spawner = FindObjectOfType<ChartNoteSpawner>();
        if (spawner != null)
        {
            float waitTimeout = 5f;
            float elapsed = 0f;
            while (!spawner.IsChartLoaded && elapsed < waitTimeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        if (_effectWarmup != null)
        {
            yield return StartCoroutine(_effectWarmup.WarmupAll());
        }
    }

    private IEnumerator SlideTransition(float fromY, float toY, float duration)
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

    private void HideRevealTexts()
    {
        if (_revealSongName != null)
        {
            _revealSongName.gameObject.SetActive(false);
            SetTMPAlpha(_revealSongName, 0f);
        }
        if (_revealArtistName != null)
        {
            _revealArtistName.gameObject.SetActive(false);
            SetTMPAlpha(_revealArtistName, 0f);
        }
        if (_readyText != null)
        {
            _readyText.gameObject.SetActive(false);
            SetTMPAlpha(_readyText, 0f);
        }
        if (_goText != null)
        {
            _goText.gameObject.SetActive(false);
            SetTMPAlpha(_goText, 0f);
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        float t = 0f;
        cg.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    private IEnumerator FadeTMP(TMP_Text text, float fromA, float toA, float duration)
    {
        if (text == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(fromA, toA, t / duration);
            SetTMPAlpha(text, a);
            yield return null;
        }
        SetTMPAlpha(text, toA);
    }

    private void SetTMPAlpha(TMP_Text text, float a)
    {
        if (text == null) return;
        Color c = text.color;
        c.a = a;
        text.color = c;
    }
}
