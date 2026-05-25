using UnityEngine;

/// <summary>
/// Scrolls a tiled-draw-mode cloud background with a parallax effect.
/// Attach to a GameObject whose SpriteRenderer uses Draw Mode = Tiled.
///
/// [DefaultExecutionOrder(100)] ensures this runs AFTER CameraFollow so
/// it always reads the final camera position for the frame — prevents shaking.
/// </summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(SpriteRenderer))]
public class CloudBackground : MonoBehaviour
{
    [Header("References")]
    public SpeedManager speedManager;
    private Camera targetCamera;

    [Header("Coverage")]
    [Tooltip("Extra world units of clouds kept above and below the camera view.")]
    public float verticalMargin = 3f;

    [Header("Parallax")]
    [Tooltip("How much clouds lag behind the camera. " +
             "0 = clouds move with the camera (no parallax). " +
             "1 = clouds are completely stationary. " +
             "Try 0.6-0.8 for a natural slow-drifting feel.")]
    [Range(0f, 1f)]
    public float parallaxMultiplier = 0.7f;

    private SpriteRenderer sr;
    private float lastCameraY;
    private int lastHalfTiles = 0;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (targetCamera == null) targetCamera = Camera.main;
    }

    void Start()
    {
        if (targetCamera != null)
            lastCameraY = targetCamera.transform.position.y;
    }

    void LateUpdate()
    {
        if (targetCamera == null || sr.sprite == null) return;

        float camY = targetCamera.transform.position.y;

        // Always track the camera so the first post-launch delta isn't a big jump.
        if (speedManager != null && !speedManager.HasLaunched)
        {
            lastCameraY = camY;
            return;
        }

        float tileHeight = sr.sprite.bounds.size.y * transform.lossyScale.y;
        if (tileHeight <= 0f) return;

        float deltaY = camY - lastCameraY;
        lastCameraY = camY;

        // Move clouds at a fraction of the camera speed (parallax).
        Vector3 pos = transform.position;
        pos.y += deltaY * (1f - parallaxMultiplier);

        // Wrap forward by whole tile heights once clouds drift a full tile behind.
        // Using Floor (not Round) means we only snap in one direction — no oscillation.
        float offset = pos.y - camY;
        if (offset <= -tileHeight)
            pos.y -= Mathf.Floor(offset / tileHeight) * tileHeight;

        // Snap to the nearest screen pixel to eliminate sub-pixel jitter.
        float ppu = Screen.height / (2f * targetCamera.orthographicSize);
        if (ppu > 0f)
            pos.y = Mathf.Round(pos.y * ppu) / ppu;

        transform.position = pos;

        // Grow the coverage rect only when needed — never shrink it.
        // Shrinking every frame causes the rect to oscillate by one tile due to
        // floating-point precision, which is what causes visible shaking.
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
