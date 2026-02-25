using UnityEngine;

public class Scrolling : MonoBehaviour
{
    [Header("Pivot")]
    [SerializeField] private Transform _pivot;

    [Header("Rotation Settings")]
    [SerializeField] private float _rotationPerSlot = 40f;
    [SerializeField] private float _rotationSpeed = 8f;

    [Header("Input")]
    [SerializeField] private bool _invertWheel = false;

    private float _targetRotationZ;
    private float _currentRotationZ;

    private void Start()
    {
        if (_pivot != null)
        {
            _currentRotationZ = _pivot.localEulerAngles.z;
            _targetRotationZ = _currentRotationZ;
        }
    }

    private void Update()
    {
        HandleWheelInput();
        ApplyRotation();
    }

    private void HandleWheelInput()
    {
        float scroll = Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            float direction = _invertWheel ? -scroll : scroll;
            _targetRotationZ += direction * _rotationPerSlot;
        }
    }

    private void ApplyRotation()
    {
        if (_pivot == null) return;

        _currentRotationZ = Mathf.Lerp(_currentRotationZ, _targetRotationZ, Time.deltaTime * _rotationSpeed);

        Vector3 euler = _pivot.localEulerAngles;
        euler.z = _currentRotationZ;
        _pivot.localEulerAngles = euler;
    }
}