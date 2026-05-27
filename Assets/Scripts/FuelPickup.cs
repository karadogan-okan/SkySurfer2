using UnityEngine;

public class FuelPickup : MonoBehaviour
{
    public float fuelAmount = 20f;

    [Header("Cleanup")]
    public float offScreenMargin = 0.1f;

    private Camera mainCamera;

    void OnEnable()
    {
        // Reset camera ref in case it changed (e.g. scene reload)
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Scroll down with the world
        if (SpeedManager.Instance != null)
            transform.position += Vector3.down * SpeedManager.Instance.ScrollSpeed * Time.deltaTime;

        DestroyIfOffScreen();
    }

    void DestroyIfOffScreen()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 vp = mainCamera.WorldToViewportPoint(transform.position);
        if (vp.y < -offScreenMargin)
            ReturnToPool();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (SpeedManager.Instance != null)
                SpeedManager.Instance.AddFuel(fuelAmount);

            ReturnToPool();
        }
    }

    void ReturnToPool()
    {
        var pooled = GetComponent<PooledObject>();
        if (pooled != null) pooled.ReturnToPool();
        else Destroy(gameObject);
    }
}
