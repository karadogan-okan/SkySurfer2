using UnityEngine;

public enum ObstacleMovePattern
{
    Static,       // Stays in place — use for birds, balloons
    CrossScreen,  // Enters from one side, exits the other — use for planes
    PingPong      // Bounces back and forth across the screen — use for UFOs
}

public class Obstacle : MonoBehaviour
{
    [Header("Hit Settings")]
    [Tooltip("How much upward speed the player loses on collision.")]
    public float speedPenalty = 5f;
    [Tooltip("Particle prefab spawned at the hit position. Assign in Inspector.")]
    public GameObject hitParticlePrefab;

    [Header("Movement")]
    public ObstacleMovePattern movePattern = ObstacleMovePattern.Static;
    [Tooltip("Horizontal movement speed for CrossScreen and PingPong patterns.")]
    public float moveSpeed = 4f;

    [Header("Cleanup")]
    [Tooltip("Viewport margin below screen before object is destroyed.")]
    public float offScreenMargin = 0.2f;

    private Camera mainCamera;
    private float moveDir = 1f;
    private float halfWidth;
    private bool hasEnteredScreen = false; // CrossScreen: don't destroy until we've entered first
    private SpriteRenderer sr;

    void Start()
    {
        mainCamera = Camera.main;
        sr = GetComponent<SpriteRenderer>();

        if (mainCamera != null)
            halfWidth = mainCamera.orthographicSize * mainCamera.aspect;

        // CrossScreen: direction is determined by which side we spawned from.
        // PingPong: start with a random direction.
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
        if (movePattern == ObstacleMovePattern.Static) return;

        transform.position += Vector3.right * moveDir * moveSpeed * Time.deltaTime;

        if (movePattern == ObstacleMovePattern.PingPong)
        {
            bool flipped = false;
            if (transform.position.x >  halfWidth - 0.5f) { moveDir = -1f; flipped = true; }
            if (transform.position.x < -halfWidth + 0.5f) { moveDir =  1f; flipped = true; }
            if (flipped) ApplySpriteFlip();
        }
    }

    // Flip the sprite to face the direction of travel.
    // moveDir > 0 = moving right = no flip (default sprite faces right).
    // moveDir < 0 = moving left  = flip X.
    void ApplySpriteFlip()
    {
        if (sr != null)
            sr.flipX = moveDir < 0f;
    }

    void CheckOffScreen()
    {
        if (mainCamera == null) return;

        Vector3 vp = mainCamera.WorldToViewportPoint(transform.position);

        // Destroy once it scrolls below the screen
        if (vp.y < -offScreenMargin)
        {
            Destroy(gameObject);
            return;
        }

        // CrossScreen: wait until the obstacle has fully entered the screen
        // before checking if it has exited the other side. Without this guard
        // it gets destroyed on the very first frame (spawns off-screen).
        if (movePattern == ObstacleMovePattern.CrossScreen)
        {
            if (!hasEnteredScreen)
            {
                if (vp.x >= 0f && vp.x <= 1f)
                    hasEnteredScreen = true;
            }
            else
            {
                if (vp.x < -offScreenMargin || vp.x > 1f + offScreenMargin)
                    Destroy(gameObject);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Reduce the player's upward speed
        Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            float newSpeed = Mathf.Max(0f, playerRb.linearVelocity.y - speedPenalty);
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, newSpeed);
        }

        // Spawn hit particle effect
        if (hitParticlePrefab != null)
            Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
