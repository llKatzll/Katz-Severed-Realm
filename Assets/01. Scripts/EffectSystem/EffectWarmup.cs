using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectWarmup : MonoBehaviour
{
    [SerializeField] private bool _autoIncludeEffPresets = true;
    [SerializeField] private GameObject[] _additionalPrefabs;
    [SerializeField] private Vector3 _warmupPosition = new Vector3(0f, -100f, 0f);
    [SerializeField] private float _waitSec = 1f;
    [SerializeField] private int _poolPrewarmCount = 4;

    public IEnumerator WarmupAll()
    {
        List<GameObject> prefabs = new List<GameObject>();

        if (_autoIncludeEffPresets)
        {
            foreach (var preset in EffectPresetCache.AllByCategory(EffectCategory.Eff))
            {
                if (preset != null && preset.particlePrefab != null)
                {
                    prefabs.Add(preset.particlePrefab);
                }
            }
        }

        if (_additionalPrefabs != null)
        {
            for (int i = 0; i < _additionalPrefabs.Length; i++)
            {
                if (_additionalPrefabs[i] != null) prefabs.Add(_additionalPrefabs[i]);
            }
        }

        if (prefabs.Count == 0) yield break;

        if (FxPoolManager.I != null && _poolPrewarmCount > 0)
        {
            for (int i = 0; i < prefabs.Count; i++)
            {
                FxPoolManager.I.Prewarm(prefabs[i], _poolPrewarmCount);
            }
            yield return null;
        }

        List<GameObject> spawned = new List<GameObject>(prefabs.Count);
        for (int i = 0; i < prefabs.Count; i++)
        {
            var go = Instantiate(prefabs[i], _warmupPosition, Quaternion.identity);
            spawned.Add(go);
        }

        yield return null;
        if (_waitSec > 0f) yield return new WaitForSecondsRealtime(_waitSec);

        for (int i = 0; i < spawned.Count; i++)
        {
            if (spawned[i] != null) Destroy(spawned[i]);
        }
    }
}
