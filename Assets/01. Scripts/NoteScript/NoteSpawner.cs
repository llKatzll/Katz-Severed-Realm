using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("Default Note Setting (optional)")]
    [SerializeField] private Note _defaultNotePrefab;

    public enum NoteType { Ground, Upper, Splash }
    public enum NoteForm { Tap, HoldHead, HoldBody, HoldTail }

    [System.Serializable]
    public class NoteLane
    {
        public string _laneName;
        public NoteType _noteType;

        [Header("Prefab (Ground / Upper / Splash 개별 세팅)")]
        public Note _notePrefab;

        [Header("Spawn / Hit(=Despawn)")]
        public Transform _spawnPoint;
        public Transform _despawnPoint;

        [HideInInspector] public float _laneSpeed;   // 이 레일 전용 속도

        [Header("Parent (비우면 spawnPoint.parent)")]
        public Transform _noteParent;
    }

    [Header("Rail List (Ground 4 Upper 4 Splash 1)")]
    [SerializeField] private NoteLane[] _lanes;

    [Header("공통 접근 시간 (노트 스피드 느낌)")]
    [SerializeField] private float _approachTime = 4.0f;

    [Header("테스트 스폰")]
    [SerializeField] private float _spawnInterval = 1.0f;
    private float _timer;

    private void Awake()
    {
        RecalculateLaneSpeeds();
    }

    // 나중에 옵션에서 노트 스피드 바꾸면 이 함수만 다시 호출하면 됨
    public void RecalculateLaneSpeeds()
    {
        foreach (var lane in _lanes)
        {
            if (!lane._spawnPoint || !lane._despawnPoint)
                continue;

            float dist = Vector3.Distance(
                lane._spawnPoint.position,
                lane._despawnPoint.position
            );

            lane._laneSpeed = dist / _approachTime;   // ★ 핵심
            // Debug 찍어보면 모든 레일 dist는 다르고, time은 전부 _approachTime 나올 거야
            Debug.Log($"[Lane:{lane._laneName}] dist={dist:F3}, laneSpeed={lane._laneSpeed:F3}");
        }
    }

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
            if (!lane._spawnPoint || !lane._despawnPoint)
            {
                Debug.LogWarning($"Rail {lane._laneName} : Spawn / Despawn 세팅 비어 있음");
                continue;
            }

            SpawnNote(lane);
        }
    }

    private void SpawnNote(NoteLane lane)
    {
        var prefab = lane._notePrefab != null ? lane._notePrefab : _defaultNotePrefab;
        if (prefab == null)
        {
            Debug.LogError($"Rail {lane._laneName} : Note Prefab is NULL!");
            return;
        }

        Transform parent = lane._noteParent != null
            ? lane._noteParent
            : lane._spawnPoint.parent;

        var note = Instantiate(prefab,
            lane._spawnPoint.position,
            lane._spawnPoint.rotation,
            parent);

        note.Init(lane._laneSpeed, lane._despawnPoint, lane._noteType);
    }
}
