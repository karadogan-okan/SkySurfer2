using UnityEngine;

/// <summary>
/// Unified spawn coordinator. Replaces ObstacleSpawner, FuelSpawner, FreezeSpawner, StarSpawner.
/// Each wave assigns a role to every lane — obstacle, stars, fuel, freeze, or empty.
/// Overlap between obstacles and pickups is impossible by design.
/// Moving obstacles (planes, UFOs) use a separate timer since they don't use lanes.
/// </summary>
public class LaneSpawner : MonoBehaviour
{
    [Header("Lane Settings")]
    public float[] lanePositions = { -2f, 0f, 2f };

    [Header("Wave Settings")]
    [Tooltip("How far above the top of the screen objects spawn.")]
    public float spawnAheadDistance = 2f;
    public float waveIntervalMin = 1.5f;
    public float waveIntervalMax = 3f;
    [Tooltip("How many lanes get an obstacle per wave.")]
    public int obstacleCountMin = 1;
    public int obstacleCountMax = 2;

    [Header("Static Obstacle Prefabs")]
    public GameObject[] staticObstaclePrefabs;

    [Header("Moving Obstacle Settings")]
    public GameObject[] movingObstaclePrefabs;
    public float movingSpawnIntervalMin = 3f;
    public float movingSpawnIntervalMax = 6f;
    [Tooltip("How far above the screen edge moving obstacles spawn. 0 = right at the edge.")]
    public float movingSpawnAhead = 0f;

    [Header("Pickup Prefabs")]
    public GameObject starPrefab;
    public GameObject smallFuelPrefab;
    public GameObject mediumFuelPrefab;
    public GameObject fullFuelPrefab;
    public GameObject freezePrefab;

    [Header("Star Deck Settings")]
    public int starsPerDeck = 10;
    public float starSpacing = 0.5f;

    [Header("Pickup Weights (per non-obstacle lane)")]
    [Tooltip("Relative weights — higher = more likely to appear.")]
    public float starsWeight    = 30f;
    public float smallFuelWeight  = 25f;
    public float mediumFuelWeight = 15f;
    public float fullFuelWeight   = 5f;
    public float freezeWeight     = 5f;
    public float emptyWeight      = 20f;

    [Header("References")]
    public SpeedManager speedManager;

    private float waveTimer;
    private float movingTimer;
    private float nextWaveInterval;
    private float nextMovingInterval;
    private Camera mainCamera;

    enum LaneRole { Empty, Obstacle, Stars, SmallFuel, MediumFuel, FullFuel, Freeze }

    void Start()
    {
        mainCamera = Camera.main;
        nextWaveInterval   = Random.Range(waveIntervalMin, waveIntervalMax);
        nextMovingInterval = Random.Range(movingSpawnIntervalMin, movingSpawnIntervalMax);
    }

    void Update()
    {
        if (!speedManager.HasLaunched) return;
        if (speedManager.IsGameOver) return;

        waveTimer += Time.deltaTime;
        if (waveTimer >= nextWaveInterval)
        {
            waveTimer = 0f;
            nextWaveInterval = Random.Range(waveIntervalMin, waveIntervalMax);
            SpawnWave();
        }

        movingTimer += Time.deltaTime;
        if (movingTimer >= nextMovingInterval)
        {
            movingTimer = 0f;
            nextMovingInterval = Random.Range(movingSpawnIntervalMin, movingSpawnIntervalMax);
            SpawnMovingObstacle();
        }
    }

    void SpawnWave()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        float spawnY    = mainCamera.transform.position.y + mainCamera.orthographicSize + spawnAheadDistance;
        int   laneCount = lanePositions.Length;

        // Decide how many lanes get obstacles this wave
        int obstacleCount = Mathf.Clamp(
            Random.Range(obstacleCountMin, obstacleCountMax + 1),
            1, laneCount
        );

        // Fisher-Yates shuffle lane indices
        int[] indices = new int[laneCount];
        for (int i = 0; i < laneCount; i++) indices[i] = i;
        for (int i = laneCount - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // Assign roles — first N shuffled lanes get obstacles, rest roll for pickups
        LaneRole[] roles = new LaneRole[laneCount];
        for (int i = 0; i < laneCount; i++)
            roles[indices[i]] = i < obstacleCount ? LaneRole.Obstacle : RollPickupRole();

        // Spawn each lane
        for (int i = 0; i < laneCount; i++)
            SpawnLane(roles[i], lanePositions[i], spawnY);
    }

    LaneRole RollPickupRole()
    {
        float total = starsWeight + smallFuelWeight + mediumFuelWeight
                    + fullFuelWeight + freezeWeight + emptyWeight;
        float roll = Random.Range(0f, total);

        if ((roll -= starsWeight)      < 0f) return LaneRole.Stars;
        if ((roll -= smallFuelWeight)  < 0f) return LaneRole.SmallFuel;
        if ((roll -= mediumFuelWeight) < 0f) return LaneRole.MediumFuel;
        if ((roll -= fullFuelWeight)   < 0f) return LaneRole.FullFuel;
        if ((roll -= freezeWeight)     < 0f) return LaneRole.Freeze;
        return LaneRole.Empty;
    }

    void SpawnLane(LaneRole role, float laneX, float spawnY)
    {
        switch (role)
        {
            case LaneRole.Obstacle:
                if (staticObstaclePrefabs.Length == 0) return;
                var obsPrefab = staticObstaclePrefabs[Random.Range(0, staticObstaclePrefabs.Length)];
                if (obsPrefab != null)
                    PoolManager.Instance.Get(obsPrefab, new Vector3(laneX, spawnY, 0f), Quaternion.identity);
                break;

            case LaneRole.Stars:
                if (starPrefab == null) return;
                for (int i = 0; i < starsPerDeck; i++)
                    PoolManager.Instance.Get(starPrefab, new Vector3(laneX, spawnY + i * starSpacing, 0f), Quaternion.identity);
                break;

            case LaneRole.SmallFuel:
                if (smallFuelPrefab != null)
                    PoolManager.Instance.Get(smallFuelPrefab, new Vector3(laneX, spawnY, 0f), Quaternion.identity);
                break;

            case LaneRole.MediumFuel:
                if (mediumFuelPrefab != null)
                    PoolManager.Instance.Get(mediumFuelPrefab, new Vector3(laneX, spawnY, 0f), Quaternion.identity);
                break;

            case LaneRole.FullFuel:
                if (fullFuelPrefab != null)
                    PoolManager.Instance.Get(fullFuelPrefab, new Vector3(laneX, spawnY, 0f), Quaternion.identity);
                break;

            case LaneRole.Freeze:
                if (freezePrefab != null)
                    PoolManager.Instance.Get(freezePrefab, new Vector3(laneX, spawnY, 0f), Quaternion.identity);
                break;
        }
    }

    void SpawnMovingObstacle()
    {
        if (movingObstaclePrefabs.Length == 0) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float spawnY    = mainCamera.transform.position.y + mainCamera.orthographicSize + movingSpawnAhead;

        var prefab = movingObstaclePrefabs[Random.Range(0, movingObstaclePrefabs.Length)];
        if (prefab == null) return;

        bool fromLeft = Random.value > 0.5f;
        float spawnX  = fromLeft ? -halfWidth : halfWidth;

        GameObject obj = PoolManager.Instance.Get(prefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity);
        obj.GetComponent<Obstacle>()?.InitMovement(fromLeft);
    }
}
