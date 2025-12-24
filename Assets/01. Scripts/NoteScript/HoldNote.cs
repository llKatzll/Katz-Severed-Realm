using UnityEngine;

public class HoldNote : Note
{
    [SerializeField] private Transform _head;
    [SerializeField] private Transform _body;
    [SerializeField] private Transform _tail;
    [SerializeField] private Transform _bodyExtra;

    public double HeadDspTime { get; private set; }
    public double TailDspTime { get; private set; }

    public bool IsActive { get; private set; }
    public bool IsFailed { get; private set; }

    private double _holdBeats;
    private double _secPerBeat;

    private float _holdLenZ;
    private Vector3 _bodyBaseScale;
    private Vector3 _bodyExtraBaseScale;

    private bool _built;

    private float _moveSignZ;
    private float _speedZ;
    private float _speedLocal;

    private Vector3 _postDirLocal;

    private Renderer[] _renderers;

    private void Awake()
    {
        if (_body != null) _bodyBaseScale = _body.localScale;
        if (_bodyExtra != null) _bodyExtraBaseScale = _bodyExtra.localScale;

        _renderers = GetComponentsInChildren<Renderer>(true);

        IsActive = false;
        IsFailed = false;
    }

    private static bool TryGetMeshZ(Transform t, out float minZ, out float maxZ)
    {
        minZ = -0.05f;
        maxZ = 0.05f;

        if (t == null) return false;

        var mf = t.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return false;

        var b = mf.sharedMesh.bounds;
        minZ = b.min.z;
        maxZ = b.max.z;
        return true;
    }

    private static float GetEdgeOffsetZ(Transform t, float dir)
    {
        float minZ, maxZ;
        if (!TryGetMeshZ(t, out minZ, out maxZ)) return 0.05f * dir;

        float edge = (dir >= 0f) ? maxZ : minZ;
        float sc = (t != null) ? t.localScale.z : 1f;
        return edge * sc;
    }

    public new void SetExpectedHitDspTime(double hitDspTime)
    {
        base.SetExpectedHitDspTime(hitDspTime);
        SyncDspTimes();
    }

    public void SetupHoldBeats(double holdBeats, double secPerBeat)
    {
        _holdBeats = holdBeats < 0.0 ? 0.0 : holdBeats;
        _secPerBeat = secPerBeat <= 0.0 ? (60.0 / 120.0) : secPerBeat;

        _moveSignZ = Mathf.Sign(_hitZ - _spawnZ);
        if (_moveSignZ == 0f) _moveSignZ = 1f;

        _speedZ = GetSpeedLocalZ();

        float distA = Vector3.Distance(_spawnLocal, _hitLocal);
        _speedLocal = distA / Mathf.Max(0.0001f, _travelTime);

        if (_useDespawn)
        {
            Vector3 dir = (_despawnLocal - _hitLocal);
            if (dir.sqrMagnitude < 0.000001f) dir = (_hitLocal - _spawnLocal);
            _postDirLocal = dir.normalized;
        }
        else
        {
            _postDirLocal = Vector3.forward;
        }

        double holdSec = _holdBeats * _secPerBeat;
        _holdLenZ = _speedZ * (float)holdSec;

        _built = true;

        SyncDspTimes();
        ApplyBodyTransform();

        //Å×½ºÆ®·Î ¾Æ²¸µÒ
        //Debug.Log(
        //   "spawnLocal=" + _spawnLocal +
        //   " hitLocal=" + _hitLocal +
        //   " despawnLocal=" + _despawnLocal
        //);

    }

    private void SyncDspTimes()
    {
        HeadDspTime = ExpectedHitDspTime;
        TailDspTime = HeadDspTime + (_holdBeats * _secPerBeat);
    }

    public void StartHold()
    {
        if (IsFailed) return;
        IsActive = true;
    }

    public void SuccessAndDestroy()
    {
        Destroy(gameObject);
    }

    public void Fail(HitFxPaletteSO palette, NoteSpawner.NoteType laneType)
    {
        if (IsFailed) return;

        IsFailed = true;
        IsActive = false;

        if (palette != null)
        {
            Color c;
            if (palette.TryGetHoldFailColor(laneType, out c))
                ApplyTint(c);
        }
    }

