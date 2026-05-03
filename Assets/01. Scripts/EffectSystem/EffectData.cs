using System;
using System.Collections.Generic;

[Serializable]
public class EffectData
{
    public string songName;
    public string difficulty;
    public List<EffectTrigger> triggers = new List<EffectTrigger>();

    public void SortByBeat()
    {
        if (triggers == null) return;
        triggers.Sort((a, b) => a.beat.CompareTo(b.beat));
    }
}

[Serializable]
public class EffectTrigger
{
    public double beat;
    public string presetId;
    public int lane;
    public TriggerKind kind;
    public double inBeats;
    public double outBeats;
}
