using UnityEngine;

public enum DifficultyType
{
    Easy,
    Medium,
    Hard,
    Insane,
    Master,
    Del
}

[System.Serializable]
public class DifficultyData
{
    public DifficultyType type;
    public int level;
    public float constant;

    [Header("Records")]
    public int highScore;
    public float accuracy;
}

[CreateAssetMenu(menuName = "Katz/Song Data", fileName = "NewSong")]
public class SongData : ScriptableObject
{
    [Header("Basic Info")]
    public string songName;
    public string artist;
    public float bpm;
    public bool hasTempoShift;
    public float durationSeconds;

    [Header("Timing")]
    public double audioOffsetSec;

    [Header("Credits")]
    public string charter;
    public string mapper;

    [Header("Assets")]
    public Sprite songImage;
    public AudioClip previewClip;
    public AudioClip fullClip;

    [Header("Difficulties")]
    public DifficultyData[] difficulties;

    public DifficultyData GetDifficulty(DifficultyType type)
    {
        foreach (var diff in difficulties)
        {
            if (diff != null && diff.type == type)
                return diff;
        }
        return null;
    }

    public bool HasDifficulty(DifficultyType type)
    {
        return GetDifficulty(type) != null;
    }

    public string GetFormattedDuration()
    {
        int minutes = (int)(durationSeconds / 60);
        int seconds = (int)(durationSeconds % 60);
        return string.Format("{0}:{1:D2}", minutes, seconds);
    }
}