using UnityEngine;
using UnityEngine.EventSystems;

public class EffectInput : MonoBehaviour
{
    [SerializeField] private EffectBootstrap _bootstrap;
    [SerializeField] private EffectChart _chart;
    [SerializeField] private EffectListUI _listUI;
    [SerializeField] private EditorTimeline _timeline;

    private bool _holdPending;
    private double _holdStartBeat;
    private int _holdStartLane;
    private string _holdStartPresetId;

    private void Awake()
    {
        if (_bootstrap == null) _bootstrap = GetComponent<EffectBootstrap>();
        if (_chart == null) _chart = GetComponent<EffectChart>();
        if (_timeline == null && _bootstrap != null && _bootstrap.EditorBootstrap != null)
        {
            _timeline = _bootstrap.EditorBootstrap.GetComponent<EditorTimeline>();
        }
    }

    private void Update()
    {
        if (IsPointerOverUI()) return;
        if (Input.GetMouseButtonDown(0)) HandleLeftClick();
        else if (Input.GetMouseButtonDown(1))
        {
            if (IsCtrlHeld()) HandleIdentify();
            else HandleRightClick();
        }
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private bool TryGetLaneBeat(out int lane, out double beat)
    {
        lane = -1;
        beat = 0;

        var cam = _bootstrap != null && _bootstrap.EditorBootstrap != null
            ? _bootstrap.EditorBootstrap.EditorCamera
            : null;
        if (cam == null || _timeline == null || _bootstrap == null) return false;

        Vector3 mp = Input.mousePosition;
        mp.z = -cam.transform.position.z;
        Vector3 world = cam.ScreenToWorldPoint(mp);

        if (!_bootstrap.WorldXToLane(world.x, out lane)) return false;

        float rawBeat = _timeline.WorldYToBeat(world.y);
        if (rawBeat < 0f) return false;

        beat = _timeline.SnapBeat(rawBeat);
        return true;
    }

    private void HandleLeftClick()
    {
        if (_listUI == null || _chart == null) return;
        var preset = _listUI.SelectedPreset;
        if (preset == null) return;

        if (!TryGetLaneBeat(out int lane, out double beat)) return;

        switch (preset.triggerType)
        {
            case TriggerType.Burst:
                AddBurst(beat, lane, preset);
                break;
            case TriggerType.OnOff:
                HandleOnOffClick(beat, lane, preset);
                break;
            case TriggerType.InOut:
                HandleInOutClick(beat, lane, preset);
                break;
        }
    }

    private void AddBurst(double beat, int lane, EffectPresetSO preset)
    {
        var trig = new EffectTrigger
        {
            beat = beat,
            presetId = preset.presetId,
            lane = lane,
            kind = TriggerKind.Single,
            inBeats = 0,
            outBeats = 0
        };
        _chart.AddTrigger(trig);
    }

    private void HandleOnOffClick(double beat, int lane, EffectPresetSO preset)
    {
        if (!_holdPending || _holdStartPresetId != preset.presetId)
        {
            _holdPending = true;
            _holdStartBeat = beat;
            _holdStartLane = lane;
            _holdStartPresetId = preset.presetId;

            var onTrig = new EffectTrigger
            {
                beat = beat,
                presetId = preset.presetId,
                lane = lane,
                kind = TriggerKind.On,
                inBeats = 0,
                outBeats = 0
            };
            _chart.AddTrigger(onTrig);
            return;
        }

        if (System.Math.Abs(beat - _holdStartBeat) < 0.0001)
        {
            ResetPending();
            return;
        }

        double offBeat = System.Math.Max(beat, _holdStartBeat);
        var offTrig = new EffectTrigger
        {
            beat = offBeat,
            presetId = preset.presetId,
            lane = lane,
            kind = TriggerKind.Off,
            inBeats = 0,
            outBeats = 0
        };
        _chart.AddTrigger(offTrig);
        ResetPending();
    }

    private void HandleInOutClick(double beat, int lane, EffectPresetSO preset)
    {
        if (!_holdPending || _holdStartPresetId != preset.presetId)
        {
            _holdPending = true;
            _holdStartBeat = beat;
            _holdStartLane = lane;
            _holdStartPresetId = preset.presetId;
            return;
        }

        if (System.Math.Abs(beat - _holdStartBeat) < 0.0001)
        {
            ResetPending();
            return;
        }

        double startBeat = System.Math.Min(beat, _holdStartBeat);
        double endBeat = System.Math.Max(beat, _holdStartBeat);
        double inBeats = endBeat - startBeat;

        if (preset.category == EffectCategory.Cam)
        {
            if (_chart.HasCameraConflict(startBeat, endBeat))
            {
                Debug.LogWarning("[EffectInput] Camera conflict at beat range " + startBeat + "~" + endBeat);
                ResetPending();
                return;
            }
        }
        else if (preset.category == EffectCategory.Scr)
        {
            if (HasScrConflict(preset.presetId, startBeat, endBeat))
            {
                Debug.LogWarning("[EffectInput] Scr conflict for " + preset.presetId);
                ResetPending();
                return;
            }
        }

        var trig = new EffectTrigger
        {
            beat = startBeat,
            presetId = preset.presetId,
            lane = lane,
            kind = TriggerKind.Sustained,
            inBeats = inBeats,
            outBeats = 0
        };
        _chart.AddTrigger(trig);
        ResetPending();
    }

    private bool HasScrConflict(string presetId, double start, double end)
    {
        if (_chart == null || _chart.Data == null) return false;
        foreach (var t in _chart.Data.triggers)
        {
            if (t == null) continue;
            if (t.presetId != presetId) continue;
            double tStart = t.beat;
            double tEnd = t.beat + t.inBeats + t.outBeats;
            if (!(tEnd < start || tStart > end)) return true;
        }
        return false;
    }

    private void HandleRightClick()
    {
        if (_chart == null) return;
        if (!TryGetLaneBeat(out int lane, out double beat)) return;

        int bsd = _timeline != null ? _timeline.Bsd : 4;
        float tol = Mathf.Max(0.25f, 1f / Mathf.Max(1, bsd));
        _chart.RemoveTriggerAt(beat, lane, tol);
        ResetPending();
    }

    private void HandleIdentify()
    {
        if (_chart == null) return;
        if (!TryGetLaneBeat(out int lane, out double beat)) return;

        int bsd = _timeline != null ? _timeline.Bsd : 4;
        float tol = Mathf.Max(0.25f, 1f / Mathf.Max(1, bsd));
        var trig = _chart.FindTriggerCovering(beat, lane, tol);
        if (trig == null) return;

        var preset = EffectPresetCache.Get(trig.presetId);
        string label = preset != null ? preset.displayName : trig.presetId;
        Debug.Log("[EffectNote] " + label + " (kind " + trig.kind + ", lane " + lane + ", beat " + trig.beat + ")");
    }

    private bool IsCtrlHeld()
    {
        return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
    }

    private void ResetPending()
    {
        _holdPending = false;
    }
}
