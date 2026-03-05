using UnityEngine;
using UnityEngine.UI;

public class DifficultySelector : MonoBehaviour
{
    [System.Serializable]
    public class DifficultyButton
    {
        public DifficultyType type;
        public Button button;
        public Image buttonImage;
        public CanvasGroup canvasGroup;
    }

    [Header("Buttons")]
    [SerializeField] private DifficultyButton[] _buttons;

    [Header("Canvas Group (All Buttons)")]
    [SerializeField] private CanvasGroup _allButtonsGroup;
    [SerializeField] private float _fadeSpeed = 5f;

    [Header("Visual")]
    [SerializeField] private float _disabledAlpha = 0.3f;
    [SerializeField] private float _enabledAlpha = 1f;

    private SongData _currentSong;
    private DifficultyType _selectedDifficulty;
    private float _targetAlpha = 0f;

    public event System.Action<DifficultyType> OnDifficultySelected;

    private void Start()
    {
        foreach (var btn in _buttons)
        {
            if (btn.button != null)
            {
                DifficultyType capturedType = btn.type;
                btn.button.onClick.AddListener(() => SelectDifficulty(capturedType));
            }
        }

        if (_allButtonsGroup != null)
            _allButtonsGroup.alpha = 0f;
    }

    private void Update()
    {
        if (_allButtonsGroup != null)
        {
            _allButtonsGroup.alpha = Mathf.Lerp(
                _allButtonsGroup.alpha,
                _targetAlpha,
                Time.deltaTime * _fadeSpeed
            );
        }
    }

    public void ShowButtons(bool show)
    {
        _targetAlpha = show ? 1f : 0f;

        if (_allButtonsGroup != null)
            _allButtonsGroup.blocksRaycasts = show;
    }

    public void SetupForSong(SongData song)
    {
        _currentSong = song;

        DifficultyType firstAvailable = DifficultyType.Easy;
        bool foundFirst = false;

        foreach (var btn in _buttons)
        {
            bool exists = song != null && song.HasDifficulty(btn.type);

            if (btn.button != null)
                btn.button.interactable = exists;

            if (btn.canvasGroup != null)
                btn.canvasGroup.alpha = exists ? _enabledAlpha : _disabledAlpha;

            if (exists && !foundFirst)
            {
                firstAvailable = btn.type;
                foundFirst = true;
            }
        }

        if (foundFirst)
            SelectDifficulty(firstAvailable);
    }

    public void SelectDifficulty(DifficultyType type)
    {
        if (_currentSong == null) return;
        if (!_currentSong.HasDifficulty(type)) return;

        _selectedDifficulty = type;

        UpdateButtonVisuals();

        OnDifficultySelected?.Invoke(type);

        Debug.Log("[DifficultySelector] Selected: " + type);
    }

    private void UpdateButtonVisuals()
    {
        foreach (var btn in _buttons)
        {
            if (btn.buttonImage == null) continue;

            bool isSelected = (btn.type == _selectedDifficulty);

            Color c = btn.buttonImage.color;
            c.a = isSelected ? 1f : 0.6f;
            btn.buttonImage.color = c;
        }
    }

    public DifficultyType GetSelectedDifficulty()
    {
        return _selectedDifficulty;
    }
}