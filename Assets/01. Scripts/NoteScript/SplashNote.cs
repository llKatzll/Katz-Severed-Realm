using UnityEngine;

public class SplashNote : Note
{
    [SerializeField] private float _startScale = 0.5f;

    private float _hitScale = 1f;
    private MeshFilter _mf;

    public override void Init(Vector3 spawnPos, Vector3 hitPos, float travelTime, NoteSpawner.NoteType noteType)
    {
        base.Init(spawnPos, hitPos, travelTime, noteType);

        if (_mf == null) _mf = GetComponentInChildren<MeshFilter>();

        float baseRadiusLocal = 0.5f;
        if (_mf && _mf.sharedMesh != null)
        {
            var b = _mf.sharedMesh.bounds;
            baseRadiusLocal = Mathf.Max(b.extents.x, b.extents.y);
        }

        // 목표 반지름(월드)에서 = 중앙~판정선까지 거리
        float targetRadiusWorld = Vector3.Distance(_spawnPos, _hitPos);

        // 로컬 반지름에서 월드 반지름으로 변환
        float scaleWorld = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        float baseRadiusWorld = baseRadiusLocal * scaleWorld;

        _hitScale = baseRadiusWorld > 0.0001f ? (targetRadiusWorld / baseRadiusWorld) : 1f;

        transform.position = _spawnPos;

        if (transform.lossyScale.x >= 2.99f)
        {
            Destroy(gameObject);
        }

        transform.localScale = Vector3.one * _startScale;
    }

    protected override void Apply(float t)
    {
        transform.position = _spawnPos;
        float s = Mathf.Lerp(_startScale, _hitScale, t);
        transform.localScale = Vector3.one * s;
    }
}
