using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("기본 설정")]
    public Note _notePrefab;
    public Transform _spawnPoint;
    public Transform _despawnPoint;
    [SerializeField] float _noteSpeed = 5f;

    [Header("테스트용 자동 스폰")]
    [SerializeField] private float _spawnInterval = 1.0f;

    private float _timer;

    public enum NoteType
    {
        Ground,   // 그라운드 노트 4
        Upper,    // 어퍼 노트 4
        Splash    // 원형 스플래시 노트
    }
    public enum NoteForm
    {
        Tap,
        HoldHead, // 슬라이드 시작
        HoldBody, 
        HoldTail  // 슬라이드 끝
    }



    private void Awake()
    {
        
    }

    private void Update()
    {
        //For test
        _timer += Time.deltaTime;
        if (_timer >= _spawnInterval)
        {
            _timer -= _spawnInterval;
            Debug.LogWarning("스폰");
            SpawnNote();
        }
    }

    public void SpawnNote()
    {
        // 레일의 자식으로 노트를 생성해서, 레일 기준 로컬 이동이 가능하게 함
        Note newNote = Instantiate(_notePrefab, _spawnPoint.position, _spawnPoint.rotation, _spawnPoint.transform.parent);
        Debug.LogWarning("노트 생성");
        // ↑ parent는 상황에 따라:
        //  - 레일 오브젝트(예: Rail_1)의 Transform
        //  - 혹은 spawnPoint.parent
        // 로 맞춰주면 됨 (레일 로컬 기준으로 움직이게 하려는 목적)

        newNote.Init(_noteSpeed, _despawnPoint);
    }
}