    private void ApplyTint(Color c)
    {
        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;

                var mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);

                mpb.SetColor("_BaseColor", c);
                mpb.SetColor("_Color", c);
                mpb.SetColor("_TintColor", c);
                mpb.SetColor("_EmissionColor", c);
                mpb.SetColor("_StartColor", c);

                r.SetPropertyBlock(mpb);
            }
        }

        var pss = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < pss.Length; i++)
        {
            var ps = pss[i];
            if (ps == null) continue;
            var main = ps.main;
            main.startColor = c;
        }
    }

    private Vector3 EvaluateHeadLocalUnclamped(float elapsed)
    {
        if (!_useDespawn)
        {
            float t = Mathf.Clamp01(elapsed / _travelTime);
            return Vector3.Lerp(_spawnLocal, _hitLocal, t);
        }

        if (elapsed <= _travelTime)
        {
            float t = Mathf.Clamp01(elapsed / _travelTime);
            return Vector3.Lerp(_spawnLocal, _hitLocal, t);
        }

        if (elapsed <= (_travelTime + _postTime))
        {
            float e2 = elapsed - _travelTime;
            float t2 = Mathf.Clamp01(e2 / Mathf.Max(0.0001f, _postTime));
            return Vector3.Lerp(_hitLocal, _despawnLocal, t2);
        }

        float extra = elapsed - (_travelTime + _postTime);
        return _despawnLocal + (_postDirLocal * _speedLocal * extra);
    }

    protected override void Update()
    {
        if (_space == null) return;

        float headElapsed = (float)(AudioSettings.dspTime - _spawnDspTime);
        if (headElapsed < 0f) headElapsed = 0f;

        Vector3 headLocal = EvaluateHeadLocalUnclamped(headElapsed);
        headLocal.y += _yOffsetLocal;

        transform.position = _space.TransformPoint(headLocal);

        if (_rotateSource != null)
            transform.rotation = _rotateSource.rotation;

        if (_built)
            ApplyBodyTransform();

        if (!_useDespawn)
            return;

        float tailLocalZ = (_tail != null) ? _tail.localPosition.z : (-_moveSignZ * _holdLenZ);
        float tailFrontOffset = GetEdgeOffsetZ(_tail, _moveSignZ);

        float tailFrontZ = headLocal.z + tailLocalZ + tailFrontOffset;

        bool tailReached = (_moveSignZ > 0f) ? (tailFrontZ >= _despawnZ) : (tailFrontZ <= _despawnZ);
        if (tailReached)
            Destroy(gameObject);
    }

    private void ApplyBodyTransform()
    {
        float dirToTail = -_moveSignZ;
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

        float headInnerZ = GetEdgeOffsetZ(_head, dirToTail);
        float tailInnerZ = tailZ + GetEdgeOffsetZ(_tail, -dirToTail);

        float bodyLen = Mathf.Abs(tailInnerZ - headInnerZ);
        float bodyCenterZ = (headInnerZ + tailInnerZ) * 0.5f;

        if (_body != null)
        {
            _body.localPosition = new Vector3(0f, 0f, bodyCenterZ);

            float minZ, maxZ;
            TryGetMeshZ(_body, out minZ, out maxZ);
            float meshLenZ = Mathf.Max(0.0001f, (maxZ - minZ));

            Vector3 sc = _bodyBaseScale;
            sc.z = Mathf.Max(0.0001f, bodyLen / meshLenZ);
            _body.localScale = sc;
        }

        if (_bodyExtra != null)
        {
            _bodyExtra.localPosition = new Vector3(0f, 0f, bodyCenterZ);

            float minZ, maxZ;
            TryGetMeshZ(_bodyExtra, out minZ, out maxZ);
            float meshLenZ = Mathf.Max(0.0001f, (maxZ - minZ));

            Vector3 sc = _bodyExtraBaseScale;
            sc.z = Mathf.Max(0.0001f, bodyLen / meshLenZ);
            _bodyExtra.localScale = sc;
        }
    }

    private void OnDestroy()
    {
        Debug.Log("HoldNote destroyed: " + name);
    }
}
