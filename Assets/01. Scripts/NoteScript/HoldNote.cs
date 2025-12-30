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

    private float _holdLen;

    private Vector3 _bodyBaseScale;
    private Vector3 _bodyExtraBaseScale;

    private bool _built;

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
        _built = false;
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

        _speedLocal = GetSpeedLocal();

        if (_useDespawn)
        {
            Vector3 dir = (_despawnLocal - _hitLocal);
            if (dir.sqrMagnitude < 0.000001f) dir = (_hitLocal - _spawnLocal);
            if (dir.sqrMagnitude < 0.000001f) dir = _axisLocal;
            _postDirLocal = dir.normalized;
        }
        else
        {
            _postDirLocal = _axisLocal;
        }

        double holdSecD = _holdBeats * _secPerBeat;
        float holdSec = (float)holdSecD;

        // --- FIX: compute hold length in world units, then convert to local z units.
        // This removes "double length" caused by prefab/parent scaling.
        float worldDistA = 0f;
        float worldSpeed = 0f;

        if (_space != null)
        {
            Vector3 wSpawn = _space.TransformPoint(_spawnLocal);
            Vector3 wHit = _space.TransformPoint(_hitLocal);
            worldDistA = Vector3.Distance(wSpawn, wHit);
        }

        worldSpeed = worldDistA / Mathf.Max(0.0001f, _travelTime);

        float holdWorldLen = worldSpeed * Mathf.Max(0f, holdSec);

        float worldPerLocalZ = transform.TransformVector(Vector3.forward).magnitude;
        if (worldPerLocalZ < 0.000001f) worldPerLocalZ = 1f;

        _holdLen = holdWorldLen / worldPerLocalZ;
        // --- end FIX

        _built = true;

        SyncDspTimes();
        ApplyBodyTransform();
    }

    private void SyncDspTimes()
    {
        HeadDspTime = ExpectedHitDspTime;

        double chartTail = HeadDspTime + (_holdBeats * _secPerBeat);
        TailDspTime = chartTail;
    }

    public void StartHold()
    {
        if (IsFailed) return;
        IsActive = true;

        SyncDspTimes();

        if (_head != null)
        {
            Destroy(_head.gameObject);
            _head = null;
        }
    }

    public void SuccessAndDestroy()
    {
        if (_tail != null)
        {
            Destroy(_tail.gameObject);
            _tail = null;
        }

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

        if (_tail != null && _space != null)
        {
            Vector3 tailSpaceLocal = _space.InverseTransformPoint(_tail.position);
            float tailS = Vector3.Dot(tailSpaceLocal, _axisLocal);

            bool tailReached = (_moveSignS > 0f) ? (tailS >= _despawnS) : (tailS <= _despawnS);
            if (tailReached)
                Destroy(gameObject);
        }
    }

    private void ApplyBodyTransform()
    {
        if (_space == null) return;

        Vector3 worldMove = _space.TransformDirection(_axisLocal) * _moveSignS;

        Vector3 worldLocalFwd = transform.TransformDirection(Vector3.forward);
        float align = Mathf.Sign(Vector3.Dot(worldLocalFwd, worldMove));
        if (align == 0f) align = 1f;

        float tailSign = -align;

        Vector3 tailDirLocal = Vector3.forward * tailSign;

        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, tailDirLocal);

        Vector3 headPos = Vector3.zero;
        Vector3 tailPos = tailDirLocal * _holdLen;

        if (_head != null)
        {
            _head.localPosition = headPos;
            _head.localRotation = rot;
        }

        if (_tail != null)
        {
            _tail.localPosition = tailPos;
            _tail.localRotation = rot;
        }

        Vector3 headInner = headPos + (tailDirLocal * GetEdgeOffsetZ(_head, +1f));
        Vector3 tailInner = tailPos + (tailDirLocal * GetEdgeOffsetZ(_tail, -1f));

        float bodyLen = Mathf.Abs(Vector3.Dot(tailInner - headInner, tailDirLocal));
        Vector3 bodyCenter = (headInner + tailInner) * 0.5f;

        if (_body != null)
        {
            float minZ, maxZ;
            TryGetMeshZ(_body, out minZ, out maxZ);
            float meshLenZ = Mathf.Max(0.0001f, (maxZ - minZ));

            Vector3 sc = _bodyBaseScale;
            sc.z = Mathf.Max(0.0001f, bodyLen / meshLenZ);
            _body.localScale = sc;

            _body.localPosition = bodyCenter;
            _body.localRotation = rot;
        }

        if (_bodyExtra != null)
        {
            float minZ, maxZ;
            TryGetMeshZ(_bodyExtra, out minZ, out maxZ);
            float meshLenZ = Mathf.Max(0.0001f, (maxZ - minZ));

            Vector3 sc = _bodyExtraBaseScale;
            sc.z = Mathf.Max(0.0001f, bodyLen / meshLenZ);
            _bodyExtra.localScale = sc;

            _bodyExtra.localPosition = bodyCenter;
            _bodyExtra.localRotation = rot;
        }
    }
}
