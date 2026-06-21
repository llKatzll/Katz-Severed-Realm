using UnityEngine;

public class SfxManager : MonoBehaviour
{
    public static SfxManager I { get; private set; }

    [SerializeField] private AudioSource _source;
    [SerializeField] private AudioClip _buttonClip;
    [SerializeField] private AudioClip _wheelClip;
    [SerializeField] private AudioClip _diffClip;
    [SerializeField] private AudioClip _transitionClip;
    [SerializeField] private AudioClip _returnClip;
    [SerializeField] private AudioClip _anomalyClip;
    [SerializeField] private AudioClip _hitClip;

    private const string SfxGroupName = "SFX";
    private const string HitGroupName = "Hit";

    private AudioSource _hitSource;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);

        if (_source == null) _source = GetComponent<AudioSource>();
        if (_source != null) _source.playOnAwake = false;

        _hitSource = gameObject.AddComponent<AudioSource>();
        _hitSource.playOnAwake = false;
    }

    private void Start()
    {
        EnsureOutput();
    }

    private void EnsureOutput()
    {
        if (_source != null && _source.outputAudioMixerGroup == null)
        {
            var group = AudioMixerBinder.GetGroup(SfxGroupName);
            if (group != null) _source.outputAudioMixerGroup = group;
        }

        if (_hitSource != null && _hitSource.outputAudioMixerGroup == null)
        {
            var hitGroup = AudioMixerBinder.GetGroup(HitGroupName);
            if (hitGroup != null) _hitSource.outputAudioMixerGroup = hitGroup;
        }
    }

    private void Play(AudioClip clip)
    {
        if (_source == null || clip == null) return;
        _source.PlayOneShot(clip);
    }

    public void PlayButton() { Play(_buttonClip); }
    public void PlayWheel() { Play(_wheelClip); }
    public void PlayDiff() { Play(_diffClip); }
    public void PlayTransition() { Play(_transitionClip); }
    public void PlayReturn() { Play(_returnClip); }
    public void PlayAnomaly() { Play(_anomalyClip); }

    public void PlayHit()
    {
        if (_hitSource == null || _hitClip == null) return;
        _hitSource.PlayOneShot(_hitClip);
    }
}
