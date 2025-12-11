using UnityEngine;

public class HitLine : MonoBehaviour
{
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
