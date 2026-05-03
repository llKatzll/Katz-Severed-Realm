using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EffectListUI : MonoBehaviour
{
    [Header("List")]
    [SerializeField] private Transform _content;
    [SerializeField] private GameObject _itemPrefab;

    [Header("Category Buttons")]
    [SerializeField] private Button _effButton;
    [SerializeField] private Button _camButton;
    [SerializeField] private Button _railButton;
    [SerializeField] private Button _scrButton;

    [Header("Item Colors (per category)")]
    [SerializeField] private Color _effColor = Color.yellow;
    [SerializeField] private Color _camColor = Color.red;
    [SerializeField] private Color _railColor = Color.green;
    [SerializeField] private Color _scrColor = Color.blue;

    private EffectCategory _currentCategory = EffectCategory.Eff;

    public EffectPresetSO SelectedPreset { get; private set; }
    public event System.Action<EffectPresetSO> OnPresetSelected;

    private void Awake()
    {
        EffectPresetCache.LoadAll();
        BindCategoryButtons();
    }

    private void Start()
    {
        ShowCategory(_currentCategory);
    }

    private void BindCategoryButtons()
    {
        if (_effButton != null) _effButton.onClick.AddListener(() => ShowCategory(EffectCategory.Eff));
        if (_camButton != null) _camButton.onClick.AddListener(() => ShowCategory(EffectCategory.Cam));
        if (_railButton != null) _railButton.onClick.AddListener(() => ShowCategory(EffectCategory.Rail));
        if (_scrButton != null) _scrButton.onClick.AddListener(() => ShowCategory(EffectCategory.Scr));
    }

    public void ShowCategory(EffectCategory cat)
    {
        _currentCategory = cat;
        ClearContent();

        if (_content == null || _itemPrefab == null) return;

        Color tint = GetCategoryColor(cat);
        foreach (var preset in EffectPresetCache.AllByCategory(cat))
        {
            CreateItem(preset, tint);
        }
    }

    private void ClearContent()
    {
        if (_content == null) return;
        for (int i = _content.childCount - 1; i >= 0; i--)
        {
            Destroy(_content.GetChild(i).gameObject);
        }
    }

    private void CreateItem(EffectPresetSO preset, Color tint)
    {
        var item = Instantiate(_itemPrefab, _content);

        var text = item.GetComponentInChildren<TMP_Text>();
        if (text != null) text.text = preset.displayName;

        var image = item.GetComponent<Image>();
        if (image != null) image.color = tint;

        var button = item.GetComponent<Button>();
        if (button != null)
        {
            EffectPresetSO captured = preset;
            button.onClick.AddListener(() => SelectPreset(captured));
        }
    }

    private void SelectPreset(EffectPresetSO preset)
    {
        SelectedPreset = preset;
        OnPresetSelected?.Invoke(preset);
    }

    private Color GetCategoryColor(EffectCategory cat)
    {
        switch (cat)
        {
            case EffectCategory.Eff: return _effColor;
            case EffectCategory.Cam: return _camColor;
            case EffectCategory.Rail: return _railColor;
            case EffectCategory.Scr: return _scrColor;
            default: return Color.white;
        }
    }
}
