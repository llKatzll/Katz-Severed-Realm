using System;
using UnityEngine;

[DefaultExecutionOrder(-9)]
public class RuntimeColorPalette : MonoBehaviour
{
    public static RuntimeColorPalette I { get; private set; }

    [Header("Ground Colors (HDR)")]
    [SerializeField][ColorUsage(true, true)] private Color _groundDismaller;
    [SerializeField][ColorUsage(true, true)] private Color _groundSeparation;
    [SerializeField][ColorUsage(true, true)] private Color _groundCorridor;

    [Header("Upper Colors (HDR)")]
    [SerializeField][ColorUsage(true, true)] private Color _upperDismaller;
    [SerializeField][ColorUsage(true, true)] private Color _upperSeparation;
    [SerializeField][ColorUsage(true, true)] private Color _upperCorridor;

    public Color GroundDismaller => _groundDismaller;
    public Color GroundSeparation => _groundSeparation;
    public Color GroundCorridor => _groundCorridor;
    public Color UpperDismaller => _upperDismaller;
    public Color UpperSeparation => _upperSeparation;
    public Color UpperCorridor => _upperCorridor;

    public event Action OnColorsChanged;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
    }

    public Color GetNoteColor(NoteSpawner.NoteType noteType, DimensionType dimension)
    {
        if (DimensionManager.I != null && DimensionManager.I.IsCorridorActive)
        {
            return (noteType == NoteSpawner.NoteType.Ground)
                ? _groundCorridor
                : _upperCorridor;
        }

        if (noteType == NoteSpawner.NoteType.Ground)
        {
            return (dimension == DimensionType.Dismaller)
                ? _groundDismaller
                : _groundSeparation;
        }
        else
        {
            return (dimension == DimensionType.Dismaller)
                ? _upperDismaller
                : _upperSeparation;
        }
    }

    public Color GetRailColor(NoteSpawner.NoteType railType)
    {
        if (DimensionManager.I != null && DimensionManager.I.IsCorridorActive)
        {
            return (railType == NoteSpawner.NoteType.Ground)
                ? _groundCorridor
                : _upperCorridor;
        }

        DimensionType currentDim = DimensionManager.I != null
            ? DimensionManager.I.CurrentDimension
            : DimensionType.Dismaller;

        if (railType == NoteSpawner.NoteType.Ground)
        {
            return (currentDim == DimensionType.Dismaller)
                ? _groundDismaller
                : _groundSeparation;
        }
        else
        {
            return (currentDim == DimensionType.Dismaller)
                ? _upperDismaller
                : _upperSeparation;
        }
    }

    public Color GetSevHitFxColor(NoteSpawner.NoteType noteType)
    {
        if (DimensionManager.I != null && DimensionManager.I.IsCorridorActive)
        {
            return (noteType == NoteSpawner.NoteType.Ground)
                ? _groundCorridor
                : _upperCorridor;
        }

        DimensionType currentDim = DimensionManager.I != null
            ? DimensionManager.I.CurrentDimension
            : DimensionType.Dismaller;

        if (noteType == NoteSpawner.NoteType.Ground)
        {
            return (currentDim == DimensionType.Dismaller)
                ? _groundDismaller
                : _groundSeparation;
        }
        else
        {
            return (currentDim == DimensionType.Dismaller)
                ? _upperDismaller
                : _upperSeparation;
        }
    }

    public Color GetCorridorColor(NoteSpawner.NoteType noteType)
    {
        return (noteType == NoteSpawner.NoteType.Ground)
            ? _groundCorridor
            : _upperCorridor;
    }

    public void SetGroundDismaller(Color color)
    {
        _groundDismaller = color;
        OnColorsChanged?.Invoke();
    }

    public void SetGroundSeparation(Color color)
    {
        _groundSeparation = color;
        OnColorsChanged?.Invoke();
    }

    public void SetGroundCorridor(Color color)
    {
        _groundCorridor = color;
        OnColorsChanged?.Invoke();
    }

    public void SetUpperDismaller(Color color)
    {
        _upperDismaller = color;
        OnColorsChanged?.Invoke();
    }

    public void SetUpperSeparation(Color color)
    {
        _upperSeparation = color;
        OnColorsChanged?.Invoke();
    }

    public void SetUpperCorridor(Color color)
    {
        _upperCorridor = color;
        OnColorsChanged?.Invoke();
    }

    public void SetAllColors(ColorPaletteSnapshot snapshot)
    {
        _groundDismaller = snapshot.groundDismaller;
        _groundSeparation = snapshot.groundSeparation;
        _groundCorridor = snapshot.groundCorridor;
        _upperDismaller = snapshot.upperDismaller;
        _upperSeparation = snapshot.upperSeparation;
        _upperCorridor = snapshot.upperCorridor;
        OnColorsChanged?.Invoke();
    }

    public ColorPaletteSnapshot GetSnapshot()
    {
        return new ColorPaletteSnapshot
        {
            groundDismaller = _groundDismaller,
            groundSeparation = _groundSeparation,
            groundCorridor = _groundCorridor,
            upperDismaller = _upperDismaller,
            upperSeparation = _upperSeparation,
            upperCorridor = _upperCorridor
        };
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
            OnColorsChanged?.Invoke();
    }
#endif

}

[System.Serializable]
public struct ColorPaletteSnapshot
{
    [ColorUsage(true, true)] public Color groundDismaller;
    [ColorUsage(true, true)] public Color groundSeparation;
    [ColorUsage(true, true)] public Color groundCorridor;
    [ColorUsage(true, true)] public Color upperDismaller;
    [ColorUsage(true, true)] public Color upperSeparation;
    [ColorUsage(true, true)] public Color upperCorridor;
}