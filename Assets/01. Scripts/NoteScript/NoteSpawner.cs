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
        public Transform _spawnPoint;
        public Transform _despawnPoint;
        public Transform _noteParent;
    }

    [SerializeField] private NoteLane[] _lanes;

    [SerializeField] private float _baseApproachTime = 4.0f;
    [SerializeField] private float _noteSpeed = 1.0f;
    private float CurrentApproachTime => _baseApproachTime / Mathf.Max(0.0001f, _noteSpeed);

    [SerializeField] private float _spawnInterval = 1.0f;
    private float _timer;

    [Header("Spawn Mode")]
    [SerializeField] private bool _spawnRandomSingleLane = true;          // 8개 중 1개 랜덤 스폰
    [SerializeField] private bool _spawnGroundAndSplashTogether = false;  // 그라운드 1 + 스플래시 1 동시 스폰
    [SerializeField] private bool _avoidSameLaneTwice = true;

    private int _lastRandomIndex = -1;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < _spawnInterval) return;

        _timer -= _spawnInterval;

        if (_spawnGroundAndSplashTogether)
        {
            SpawnGroundAndSplash();
            return;
        }

        if (_spawnRandomSingleLane)
        {
            SpawnRandomSingle();
            return;
        }

        // 둘 다 꺼져있으면 아무것도 안 함
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

    private void SpawnGroundAndSplash()
    {
        int groundIndex = PickRandomLaneIndexByType(NoteType.Ground);
        int splashIndex = PickRandomLaneIndexByType(NoteType.Splash);

        if (groundIndex >= 0) SpawnNote(_lanes[groundIndex]);
        if (splashIndex >= 0) SpawnNote(_lanes[splashIndex]);
    }

    private int PickRandomNonSplashLaneIndex()
    {
        int count = 0;
        for (int i = 0; i < _lanes.Length; i++)
        {
            var lane = _lanes[i];
            if (lane == null) continue;
            if (!lane._spawnPoint || !lane._despawnPoint) continue;
            if (lane._noteType == NoteType.Splash) continue;
            count++;
        }

        if (count == 0) return -1;

        int r = Random.Range(0, count);
        for (int i = 0; i < _lanes.Length; i++)
        {
            var lane = _lanes[i];
            if (lane == null) continue;
            if (!lane._spawnPoint || !lane._despawnPoint) continue;
            if (lane._noteType == NoteType.Splash) continue;

            if (r == 0) return i;
            r--;
        }

        return -1;
    }

    private int PickRandomLaneIndexByType(NoteType type)
    {
        int count = 0;
        for (int i = 0; i < _lanes.Length; i++)
        {
            var lane = _lanes[i];
            if (lane == null) continue;
            if (!lane._spawnPoint || !lane._despawnPoint) continue;
            if (lane._noteType != type) continue;
            count++;
        }

        if (count == 0) return -1;

        int r = Random.Range(0, count);
        for (int i = 0; i < _lanes.Length; i++)
        {
            var lane = _lanes[i];
            if (lane == null) continue;
            if (!lane._spawnPoint || !lane._despawnPoint) continue;
            if (lane._noteType != type) continue;

            if (r == 0) return i;
            r--;
        }

        return -1;
    }

    private void SpawnNote(NoteLane lane)
    {
        var prefab = lane._notePrefab != null ? lane._notePrefab : _defaultNotePrefab;
        if (prefab == null) return;

        Transform parent = lane._noteParent != null ? lane._noteParent : lane._spawnPoint.parent;

        float travelTime = CurrentApproachTime;

        Vector3 spawnPos = lane._spawnPoint.position;
        Vector3 hitPos = lane._despawnPoint.position;

        if (lane._noteType == NoteType.Splash)
        {
            var note = Instantiate(prefab, hitPos, lane._spawnPoint.rotation, parent);
            note.Init(hitPos, hitPos, travelTime, lane._noteType);
        }
        else
        {
            var note = Instantiate(prefab, spawnPos, lane._spawnPoint.rotation, parent);
            note.Init(spawnPos, hitPos, travelTime, lane._noteType);
        }
    }
}
