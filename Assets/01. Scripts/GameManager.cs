using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    public SongData SelectedSong { get; private set; }
    public DifficultyType SelectedDifficulty { get; private set; }
    public bool AnomalyEnabled { get; private set; }

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetSong(SongData song, DifficultyType difficulty)
    {
        SelectedSong = song;
        SelectedDifficulty = difficulty;
    }

    public void SetAnomaly(bool enabled)
    {
        AnomalyEnabled = enabled;
    }
}
