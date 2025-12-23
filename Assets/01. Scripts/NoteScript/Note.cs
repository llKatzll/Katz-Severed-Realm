using UnityEngine;

public class Note : MonoBehaviour
{
    protected float _travelTime;
    protected float _elapsed;

    protected NoteSpawner.NoteType _noteType;

    protected Transform _space;
    protected Transform _rotateSource;

    protected float _spawnZ;
    protected float _hitZ;
    protected float _despawnZ;

    protected float _fixedX;
    protected float _fixedY;

    protected bool _useDespawn;
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

        _yOffsetLocal = yOffsetLocal;

        Vector3 spawnLocal = _space.InverseTransformPoint(spawnPoint.position);
        Vector3 hitLocal = _space.InverseTransformPoint(hitPoint.position);

        _fixedX = hitLocal.x;
        _fixedY = hitLocal.y;

        _spawnZ = spawnLocal.z;
        _hitZ = hitLocal.z;

        if (_useDespawn)
        {
            Vector3 despawnLocal = _space.InverseTransformPoint(despawnPoint.position);
            _despawnZ = despawnLocal.z;

            float distA = Mathf.Abs(_spawnZ - _hitZ);
            float speed = distA / _travelTime;

            float distB = Mathf.Abs(_hitZ - _despawnZ);
            _postTime = distB / Mathf.Max(0.0001f, speed);
        }
        else
        {
            _despawnZ = _hitZ;
            _postTime = 0f;
        }

        _spawnDspTime = AudioSettings.dspTime;
        ExpectedHitDspTime = _spawnDspTime + _travelTime;

        Vector3 local0 = new Vector3(_fixedX, _fixedY + _yOffsetLocal, _spawnZ);
        transform.position = _space.TransformPoint(local0);

        if (_rotateSource != null)
            transform.rotation = _rotateSource.rotation;
    }

    public void SetExpectedHitDspTime(double hitDspTime)
    {
        ExpectedHitDspTime = hitDspTime;
        _spawnDspTime = ExpectedHitDspTime - _travelTime;
    }

    protected virtual void Update()
    {
        if (_space == null) return;

        _elapsed = (float)(AudioSettings.dspTime - _spawnDspTime);
        if (_elapsed < 0f) _elapsed = 0f;

        Vector3 localPos;
        bool finished;
        EvaluateLocal(_elapsed, out localPos, out finished);

        if (finished)
        {
            Destroy(gameObject);
            return;
        }

        localPos.y += _yOffsetLocal;
        transform.position = _space.TransformPoint(localPos);

        if (_rotateSource != null)
            transform.rotation = _rotateSource.rotation;
    }

    protected void EvaluateLocal(float elapsed, out Vector3 localPos, out bool finished)
    {
        finished = false;

        if (!_useDespawn)
        {
            float t = Mathf.Clamp01(elapsed / _travelTime);
            float z = Mathf.Lerp(_spawnZ, _hitZ, t);
            localPos = new Vector3(_fixedX, _fixedY, z);
            return;
        }

        if (elapsed <= _travelTime)
        {
            float t = Mathf.Clamp01(elapsed / _travelTime);
            float z = Mathf.Lerp(_spawnZ, _hitZ, t);
            localPos = new Vector3(_fixedX, _fixedY, z);
            return;
        }

        float e2 = elapsed - _travelTime;
        float t2 = Mathf.Clamp01(e2 / Mathf.Max(0.0001f, _postTime));
        float z2 = Mathf.Lerp(_hitZ, _despawnZ, t2);
        localPos = new Vector3(_fixedX, _fixedY, z2);

        if (t2 >= 1f) finished = true;
    }

    public float GetSpeedLocalZ()
    {
        float distA = Mathf.Abs(_spawnZ - _hitZ);
        return distA / Mathf.Max(0.0001f, _travelTime);
    }
}
