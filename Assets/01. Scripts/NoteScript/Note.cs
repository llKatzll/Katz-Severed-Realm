using UnityEngine;

public class Note : MonoBehaviour
{
    protected float _travelTime;
    protected NoteSpawner.NoteType _noteType;

    protected Transform _space;
    protected Transform _rotateSource;

    protected Transform _spawnPointRef;
    protected Transform _hitPointRef;
    protected Transform _despawnPointRef;

    protected Vector3 _spawnLocal;
    protected Vector3 _hitLocal;
    protected Vector3 _despawnLocal;

    protected bool _useDespawn;
    protected float _postTime;

    protected float _yOffsetLocal;

    protected double _spawnDspTime;
    public double ExpectedHitDspTime { get; protected set; }

    protected Vector3 _axisLocal;
    protected float _spawnS;
    protected float _hitS;
    protected float _despawnS;
    protected float _moveSignS;

    public NoteSpawner.NoteType NoteType => _noteType;
    public Transform HitPointRef => _hitPointRef;
    public bool IsDimensionNote { get; private set; }

    public void MarkAsDimensionNote()
    {
        IsDimensionNote = true;
    }

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

        _spawnPointRef = spawnPoint;
        _hitPointRef = hitPoint;
        _despawnPointRef = despawnPoint;

        _useDespawn = (despawnPoint != null);

        _travelTime = Mathf.Max(0.0001f, travelTime);
        _noteType = noteType;

        _yOffsetLocal = yOffsetLocal;

        UpdateLocalPositions();

        Vector3 axis = _hitLocal - _spawnLocal;
        if (axis.sqrMagnitude < 0.000001f) axis = Vector3.forward;
        _axisLocal = axis.normalized;

        _spawnS = Vector3.Dot(_spawnLocal, _axisLocal);
        _hitS = Vector3.Dot(_hitLocal, _axisLocal);
        _despawnS = Vector3.Dot(_despawnLocal, _axisLocal);

        _moveSignS = Mathf.Sign(_hitS - _spawnS);
        if (_moveSignS == 0f) _moveSignS = 1f;

        float distA = Vector3.Distance(_spawnLocal, _hitLocal);
        float speed = distA / _travelTime;

        if (_useDespawn)
        {
            float distB = Vector3.Distance(_hitLocal, _despawnLocal);
            _postTime = distB / Mathf.Max(0.0001f, speed);
        }
        else
        {
            _postTime = 0f;
        }

        _spawnDspTime = RhythmConductor.Now;
        ExpectedHitDspTime = _spawnDspTime + _travelTime;

        Vector3 local0 = _spawnLocal;
        local0.y += _yOffsetLocal;
        transform.position = _space.TransformPoint(local0);

        if (_rotateSource != null)
            transform.rotation = _rotateSource.rotation;
    }

    protected void UpdateLocalPositions()
    {
        if (_space == null) return;

        if (_spawnPointRef != null)
            _spawnLocal = _space.InverseTransformPoint(_spawnPointRef.position);

        if (_hitPointRef != null)
            _hitLocal = _space.InverseTransformPoint(_hitPointRef.position);

        if (_useDespawn && _despawnPointRef != null)
            _despawnLocal = _space.InverseTransformPoint(_despawnPointRef.position);
        else
            _despawnLocal = _hitLocal;
    }

    public void ApplyColor(Color color)
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            var mat = renderers[i].material;
            if (mat == null) continue;

            mat.color = color;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            if (mat.HasProperty("_TintColor"))
                mat.SetColor("_TintColor", color);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
        }

        var particles = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null) continue;
            var main = particles[i].main;
            main.startColor = new ParticleSystem.MinMaxGradient(color);
        }
    }

    public void SetSortingOrder(int order)
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].sortingOrder = order;
        }
    }

    public void SetExpectedHitDspTime(double hitDspTime)
    {
        ExpectedHitDspTime = hitDspTime;
        _spawnDspTime = ExpectedHitDspTime - _travelTime;
    }

    protected virtual void Update()
    {
        if (_space == null) return;

        UpdateLocalPositions();

        float elapsed = (float)(RhythmConductor.Now - _spawnDspTime);
        if (elapsed < 0f) elapsed = 0f;

        Vector3 localPos;
        bool finished;
        EvaluateLocal(elapsed, out localPos, out finished);

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

    public float GetSpeedLocal()
    {
        float distA = Vector3.Distance(_spawnLocal, _hitLocal);
        return distA / Mathf.Max(0.0001f, _travelTime);
    }

    protected virtual void OnDestroy()
    {
    }
}