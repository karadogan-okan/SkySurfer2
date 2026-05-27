using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Central object pool manager.
/// Spawners call PoolManager.Instance.Get() instead of Instantiate().
/// Objects call PoolManager.Instance.Release(gameObject) instead of Destroy().
/// No extra components needed on pooled objects.
/// </summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [Tooltip("Max inactive objects kept per prefab type. For most mobile endless runners 3–5 is enough.")]
    public int maxPoolSize = 5;

    // One pool per unique prefab (keyed by prefab instance ID)
    private readonly Dictionary<int, ObjectPool<GameObject>> _prefabPools = new();

    // Maps live instance ID → its pool so Release() knows where to return it
    private readonly Dictionary<int, ObjectPool<GameObject>> _instancePools = new();

    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Get a pooled instance of this prefab, positioned and activated.
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var pool = GetOrCreatePool(prefab);
        GameObject obj = pool.Get();
        obj.transform.SetPositionAndRotation(position, rotation);

        // Track which pool this instance belongs to
        _instancePools[obj.GetInstanceID()] = pool;

        return obj;
    }

    /// <summary>
    /// Return an object to its pool. Call instead of Destroy(gameObject).
    /// </summary>
    public void Release(GameObject obj)
    {
        int id = obj.GetInstanceID();
        if (_instancePools.TryGetValue(id, out var pool))
        {
            _instancePools.Remove(id);
            pool.Release(obj);  // SetActive(false) — stays in hierarchy, ready to reuse
        }
        else
        {
            Destroy(obj);  // Fallback: not a pooled object
        }
    }

    private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
    {
        int id = prefab.GetInstanceID();
        if (_prefabPools.TryGetValue(id, out var existing))
            return existing;

        var pool = new ObjectPool<GameObject>(
            createFunc:      () => Instantiate(prefab),
            actionOnGet:     obj => obj.SetActive(true),
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: obj => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: 3,
            maxSize:         maxPoolSize
        );

        _prefabPools[id] = pool;
        return pool;
    }
}
