using UnityEngine;

public enum ObstacleMovePattern
{
    Static,       // Scrolls down with world only — birds, balloons
    CrossScreen,  // Scrolls down + crosses horizontally — planes
    PingPong      // Scrolls down + bounces left/right within a range — UFOs
}

public class Obstacle : MonoBehaviour
{
    [Header("Hit Settings")]
    [Tooltip("How much scroll speed the player loses on collision.")]
    public float speedPenalty = 5f;
    [Tooltip("Particle prefab spawned at the hit position. Assign in Inspector.")]
    public GameObject hitParticlePrefab;

    [Header("Movement")]
    public ObstacleMovePattern movePattern = ObstacleMovePattern.Static;
    [Tooltip("Fixed horizontal movement speed (world units/sec). Applies to CrossScreen and PingPong.")]
    public float moveSpeed = 4f;

    [Header("PingPong Settings")]
    [Tooltip("How far left and right the UFO bounces from its spawn X position (world units).")]
    public float pingPongRange = 1.5f;

    [Header("Cleanup")]
    [Tooltip("Viewport margin before object is returned to pool off-screen.")]
    public float offScreenMargin = 0.2f;

    private Camera mainCamera;
    private float moveDir = 1f;
    private float halfWidth;
    private bool hasEnteredScreen = false;
    private SpriteRenderer sr;
    private float pingPongCenter;

    void Awake()
    {
        mainCamera = Camera.main;
        sr = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        hasEnteredScreen = false;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null)
            halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
    }

    /// <summary>
    /// Called by ObstacleSpawner after the object is positioned.
    /// Sets direction and ping-pong center from the correct spawn position.
    /// </summary>
    public void InitMovement(bool spawnedOnLeft)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null)
            halfWidth = mainCamera.orthographicSize * mainCamera.aspect;

        moveDir = spawnedOnLeft ? 1f : -1f;
        pingPongCenter = transform.position.x;

        ApplySpriteFlip();
    }

    void Update()
    {
        HandleMovement();
        CheckOffScreen();
    }

    void HandleMovement()
    {
        float scrollSpeed = SpeedManager.Instance != null ? SpeedManager.Instance.ScrollSpeed : 0f;

        transform.position += Vector3.down * scrollSpeed * Time.deltaTime;

        if (movePattern == ObstacleMovePattern.Static) return;

        transform.position += Vector3.right * moveDir * moveSpeed * Time.deltaTime;

        if (movePattern == ObstacleMovePattern.PingPong)
        {
            float leftBound  = Mathf.Max(pingPongCenter - pingPongRange, -halfWidth + 0.3f);
            float rightBound = Mathf.Min(pingPongCenter + pingPongRange,  halfWidth - 0.3f);

            bool flipped = false;
            if (transform.position.x > rightBound) { transform.position = new Vector3(rightBound, transform.position.y, 0f); moveDir = -1f; flipped = true; }
            if (transform.position.x < leftBound)  { transform.position = new Vector3(leftBound,  transform.position.y, 0f); moveDir =  1f; flipped = true; }
            if (flipped) ApplySpriteFlip();
        }
    }

    void ApplySpriteFlip()
    {
        if (sr != null)
            sr.flipX = moveDir < 0f;
    }

    void CheckOffScreen()
    {
        if (mainCamera == null) return;

        Vector3 vp = mainCamera.WorldToViewportPoint(transform.position);

        if (vp.y < -offScreenMargin)
        {
            ReturnToPool();
            return;
        }

        if (movePattern == ObstacleMovePattern.CrossScreen)
        {
            if (!hasEnteredScreen)
            {
                if (vp.x >= 0f && vp.x <= 1f && vp.y <= 1f)
                    hasEnteredScreen = true;
            }
            else
            {
                if (vp.x < -offScreenMargin || vp.x > 1f + offScreenMargin)
                    ReturnToPool();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (SpeedManager.Instance != null)
            SpeedManager.Instance.ReduceScrollSpeed(speedPenalty);

        if (hitParticlePrefab != null)
            Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);

        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (PoolManager.Instance != null)
            PoolManager.Instance.Release(gameObject);
        else
            Destroy(gameObject);
    }
}
