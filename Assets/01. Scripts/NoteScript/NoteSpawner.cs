using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private Note _defaultNotePrefab;

    public enum NoteType { Ground, Upper, Splash }
    public enum NoteForm { Tap, HoldHead, HoldBody, HoldTail }

    [System.Serializable]
    public class NoteLane
    {
        public string _laneName;
        public NoteType _noteType;
        public Note _notePrefab;

        public Transform _space;        // RailControl_i
        public Transform _spawnPoint;   // SpawnPoint_i
        public Transform _hitPoint;     // HitRail_i
        public Transform _despawnPoint; // (유지, 지금은 안 씀)

        public Transform _noteParent;   // Ground는 Rail_i 넣어둔 상태
        public NoteHitLine _hitLine;    // HitRail_i에 붙은 스크립트
    }

    [SerializeField] private NoteLane[] _lanes;

    [SerializeField] private float _baseApproachTime = 4.0f;
    [SerializeField] private float _noteSpeed = 1.0f;
    private float CurrentApproachTime => _baseApproachTime / Mathf.Max(0.0001f, _noteSpeed);

    [SerializeField] private float _spawnInterval = 1.0f;
    private float _timer;

    [Header("Spawn Mode")]
    [SerializeField] private bool _spawnRandomSingleLane = true;
    [SerializeField] private bool _avoidSameLaneTwice = true;

    private int _lastRandomIndex = -1;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < _spawnInterval) return;

        _timer -= _spawnInterval;

        if (_spawnRandomSingleLane)
        {
            SpawnRandomSingle();
            return;
        }
    }

    private void SpawnRandomSingle()
    {
        int pick = PickRandomNonSplashLaneIndex();
        if (pick < 0) return;

        if (_avoidSameLaneTwice && pick == _lastRandomIndex)
        {
            int retry = PickRandomNonSplashLaneIndex();
            if (retry >= 0) pick = retry;
        }

        _lastRandomIndex = pick;
        SpawnNote(_lanes[pick]);
    }

    private int PickRandomNonSplashLaneIndex()
    {
        int count = 0;
        for (int i = 0; i < _lanes.Length; i++)
        {
            var lane = _lanes[i];
            if (lane == null) continue;
            if (!lane._spawnPoint || !lane._hitPoint) continue;
            if (lane._noteType == NoteType.Splash) continue;
            count++;
        }

        if (count == 0) return -1;

        int r = Random.Range(0, count);
        for (int i = 0; i < _lanes.Length; i++)
        {
            var lane = _lanes[i];
            if (lane == null) continue;
            if (!lane._spawnPoint || !lane._hitPoint) continue;
            if (lane._noteType == NoteType.Splash) continue;

            if (r == 0) return i;
            r--;
        }

        return -1;
    }

    private void SpawnNote(NoteLane lane)
    {
        var prefab = lane._notePrefab != null ? lane._notePrefab : _defaultNotePrefab;
        if (prefab == null) return;

        float travelTime = CurrentApproachTime;

        Transform space = lane._space != null ? lane._space : lane._spawnPoint.parent;

        // 정리용 부모(네 구조 유지)
        Transform parent = lane._noteParent != null ? lane._noteParent : null;

        var note = Instantiate(prefab, parent);

        // ★ 통과 이동: despawn이 있으면 무조건 4포인트 InitFollow 사용
        if (lane._despawnPoint != null)
        {
            note.InitFollow(space, lane._spawnPoint, lane._hitPoint, lane._despawnPoint, travelTime, lane._noteType);
        }
        else
        {
            note.InitFollow(space, lane._spawnPoint, lane._hitPoint, travelTime, lane._noteType);
        }

        // 판정선에 레인 타입 알려주기(선택)
        if (lane._hitLine != null)
            lane._hitLine.SetLane(lane._noteType);
    }

}
