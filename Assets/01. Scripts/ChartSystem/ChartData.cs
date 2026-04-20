using System.Collections.Generic;

[System.Serializable]
public class TimingPoint
{
    public float beat;
    public float bpm;
    public float svMultiplier = 1f;

    public TimingPoint() { }

    public TimingPoint(float beat, float bpm, float sv = 1f)
    {
        this.beat = beat;
        this.bpm = bpm;
        svMultiplier = sv;
    }
}

[System.Serializable]
public class ChartData
{
    public string songName;
    public string difficulty;
    public float bpm;
    public float audioOffset;
    public List<TimingPoint> timingPoints = new List<TimingPoint>();
    public List<NoteData> notes = new List<NoteData>();
    public List<SVData> svNotes = new List<SVData>();

    public float GetBpmAtBeat(float beat)
    {
        if (timingPoints == null || timingPoints.Count == 0) return bpm;

        float result = bpm;
        for (int i = 0; i < timingPoints.Count; i++)
        {
            if (timingPoints[i].beat <= beat)
                result = timingPoints[i].bpm;
            else
                break;
        }
        return result;
    }

    public float GetSvAtBeat(float beat)
    {
        if (timingPoints == null || timingPoints.Count == 0) return 1f;

        float result = 1f;
        for (int i = 0; i < timingPoints.Count; i++)
        {
            if (timingPoints[i].beat <= beat)
                result = timingPoints[i].svMultiplier;
            else
                break;
        }
        return result;
    }

    public void SortAll()
    {
        if (timingPoints != null) timingPoints.Sort((a, b) => a.beat.CompareTo(b.beat));
        if (notes != null) notes.Sort((a, b) => a.beat.CompareTo(b.beat));
        if (svNotes != null) svNotes.Sort((a, b) => a.beat.CompareTo(b.beat));
    }
}
