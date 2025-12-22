using UnityEngine;

public class Note : MonoBehaviour
{
    [Header("Runtime")]
    protected float _travelTime;
    protected float _elapsed;

    protected NoteSpawner.NoteType _noteType;

    protected Transform _space;        // 보통 HitPoint(판정선)
    protected Transform _rotateSource; // 보통 HitPoint(판정선)

    protected Vector3 _spawnLocal;
    protected Vector3 _hitLocal;

    protected bool _useDespawn;
    protected Vector3 _despawnLocal;
    protected float _postTime;

    protected float _yOffsetLocal;

    protected double _spawnDspTime;
    public double ExpectedHitDspTime { get; protected set; }

    public void InitFollow(
        Transform space,
        Transform spawnPoint,
        Transform hitPoint,
        Transform despawnPoint,
        float travelTime,
        NoteSpawner.NoteType noteType,
        float yOffsetLocal = 0f
    )
    {
        _space = space != null ? space : hitPoint;
        _rotateSource = hitPoint != null ? hitPoint : _space;

        _useDespawn = (despawnPoint != null);

        _travelTime = Mathf.Max(0.0001f, travelTime);
        _noteType = noteType;
        _elapsed = 0f;

        _yOffsetLocal = yOffsetLocal;

        _spawnDspTime = AudioSettings.dspTime;
        ExpectedHitDspTime = _spawnDspTime + _travelTime;

        _spawnLocal = _space.InverseTransformPoint(spawnPoint.position);
        _hitLocal = _space.InverseTransformPoint(hitPoint.position);

        if (_useDespawn)
        {
            _despawnLocal = _space.InverseTransformPoint(despawnPoint.position);

            float distA = Vector3.Distance(_spawnLocal, _hitLocal);
            float speed = distA / _travelTime;

            float distB = Vector3.Distance(_hitLocal, _despawnLocal);
            _postTime = distB / Mathf.Max(0.0001f, speed);
        }
        else
        {
            _postTime = 0f;
        }

        // 첫 위치 세팅
        transform.position = _space.TransformPoint(_spawnLocal);

        // 첫 회전 세팅: 판정선 각도 유지
        if (_rotateSource != null)
            transform.rotation = _rotateSource.rotation;
    }

    protected virtual void Update()
    {
        _elapsed += Time.deltaTime;

        if (_space == null) return;

        Vector3 localPos;
        bool finished;
        EvaluateLocal(_elapsed, out localPos, out finished);

        if (finished)
        {
            Destroy(gameObject);
            return;
        }

        localPos += Vector3.up * _yOffsetLocal;

        transform.position = _space.TransformPoint(localPos);

        // Upper/Ground 모두 동일: 판정선 각도 유지 (레일이 돌면 같이 돈다)
        if (_rotateSource != null)
            transform.rotation = _rotateSource.rotation;
    }

    private void EvaluateLocal(float elapsed, out Vector3 localPos, out bool finished)
    {
        finished = false;

        if (!_useDespawn)
        {
            float t = Mathf.Clamp01(elapsed / _travelTime);
            localPos = Vector3.Lerp(_spawnLocal, _hitLocal, t);
            return;
        }

        if (elapsed <= _travelTime)
        {
            float t = Mathf.Clamp01(elapsed / _travelTime);
            localPos = Vector3.Lerp(_spawnLocal, _hitLocal, t);
            return;
        }

        float e2 = elapsed - _travelTime;
        float t2 = Mathf.Clamp01(e2 / Mathf.Max(0.0001f, _postTime));
        localPos = Vector3.Lerp(_hitLocal, _despawnLocal, t2);

        if (t2 >= 1f) finished = true;
    }
}
