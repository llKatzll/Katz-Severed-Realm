using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SongBar : MonoBehaviour
{
    [Header("Song Data")]
    [SerializeField] private SongData _songData;
    public SongData SongData => _songData;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text _songNameText;

    [Header("Selection Detection")]
    [SerializeField] private float _selectionThreshold = 20f;

    private RectTransform _rectTransform;
    private Transform _pivot;
    private bool _isSelected;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        if (_songData != null)
        {
            UpdateDisplay();
        }
    }

    private void Start()
    {
        _pivot = transform.parent;
    }

    private void Update()
    {
        CheckSelection();
    }

    private void CheckSelection()
    {
        if (_pivot == null) return;

        float pivotZ = _pivot.localEulerAngles.z;
        float barLocalZ = transform.localEulerAngles.z;
        float totalAngle = NormalizeAngle(pivotZ + barLocalZ);

        bool shouldBeSelected = Mathf.Abs(totalAngle) <= _selectionThreshold;

        if (shouldBeSelected && !_isSelected)
        {
            _isSelected = true;
            OnSelected();
        }
        else if (!shouldBeSelected && _isSelected)
        {
            _isSelected = false;
            OnDeselected();
        }
    }

    private void OnSelected()
    {
        if (SongSelectManager.I != null)
        {
            SongSelectManager.I.OnSongBarSelected(this);
        }
    }

    private void OnDeselected()
    {
        if (SongSelectManager.I != null)
        {
            SongSelectManager.I.OnSongBarDeselected(this);
        }
    }

    public void SetSongData(SongData data)
    {
        _songData = data;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (_songData == null) return;

        if (_songNameText != null)
            _songNameText.text = _songData.songName;
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}