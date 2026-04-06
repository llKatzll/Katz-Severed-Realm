using System.Collections.Generic;

[System.Serializable]
public class ChartData
{
    public string songName;
    public string difficulty;
    public float bpm;
    public float audioOffset;
    public List<NoteData> notes = new List<NoteData>();
    public List<SVData> svNotes = new List<SVData>();
}
