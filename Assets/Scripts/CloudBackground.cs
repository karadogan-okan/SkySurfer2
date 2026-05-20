using UnityEngine;

/// <summary>
/// Keeps a tiled-draw-mode cloud background covering the camera while the
/// player ascends. After launch it extends the tiled area upward (adds cloud
/// rows above the view) and trims it from the bottom (drops rows that have
/// scrolled below the view), so the clouds always fill the screen without
/// rendering wasted tiles below it.
///
/// Attach this to the GameObject whose SpriteRenderer uses Draw Mode = Tiled
/// (the "Untitled design" cloud background object).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class CloudBackground : MonoBehaviour
{
    [Header("References")]
    public SpeedManager speedManager;   // used to detect when the player has launched
    public Camera targetCamera;         // defaults to Camera.main if left empty

    [Header("Coverage")]
    [Tooltip("Extra world units of clouds kept above and below the camera view.")]
    public float verticalMargin = 3f;

    private SpriteRenderer sr;
    private float originY;   // tiling lattice origin; keeps the cloud pattern seamless

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (targetCamera == null) targetCamera = Camera.main;
        originY = transform.position.y;
    }

    void LateUpdate()
    {
        // Only manage the background once the player has launched.
        if (speedManager != null && !speedManager.HasLaunched) return;
        if (targetCamera == null || sr.sprite == null) return;

        // World height of a single cloud tile (sprite size scaled by this object).
        float tileHeight = sr.sprite.bounds.size.y * transform.lossyScale.y;
        if (tileHeight <= 0f) return;

        // Camera view edges in world space.
        float camHalfHeight = targetCamera.orthographicSize;
        float camCenterY = targetCamera.transform.position.y;
        float viewTop = camCenterY + camHalfHeight + verticalMargin;
        float viewBottom = camCenterY - camHalfHeight - verticalMargin;

        // Snap the rect centre to the tile lattice so the cloud pattern never
        // jumps when the background moves (a whole-tile shift looks identical).
        float centerY = originY + Mathf.Round((camCenterY - originY) / tileHeight) * tileHeight;

        // Whole tiles needed on each side of the centre to clear both edges.
        float reach = Mathf.Max(viewTop - centerY, centerY - viewBottom);
        int halfTiles = Mathf.Max(1, Mathf.CeilToInt(reach / tileHeight));
        float worldHeight = 2f * halfTiles * tileHeight;

        // Move the rect up with the camera...
        Vector3 pos = transform.position;
        pos.y = centerY;
        transform.position = pos;

        // ...and resize it: the top edge rises (clouds added above) while the
        // bottom edge rises with it (clouds trimmed from below).
        sr.size = new Vector2(sr.size.x, worldHeight / transform.lossyScale.y);
    }
}
