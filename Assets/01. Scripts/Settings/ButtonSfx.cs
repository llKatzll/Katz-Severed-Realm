using UnityEngine;
using UnityEngine.UI;

public class ButtonSfx : MonoBehaviour
{
    [SerializeField] private bool _includeChildren = false;

    private void Start()
    {
        if (_includeChildren)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++) Hook(buttons[i]);
        }
        else
        {
            Hook(GetComponent<Button>());
        }
    }

    private void Hook(Button button)
    {
        if (button == null) return;
        if (button.GetComponent<ButtonSfxIgnore>() != null) return;
        button.onClick.AddListener(PlayClick);
    }

    private void PlayClick()
    {
        if (SfxManager.I != null) SfxManager.I.PlayButton();
    }
}
