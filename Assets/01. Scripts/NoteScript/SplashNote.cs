using UnityEngine;

public class SplashNote : Note
{
    [Header("Scale Settings")]
    [SerializeField] private float _startScale = 0.5f;
    [SerializeField] private float _endScale = 3f;
    [SerializeField] private float _lifeTime = 1.2f;

    private float _elapsed;

    public override void Init(float speed, Transform despawnPoint, NoteSpawner.NoteType noteType)
    {
        //Not using rn
        base.Init(speed, despawnPoint, noteType);

        _elapsed = 0f;
        transform.localScale = Vector3.one * _startScale;
    }   

    //customize only moves
    protected override void Update()
    {
        _elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(_elapsed / _lifeTime);
        float currentScale = Mathf.Lerp(_startScale, _endScale, t);
        transform.localScale = Vector3.one * currentScale;

        if (_elapsed >= _lifeTime)
        {
            Destroy(gameObject);
        }
    }
}
