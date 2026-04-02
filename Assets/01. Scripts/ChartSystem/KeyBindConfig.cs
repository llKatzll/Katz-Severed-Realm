using UnityEngine;

[CreateAssetMenu(fileName = "KeyBindConfig", menuName = "KSR/Key Bind Config")]
public class KeyBindConfig : ScriptableObject
{
    [Header("Ground Keys (4)")]
    [SerializeField] private KeyCode[] _groundKeys = new KeyCode[]
    {
        KeyCode.A, KeyCode.S, KeyCode.L, KeyCode.Semicolon
    };

    [Header("Upper Keys (4)")]
    [SerializeField] private KeyCode[] _upperKeys = new KeyCode[]
    {
        KeyCode.Q, KeyCode.W, KeyCode.O, KeyCode.P
    };

    [Header("Dimension Key")]
    [SerializeField] private KeyCode _dimensionKey = KeyCode.Space;

    public KeyCode[] GroundKeys => _groundKeys;
    public KeyCode[] UpperKeys => _upperKeys;
    public KeyCode DimensionKey => _dimensionKey;

    public KeyCode GetKey(ChartLaneType laneType, int laneIndex)
    {
        KeyCode[] arr = laneType == ChartLaneType.Ground ? _groundKeys : _upperKeys;
        if (laneIndex < 0 || laneIndex >= arr.Length) return KeyCode.None;
        return arr[laneIndex];
    }

    public KeyCode GetKeyByGlobalLane(int lane)
    {
        if (lane < 0) return KeyCode.None;
        if (lane < _groundKeys.Length) return _groundKeys[lane];

        int upperIdx = lane - _groundKeys.Length;
        if (upperIdx < _upperKeys.Length) return _upperKeys[upperIdx];

        return KeyCode.None;
    }

    public int TotalLaneCount => _groundKeys.Length + _upperKeys.Length;

    public void SetKey(ChartLaneType laneType, int laneIndex, KeyCode newKey)
    {
        KeyCode[] arr = laneType == ChartLaneType.Ground ? _groundKeys : _upperKeys;
        if (laneIndex < 0 || laneIndex >= arr.Length) return;
        arr[laneIndex] = newKey;
    }

    public void SetDimensionKey(KeyCode newKey)
    {
        _dimensionKey = newKey;
    }

    public void ResetToDefault()
    {
        _groundKeys = new KeyCode[] { KeyCode.A, KeyCode.S, KeyCode.L, KeyCode.Semicolon };
        _upperKeys = new KeyCode[] { KeyCode.Q, KeyCode.W, KeyCode.O, KeyCode.P };
        _dimensionKey = KeyCode.Space;
    }
}
