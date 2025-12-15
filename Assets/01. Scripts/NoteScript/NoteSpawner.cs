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

    [Header("Random Spawn Option")]
    [SerializeField] private bool _includeSplashInRandom = false;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _spawnInterval)
        {
            _timer -= _spawnInterval;
            SpawnRandomLane();
        }
    }

    private void SpawnRandomLane()
    {
        int pick = PickRandomLaneIndex();
        if (pick < 0) return;

        SpawnNote(_lanes[pick]);
    }

    private int PickRandomLaneIndex()
    {
        if (_lanes == null || _lanes.Length == 0) return -1;

        int count = 0;
        for (int i = 0; i < _lanes.Length; i++)
        {
            var lane = _lanes[i];
            if (lane == null) continue;
            if (!lane._spawnPoint || !lane._despawnPoint) continue;
            if (!_includeSplashInRandom && lane._noteType == NoteType.Splash) continue;

            count++;
        }

        if (count == 0) return -1;

        int r = Random.Range(0, count);

        for (int i = 0; i < _lanes.Length; i++)
        {
            var lane = _lanes[i];
            if (lane == null) continue;
            if (!lane._spawnPoint || !lane._despawnPoint) continue;
            if (!_includeSplashInRandom && lane._noteType == NoteType.Splash) continue;

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
