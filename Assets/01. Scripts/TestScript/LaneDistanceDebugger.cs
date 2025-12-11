using UnityEngine;

public class LaneDistanceDebugger : MonoBehaviour
{
    public Transform _spawnPoint;
    public Transform _hitPoint;
    public float _noteSpeed = 5f;   // 인스펙터에서 넣는 속도
    public string _laneName = "Lane";

    public float _approachTime = 4f;

    void Start()
    {
        if (_spawnPoint == null || _hitPoint == null)
        {
            Debug.LogWarning($"[{_laneName}] Spawn/Hit 포인트가 비어 있음");
            return;
        }

        float dist = Vector3.Distance(_spawnPoint.position, _hitPoint.position);
        float travelTime = dist / _noteSpeed;

        Debug.Log(
            $"[{_laneName}] dist = {dist:F3}, " +
            $"travel = {travelTime:F3}s, " +
            $"approachTime(기대값) = {_approachTime:F3}s"
        );
    }
}
