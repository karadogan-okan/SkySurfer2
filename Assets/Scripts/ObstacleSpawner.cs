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
    public int staticSpawnCountMin = 1;
    public int staticSpawnCountMax = 3;
    [Tooltip("How far above the top of the screen static obstacles spawn.")]
    public float staticSpawnAhead = 2f;

    [Header("Moving Spawn Settings")]
    public float movingSpawnIntervalMin = 3f;
    public float movingSpawnIntervalMax = 6f;
    [Tooltip("How far above the top of the screen moving obstacles spawn. 0 = right at the screen edge.")]
    public float movingSpawnAhead = 0f;
    [Tooltip("Margin from the screen edge when placing PingPong obstacles.")]
    public float spawnEdgeMargin = 1f;

    [Tooltip("Lane X positions used for static obstacle placement.")]
    public float[] lanePositions = { -2f, 0f, 2f };

    [Header("References")]
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
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        float spawnY = mainCamera.transform.position.y + mainCamera.orthographicSize + staticSpawnAhead;

        int count = Mathf.Clamp(
            Random.Range(staticSpawnCountMin, staticSpawnCountMax + 1),
            1, lanePositions.Length
        );

        // Fisher-Yates shuffle to pick unique lanes
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
            PoolManager.Instance.Get(prefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity);
        }
    }

    void SpawnMovingObstacle()
    {
        if (movingObstaclePrefabs.Length == 0) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float spawnY = mainCamera.transform.position.y + mainCamera.orthographicSize + movingSpawnAhead;

        GameObject prefab = movingObstaclePrefabs[Random.Range(0, movingObstaclePrefabs.Length)];
        if (prefab == null) return;

        // All moving obstacles spawn at one of the two top corners
        bool fromLeft = Random.value > 0.5f;
        float spawnX = fromLeft ? -halfWidth : halfWidth;

        GameObject obj = PoolManager.Instance.Get(prefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity);

        // Init direction after position is set — OnEnable fires too early to read spawn position
        Obstacle obstacle = obj.GetComponent<Obstacle>();
        if (obstacle != null) obstacle.InitMovement(fromLeft);
    }
}
