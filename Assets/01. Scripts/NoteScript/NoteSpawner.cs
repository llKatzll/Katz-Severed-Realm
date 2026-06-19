using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    public enum NoteType { Ground, Upper }

    [System.Serializable]
    public class NoteLane
    {
        public string _laneName;
        public NoteType _noteType;

        public Transform _spawnPoint;
        public Transform _hitPoint;
        public Transform _despawnPoint;

        public Transform _noteParent;
        public LaneJudge _judge;

        public Note _tapPrefab;
        public HoldNote _holdPrefab;

        public float _yOffsetLocal;
    }

    public NoteLane[] Lanes => _lanes;
    public RhythmConductor Conductor => _conductor;

    [SerializeField] private RhythmConductor _conductor;
    [SerializeField] private NoteLane[] _lanes;
}
