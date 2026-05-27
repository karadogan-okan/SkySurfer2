using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Central object pool manager.
/// Each prefab gets its own ObjectPool<GameObject>.
/// Spawners call PoolManager.Instance.Get() instead of Instantiate().
/// Objects call their PooledObject.ReturnToPool() instead of Destroy().
/// </summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [Tooltip("Max number of inactive objects kept per prefab type. Excess are destroyed.")]
    public int maxPoolSize = 20;

    // One pool per unique prefab (keyed by prefab instance ID)
    private readonly Dictionary<int, ObjectPool<GameObject>> _pools = new();

    // Maps live instance ID → pool, so ReturnToPool knows where to go
    private readonly Dictionary<int, ObjectPool<GameObject>> _instanceToPool = new();

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

        // Stamp the instance so it can find its way back to the right pool
        var pooled = obj.GetComponent<PooledObject>();
        if (pooled == null) pooled = obj.AddComponent<PooledObject>();
        pooled.Init(pool);

        _instanceToPool[obj.GetInstanceID()] = pool;

        return obj;
    }

    /// <summary>
    /// Return an object to its pool (called by PooledObject.ReturnToPool).
    /// </summary>
    public void Return(GameObject obj, ObjectPool<GameObject> pool)
    {
        _instanceToPool.Remove(obj.GetInstanceID());
        pool.Release(obj);
    }

    private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
    {
        int id = prefab.GetInstanceID();
        if (_pools.TryGetValue(id, out var existing))
            return existing;

        var pool = new ObjectPool<GameObject>(
            createFunc:      () => Instantiate(prefab),
            actionOnGet:     obj => obj.SetActive(true),
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: obj => Destroy(obj),
            collectionCheck: false,
            defaultCapacity: 8,
            maxSize:         maxPoolSize
        );

        _pools[id] = pool;
        return pool;
    }
}
