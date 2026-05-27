using UnityEngine;

/// <summary>
/// Scrolls a tiled-draw-mode cloud background in a world-scroll game.
/// The camera is fixed — clouds move downward at a fraction of the world
/// scroll speed (parallax effect) and wrap seamlessly.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CloudBackground : MonoBehaviour
{
    [Header("Coverage")]
    [Tooltip("Extra world units of clouds kept above and below the camera view.")]
    public float verticalMargin = 3f;

    [Header("Parallax")]
    [Tooltip("How much clouds lag behind the world scroll. " +
             "0 = clouds scroll at full world speed (no parallax). " +
             "1 = clouds are completely stationary. " +
             "Try 0.6-0.8 for a natural slow-drifting feel.")]
    [Range(0f, 1f)]
    public float parallaxMultiplier = 0.7f;

    private SpriteRenderer sr;
    private Camera targetCamera;
    private int lastHalfTiles = 0;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (targetCamera == null || sr.sprite == null) return;
        if (SpeedManager.Instance == null) return;

        float scrollSpeed = SpeedManager.Instance.ScrollSpeed;

        float tileHeight = sr.sprite.bounds.size.y * transform.lossyScale.y;
        if (tileHeight <= 0f) return;

        // Move clouds down at a parallax fraction of world scroll speed
        Vector3 pos = transform.position;
        pos.y -= scrollSpeed * (1f - parallaxMultiplier) * Time.deltaTime;

        // Wrap: once clouds have scrolled down a full tile, jump back up
        // Camera is fixed so camY is constant — this produces seamless looping
        float camY = targetCamera.transform.position.y;
        float offset = pos.y - camY;
        if (offset <= -tileHeight)
            pos.y += tileHeight;

        transform.position = pos;

        // Grow coverage rect only when needed — never shrink it
        float camHalfHeight = targetCamera.orthographicSize;
        float viewTop    = camY + camHalfHeight + verticalMargin;
        float viewBottom = camY - camHalfHeight - verticalMargin;
        float centerY    = transform.position.y;
        float reach      = Mathf.Max(viewTop - centerY, centerY - viewBottom);
        int halfTiles    = Mathf.Max(1, Mathf.CeilToInt(reach / tileHeight) + 1);

        if (halfTiles > lastHalfTiles)
        {
            lastHalfTiles = halfTiles;
            float worldHeight = 2f * halfTiles * tileHeight;
            sr.size = new Vector2(sr.size.x, worldHeight / transform.lossyScale.y);
        }
    }
}
