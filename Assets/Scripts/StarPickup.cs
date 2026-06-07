using UnityEngine;

public class StarPickup : MonoBehaviour
{
    [Header("Cleanup")]
    public float offScreenMargin = 0.1f;

    private Camera mainCamera;

    void OnEnable()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (SpeedManager.Instance != null)
            transform.position += Vector3.down * SpeedManager.Instance.ScrollSpeed * Time.deltaTime;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 vp = mainCamera.WorldToViewportPoint(transform.position);
        if (vp.y < -offScreenMargin)
            ReturnToPool();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (StarManager.Instance != null)
            StarManager.Instance.AddStar();

        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (PoolManager.Instance != null)
            PoolManager.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }
}
