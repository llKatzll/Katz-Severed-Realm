using UnityEngine;

public class Note : MonoBehaviour
{
    protected float _travelTime;// Spawn -> Hit 시간
    protected float _elapsed;
    protected NoteSpawner.NoteType _noteType;

    // Follow용(레일 기준 공간)
    private Transform _space;
    private Vector3 _spawnLocal;
    private Vector3 _hitLocal;

    // 통과(Spawn -> Hit -> Despawn)
    private bool _useDespawn;
    private Vector3 _despawnLocal;
    private float _postTime;// Hit -> Despawn 시간(속도 유지)

    private double _spawnDspTime;
    public double ExpectedHitDspTime { get; private set; }

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

    // Spawn -> Hit 통과 -> Despawn
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

        // Spawn->Hit 속도 유지로 Hit->Despawn 시간 계산
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

        if (_useDespawn)
        {
            if (_elapsed <= _travelTime)
            {
                float t = Mathf.Clamp01(_elapsed / _travelTime);
                localPos = Vector3.Lerp(_spawnLocal, _hitLocal, t);
                localDir = (_hitLocal - _spawnLocal);
            }
            else
            {
                float e2 = _elapsed - _travelTime;
                float t2 = Mathf.Clamp01(e2 / Mathf.Max(0.0001f, _postTime));
                localPos = Vector3.Lerp(_hitLocal, _despawnLocal, t2);
                localDir = (_despawnLocal - _hitLocal);

                // 삭제는 Despawn에서만
                if (t2 >= 1f)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }
        else
        {
            float t = Mathf.Clamp01(_elapsed / _travelTime);
            localPos = Vector3.Lerp(_spawnLocal, _hitLocal, t);
            localDir = (_hitLocal - _spawnLocal);
        }

        // Ground만 로컬로 살짝 올리기
        if (_noteType == NoteSpawner.NoteType.Ground)
            localPos += Vector3.up * 0.05f;

        transform.position = _space.TransformPoint(localPos);

        // 회전: Upper는 진행방향 회전, Ground는 기울기 유지라면 회전 덮어쓰기 금지
        if (_noteType != NoteSpawner.NoteType.Ground)
        {
            Vector3 worldDir = _space.TransformDirection(localDir.normalized);
            transform.rotation = Quaternion.LookRotation(worldDir, _space.up);
        }
    }
}
