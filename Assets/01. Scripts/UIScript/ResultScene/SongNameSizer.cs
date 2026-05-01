using TMPro;
using UnityEngine;

public class SongNameSizer : MonoBehaviour
{
    public TMP_Text textMesh;

    public float maxFontSize = 95f;
    public float minFontSize = 42f;
    public float maxAllowedWidth = 1180f;

    void LateUpdate()
    {
        if (textMesh == null) return;

        textMesh.fontSize = maxFontSize;
        float neededWidth = textMesh.preferredWidth;

        if (neededWidth > maxAllowedWidth)
        {
            float ratio = maxAllowedWidth / neededWidth;
            textMesh.fontSize = Mathf.Max(minFontSize, maxFontSize * ratio);
        }
    }
}
