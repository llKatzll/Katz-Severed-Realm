#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;

public static class ChartCompactMigrator
{
    [MenuItem("Tools/Chart/Compact Migrate (verify)")]
    public static void Migrate() => Run(true);

    [MenuItem("Tools/Chart/Compact Verify (dry-run)")]
    public static void Verify() => Run(false);

    private static void Run(bool write)
    {
        string dir = ChartUtility.GetChartDirectory();
        if (!Directory.Exists(dir))
        {
            Debug.LogError("[ChartCompact] Chart dir not found: " + dir);
            return;
        }

        string[] files = Directory.GetFiles(dir, "*.json");
        int pass = 0, fail = 0;
        long totalOld = 0, totalNew = 0;

        for (int i = 0; i < files.Length; i++)
        {
            string path = files[i];
            string name = Path.GetFileName(path);

            string raw = File.ReadAllText(path);
            ChartData original = ChartUtility.FromJson(raw);
            if (original == null)
            {
                fail++;
                Debug.LogError("[ChartCompact] FAIL parse: " + name);
                continue;
            }

            string compact = ChartUtility.ToJson(original);
            ChartData reparsed = ChartUtility.FromJson(compact);

            string reason;
            if (!DeepEqual(original, reparsed, out reason))
            {
                fail++;
                Debug.LogError("[ChartCompact] FAIL verify: " + name + " -> " + reason + " (not written)");
                continue;
            }

            long oldBytes = new FileInfo(path).Length;
            long newBytes = Encoding.UTF8.GetByteCount(compact);
            totalOld += oldBytes;
            totalNew += newBytes;
            pass++;

            float pct = oldBytes > 0 ? (1f - (float)newBytes / oldBytes) * 100f : 0f;
            string tag = write ? "MIGRATED" : "OK(dry)";
            Debug.Log("[ChartCompact] " + tag + " " + name + " notes=" + original.notes.Count
                      + "  " + oldBytes + "B -> " + newBytes + "B  (-" + pct.ToString("F1") + "%)");

            if (write) File.WriteAllText(path, compact);
        }

        if (write) AssetDatabase.Refresh();

        float totalPct = totalOld > 0 ? (1f - (float)totalNew / totalOld) * 100f : 0f;
        Debug.Log("[ChartCompact] DONE  pass=" + pass + " fail=" + fail
                  + "  total " + totalOld + "B -> " + totalNew + "B  (-" + totalPct.ToString("F1") + "%)"
                  + (write ? "" : "  [dry-run, nothing written]"));
    }

    private static bool DeepEqual(ChartData a, ChartData b, out string reason)
    {
        reason = "";
        if (a == null || b == null) { reason = "null data"; return false; }
        if (a.songName != b.songName) { reason = "songName"; return false; }
        if (a.difficulty != b.difficulty) { reason = "difficulty"; return false; }
        if (a.bpm != b.bpm) { reason = "bpm"; return false; }
        if (a.audioOffset != b.audioOffset) { reason = "audioOffset"; return false; }

        if (Count(a.timingPoints) != Count(b.timingPoints)) { reason = "timingPoints count"; return false; }
        if (Count(a.svNotes) != Count(b.svNotes)) { reason = "svNotes count"; return false; }

        int an = Count(a.notes);
        if (an != Count(b.notes)) { reason = "notes count"; return false; }

        for (int i = 0; i < an; i++)
        {
            NoteData x = a.notes[i];
            NoteData y = b.notes[i];
            if (x.beat != y.beat) { reason = "note[" + i + "].beat"; return false; }
            if (x.lane != y.lane) { reason = "note[" + i + "].lane"; return false; }
            if (x.laneType != y.laneType) { reason = "note[" + i + "].laneType"; return false; }
            if (x.noteType != y.noteType) { reason = "note[" + i + "].noteType"; return false; }
            if (x.holdEndBeat != y.holdEndBeat) { reason = "note[" + i + "].holdEndBeat"; return false; }
        }

        return true;
    }

    private static int Count<T>(System.Collections.Generic.List<T> list)
        => list == null ? 0 : list.Count;
}
#endif
