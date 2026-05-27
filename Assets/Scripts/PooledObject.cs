using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// Added automatically by PoolManager to every pooled instance.
/// Call ReturnToPool() instead of Destroy(gameObject).
/// </summary>
public class PooledObject : MonoBehaviour
{
    private ObjectPool<GameObject> _pool;

    public void Init(ObjectPool<GameObject> pool)
    {
        _pool = pool;
    }

    public void ReturnToPool()
    {
        if (_pool != null)
            PoolManager.Instance.Return(gameObject, _pool);
        else
            Destroy(gameObject);
    }
}
