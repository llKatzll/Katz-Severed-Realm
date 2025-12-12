using UnityEngine;

public class NoteHitLine : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Tap"))
            Debug.LogWarning("ÅÇ ÅÂ±×µÊ");

        if (other.CompareTag("Upper"))
            Debug.LogWarning("¾îÆÛ ÅÂ±×µÊ");

        if (other.CompareTag("Splash"))
            Debug.LogWarning("½ºÇÃ·¡½Ã ÅÂ±×µÊ");
    }
}
