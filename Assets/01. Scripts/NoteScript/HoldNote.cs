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
    private Vector3 _lateralLocal;

    //클리핑 관련 =======
    private Material _bodyMaterial;
    private Material _bodyExtraMaterial;
    private Material _tailMaterial;

    private static readonly int _ClipCenterID = Shader.PropertyToID("_ClipCenter");
    private static readonly int _ClipRadiusID = Shader.PropertyToID("_ClipRadius");
    private static readonly int _ClipEnabledID = Shader.PropertyToID("_ClipEnabled");
    //========

    private void Awake()
    {
        if (_body != null)
        {
            _bodyBaseScale = _body.localScale;

            // 추가: Material 인스턴스 생성
            var renderer = _body.GetComponent<Renderer>();
            if (renderer != null)
                _bodyMaterial = renderer.material;
        }

        if (_bodyExtra != null)
        {
            _bodyExtraBaseScale = _bodyExtra.localScale;

            // 추가: Material 인스턴스 생성
            var renderer = _bodyExtra.GetComponent<Renderer>();
            if (renderer != null)
                _bodyExtraMaterial = renderer.material;
        }

        // 추가: Tail Material
        if (_tail != null)
        {
            var renderer = _tail.GetComponent<Renderer>();
            if (renderer != null)
                _tailMaterial = renderer.material;
        }

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

    private Vector3 GetAxisNLocal()
    {
        Vector3 a = _axisLocal;
        if (a.sqrMagnitude < 0.000001f) a = Vector3.forward;
        return a.normalized;
    }

    public void SetupHoldBeats(double holdBeats, double secPerBeat)
    {
        _holdBeats = holdBeats < 0.0 ? 0.0 : holdBeats;
        _secPerBeat = secPerBeat <= 0.0 ? (60.0 / 120.0) : secPerBeat;

        _speedLocal = GetSpeedLocal();

        Vector3 axisN = GetAxisNLocal();
        _lateralLocal = _spawnLocal - axisN * Vector3.Dot(_spawnLocal, axisN);

        if (_useDespawn)
        {
            float dirS = Vector3.Dot((_despawnLocal - _hitLocal), axisN);
            if (Mathf.Abs(dirS) < 0.000001f) dirS = Vector3.Dot((_hitLocal - _spawnLocal), axisN);
            float sign = (dirS >= 0f) ? 1f : -1f;
            _postDirLocal = axisN * sign;
        }
        else
        {
            _postDirLocal = axisN;
        }

        double holdSecD = _holdBeats * _secPerBeat;
        float holdSec = (float)holdSecD;

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

        //클리핑 활성화
        EnableClipping(true);
    }

    //클리핑 관련 메서드 =====
    private void EnableClipping(bool enabled)
    {
        float value = enabled ? 1f : 0f;

        if (_bodyMaterial != null)
            _bodyMaterial.SetFloat(_ClipEnabledID, value);

        if (_bodyExtraMaterial != null)
            _bodyExtraMaterial.SetFloat(_ClipEnabledID, value);

        if (_tailMaterial != null)
            _tailMaterial.SetFloat(_ClipEnabledID, value);
    }

    private void UpdateClipping()
    {
        if (_space == null) return;

        // 판정선 월드 위치
        Vector3 hitWorldPos = _space.TransformPoint(_hitLocal);

        // 클리핑 반경 (조절 가능)
        float clipRadius = 0.5f;

        if (_bodyMaterial != null)
        {
            _bodyMaterial.SetVector(_ClipCenterID, hitWorldPos);
            _bodyMaterial.SetFloat(_ClipRadiusID, clipRadius);
        }

        if (_bodyExtraMaterial != null)
        {
            _bodyExtraMaterial.SetVector(_ClipCenterID, hitWorldPos);
            _bodyExtraMaterial.SetFloat(_ClipRadiusID, clipRadius);
        }

        if (_tailMaterial != null)
        {
            _tailMaterial.SetVector(_ClipCenterID, hitWorldPos);
            _tailMaterial.SetFloat(_ClipRadiusID, clipRadius);
        }
    }
    // ===================================

    public void SuccessAndDestroy()
    {
        // 클리핑 비활성화
        EnableClipping(false);

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

        // 클리핑 비활성화 (실패 시 전체 보이게)
        EnableClipping(false);

        if (palette != null)
        {
            Color c;
            if (palette.TryGetHoldFailColor(laneType, out c))
                ApplyTint(c);
        }
    }

    private void ApplyTint(Color c)
    {
        var pss = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in pss)
        {
            if (ps == null) continue;

            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(c);

            var col = ps.colorOverLifetime;
            if (col.enabled)
            {
                Gradient newGrad = new Gradient();
                newGrad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
                );
                col.color = new ParticleSystem.MinMaxGradient(newGrad);
            }
        }
    }

    private Vector3 EvaluateHeadLocalUnclamped(float elapsed)
    {
        Vector3 axisN = GetAxisNLocal();

        float spawnS = Vector3.Dot(_spawnLocal, axisN);
        float hitS = Vector3.Dot(_hitLocal, axisN);
        float despawnS = Vector3.Dot(_despawnLocal, axisN);

        if (!_useDespawn)
        {
            float t = Mathf.Clamp01(elapsed / _travelTime);
            float s = Mathf.Lerp(spawnS, hitS, t);
            return _lateralLocal + axisN * s;
        }

        if (elapsed <= _travelTime)
        {
            float t = Mathf.Clamp01(elapsed / _travelTime);
            float s = Mathf.Lerp(spawnS, hitS, t);
            return _lateralLocal + axisN * s;
        }

        if (elapsed <= (_travelTime + _postTime))
        {
            float e2 = elapsed - _travelTime;
            float t2 = Mathf.Clamp01(e2 / Mathf.Max(0.0001f, _postTime));
            float s = Mathf.Lerp(hitS, despawnS, t2);
            return _lateralLocal + axisN * s;
        }

        float extra = elapsed - (_travelTime + _postTime);

        float sign = (Vector3.Dot(_postDirLocal, axisN) >= 0f) ? 1f : -1f;
        float sExtra = despawnS + (sign * _speedLocal * extra);
        return _lateralLocal + axisN * sExtra;
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

        // Hold 활성화 시 클리핑 업뎃
        if (IsActive && !IsFailed)
            UpdateClipping();

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