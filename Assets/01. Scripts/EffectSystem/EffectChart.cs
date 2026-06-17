using UnityEngine;

public class EffectChart : MonoBehaviour
{
    private EffectData _data = new EffectData();

    public EffectData Data => _data;
    public event System.Action OnDataChanged;

    public void NewData(string songName, string difficulty)
    {
        _data = new EffectData
        {
            songName = songName,
            difficulty = difficulty
        };
        OnDataChanged?.Invoke();
    }

    public void LoadData(EffectData data)
    {
        _data = data ?? new EffectData();
        OnDataChanged?.Invoke();
    }

    public void AddTrigger(EffectTrigger trig)
    {
        if (trig == null) return;
        _data.triggers.Add(trig);
        OnDataChanged?.Invoke();
    }

    public bool RemoveTriggerAt(double beat, int lane, float tolerance = 0.0001f)
    {
        bool removed = false;
        for (int i = _data.triggers.Count - 1; i >= 0; i--)
        {
            var t = _data.triggers[i];
            if (t.lane != lane) continue;
            if (System.Math.Abs(t.beat - beat) > tolerance) continue;
            _data.triggers.RemoveAt(i);
            removed = true;
        }
        if (removed) OnDataChanged?.Invoke();
        return removed;
    }

    public EffectTrigger FindTriggerAt(double beat, int lane, float tolerance = 0.0001f)
    {
        for (int i = 0; i < _data.triggers.Count; i++)
        {
            var t = _data.triggers[i];
            if (t.lane != lane) continue;
            if (System.Math.Abs(t.beat - beat) > tolerance) continue;
            return t;
        }
        return null;
    }

    public EffectTrigger FindTriggerCovering(double beat, int lane, float tolerance = 0.0001f)
    {
        for (int i = 0; i < _data.triggers.Count; i++)
        {
            var t = _data.triggers[i];
            if (t == null) continue;
            if (t.lane != lane) continue;
            double start = t.beat - tolerance;
            double end = t.beat + t.inBeats + tolerance;
            if (beat >= start && beat <= end) return t;
        }
        return null;
    }

    public bool HasCameraConflict(double beatStart, double beatEnd)
    {
        for (int i = 0; i < _data.triggers.Count; i++)
        {
            var t = _data.triggers[i];
            if (t == null) continue;

            EffectPresetSO preset = EffectPresetCache.Get(t.presetId);
            if (preset == null || preset.category != EffectCategory.Cam) continue;

            double tStart = t.beat;
            double tEnd = t.beat + t.inBeats + t.outBeats;
            if (!(tEnd < beatStart || tStart > beatEnd)) return true;
        }
        return false;
    }
}
