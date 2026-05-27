using UnityEngine;

public class FuelPickup : MonoBehaviour
{
    public float fuelAmount = 20f;

    [Header("Cleanup")]
    [Tooltip("Extra viewport margin below the screen before the object is destroyed.")]
    public float offScreenMargin = 0.1f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        DestroyIfOffScreen();
    }

    void DestroyIfOffScreen()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        // Convert the object's world position to viewport space.
        // x and y are 0..1 when inside the camera view.
        Vector3 viewportPos = mainCamera.WorldToViewportPoint(transform.position);

        // Once the object has scrolled below the bottom of the screen, remove it.
        if (viewportPos.y < -offScreenMargin)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SpeedManager speedManager = FindFirstObjectByType<SpeedManager>();
            if (speedManager != null)
                speedManager.AddFuel(fuelAmount);

            Destroy(gameObject);
        }
    }
}