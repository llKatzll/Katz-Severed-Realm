using UnityEngine;

public class PersistentSettingsUI : MonoBehaviour
{
    public static PersistentSettingsUI I { get; private set; }

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
}
