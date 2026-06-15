using UnityEngine;
using System.Collections;

public class SongPreviewPlayer : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;

    [Header("Settings")]
    [SerializeField] private float _fadeInTime = 0.5f;
    [SerializeField] private float _fadeOutTime = 0.3f;
    [SerializeField] private float _loopFadeOutTime = 1.5f;
    [SerializeField] private float _maxVolume = 0.8f;
    [SerializeField] private float _delayBeforePlay = 0.3f;

    public float FadeInTime => _fadeInTime;

    private Coroutine _currentCoroutine;
    private SongData _currentSong;

    public void PlayPreview(SongData song)
    {
        if (song == null) return;
        if (song.previewClip == null) return;

        if (_currentSong == song && _audioSource.isPlaying)
            return;

        _currentSong = song;

        if (_currentCoroutine != null)
            StopCoroutine(_currentCoroutine);

        _currentCoroutine = StartCoroutine(PlayPreviewRoutine(song.previewClip));
    }

    public void StopPreview()
    {
        FadeOut(_fadeOutTime);
    }

    public void FadeOut(float duration)
    {
        if (_currentCoroutine != null)
            StopCoroutine(_currentCoroutine);

        _currentCoroutine = StartCoroutine(FadeOutRoutine(duration));
    }

    private IEnumerator PlayPreviewRoutine(AudioClip clip)
    {
        if (_audioSource.isPlaying)
        {
            yield return FadeOutRoutine(_fadeOutTime);
        }

        yield return new WaitForSeconds(_delayBeforePlay);

        while (true)
        {
            _audioSource.clip = clip;
            _audioSource.time = 0f;
            _audioSource.volume = 0f;
            _audioSource.Play();

            float t = 0f;
            while (t < _fadeInTime)
            {
                t += Time.deltaTime;
                _audioSource.volume = Mathf.Lerp(0f, _maxVolume, t / _fadeInTime);
                yield return null;
            }
            _audioSource.volume = _maxVolume;

            float holdTime = clip.length - _fadeInTime - _loopFadeOutTime;
            if (holdTime > 0f)
                yield return new WaitForSeconds(holdTime);

            yield return FadeOutRoutine(_loopFadeOutTime);
        }
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float startVol = _audioSource.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }

        _audioSource.Stop();
        _audioSource.volume = 0f;
    }
}