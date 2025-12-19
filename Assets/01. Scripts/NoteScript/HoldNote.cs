using UnityEngine;

public class HoldNote : Note
{
    [Header("Visual Parts")]
    [SerializeField] private Transform _start;
    [SerializeField] private Transform _middle;
    [SerializeField] private Transform _end;
    [SerializeField] private Transform _middleExtraLine; // optional (Upper)

    [Header("Hold")]
    [SerializeField] private float _holdDurationSec = 1.0f;

    [Header("Colliders (Head/Tail only)")]
    [SerializeField] private Collider _headCollider;
    [SerializeField] private Collider _tailCollider;

    public double HeadDspTime { get; private set; }
    public double TailDspTime { get; private set; }

    public bool IsActive { get; private set; }
    public bool IsFailed { get; private set; }

    private Vector3 _holdVecLocal;
    private Vector3 _midBaseScale;
    private Vector3 _extraBaseScale;

    private void Awake()
    {
        IsActive = false;
        IsFailed = false;

        if (_middle != null) _midBaseScale = _middle.localScale;
        if (_middleExtraLine != null) _extraBaseScale = _middleExtraLine.localScale;
    }

    public void SetupHoldDuration(float holdDurationSec)
    {
        _holdDurationSec = Mathf.Max(0f, holdDurationSec);

        HeadDspTime = ExpectedHitDspTime;
        TailDspTime = HeadDspTime + _holdDurationSec;

        float distA = Vector3.Distance(_spawnLocal, _hitLocal);
        float speed = distA / Mathf.Max(0.0001f, _travelTime);
        float holdLen = speed * _holdDurationSec;

        Vector3 fwdLocal = (_hitLocal - _spawnLocal).normalized;
        _holdVecLocal = fwdLocal * holdLen;

        ApplyBodyTransform();
    }

    public void StartHold()
    {
        if (IsFailed) return;
        IsActive = true;

        // After head success, only tail matters
        if (_headCollider != null) _headCollider.enabled = false;
    }

    public void Fail()
    {
        if (IsFailed) return;

        IsFailed = true;
        IsActive = false;

        SetFailedOpacity(0.5f);

        if (_headCollider != null) _headCollider.enabled = false;
        if (_tailCollider != null) _tailCollider.enabled = false;
    }

    public void SuccessAndDestroy()
    {
        Destroy(gameObject);
    }

    protected override void Update()
    {
        _elapsed += Time.deltaTime;
        if (_space == null) return;

        // Head drives the root movement
        Vector3 headLocalPos, headLocalDir;
        bool headFinished;
        EvaluateLocal(_elapsed, out headLocalPos, out headLocalDir, out headFinished);

        if (_noteType == NoteSpawner.NoteType.Ground)
            headLocalPos += Vector3.up * 0.05f;

        transform.position = _space.TransformPoint(headLocalPos);

        if (_noteType != NoteSpawner.NoteType.Ground)
        {
            Vector3 worldDir = _space.TransformDirection(headLocalDir.normalized);
            transform.rotation = Quaternion.LookRotation(worldDir, _space.up);
        }

        // Destroy when tail passed despawn, not head
        if (_useDespawn)
        {
            float tailElapsed = _elapsed - _holdDurationSec;
            if (tailElapsed >= (_travelTime + _postTime))
            {
                Destroy(gameObject);
                return;
            }
        }

        ApplyBodyTransform();
    }

    private void ApplyBodyTransform()
    {
        // Root is at head, end is behind head
        if (_start != null) _start.localPosition = Vector3.zero;

        if (_end != null) _end.localPosition = -_holdVecLocal;

        if (_middle != null)
        {
            _middle.localPosition = -_holdVecLocal * 0.5f;

            float len = _holdVecLocal.magnitude;
            Vector3 s = _midBaseScale;
            s.z = Mathf.Max(0.0001f, len);
            _middle.localScale = s;
        }

        if (_middleExtraLine != null)
        {     
            _middleExtraLine.localPosition = -_holdVecLocal * 0.5f;

            float len = _holdVecLocal.magnitude;
            Vector3 s = _extraBaseScale;
            s.z = Mathf.Max(0.0001f, len);
            _middleExtraLine.localScale = s;
        }
    }

    private void SetFailedOpacity(float alpha01)
    {
        float a = Mathf.Clamp01(alpha01);

        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;

            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);

            Color baseC = Color.white;
            var mat = r.sharedMaterial;
            if (mat != null)
            {
                if (mat.HasProperty("_BaseColor")) baseC = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color")) baseC = mat.GetColor("_Color");
                else if (mat.HasProperty("_TintColor")) baseC = mat.GetColor("_TintColor");
            }

            baseC.a = a;

            mpb.SetColor("_BaseColor", baseC);
            mpb.SetColor("_Color", baseC);
            mpb.SetColor("_TintColor", baseC);

            r.SetPropertyBlock(mpb);
        }
    }
}
