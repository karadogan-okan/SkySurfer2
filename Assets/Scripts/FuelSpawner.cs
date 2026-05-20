using UnityEngine;

public class FuelSpawner : MonoBehaviour
{
    [Header("Fuel Prefabs")]
    public GameObject smallFuelPrefab;  // gives 20f
    public GameObject mediumFuelPrefab; // gives 30f
    public GameObject fullFuelPrefab;   // fills tank completely

    [Header("Spawn Settings")]
    public float spawnInterval = 3f;
    public float spawnAheadDistance = 15f;

    [Header("Spawn Chances (out of 100)")]
    public float smallFuelChance = 60f;  // 60% chance
    public float mediumFuelChance = 30f; // 30% chance
    public float fullFuelChance = 10f;   // 10% chance

    [Header("Lanes")]
    public float[] lanePositions = { -2f, 0f, 2f };

    [Header("References")]
    public Transform player;
    public SpeedManager speedManager;

    private float timer;

    void Update()
    {
        if (!speedManager.HasLaunched) return;
        if (speedManager.IsGameOver) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnFuel();
        }
    }

    void SpawnFuel()
    {
        int randomLane = Random.Range(0, lanePositions.Length);
        float spawnX = lanePositions[randomLane];
        float spawnY = player.position.y + spawnAheadDistance;

        // Pick fuel type based on chance
        float roll = Random.Range(0f, 100f);
        GameObject prefabToSpawn;

        if (roll < fullFuelChance)
            prefabToSpawn = fullFuelPrefab;
        else if (roll < fullFuelChance + mediumFuelChance)
            prefabToSpawn = mediumFuelPrefab;
        else
            prefabToSpawn = smallFuelPrefab;

        if (prefabToSpawn != null)
            Instantiate(prefabToSpawn, new Vector3(spawnX, spawnY, 0f), Quaternion.identity);
    }
}