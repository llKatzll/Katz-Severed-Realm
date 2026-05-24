using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EffectNoteVisuals : MonoBehaviour
{
    [SerializeField] private EffectChart _chart;
    [SerializeField] private EffectBootstrap _bootstrap;

    [Header("Prefabs")]
    [SerializeField] private GameObject _tapPrefab;
    [SerializeField] private GameObject _headPrefab;
    [SerializeField] private GameObject _bodyPrefab;
    [SerializeField] private GameObject _tailPrefab;

    [Header("Category Colors")]
    [SerializeField] private Color _effColor = Color.yellow;
    [SerializeField] private Color _camColor = Color.red;
    [SerializeField] private Color _railColor = Color.green;
    [SerializeField] private Color _scrColor = Color.blue;

    [Header("Saturation (IN / OUT distinction)")]
    [SerializeField, Range(0f, 1f)] private float _inSaturation = 0.5f;
    [SerializeField, Range(0f, 1f)] private float _outSaturation = 1f;

    [Header("Width Ratios (relative to laneWidth)")]
    [SerializeField] private float _noteWidthRatio = 0.9f;
    [SerializeField] private float _bodyWidthRatio = 0.7f;

    [Header("Z Order")]
    [SerializeField] private float _noteZ = -0.6f;
    [SerializeField] private float _headTailZOffset = -0.01f;

    private readonly List<GameObject> _spawned = new List<GameObject>();
    private Transform _parent;

    private void Awake()
    {
        if (_chart == null) _chart = GetComponent<EffectChart>();
        if (_bootstrap == null) _bootstrap = GetComponent<EffectBootstrap>();
    }

    private void OnEnable()
    {
        if (_chart != null) _chart.OnDataChanged += Rebuild;
    }

    private void OnDisable()
    {
        if (_chart != null) _chart.OnDataChanged -= Rebuild;
    }

    private void Start()
    {
        Rebuild();
    }

    private Transform GetSpawnParent()
    {
        if (_parent != null) return _parent;
        if (_bootstrap != null && _bootstrap.NoteCanvas != null)
            _parent = _bootstrap.NoteCanvas.transform;
        else
            _parent = transform;
        return _parent;
    }

    public void Rebuild()
    {
        ClearAll();
        if (_chart == null || _bootstrap == null) return;
        if (_chart.Data == null || _chart.Data.triggers == null) return;

        foreach (var trig in _chart.Data.triggers)
        {
            if (trig == null) continue;
            SpawnTrigger(trig);
        }
    }

    private void ClearAll()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null) Destroy(_spawned[i]);
        }
        _spawned.Clear();
    }

    private void SpawnTrigger(EffectTrigger trig)
    {
        EffectPresetSO preset = EffectPresetCache.Get(trig.presetId);
        if (preset == null) return;

        Color baseColor = GetCategoryColor(preset.category);
        string label = preset.displayName;

        switch (trig.kind)
        {
            case TriggerKind.Single:
                SpawnTap(trig, baseColor, label);
                break;
            case TriggerKind.On:
                SpawnTap(trig, ApplySaturation(baseColor, _inSaturation), label + " ON");
                break;
            case TriggerKind.Off:
                SpawnTap(trig, ApplySaturation(baseColor, _outSaturation), label + " OFF");
                break;
            case TriggerKind.Sustained:
                SpawnSustained(trig, baseColor, label);
                break;
        }
    }

    private void SpawnTap(EffectTrigger trig, Color color, string label)
    {
        if (_tapPrefab == null) return;

        float x = _bootstrap.LaneToWorldX(trig.lane);
        float y = (float)trig.beat * _bootstrap.BeatHeight;
        var pos = new Vector3(x, y, _noteZ);

        var go = Instantiate(_tapPrefab, pos, Quaternion.identity, GetSpawnParent());
        go.transform.position = pos;
        ApplyWidth(go, false);
        ApplyColor(go, color);
        SetLabel(go, label);
        _spawned.Add(go);
    }

    private void SpawnSustained(EffectTrigger trig, Color baseColor, string label)
    {
        float x = _bootstrap.LaneToWorldX(trig.lane);
        float startY = (float)trig.beat * _bootstrap.BeatHeight;
        float endY = (float)(trig.beat + trig.inBeats) * _bootstrap.BeatHeight;
        float bodyHeight = endY - startY;

        if (_bodyPrefab != null && bodyHeight > 0f)
        {
            var bodyPos = new Vector3(x, (startY + endY) * 0.5f, _noteZ);
            var body = Instantiate(_bodyPrefab, bodyPos, Quaternion.identity, GetSpawnParent());
            body.transform.position = bodyPos;
            ApplyWidth(body, true);
            StretchBodyHeight(body, bodyHeight);
            ApplyColor(body, ApplySaturation(baseColor, _inSaturation));
            SetLabel(body, label);
            _spawned.Add(body);
        }

        if (_headPrefab != null)
        {
            var headPos = new Vector3(x, startY, _noteZ + _headTailZOffset);
            var head = Instantiate(_headPrefab, headPos, Quaternion.identity, GetSpawnParent());
            head.transform.position = headPos;
            ApplyWidth(head, false);
            ApplyColor(head, baseColor);
            _spawned.Add(head);
        }

        if (_tailPrefab != null)
        {
            var tailPos = new Vector3(x, endY, _noteZ + _headTailZOffset);
            var tail = Instantiate(_tailPrefab, tailPos, Quaternion.identity, GetSpawnParent());
            tail.transform.position = tailPos;
            ApplyWidth(tail, false);
            ApplyColor(tail, ApplySaturation(baseColor, _outSaturation));
            _spawned.Add(tail);
        }
    }

    private void ApplyWidth(GameObject go, bool isBody)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;

        float canvasScale = _bootstrap.NoteCanvasScale;
        if (canvasScale <= 0f) canvasScale = 1f;

        float origSizeX = rt.sizeDelta.x;
        if (origSizeX <= 0f) origSizeX = 1f;

        float ratio = isBody ? _bodyWidthRatio : _noteWidthRatio;
        float worldWidth = _bootstrap.LaneWidth * ratio;

        var s = go.transform.localScale;
        s.x = worldWidth / (origSizeX * canvasScale);
        go.transform.localScale = s;
    }

    private void StretchBodyHeight(GameObject body, float worldHeight)
    {
        var rt = body.GetComponent<RectTransform>();
        if (rt == null) return;

        float canvasScale = _bootstrap.NoteCanvasScale;
        if (canvasScale <= 0f) canvasScale = 1f;

        float origSizeY = rt.sizeDelta.y;
        if (origSizeY <= 0f) origSizeY = 1f;

        var s = body.transform.localScale;
        s.y = worldHeight / (origSizeY * canvasScale);
        body.transform.localScale = s;
    }

    private void ApplyColor(GameObject go, Color color)
    {
        var images = go.GetComponentsInChildren<UnityEngine.UI.Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null) images[i].color = color;
        }
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            var mat = renderers[i].material;
            if (mat == null) continue;
            mat.color = color;
        }
    }

    private void SetLabel(GameObject go, string label)
    {
        var text = go.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = label;
    }

    private Color GetCategoryColor(EffectCategory cat) => cat switch
    {
        EffectCategory.Eff => _effColor,
        EffectCategory.Cam => _camColor,
        EffectCategory.Rail => _railColor,
        EffectCategory.Scr => _scrColor,
        _ => Color.white,
    };

    private Color ApplySaturation(Color color, float saturation)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        s = saturation;
        Color result = Color.HSVToRGB(h, s, v);
        result.a = color.a;
        return result;
    }
}
