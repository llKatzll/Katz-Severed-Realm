using UnityEngine;

public class Note : MonoBehaviour
{
    protected float _travelTime;
    protected float _elapsed;
    protected NoteSpawner.NoteType _noteType;

    protected Transform _space;
    protected Vector3 _spawnLocal;
    protected Vector3 _hitLocal;

    protected bool _useDespawn;
    protected Vector3 _despawnLocal;
    protected float _postTime;

    protected double _spawnDspTime;
    public double ExpectedHitDspTime { get; protected set; }

    public virtual void Init(Vector3 spawnPos, Vector3 hitPos, float travelTime, NoteSpawner.NoteType noteType)
    {
        _space = null;
        _useDespawn = false;

        _travelTime = Mathf.Max(0.0001f, travelTime);
        _noteType = noteType;
        _elapsed = 0f;

        _spawnDspTime = AudioSettings.dspTime;
        ExpectedHitDspTime = _spawnDspTime + _travelTime;

        transform.position = spawnPos;
    }

    public void InitFollow(Transform space, Transform spawnPoint, Transform hitPoint,
        float travelTime, NoteSpawner.NoteType noteType)
    {
        _space = space != null ? space : spawnPoint.parent;
        _useDespawn = false;

        _travelTime = Mathf.Max(0.0001f, travelTime);
        _noteType = noteType;
        _elapsed = 0f;

        _spawnDspTime = AudioSettings.dspTime;
        ExpectedHitDspTime = _spawnDspTime + _travelTime;

        _spawnLocal = _space.InverseTransformPoint(spawnPoint.position);
        _hitLocal = _space.InverseTransformPoint(hitPoint.position);

        transform.position = _space.TransformPoint(_spawnLocal);
    }

    public void InitFollow(Transform space, Transform spawnPoint, Transform hitPoint, Transform despawnPoint,
        float travelTime, NoteSpawner.NoteType noteType)
    {
        _space = space != null ? space : spawnPoint.parent;
        _useDespawn = true;

        _travelTime = Mathf.Max(0.0001f, travelTime);
        _noteType = noteType;
        _elapsed = 0f;

        _spawnDspTime = AudioSettings.dspTime;
        ExpectedHitDspTime = _spawnDspTime + _travelTime;

        _spawnLocal = _space.InverseTransformPoint(spawnPoint.position);
        _hitLocal = _space.InverseTransformPoint(hitPoint.position);
        _despawnLocal = _space.InverseTransformPoint(despawnPoint.position);

        float distA = Vector3.Distance(_spawnLocal, _hitLocal);
        float speed = distA / _travelTime;
        float distB = Vector3.Distance(_hitLocal, _despawnLocal);
        _postTime = distB / Mathf.Max(0.0001f, speed);

        transform.position = _space.TransformPoint(_spawnLocal);
    }

    protected virtual void Update()
    {
        _elapsed += Time.deltaTime;

        if (_space == null) return;

        Vector3 localPos;
        Vector3 localDir;
        bool finished;

        EvaluateLocal(_elapsed, out localPos, out localDir, out finished);

        if (finished)
        {
            Destroy(gameObject);
            return;
        }

        if (_noteType == NoteSpawner.NoteType.Ground)
            localPos += Vector3.up * 0.05f;

        transform.position = _space.TransformPoint(localPos);

        if (_noteType != NoteSpawner.NoteType.Ground)
        {
            Vector3 worldDir = _space.TransformDirection(localDir.normalized);
            transform.rotation = Quaternion.LookRotation(worldDir, _space.up);
        }
    }

    protected void EvaluateLocal(float elapsed, out Vector3 localPos, out Vector3 localDir, out bool finished)
    {
        finished = false;

        if (!_useDespawn)
        {
            float t = Mathf.Clamp01(elapsed / _travelTime);
            localPos = Vector3.Lerp(_spawnLocal, _hitLocal, t);
            localDir = (_hitLocal - _spawnLocal);
            return;
        }

        if (elapsed <= _travelTime)
        {
            float t = Mathf.Clamp01(elapsed / _travelTime);
            localPos = Vector3.Lerp(_spawnLocal, _hitLocal, t);
            localDir = (_hitLocal - _spawnLocal);
            return;
        }

        float e2 = elapsed - _travelTime;
        float t2 = Mathf.Clamp01(e2 / Mathf.Max(0.0001f, _postTime));
        localPos = Vector3.Lerp(_hitLocal, _despawnLocal, t2);
        localDir = (_despawnLocal - _hitLocal);

        if (t2 >= 1f) finished = true;
    }
}
