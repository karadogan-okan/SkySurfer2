using UnityEngine;

public class FuelFreezePickup : MonoBehaviour
{
    [SerializeField] private float freezeDuration = 5f;

    [Header("Cleanup")]
    [SerializeField] private float offScreenMargin = 0.1f;

    private Camera mainCamera;

    void OnEnable()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Scroll down with the world
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
        if (other.CompareTag("Player"))
        {
            if (SpeedManager.Instance != null)
                SpeedManager.Instance.ActivateFuelFreeze(freezeDuration);

            // Tight frost burst right on the pickup itself so it reads as the
            // item freezing, not a screen-wide effect.
            FreezeParticleEffect.Spawn(transform.position);

            ReturnToPool();
        }
    }

    void ReturnToPool()
    {
        if (PoolManager.Instance != null)
            PoolManager.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }
}
