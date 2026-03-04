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
    public bool exists = true;

    [Header("Song_Records")]
    public int highScore;
    public float accuracy;
}

[CreateAssetMenu(menuName = "K_S_R/Song Data", fileName = "NewSong")]
public class SongData : ScriptableObject
{
    [Header("Basic_Info")]
    public string songName;
    public string artist;
    public float bpm;
    public bool hasTempoShift;
    public string duration;

    [Header("Credits")]
    public string charter;
    public string mapper;

    [Header("Assets")]
    public Sprite songImage;
    public AudioClip previewClip;
    public AudioClip fullClip;

    [Header("Difficulties")]
    public DifficultyData[] difficulties = new DifficultyData[6];

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
        var diff = GetDifficulty(type);
        return diff != null && diff.exists;
    }
}