using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DifficultySelector : MonoBehaviour
{
    [System.Serializable]
    public class DifficultyButton
    {
        public DifficultyType type;
        public Button button;
        public Image buttonImage;
    }

    [Header("Buttons")]
    [SerializeField] private DifficultyButton[] _buttons;

    [Header("Canvas Group (All Buttons)")]
    [SerializeField] private CanvasGroup _allButtonsGroup;

    [Header("Alpha Hit Test")]
    [SerializeField] private float _alphaThreshold = 0.1f;

    private SongData _currentSong;
    private DifficultyType _selectedDifficulty;
    private int _selectedIndex = 0;
    private Coroutine _fadeCoroutine;

    public event System.Action<DifficultyType> OnDifficultySelected;

    private void Start()
    {
        foreach (var btn in _buttons)
        {
            if (btn.button != null)
            {
                DifficultyType capturedType = btn.type;
                btn.button.onClick.AddListener(() => OnUserPickDifficulty(capturedType));
            }

            if (btn.buttonImage != null)
            {
                btn.buttonImage.alphaHitTestMinimumThreshold = _alphaThreshold;
            }
        }

        if (_allButtonsGroup != null)
        {
            _allButtonsGroup.alpha = 0f;
            _allButtonsGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }

    private void Update()
    {
        HandleKeyboardInput();
    }

    private void HandleKeyboardInput()
    {
        if (_currentSong == null) return;
        if (_allButtonsGroup != null && _allButtonsGroup.alpha < 0.5f) return;

        if (Input.GetKeyDown(KeyCode.A))
        {
            SelectPrevious();
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            SelectNext();
        }
    }

    private void SelectPrevious()
    {
        int startIndex = _selectedIndex;
        int count = _buttons.Length;

        for (int i = 1; i <= count; i++)
        {
            int newIndex = (startIndex - i + count) % count;
            if (_buttons[newIndex].button != null && _buttons[newIndex].button.interactable)
            {
                _selectedIndex = newIndex;
                OnUserPickDifficulty(_buttons[newIndex].type);
                return;
            }
        }
    }

    private void SelectNext()
    {
        int startIndex = _selectedIndex;
        int count = _buttons.Length;

        for (int i = 1; i <= count; i++)
        {
            int newIndex = (startIndex + i) % count;
            if (_buttons[newIndex].button != null && _buttons[newIndex].button.interactable)
            {
                _selectedIndex = newIndex;
                OnUserPickDifficulty(_buttons[newIndex].type);
                return;
            }
        }
    }

    public void HideInstant()
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        if (_allButtonsGroup != null)
        {
            _allButtonsGroup.alpha = 0f;
            _allButtonsGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);
    }

    public void ShowWithFade(float duration)
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        gameObject.SetActive(true);

        if (_allButtonsGroup != null)
            _allButtonsGroup.blocksRaycasts = false;

        _fadeCoroutine = StartCoroutine(FadeInRoutine(duration));
    }

    private IEnumerator FadeInRoutine(float duration)
    {
        if (_allButtonsGroup == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _allButtonsGroup.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }

        _allButtonsGroup.alpha = 1f;
        _allButtonsGroup.blocksRaycasts = true;
    }

    public void SetupForSong(SongData song)
    {
        _currentSong = song;

        DifficultyType firstAvailable = DifficultyType.Easy;
        bool foundFirst = false;

        for (int i = 0; i < _buttons.Length; i++)
        {
            var btn = _buttons[i];
            bool hasDiff = song != null && song.HasDifficulty(btn.type);

            if (btn.button != null)
                btn.button.interactable = hasDiff;

            if (hasDiff && !foundFirst)
            {
                firstAvailable = btn.type;
                _selectedIndex = i;
                foundFirst = true;
            }
        }

        if (foundFirst)
            SelectDifficulty(firstAvailable);
    }

    private void OnUserPickDifficulty(DifficultyType type)
    {
        if (SfxManager.I != null) SfxManager.I.PlayDiff();
        SelectDifficulty(type);
    }

    public void SelectDifficulty(DifficultyType type)
    {
        if (_currentSong == null) return;
        if (!_currentSong.HasDifficulty(type)) return;

        _selectedDifficulty = type;

        for (int i = 0; i < _buttons.Length; i++)
        {
            if (_buttons[i].type == type)
            {
                _selectedIndex = i;
                break;
            }
        }

        UpdateButtonVisuals();

        OnDifficultySelected?.Invoke(type);
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