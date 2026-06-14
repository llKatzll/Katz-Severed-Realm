using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeyBindPanel : MonoBehaviour, IModalPanel
{
    private const int SlotCount = 9;
    private const int GroundCount = 4;
    private const int UpperCount = 4;

    [Header("Slot Buttons (0-3 Ground, 4-7 Upper, 8 Dimension)")]
    [SerializeField] private Button[] _slotButtons = new Button[SlotCount];
    [SerializeField] private TMP_Text[] _slotLabels = new TMP_Text[SlotCount];

    [Header("Capture Prompt")]
    [SerializeField] private GameObject _capturePrompt;

    [Header("Close")]
    [SerializeField] private Button _closeButton;

    private bool _capturing;
    private int _captureSlot = -1;

    private void Awake()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slotButtons[i] == null) continue;
            int slot = i;
            _slotButtons[i].onClick.AddListener(() => BeginCapture(slot));
        }
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        ModalStack.Push(this);
        CancelCapture();
        RefreshLabels();
    }

    private void OnDisable()
    {
        CancelCapture();
        ModalStack.Remove(this);
    }

    private void Update()
    {
        if (!_capturing) return;
        ScanForKey();
    }

    public void OnEscape()
    {
        if (_capturing) { CancelCapture(); return; }
        Close();
    }

    private void BeginCapture(int slot)
    {
        _capturing = true;
        _captureSlot = slot;
        if (_capturePrompt != null) _capturePrompt.SetActive(true);
    }

    private void CancelCapture()
    {
        _capturing = false;
        _captureSlot = -1;
        if (_capturePrompt != null) _capturePrompt.SetActive(false);
    }

    private void ScanForKey()
    {
        foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (kc == KeyCode.Escape) continue;
            if ((int)kc >= (int)KeyCode.Mouse0) continue;
            if (!Input.GetKeyDown(kc)) continue;
            AssignKey(_captureSlot, kc);
            return;
        }
    }

    private void AssignKey(int slot, KeyCode key)
    {
        int conflict = FindSlotWithKey(key);
        KeyCode oldKey = GetSlotKey(slot);

        SetSlotKey(slot, key);
        if (conflict >= 0 && conflict != slot)
            SetSlotKey(conflict, oldKey);

        CancelCapture();
        RefreshLabels();
    }

    private int FindSlotWithKey(KeyCode key)
    {
        for (int i = 0; i < SlotCount; i++)
            if (GetSlotKey(i) == key) return i;
        return -1;
    }

    private KeyCode GetSlotKey(int slot)
    {
        if (slot < GroundCount) return SettingsConfig.GetGroundKey(slot);
        if (slot < GroundCount + UpperCount) return SettingsConfig.GetUpperKey(slot - GroundCount);
        return SettingsConfig.DimensionKey;
    }

    private void SetSlotKey(int slot, KeyCode key)
    {
        if (slot < GroundCount) SettingsConfig.SetGroundKey(slot, key);
        else if (slot < GroundCount + UpperCount) SettingsConfig.SetUpperKey(slot - GroundCount, key);
        else SettingsConfig.DimensionKey = key;
    }

    private void RefreshLabels()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slotLabels[i] == null) continue;
            _slotLabels[i].text = KeyName(GetSlotKey(i));
        }
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    private static string KeyName(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Semicolon: return ";";
            case KeyCode.Comma: return ",";
            case KeyCode.Period: return ".";
            case KeyCode.Slash: return "/";
            case KeyCode.Backslash: return "\\";
            case KeyCode.Quote: return "'";
            case KeyCode.BackQuote: return "`";
            case KeyCode.LeftBracket: return "[";
            case KeyCode.RightBracket: return "]";
            case KeyCode.Minus: return "-";
            case KeyCode.Equals: return "=";
            case KeyCode.Space: return "Space";
            case KeyCode.Return: return "Enter";
            case KeyCode.Tab: return "Tab";
            case KeyCode.LeftShift: return "LShift";
            case KeyCode.RightShift: return "RShift";
            case KeyCode.LeftControl: return "LCtrl";
            case KeyCode.RightControl: return "RCtrl";
            case KeyCode.LeftAlt: return "LAlt";
            case KeyCode.RightAlt: return "RAlt";
            case KeyCode.Alpha0: return "0";
            case KeyCode.Alpha1: return "1";
            case KeyCode.Alpha2: return "2";
            case KeyCode.Alpha3: return "3";
            case KeyCode.Alpha4: return "4";
            case KeyCode.Alpha5: return "5";
            case KeyCode.Alpha6: return "6";
            case KeyCode.Alpha7: return "7";
            case KeyCode.Alpha8: return "8";
            case KeyCode.Alpha9: return "9";
        }
        return key.ToString();
    }
}
