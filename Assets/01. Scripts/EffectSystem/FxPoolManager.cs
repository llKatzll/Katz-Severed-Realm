using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FxPoolManager : MonoBehaviour
{
    public static FxPoolManager I { get; private set; }

    [System.Serializable]
    public class PrewarmEntry
    {
        public GameObject prefab;
        public int initialSize = 16;
    }

    [Header("Prewarm")]
    [SerializeField] private PrewarmEntry[] _prewarmEntries;

    [Header("Defaults")]
    [SerializeField] private int _defaultInitialSize = 16;
    [SerializeField] private bool _expandable = true;
    [SerializeField] private int _maxPoolSize = 128;

    private readonly Dictionary<int, Stack<GameObject>> _pools = new Dictionary<int, Stack<GameObject>>();
    private readonly Dictionary<int, GameObject> _keyToPrefab = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, int> _instanceToKey = new Dictionary<int, int>();
    private Transform _root;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;

        var rootGo = new GameObject("[FxPool]");
        rootGo.transform.SetParent(transform, false);
        _root = rootGo.transform;
    }

    private void Start()
    {
        if (_prewarmEntries == null) return;
        for (int i = 0; i < _prewarmEntries.Length; i++)
        {
            var e = _prewarmEntries[i];
            if (e == null || e.prefab == null) continue;
            int size = e.initialSize > 0 ? e.initialSize : _defaultInitialSize;
            Prewarm(e.prefab, size);
        }
    }

    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        int key = prefab.GetInstanceID();

        Stack<GameObject> stack;
        if (!_pools.TryGetValue(key, out stack))
        {
            stack = new Stack<GameObject>(count);
            _pools[key] = stack;
            _keyToPrefab[key] = prefab;
        }

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(prefab, _root);
            go.SetActive(false);
            stack.Push(go);
        }
    }

    public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, float returnAfter)
    {
        if (prefab == null) return null;
        int key = prefab.GetInstanceID();

        Stack<GameObject> stack;
        if (!_pools.TryGetValue(key, out stack))
        {
            stack = new Stack<GameObject>(_defaultInitialSize);
            _pools[key] = stack;
            _keyToPrefab[key] = prefab;
        }

        GameObject go = null;
        while (stack.Count > 0)
        {
            var candidate = stack.Pop();
            if (candidate != null) { go = candidate; break; }
        }

        if (go == null)
        {
            if (!_expandable) return null;
            go = Instantiate(prefab);
        }

        go.transform.SetParent(null, false);
        go.transform.position = pos;
        go.transform.rotation = rot;
        go.SetActive(true);

        var pss = go.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < pss.Length; i++)
        {
            var ps = pss[i];
            if (ps == null) continue;
            ps.Clear(true);
            ps.Play(true);
        }

        _instanceToKey[go.GetInstanceID()] = key;

        if (returnAfter > 0f)
        {
            StartCoroutine(ReturnAfter(go, returnAfter));
        }

        return go;
    }

    public void Return(GameObject go)
    {
        if (go == null) return;
        int instId = go.GetInstanceID();

        int key;
        if (!_instanceToKey.TryGetValue(instId, out key))
        {
            Destroy(go);
            return;
        }

        var pss = go.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < pss.Length; i++)
        {
            var ps = pss[i];
            if (ps == null) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            ps.Clear(true);
        }

        go.SetActive(false);
        go.transform.SetParent(_root, false);
        _instanceToKey.Remove(instId);

        Stack<GameObject> stack;
        if (!_pools.TryGetValue(key, out stack))
        {
            stack = new Stack<GameObject>();
            _pools[key] = stack;
        }

        if (stack.Count >= _maxPoolSize)
        {
            Destroy(go);
            return;
        }

        stack.Push(go);
    }

    public void ReturnDelayed(GameObject go, float delay)
    {
        if (go == null) return;
        if (delay <= 0f) { Return(go); return; }
        StartCoroutine(ReturnAfter(go, delay));
    }

    private IEnumerator ReturnAfter(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        Return(go);
    }
}
