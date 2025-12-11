using UnityEngine;

public class Note : MonoBehaviour
{
    protected float _speed;
    protected Transform _despawnPoint;
    protected NoteSpawner.NoteType _noteType;

    public virtual void Init(float speed, Transform despawnPoint, NoteSpawner.NoteType noteType)
    {
        _speed = speed;
        _despawnPoint = despawnPoint;
        _noteType = noteType;
    }

    protected virtual void Update()
    {
        //Go downward
        transform.Translate(Vector3.back * _speed * Time.deltaTime, Space.Self);

        if (_despawnPoint != null &&
            transform.position.z <= _despawnPoint.position.z)
        {
            Destroy(gameObject);
        }
    }
}
