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
    public int level; //레벨
    public float constant; //상수

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
    public string duration;

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
}