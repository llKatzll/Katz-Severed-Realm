using UnityEngine;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

public static class ChartUtility
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    [System.Serializable]
    private class ChartCompact
    {
        public string songName;
        public string difficulty;
        public float bpm;
        public float audioOffset;
        public List<TimingPoint> timingPoints = new List<TimingPoint>();
        public List<string> notes = new List<string>();
        public List<SVData> svNotes = new List<SVData>();
    }

    public static string ToJson(ChartData data)
    {
        if (data == null) return null;

        ChartCompact c = new ChartCompact
        {
            songName = data.songName,
            difficulty = data.difficulty,
            bpm = data.bpm,
            audioOffset = data.audioOffset,
            timingPoints = data.timingPoints ?? new List<TimingPoint>(),
            svNotes = data.svNotes ?? new List<SVData>(),
            notes = new List<string>()
        };

        if (data.notes != null)
        {
            for (int i = 0; i < data.notes.Count; i++)
                c.notes.Add(NoteToCsv(data.notes[i]));
        }

        return JsonUtility.ToJson(c, false);
    }

    public static ChartData FromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        if (json.Contains("\"laneType\""))
            return JsonUtility.FromJson<ChartData>(json);

        ChartCompact c = JsonUtility.FromJson<ChartCompact>(json);
        if (c == null) return null;

        ChartData data = new ChartData
        {
            songName = c.songName,
            difficulty = c.difficulty,
            bpm = c.bpm,
            audioOffset = c.audioOffset,
            timingPoints = c.timingPoints ?? new List<TimingPoint>(),
            svNotes = c.svNotes ?? new List<SVData>(),
            notes = new List<NoteData>()
        };

        if (c.notes != null)
        {
            for (int i = 0; i < c.notes.Count; i++)
            {
                NoteData n = CsvToNote(c.notes[i]);
                if (n != null) data.notes.Add(n);
            }
        }

        return data;
    }

    private static string NoteToCsv(NoteData n)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(n.beat.ToString("R", Inv));
        sb.Append(',');
        sb.Append(n.lane.ToString(Inv));
        sb.Append(',');
        sb.Append(((int)n.laneType).ToString(Inv));
        sb.Append(',');
        sb.Append(((int)n.noteType).ToString(Inv));
        if (n.holdEndBeat != 0f)
        {
            sb.Append(',');
            sb.Append(n.holdEndBeat.ToString("R", Inv));
        }
        return sb.ToString();
    }

    private static NoteData CsvToNote(string line)
    {
        if (string.IsNullOrEmpty(line)) return null;

        string[] p = line.Split(',');
        if (p.Length < 4) return null;

        float beat = float.Parse(p[0], NumberStyles.Float, Inv);
        int lane = int.Parse(p[1], NumberStyles.Integer, Inv);
        int laneType = int.Parse(p[2], NumberStyles.Integer, Inv);
        int noteType = int.Parse(p[3], NumberStyles.Integer, Inv);
        float holdEnd = p.Length >= 5 ? float.Parse(p[4], NumberStyles.Float, Inv) : 0f;

        return new NoteData(beat, lane, (ChartLaneType)laneType, (ChartNoteType)noteType, holdEnd);
    }

    private const int MaxLane = 3;

    private static bool IsFinite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);

    private static bool Sanitize(ChartData d, string filePath)
    {
        string fn = Path.GetFileName(filePath);

        if (!IsFinite(d.bpm) || d.bpm <= 0f)
        {
            Debug.LogWarning("[ChartUtility] Reject (bad bpm " + d.bpm + "): " + fn);
            return false;
        }

        if (!IsFinite(d.audioOffset)) d.audioOffset = 0f;

        if (d.notes == null)
        {
            d.notes = new List<NoteData>();
            return true;
        }

        int removed = 0;
        List<NoteData> kept = new List<NoteData>(d.notes.Count);
        for (int i = 0; i < d.notes.Count; i++)
        {
            NoteData n = d.notes[i];
            if (n == null) { removed++; continue; }
            if (!IsFinite(n.beat)) { removed++; continue; }
            if (n.lane < 0 || n.lane > MaxLane) { removed++; continue; }
            if (!System.Enum.IsDefined(typeof(ChartLaneType), n.laneType)) { removed++; continue; }
            if (!System.Enum.IsDefined(typeof(ChartNoteType), n.noteType)) { removed++; continue; }
            if (!IsFinite(n.holdEndBeat)) n.holdEndBeat = 0f;
            if (n.noteType == ChartNoteType.Hold && n.holdEndBeat <= n.beat) { removed++; continue; }
            kept.Add(n);
        }

        if (removed > 0)
        {
            d.notes = kept;
            Debug.LogWarning("[ChartUtility] Sanitized " + fn + ": dropped " + removed + " invalid note(s)");
        }

        return true;
    }

    public static bool SaveToFile(ChartData data, string filePath)
    {
        try
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            data.SortAll();
            File.WriteAllText(filePath, ToJson(data));
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[ChartUtility] Save failed: " + e.Message);
            return false;
        }
    }

    public static ChartData LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            string json = File.ReadAllText(filePath);
            ChartData data = FromJson(json);
            if (data == null) return null;
            if (!Sanitize(data, filePath)) return null;
            data.SortAll();
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[ChartUtility] Load failed: " + e.Message);
            return null;
        }
    }

    public static string GetChartDirectory()
        => Path.Combine(Application.streamingAssetsPath, "Charts");

    public static string GetChartPath(string songName, string difficulty)
        => Path.Combine(GetChartDirectory(), songName + "_" + difficulty + ".json");
}
