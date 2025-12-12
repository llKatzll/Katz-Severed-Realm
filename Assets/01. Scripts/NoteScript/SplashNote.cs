using UnityEngine;

public class SplashNote : Note
{
    [SerializeField] private float _startScale = 0.5f;
    [SerializeField] private float _endScale = 3f;

    public override void Init(Vector3 spawnPos, Vector3 hitPos, float travelTime, NoteSpawner.NoteType noteType)
    {
        base.Init(spawnPos, hitPos, travelTime, noteType);
        transform.localScale = Vector3.one * _startScale;
    }

    protected override void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _travelTime);

        transform.position = Vector3.Lerp(_spawnPos, _hitPos, t);

        float s = Mathf.Lerp(_startScale, _endScale, t);
        transform.localScale = Vector3.one * s;

        if (t >= 1f)
            Destroy(gameObject);
    }
}
