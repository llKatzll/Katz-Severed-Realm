using UnityEngine;

public class ModalEscController : MonoBehaviour
{
    private static ModalEscController _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        GameObject go = new GameObject("ModalEscController");
        _instance = go.AddComponent<ModalEscController>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        IModalPanel top = ModalStack.Top;
        if (top == null) return;

        MonoBehaviour mb = top as MonoBehaviour;
        if (mb == null || !mb.isActiveAndEnabled)
        {
            ModalStack.Remove(top);
            return;
        }

        top.OnEscape();
    }
}
