using UnityEngine;

public class HoldNote : Note
{
    [SerializeField] private Transform _head;
    [SerializeField] private Transform _body;
    [SerializeField] private Transform _tail;
    [SerializeField] private Transform _bodyExtra;

    private double _holdBeats;
    private double _secPerBeat;

    private float _holdLenZ;
    private Vector3 _bodyBaseScale;
    private Vector3 _bodyExtraBaseScale;

    private float _headMeshLenZ = 0.1f;
    private float _tailMeshLenZ = 0.1f;

    private bool _built;

    private void Awake()
    {
        if (_body != null) _bodyBaseScale = _body.localScale;
        if (_bodyExtra != null) _bodyExtraBaseScale = _bodyExtra.localScale;

        _headMeshLenZ = GetMeshLenZ(_head);
        _tailMeshLenZ = GetMeshLenZ(_tail);
    }

    private float GetMeshLenZ(Transform t)
    {
        if (t == null) return 0.1f;
        var mf = t.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return 0.1f;
        return Mathf.Max(0.0001f, mf.sharedMesh.bounds.size.z);
    }

    public void SetupHoldBeats(double holdBeats, double secPerBeat)
    {
        _holdBeats = holdBeats < 0.0 ? 0.0 : holdBeats;
        _secPerBeat = secPerBeat <= 0.0 ? (60.0 / 120.0) : secPerBeat;

        double holdSec = _holdBeats * _secPerBeat;

        float speedZ = GetSpeedLocalZ();
        _holdLenZ = speedZ * (float)holdSec;

        _built = true;
        ApplyBodyTransform();
    }

    protected override void Update()
    {
        if (_space == null) return;

        float headElapsed = (float)(AudioSettings.dspTime - _spawnDspTime);
        if (headElapsed < 0f) headElapsed = 0f;

        Vector3 headLocal;
        bool headFinished;
        EvaluateLocal(headElapsed, out headLocal, out headFinished);

        headLocal.y += _yOffsetLocal;
        transform.position = _space.TransformPoint(headLocal);

        if (_rotateSource != null)
            transform.rotation = _rotateSource.rotation;

        if (_built) ApplyBodyTransform();

        double holdSec = _holdBeats * _secPerBeat;
        float tailElapsed = headElapsed - (float)holdSec;
        if (tailElapsed < 0f) return;

        Vector3 tailLocal;
        bool tailFinished;
        EvaluateLocal(tailElapsed, out tailLocal, out tailFinished);

        if (tailFinished) Destroy(gameObject);
    }

    private void ApplyBodyTransform()
    {
        float moveDir = Mathf.Sign(_hitZ - _spawnZ);
        if (moveDir == 0f) moveDir = 1f;

        float dirToTail = -moveDir;
        float tailZ = dirToTail * _holdLenZ;

        if (_head != null)
        {
            _head.localPosition = Vector3.zero;
            _head.localRotation = Quaternion.identity;
        }

        if (_tail != null)
        {
            _tail.localPosition = new Vector3(0f, 0f, tailZ);
            _tail.localRotation = Quaternion.identity;
        }

        float headHalf = _headMeshLenZ * 0.5f;
        float tailHalf = _tailMeshLenZ * 0.5f;

        float headInnerZ = dirToTail * headHalf;
        float tailInnerZ = tailZ - dirToTail * tailHalf;

        float bodyLen = Mathf.Abs(tailInnerZ - headInnerZ);
        float bodyCenterZ = (headInnerZ + tailInnerZ) * 0.5f;

        if (_body != null)
        {
            _body.localPosition = new Vector3(0f, 0f, bodyCenterZ);

            float meshLenZ = Mathf.Max(0.0001f, GetMeshLenZ(_body));
            Vector3 sc = _bodyBaseScale;
            sc.z = Mathf.Max(0.0001f, bodyLen / meshLenZ);
            _body.localScale = sc;
        }

        if (_bodyExtra != null)
        {
            _bodyExtra.localPosition = new Vector3(0f, 0f, bodyCenterZ);

            float meshLenZ = Mathf.Max(0.0001f, GetMeshLenZ(_bodyExtra));
            Vector3 sc = _bodyExtraBaseScale;
            sc.z = Mathf.Max(0.0001f, bodyLen / meshLenZ);
            _bodyExtra.localScale = sc;
        }
    }
}
