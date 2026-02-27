using UnityEngine;
using UnityEngine.UI;

public class Selected : MonoBehaviour
{
    private void Start()
    {
        var _btn = GetComponent<Button>();
        if (_btn != null)
        {
            _btn.onClick.AddListener(OnPressed);
        }
    }

    public void OnPressed()
    {
        Debug.Log("Button Selected)");
    }
}