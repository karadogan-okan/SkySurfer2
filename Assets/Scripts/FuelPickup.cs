using UnityEngine;

public class FuelPickup : MonoBehaviour
{
    public float fuelAmount = 20f;

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