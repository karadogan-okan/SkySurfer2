using UnityEngine;

public enum ObstacleMovePattern
{
    Static,       // Scrolls down with world only — birds, balloons
    CrossScreen,  // Scrolls down + crosses horizontally — planes
    PingPong      // Scrolls down + bounces left/right — UFOs
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
    [Tooltip("Horizontal movement speed for CrossScreen and PingPong patterns.")]
    public float moveSpeed = 4f;

    [Header("Cleanup")]
    [Tooltip("Viewport margin before object is returned to pool off-screen.")]
    public float offScreenMargin = 0.2f;

    private Camera mainCamera;
    private float moveDir = 1f;
    private float halfWidth;
    private bool hasEnteredScreen = false;
    private SpriteRenderer sr;

    void Awake()
    {
        mainCamera = Camera.main;
        sr = GetComponent<SpriteRenderer>();
    }

    // OnEnable runs every time the object is retrieved from the pool — resets state.
    void OnEnable()
    {
        hasEnteredScreen = false;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null)
            halfWidth = mainCamera.orthographicSize * mainCamera.aspect;

        // CrossScreen: direction determined by spawn side.
        // PingPong: random starting direction.
        if (movePattern == ObstacleMovePattern.CrossScreen)
            moveDir = transform.position.x < 0f ? 1f : -1f;
        else
            moveDir = Random.value > 0.5f ? 1f : -1f;

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

        // All obstacles scroll down with the world
        transform.position += Vector3.down * scrollSpeed * Time.deltaTime;

        // Static obstacles only scroll — no horizontal movement needed
        if (movePattern == ObstacleMovePattern.Static) return;

        // CrossScreen and PingPong add their own horizontal movement
        transform.position += Vector3.right * moveDir * moveSpeed * Time.deltaTime;

        if (movePattern == ObstacleMovePattern.PingPong)
        {
            bool flipped = false;
            if (transform.position.x >  halfWidth - 0.5f) { moveDir = -1f; flipped = true; }
            if (transform.position.x < -halfWidth + 0.5f) { moveDir =  1f; flipped = true; }
            if (flipped) ApplySpriteFlip();
        }
    }

    // Flip sprite to face direction of travel.
    void ApplySpriteFlip()
    {
        if (sr != null)
            sr.flipX = moveDir < 0f;
    }

    void CheckOffScreen()
    {
        if (mainCamera == null) return;

        Vector3 vp = mainCamera.WorldToViewportPoint(transform.position);

        // All obstacles: return to pool once scrolled below bottom of screen
        if (vp.y < -offScreenMargin)
        {
            ReturnToPool();
            return;
        }

        // CrossScreen: wait until the plane has entered the viewport,
        // then return to pool once it exits horizontally.
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

        // Reduce world scroll speed instead of modifying player velocity
        if (SpeedManager.Instance != null)
            SpeedManager.Instance.ReduceScrollSpeed(speedPenalty);

        // Spawn hit particle effect
        if (hitParticlePrefab != null)
            Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);

        ReturnToPool();
    }

    void ReturnToPool()
    {
        var pooled = GetComponent<PooledObject>();
        if (pooled != null) pooled.ReturnToPool();
        else Destroy(gameObject);
    }
}
