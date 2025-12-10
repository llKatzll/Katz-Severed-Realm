using UnityEngine;

public class Note : MonoBehaviour
{
    //스포너에서 해결해줄 것.
    private float _speed;
    private Transform _despawnPoint;

    // 초기화 함수 (스포너에서 호출)
    public void Init(float speed, Transform despawnPoint)
    {
        _speed = speed;
        _despawnPoint = despawnPoint;
    }

    void Update()
    {
        // 레일의 로컬 기준 아래로 이동.
        // 아래 방향축 back이다 노트 생성은 back으로 해라.
        transform.Translate(Vector3.back * _speed * Time.deltaTime, Space.Self);

        if (_despawnPoint != null)
        {
            // 기준은 상황에 맞게 변경 가능 (z 대신 거리 비교 등)
            if (transform.position.z <= _despawnPoint.position.z)
            {
                Debug.LogWarning("삭제");
                Destroy(gameObject);
            }
        }
    }
}
