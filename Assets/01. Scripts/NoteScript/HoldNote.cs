using UnityEngine;

public class HoldNote : Note
{
    [SerializeField] private Transform _head;
    [SerializeField] private Transform _body;
    [SerializeField] private Transform _tail;

    public double HeadDspTime { get; private set; }
    public double TailDspTime { get; private set; }

    public bool IsActive { get; private set; }
    public bool IsFailed { get; private set; }

    private double _holdBeats;
    private double _secPerBeat;

    private float _holdLen;

    private Vector3 _bodyBaseScale;

    private bool _built;

    private float _speedLocal;
    private Vector3 _postDirLocal;

    private Vector3 _lateralLocal;

    private double _totalHoldDuration;
    private double _holdStartDspTime;

    private float _savedBodyLen = -1f;

    private void Awake()
    {
        if (_body != null)
            _bodyBaseScale = _body.localScale;

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

        float holdSec = (float)(_holdBeats * _secPerBeat);
        _totalHoldDuration = holdSec;

        float worldDistA = 0f;

        if (_space != null)
        {
            Vector3 wSpawn = _space.TransformPoint(_spawnLocal);
            Vector3 wHit = _space.TransformPoint(_hitLocal);
            worldDistA = Vector3.Distance(wSpawn, wHit);
        }

        float worldSpeed = worldDistA / Mathf.Max(0.0001f, _travelTime);

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

        _holdStartDspTime = AudioSettings.dspTime;
        _savedBodyLen = -1f;

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

        if (IsActive && _totalHoldDuration > 0)
        {
            double elapsed = AudioSettings.dspTime - _holdStartDspTime;
            float ratio = Mathf.Clamp01((float)(elapsed / _totalHoldDuration));
            _savedBodyLen = _holdLen * (1f - ratio);
        }

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
        Vector3 spawnLocal = _spawnLocal;
        Vector3 hitLocal = _hitLocal;
        Vector3 despawnLocal = _despawnLocal;

        if (_spawnPointRef != null && _space != null)
            spawnLocal = _space.InverseTransformPoint(_spawnPointRef.position);
        if (_hitPointRef != null && _space != null)
            hitLocal = _space.InverseTransformPoint(_hitPointRef.position);
        if (_despawnPointRef != null && _space != null)
            despawnLocal = _space.InverseTransformPoint(_despawnPointRef.position);

        Vector3 result;

        if (!_useDespawn)
        {
            float t = Mathf.Clamp01(elapsed / _travelTime);
            result = Vector3.Lerp(spawnLocal, hitLocal, t);
        }
        else if (elapsed <= _travelTime)
        {
            float t = Mathf.Clamp01(elapsed / _travelTime);
            result = Vector3.Lerp(spawnLocal, hitLocal, t);
        }
        else if (elapsed <= (_travelTime + _postTime))
        {
            float e2 = elapsed - _travelTime;
            float t2 = Mathf.Clamp01(e2 / Mathf.Max(0.0001f, _postTime));
            result = Vector3.Lerp(hitLocal, despawnLocal, t2);
        }
        else
        {
            float extra = elapsed - (_travelTime + _postTime);
            Vector3 direction = (despawnLocal - hitLocal).normalized;
            if (direction.sqrMagnitude < 0.0001f)
                direction = (hitLocal - spawnLocal).normalized;

            float speed = Vector3.Distance(spawnLocal, hitLocal) / Mathf.Max(0.0001f, _travelTime);
            result = despawnLocal + direction * (speed * extra);
        }

        result.y = hitLocal.y;

        return result;
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
            Vector3 tailWorldPos = _tail.position;
            Vector3 tailSpaceLocal = _space.InverseTransformPoint(tailWorldPos);
            float tailS = Vector3.Dot(tailSpaceLocal, _axisLocal);

            bool tailReached = (_moveSignS > 0f) ? (tailS >= _despawnS) : (tailS <= _despawnS);
            if (tailReached)
                Destroy(gameObject);
        }
    }

    private void ApplyBodyTransform()
    {
        if (_space == null) return;

        Vector3 spawnLocal = _spawnLocal;
        Vector3 hitLocal = _hitLocal;

        if (_spawnPointRef != null)
            spawnLocal = _space.InverseTransformPoint(_spawnPointRef.position);
        if (_hitPointRef != null)
            hitLocal = _space.InverseTransformPoint(_hitPointRef.position);

        Vector3 currentAxis = (hitLocal - spawnLocal).normalized;
        if (currentAxis.sqrMagnitude < 0.0001f) currentAxis = Vector3.forward;

        float currentMoveSign = Mathf.Sign(Vector3.Dot(hitLocal, currentAxis) - Vector3.Dot(spawnLocal, currentAxis));
        if (currentMoveSign == 0f) currentMoveSign = 1f;

        Vector3 worldMove = _space.TransformDirection(currentAxis) * currentMoveSign;
        Vector3 worldLocalFwd = transform.TransformDirection(Vector3.forward);
        float align = Mathf.Sign(Vector3.Dot(worldLocalFwd, worldMove));
        if (align == 0f) align = 1f;

        float tailSign = -align;
        Vector3 tailDirLocal = Vector3.forward * tailSign;
        Quaternion rot = Quaternion.FromToRotation(Vector3.forward, tailDirLocal);

        Vector3 headLocalPos = Vector3.zero;
        Vector3 tailLocalPos = tailDirLocal * _holdLen;

        if (_head != null)
        {
            _head.localPosition = headLocalPos;
            _head.localRotation = rot;
        }

        if (_tail != null)
        {
            _tail.localPosition = tailLocalPos;
            _tail.localRotation = rot;
        }

        float fullBodyLen = _holdLen;
        float currentBodyLen = fullBodyLen;

        Vector3 bodyCenter;

        if (IsActive && !IsFailed)
        {
            double now = AudioSettings.dspTime;
            double elapsed = now - HeadDspTime;
            double totalDuration = TailDspTime - HeadDspTime;

            if (totalDuration > 0)
            {
                float ratio = Mathf.Clamp01((float)(elapsed / totalDuration));
                currentBodyLen = fullBodyLen * (1f - ratio);
            }

            Vector3 hitLocalWithOffset = hitLocal;
            hitLocalWithOffset.y += _yOffsetLocal;
            Vector3 hitWorld = _space.TransformPoint(hitLocalWithOffset);
            Vector3 hitInNoteLocal = transform.InverseTransformPoint(hitWorld);

            Vector3 bodyFrontIfPivotAtTail = tailLocalPos - tailDirLocal * currentBodyLen;

            float distToHitLine = Vector3.Dot(hitInNoteLocal - bodyFrontIfPivotAtTail, -tailDirLocal);

            if (distToHitLine > 0f)
            {
                currentBodyLen += distToHitLine;
            }

            _savedBodyLen = currentBodyLen;

            bodyCenter = tailLocalPos - tailDirLocal * (currentBodyLen / 2f);
        }
        else if (IsFailed && _savedBodyLen >= 0f)
        {
            currentBodyLen = _savedBodyLen;
            bodyCenter = tailLocalPos - tailDirLocal * (currentBodyLen / 2f);
        }
        else
        {
            bodyCenter = tailLocalPos - tailDirLocal * (currentBodyLen / 2f);
        }

        if (_body != null)
        {
            float minZ, maxZ;
            TryGetMeshZ(_body, out minZ, out maxZ);
            float meshLenZ = Mathf.Max(0.0001f, (maxZ - minZ));

            Vector3 sc = _bodyBaseScale;
            sc.z = Mathf.Max(0.0001f, currentBodyLen / meshLenZ);
            _body.localScale = sc;

            _body.localPosition = bodyCenter;
            _body.localRotation = rot;
        }
    }

}