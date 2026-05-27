using UnityEngine;

/// <summary>
/// Attach to any world object (Ground, decorations, etc.) that should
/// scroll downward at the world scroll speed and be destroyed once
/// it leaves the bottom of the screen.
/// </summary>
public class WorldScroller : MonoBehaviour
{
    [Tooltip("Destroy this object when it scrolls below the bottom of the screen.")]
    public bool destroyWhenOffScreen = true;
    [Tooltip("Extra viewport margin below screen before destroying.")]
    public float offScreenMargin = 0.1f;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (SpeedManager.Instance == null) return;
        transform.position += Vector3.down * SpeedManager.Instance.ScrollSpeed * Time.deltaTime;

        if (!destroyWhenOffScreen) return;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 vp = mainCamera.WorldToViewportPoint(transform.position);
        if (vp.y < -offScreenMargin)
            Destroy(gameObject);
    }
}
