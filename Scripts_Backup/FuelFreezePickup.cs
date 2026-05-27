using UnityEngine;

public class FuelFreezePickup : MonoBehaviour
{
    [SerializeField] private float freezeDuration = 5f;

    [Header("Cleanup")]
    [Tooltip("Extra viewport margin below the screen before the object is destroyed.")]
    [SerializeField] private float offScreenMargin = 0.1f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);
        if (viewportPos.y < -offScreenMargin)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SpeedManager speedManager = FindFirstObjectByType<SpeedManager>();
            if (speedManager != null)
                speedManager.ActivateFuelFreeze(freezeDuration);

            Destroy(gameObject);
        }
    }
}
