using UnityEngine;

public class SplashNote : Note
{
    [SerializeField] private Transform _hitRing; 
    [SerializeField] private float _startScale = 0.5f;

    private float _hitScale = 1f;

    public override void Init(Vector3 spawnPos, Vector3 hitPos, float travelTime, NoteSpawner.NoteType noteType)
    {
        base.Init(spawnPos, hitPos, travelTime, noteType);

        transform.position = _spawnPos;

        _hitScale = CalculateHitScale();
        transform.localScale = Vector3.one * _startScale;
    }

    protected override void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _travelTime);

        transform.position = _spawnPos;

        float s = Mathf.Lerp(_startScale, _hitScale, t);
        transform.localScale = Vector3.one * s;

        if (t >= 1f)
            Destroy(gameObject);
    }

    private float CalculateHitScale()
    {
        if (_hitRing == null)
            return 3f;

        float ringWorldRadius = GetWorldRadius(_hitRing);
        float noteLocalRadius = GetLocalRadiusAtScale1(transform);

        float parentScaleX = transform.parent ? transform.parent.lossyScale.x : 1f;

        float denom = noteLocalRadius * Mathf.Max(0.0001f, parentScaleX);
        return ringWorldRadius / denom;
    }

    private float GetWorldRadius(Transform t)
    {
        var r = t.GetComponentInChildren<Renderer>();
        if (r != null)
            return r.bounds.extents.x;

        return 1f;
    }

    private float GetLocalRadiusAtScale1(Transform t)
    {
        Vector3 prev = t.localScale;
        t.localScale = Vector3.one;

        float radius = 0.5f;

        var mf = t.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
            radius = mf.sharedMesh.bounds.extents.x;

        var sr = t.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
            radius = sr.sprite.bounds.extents.x;

        var rr = t.GetComponentInChildren<Renderer>();
        if (rr != null && (mf == null && sr == null))
            radius = rr.bounds.extents.x / Mathf.Max(0.0001f, t.lossyScale.x);

        t.localScale = prev;
        return Mathf.Max(0.0001f, radius);
    }
}
