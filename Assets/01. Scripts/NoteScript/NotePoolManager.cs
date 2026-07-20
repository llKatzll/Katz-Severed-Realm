using System.Collections.Generic;
using UnityEngine;

public class NotePoolManager : MonoBehaviour
{
    public static NotePoolManager I { get; private set; }

    private const int MaxPoolSize = 128;

    private readonly Dictionary<int, Stack<Note>> _pools = new Dictionary<int, Stack<Note>>();
    private Transform _root;
    private readonly List<ParticleSystem> _pssList = new List<ParticleSystem>(16);
    private readonly List<TrailRenderer> _trailList = new List<TrailRenderer>(8);

    public static NotePoolManager Ensure()
    {
        if (I != null) return I;
        var go = new GameObject("[NotePool]");
        go.AddComponent<NotePoolManager>();
        return I;
    }

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        var rootGo = new GameObject("[NotePoolRoot]");
        rootGo.transform.SetParent(transform, false);
        _root = rootGo.transform;
    }

    private void OnDestroy()
    {
        if (I == this) I = null;
    }

    private Stack<Note> GetStack(int key)
    {
        Stack<Note> stack;
        if (!_pools.TryGetValue(key, out stack))
        {
            stack = new Stack<Note>(32);
            _pools[key] = stack;
        }
        return stack;
    }

    public void Prewarm(Note prefab, int count)
    {
        if (prefab == null || count <= 0) return;

        int key = prefab.gameObject.GetInstanceID();
        Stack<Note> stack = GetStack(key);

        for (int i = 0; i < count; i++)
        {
            Note n = Instantiate(prefab, _root);
            n.MarkPooled(key);
            n.gameObject.SetActive(false);
            stack.Push(n);
        }
    }

    public Note Spawn(Note prefab)
    {
        if (prefab == null) return null;

        int key = prefab.gameObject.GetInstanceID();
        Stack<Note> stack = GetStack(key);

        Note n = null;
        while (stack.Count > 0)
        {
            var candidate = stack.Pop();
            if (candidate != null) { n = candidate; break; }
        }

        if (n == null)
        {
            n = Instantiate(prefab);
            n.MarkPooled(key);
        }

        n.transform.SetParent(null, false);
        n.transform.localScale = prefab.transform.localScale;
        n.ResetForSpawn();
        n.gameObject.SetActive(true);

        n.gameObject.GetComponentsInChildren(true, _pssList);
        for (int i = 0; i < _pssList.Count; i++)
        {
            var ps = _pssList[i];
            if (ps == null) continue;
            ps.Clear(true);
            ps.Play(true);
        }

        return n;
    }

    public void Return(Note note)
    {
        if (note == null) return;

        var go = note.gameObject;

        go.GetComponentsInChildren(true, _pssList);
        for (int i = 0; i < _pssList.Count; i++)
        {
            var ps = _pssList[i];
            if (ps == null) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            ps.Clear(true);
        }

        go.GetComponentsInChildren(true, _trailList);
        for (int i = 0; i < _trailList.Count; i++)
        {
            if (_trailList[i] != null) _trailList[i].Clear();
        }

        go.SetActive(false);
        go.transform.SetParent(_root, false);

        Stack<Note> stack = GetStack(note.PoolKey);
        if (stack.Count >= MaxPoolSize)
        {
            Destroy(go);
            return;
        }

        stack.Push(note);
    }
}
