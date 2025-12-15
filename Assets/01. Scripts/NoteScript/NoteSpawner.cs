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

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _spawnInterval)
        {
            _timer -= _spawnInterval;
            SpawnAllLanes();
        }
    }

    private void SpawnAllLanes()
    {
        foreach (var lane in _lanes)
        {
            if (!lane._spawnPoint || !lane._despawnPoint) continue;
            SpawnNote(lane);
        }
    }

    private void SpawnNote(NoteLane lane)
    {
        var prefab = lane._notePrefab != null ? lane._notePrefab : _defaultNotePrefab;
        if (prefab == null) return;

        Transform parent = lane._noteParent != null ? lane._noteParent : lane._spawnPoint.parent;

        if (lane._noteType == NoteType.Splash)
        {
            parent = lane._noteParent != null ? lane._noteParent : parent;
        }

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
