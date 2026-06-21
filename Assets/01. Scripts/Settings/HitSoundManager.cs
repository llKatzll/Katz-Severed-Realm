using UnityEngine;

public class HitSoundManager : MonoBehaviour
{
    public static HitSoundManager I { get; private set; }

    [SerializeField] private AudioSource _source;
    [SerializeField] private AudioClip _hitClip;

    private const string HitGroupName = "Hit";

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        if (_source == null) _source = GetComponent<AudioSource>();
        if (_source != null) _source.playOnAwake = false;
    }

    private void Start()
    {
        EnsureOutput();
    }

    private void EnsureOutput()
    {
        if (_source == null) return;
        var group = AudioMixerBinder.GetGroup(HitGroupName);
        if (group != null) _source.outputAudioMixerGroup = group;
    }

    public void PlayHit()
    {
        if (_source == null || _hitClip == null) return;
        _source.PlayOneShot(_hitClip);
    }
}
