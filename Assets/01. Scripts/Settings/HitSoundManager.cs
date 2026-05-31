using UnityEngine;

public class HitSoundManager : MonoBehaviour
{
    public static HitSoundManager I { get; private set; }

    [SerializeField] private AudioSource _source;
    [SerializeField] private AudioClip _hitClip;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
    }

    public void PlayHit()
    {
        if (_source == null || _hitClip == null) return;
        _source.PlayOneShot(_hitClip);
    }
}
