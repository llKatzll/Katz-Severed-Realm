using UnityEngine;

public class HoldNote : Note
{
    [Header("Parts")]
    [SerializeField] private Transform _head;
    [SerializeField] private Transform _body;
    [SerializeField] private Transform _tail;
    [SerializeField] private Transform _bodyExtra;

    [Header("Hold")]
    [SerializeField] private float _holdSeconds = 0.2f;

    public double HeadDspTime { get; private set; }
    public double TailDspTime { get; private set; }

    public bool IsActive { get; private set; }
    public bool IsFailed { get; private set; }

    private Vector3 _bodyBaseScale;
    private Vector3 _bodyExtraBaseScale;

    private void Awake()
    {
        if (!Application.isPlaying) return;

        IsActive = false;
        IsFailed = false;

        if (_body != null) _bodyBaseScale = _body.localScale;
        if (_bodyExtra != null) _bodyExtraBaseScale = _bodyExtra.localScale;
    }

    public void SetupHoldSeconds(float holdSeconds)
    {
        if (!Application.isPlaying) return;

        _holdSeconds = Mathf.Max(0f, holdSeconds);

        HeadDspTime = ExpectedHitDspTime;
        TailDspTime = HeadDspTime + _holdSeconds;

        // first build apply once
        ApplyBodyTransformFromTime();
    }

    public void StartHold()
    {
        if (!Application.isPlaying) return;
        if (IsFailed) return;
        IsActive = true;
    }

    public void Fail()
    {
        if (!Application.isPlaying) return;
        if (IsFailed) return;

        IsFailed = true;
        IsActive = false;
    }

    public void SuccessAndDestroy()
    {
        if (!Application.isPlaying) return;
        Destroy(gameObject);
    }

    protected override void Update()
    {
        if (!Application.isPlaying) return;

        _elapsed += Time.deltaTime;
        if (_space == null) return;

        Vector3 headLocalPos;
        bool headFinished;
        EvaluateLocal(_elapsed, out headLocalPos, out headFinished);

        float tailElapsed = _elapsed - _holdSeconds;

        if (tailElapsed < 0f) tailElapsed = 0f;

        Vector3 tailLocalPos;
        bool tailFinished;
        EvaluateLocal(tailElapsed, out tailLocalPos, out tailFinished);

        if (_useDespawn)
        {
            float tailEnd = _travelTime + _postTime;
            if (tailElapsed >= tailEnd)
            {
                Destroy(gameObject);
                return;
            }
        }

        // apply y offset
        headLocalPos += Vector3.up * _yOffsetLocal;

        transform.position = _space.TransformPoint(headLocalPos);

        if (_rotateSource != null)
            transform.rotation = _rotateSource.rotation;

        ApplyBodyTransform(headLocalPos, tailLocalPos);
    }

    private void ApplyBodyTransformFromTime()
    {
        if (_space == null) return;

        Vector3 headLocalPos;
        bool headFinished;
        EvaluateLocal(_elapsed, out headLocalPos, out headFinished);

        float tailElapsed = _elapsed - _holdSeconds;
        if (tailElapsed < 0f) tailElapsed = 0f;

        Vector3 tailLocalPos;
        bool tailFinished;
        EvaluateLocal(tailElapsed, out tailLocalPos, out tailFinished);

        ApplyBodyTransform(headLocalPos, tailLocalPos);
    }

    private void ApplyBodyTransform(Vector3 headLocalPos, Vector3 tailLocalPos)
    {
        Vector3 tailOffsetLocal = (tailLocalPos - headLocalPos);

        if (_head != null)
        {
            _head.localPosition = Vector3.zero;
            _head.localRotation = Quaternion.identity;
        }

        if (_tail != null)
        {
            _tail.localPosition = tailOffsetLocal;
            _tail.localRotation = Quaternion.identity;
        }

        float len = tailOffsetLocal.magnitude;
        if (len < 0.0001f) len = 0.0001f;

        Vector3 mid = tailOffsetLocal * 0.5f;

        if (_body != null)
        {
            _body.localPosition = mid;

            Vector3 s = _bodyBaseScale;
            s.z = len;
            _body.localScale = s;
        }

        if (_bodyExtra != null)
        {
            _bodyExtra.localPosition = mid;

            Vector3 s = _bodyExtraBaseScale;
            s.z = len;
            _bodyExtra.localScale = s;
        }
    }
}
