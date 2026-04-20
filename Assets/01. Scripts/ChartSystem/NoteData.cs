[System.Serializable]
public class NoteData
{
    public float beat;
    public int lane;
    public ChartLaneType laneType;
    public ChartNoteType noteType;
    public float holdEndBeat;

    public NoteData() { }

    public NoteData(float beat, int lane, ChartLaneType laneType, ChartNoteType noteType, float holdEndBeat = 0f)
    {
        this.beat = beat;
        this.lane = lane;
        this.laneType = laneType;
        this.noteType = noteType;
        this.holdEndBeat = holdEndBeat;
    }

    public bool IsHold => noteType == ChartNoteType.Hold || (noteType == ChartNoteType.Dimension && holdEndBeat > beat);

    public bool SamePosition(NoteData other)
    {
        return other != null
            && UnityEngine.Mathf.Approximately(beat, other.beat)
            && lane == other.lane
            && laneType == other.laneType
            && noteType == other.noteType;
    }
}
