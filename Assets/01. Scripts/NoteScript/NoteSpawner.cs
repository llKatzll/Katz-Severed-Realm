using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private Note _defaultNotePrefab;

    public enum NoteType { Ground, Upper, Splash }

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

    [Header("Note Speed Set")]
    [SerializeField] private float _baseApproachTime = 4.0f;
    [SerializeField] private float _noteSpeed = 1.0f;
    private float _currentApproachTime => _baseApproachTime / Mathf.Max(0.0001f, _noteSpeed);

    [Header("Test")]
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
        float travelTime = _currentApproachTime;

        foreach (var lane in _lanes)
        {
            if (!lane._spawnPoint || !lane._despawnPoint) continue;

            var prefab = lane._notePrefab != null ? lane._notePrefab : _defaultNotePrefab;
            if (!prefab) continue;

            Transform parent = lane._noteParent != null ? lane._noteParent : lane._spawnPoint.parent;

            var note = Instantiate(prefab, lane._spawnPoint.position, lane._spawnPoint.rotation, parent);

            note.Init(lane._spawnPoint.position, lane._despawnPoint.position, travelTime, lane._noteType);
        }
    }
}
