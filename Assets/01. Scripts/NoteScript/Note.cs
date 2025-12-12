using UnityEngine;

public class Note : MonoBehaviour
{
    protected Vector3 _spawnPos;
    protected Vector3 _hitPos;
    protected float _travelTime;
    protected float _elapsed;
    protected NoteSpawner.NoteType _noteType;

    public virtual void Init(Vector3 spawnPos, Vector3 hitPos, float travelTime, NoteSpawner.NoteType noteType)
    {
        _spawnPos = spawnPos;
        _hitPos = hitPos;
        _travelTime = Mathf.Max(0.0001f, travelTime);
        _noteType = noteType;
        _elapsed = 0f;

        transform.position = _spawnPos;
    }

    protected virtual void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _travelTime);

        transform.position = Vector3.Lerp(_spawnPos, _hitPos, t);

        if (t >= 1f)
            Destroy(gameObject);
    }
}
