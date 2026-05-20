using UnityEngine;

public class FreezeSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject freezePowerupPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnIntervalMin = 8f;
    [SerializeField] private float spawnIntervalMax = 12f;
    [SerializeField] private float spawnAheadDistance = 15f;

    [Header("Lanes")]
    public float[] lanePositions = { -2f, 0f, 2f };

    [Header("References")]
    public Transform player;
    public SpeedManager speedManager;

    private float timer;
    private float nextSpawnInterval;

    void Start()
    {
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

        int randomLane = Random.Range(0, lanePositions.Length);
        float spawnX = lanePositions[randomLane];
        float spawnY = player.position.y + spawnAheadDistance;

        Instantiate(freezePowerupPrefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity);
    }
}
