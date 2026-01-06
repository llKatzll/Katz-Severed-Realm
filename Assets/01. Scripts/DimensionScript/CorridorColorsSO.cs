using UnityEngine;

[CreateAssetMenu(menuName = "Rhythm/Corridor FX Palette", fileName = "CorridorColorsSO")]
public class CorridorColorsSO : ScriptableObject
{
    [Header("Ground Note Colors (HDR)")]
    [ColorUsage(true, true)]
    public Color groundNoteColor = new Color(0f, 0.5f, 1f, 1f);

    [ColorUsage(true, true)]
    public Color groundHitFxColor = new Color(0f, 0.7f, 1f, 1f);

    [Header("Upper Note Colors (HDR)")]
    [ColorUsage(true, true)]
    public Color upperNoteColor = new Color(0.3f, 0.6f, 1f, 1f);

    [ColorUsage(true, true)]
    public Color upperHitFxColor = new Color(0.4f, 0.8f, 1f, 1f);

    [Header("Long Note Colors (HDR)")]
    [ColorUsage(true, true)]
    public Color groundLongNoteColor = new Color(0f, 0.4f, 0.9f, 1f);

    [ColorUsage(true, true)]
    public Color upperLongNoteColor = new Color(0.2f, 0.5f, 0.9f, 1f);

    public Color GetNoteColor(NoteSpawner.NoteType noteType, bool isLongNote = false)
    {
        if (isLongNote)
        {
            return (noteType == NoteSpawner.NoteType.Ground)
                ? groundLongNoteColor
                : upperLongNoteColor;
        }

        return (noteType == NoteSpawner.NoteType.Ground)
            ? groundNoteColor
            : upperNoteColor;
    }

    public Color GetHitFxColor(NoteSpawner.NoteType noteType)
    {
        return (noteType == NoteSpawner.NoteType.Ground)
            ? groundHitFxColor
            : upperHitFxColor;
    }
}