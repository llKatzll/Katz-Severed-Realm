using TMPro;
using UnityEngine;

public class SongNameSizer : MonoBehaviour
{
    public TMP_Text textMesh;

    public float maxFontSize = 95f;      // 짧은 글자일 때 크기
    public float minFontSize = 42f;      // 긴 글자일 때 최소 크기
    public float maxAllowedWidth = 1180f; // 박스 너비에 맞게 조정

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