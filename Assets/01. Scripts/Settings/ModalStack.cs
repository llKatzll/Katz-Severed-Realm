using System.Collections.Generic;

public interface IModalPanel
{
    void OnEscape();
}

public static class ModalStack
{
    private static readonly List<IModalPanel> _stack = new List<IModalPanel>(8);

    public static int Count => _stack.Count;

    public static IModalPanel Top => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;

    public static void Push(IModalPanel panel)
    {
        if (panel == null) return;
        _stack.Remove(panel);
        _stack.Add(panel);
    }

    public static void Remove(IModalPanel panel)
    {
        if (panel == null) return;
        _stack.Remove(panel);
    }

    public static void Clear() => _stack.Clear();
}
