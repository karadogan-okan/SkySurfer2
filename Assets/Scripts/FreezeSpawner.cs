using UnityEngine;

public class FreezeSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject freezePowerupPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnIntervalMin = 8f;
    [SerializeField] private float spawnIntervalMax = 12f;
    [Tooltip("How far above the top of the screen freeze pickups spawn.")]
    [SerializeField] private float spawnAheadDistance = 2f;

    [Header("Spawn Bounds")]
    [SerializeField] private float spawnEdgeMargin = 0.5f;

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
            SpawnFreeze();
        }
    }

    void SpawnFreeze()
    {
        if (freezePowerupPrefab == null) return;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float spawnX = Random.Range(-halfWidth + spawnEdgeMargin, halfWidth - spawnEdgeMargin);
        float spawnY = mainCamera.transform.position.y + mainCamera.orthographicSize + spawnAheadDistance;

        PoolManager.Instance.Get(freezePowerupPrefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity);
    }
}
