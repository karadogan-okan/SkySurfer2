using UnityEngine;

public class FuelSpawner : MonoBehaviour
{
    [Header("Fuel Prefabs")]
    public GameObject smallFuelPrefab;  // gives 20f
    public GameObject mediumFuelPrefab; // gives 30f
    public GameObject fullFuelPrefab;   // fills tank completely

    [Header("Spawn Settings")]
    public float spawnIntervalMin = 2f;
    public float spawnIntervalMax = 4f;
    [Tooltip("How far above the top of the screen fuel spawns.")]
    public float spawnAheadDistance = 2f;

    [Header("Spawn Chances (out of 100)")]
    public float smallFuelChance = 60f;
    public float mediumFuelChance = 30f;
    public float fullFuelChance = 10f;

    [Header("Spawn Bounds")]
    public float spawnEdgeMargin = 0.5f;

    [Header("References")]
    public SpeedManager speedManager;

    private float timer;
    private float nextSpawnInterval;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        nextSpawnInterval = Random.Range(spawnIntervalMin, spawnIntervalMax);
    }

    void Update()
    {
        if (!speedManager.HasLaunched) return;
        if (speedManager.IsGameOver) return;

        timer += Time.deltaTime;
        if (timer >= nextSpawnInterval)
        {
            timer = 0f;
            nextSpawnInterval = Random.Range(spawnIntervalMin, spawnIntervalMax);
            SpawnFuel();
        }
    }

    void SpawnFuel()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float spawnX = Random.Range(-halfWidth + spawnEdgeMargin, halfWidth - spawnEdgeMargin);
        float spawnY = mainCamera.transform.position.y + mainCamera.orthographicSize + spawnAheadDistance;

        float roll = Random.Range(0f, 100f);
        GameObject prefabToSpawn;

        if (roll < fullFuelChance)
            prefabToSpawn = fullFuelPrefab;
        else if (roll < fullFuelChance + mediumFuelChance)
            prefabToSpawn = mediumFuelPrefab;
        else
            prefabToSpawn = smallFuelPrefab;

        if (prefabToSpawn != null)
            PoolManager.Instance.Get(prefabToSpawn, new Vector3(spawnX, spawnY, 0f), Quaternion.identity);
    }
}
