using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    [Tooltip("Static obstacles that sit in place — birds, balloons.")]
    public GameObject[] staticObstaclePrefabs;
    [Tooltip("Moving obstacles that cross or ping-pong the screen — planes, UFOs.")]
    public GameObject[] movingObstaclePrefabs;

    [Header("Static Spawn Settings")]
    public float staticSpawnIntervalMin = 1f;
    public float staticSpawnIntervalMax = 2.5f;
    [Tooltip("Minimum number of static obstacles spawned at once.")]
    public int staticSpawnCountMin = 1;
    [Tooltip("Maximum number of static obstacles spawned at once.")]
    public int staticSpawnCountMax = 3;

    [Header("Moving Spawn Settings")]
    public float movingSpawnIntervalMin = 3f;
    public float movingSpawnIntervalMax = 6f;

    [Header("General Settings")]
    [Tooltip("How far above the player obstacles spawn.")]
    public float spawnAheadDistance = 18f;
    [Tooltip("Margin from the screen edge when placing moving obstacles.")]
    public float spawnEdgeMargin = 1f;
    [Tooltip("Lane X positions used for static obstacle placement.")]
    public float[] lanePositions = { -2f, 0f, 2f };

    [Header("References")]
    public Transform player;
    public SpeedManager speedManager;

    private float staticTimer;
    private float movingTimer;
    private float nextStaticInterval;
    private float nextMovingInterval;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        nextStaticInterval = Random.Range(staticSpawnIntervalMin, staticSpawnIntervalMax);
        nextMovingInterval = Random.Range(movingSpawnIntervalMin, movingSpawnIntervalMax);
    }

    void Update()
    {
        if (!speedManager.HasLaunched) return;
        if (speedManager.IsGameOver) return;

        staticTimer += Time.deltaTime;
        if (staticTimer >= nextStaticInterval)
        {
            staticTimer = 0f;
            nextStaticInterval = Random.Range(staticSpawnIntervalMin, staticSpawnIntervalMax);
            SpawnStaticWave();
        }

        movingTimer += Time.deltaTime;
        if (movingTimer >= nextMovingInterval)
        {
            movingTimer = 0f;
            nextMovingInterval = Random.Range(movingSpawnIntervalMin, movingSpawnIntervalMax);
            SpawnMovingObstacle();
        }
    }

    void SpawnStaticWave()
    {
        if (staticObstaclePrefabs.Length == 0) return;
        if (lanePositions.Length == 0) return;

        float spawnY = player.position.y + spawnAheadDistance;

        // Clamp count to available lanes so we never spawn two in the same lane
        int count = Mathf.Clamp(
            Random.Range(staticSpawnCountMin, staticSpawnCountMax + 1),
            1, lanePositions.Length
        );

        // Shuffle a copy of the lane indices and pick the first `count`
        int[] indices = new int[lanePositions.Length];
        for (int i = 0; i < indices.Length; i++) indices[i] = i;
        for (int i = indices.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = staticObstaclePrefabs[Random.Range(0, staticObstaclePrefabs.Length)];
            if (prefab == null) continue;

            float spawnX = lanePositions[indices[i]];
            Instantiate(prefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity);
        }
    }

    void SpawnMovingObstacle()
    {
        if (movingObstaclePrefabs.Length == 0) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float spawnY = player.position.y + spawnAheadDistance;

        GameObject prefab = movingObstaclePrefabs[Random.Range(0, movingObstaclePrefabs.Length)];
        if (prefab == null) return;

        float spawnX;
        Obstacle obs = prefab.GetComponent<Obstacle>();

        if (obs != null && obs.movePattern == ObstacleMovePattern.CrossScreen)
        {
            // Spawn just off the left or right edge so it enters the screen
            bool fromLeft = Random.value > 0.5f;
            spawnX = fromLeft ? -halfWidth - 2f : halfWidth + 2f;
        }
        else
        {
            // PingPong — random position within screen
            spawnX = Random.Range(-halfWidth + spawnEdgeMargin, halfWidth - spawnEdgeMargin);
        }

        Instantiate(prefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity);
    }
}
