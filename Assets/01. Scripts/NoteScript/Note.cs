using UnityEngine;

public class Note : MonoBehaviour
{
    protected Vector3 _spawnPos;
    protected Vector3 _hitPos;
    protected float _travelTime;
    protected NoteSpawner.NoteType _noteType;

    private double _startTime;

    public virtual void Init(Vector3 spawnPos, Vector3 hitPos, float travelTime, NoteSpawner.NoteType noteType)
    {
        _spawnPos = spawnPos;
        _hitPos = hitPos;
        _travelTime = Mathf.Max(0.0001f, travelTime);
        _noteType = noteType;

        _startTime = Time.timeAsDouble;
        transform.position = _spawnPos;
    }

    protected virtual void Update()
    {
        float t = (float)((Time.timeAsDouble - _startTime) / _travelTime);
        t = Mathf.Clamp01(t);

        Apply(t);

        if (t >= 1f)
            Destroy(gameObject);
    }

    protected virtual void Apply(float t)
    {
        transform.position = Vector3.Lerp(_spawnPos, _hitPos, t);
    }
}
