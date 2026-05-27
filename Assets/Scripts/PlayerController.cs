using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Horizontal movement speed in world units per second.")]
    public float moveSpeed = 8f;
    [Tooltip("How far from the screen edge the player is clamped.")]
    public float screenEdgeMargin = 0.3f;

    [Header("Slingshot")]
    [Tooltip("How far the band can physically stretch in world units. Controls the feel of the pull.")]
    public float maxDragDistance = 3f;
    [Tooltip("Scroll speed reached at full stretch. Upgrade this to make the slingshot more powerful.")]
    public float maxLaunchSpeed = 20f;

    [Header("Fall Settings")]
    [Tooltip("Gravity scale applied to the player when speed hits zero and they fall off screen.")]
    public float fallGravityScale = 3f;

    private Rigidbody2D rb;
    private Camera mainCamera;
    private bool isLaunched = false;
    public bool IsLaunched => isLaunched;

    void Awake()
    {
        EnhancedTouchSupport.Enable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;     // world scrolls — no physics needed
        mainCamera = Camera.main;
    }

    void Update()
    {
#if UNITY_EDITOR
        HandleKeyboardInput();
#else
        HandleTouchMovement();
#endif
        ClampToScreen();
    }

    void HandleKeyboardInput()
    {
        float moveDir = 0f;
        if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
            moveDir = -1f;
        else if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
            moveDir = 1f;

        transform.position = new Vector3(
            transform.position.x + moveDir * moveSpeed * Time.deltaTime,
            transform.position.y,
            0f
        );

        // Launch on S / Down arrow release — keyboard always launches at full power
        if (!isLaunched)
        {
            if (Keyboard.current.downArrowKey.wasReleasedThisFrame ||
                Keyboard.current.sKey.wasReleasedThisFrame)
            {
                Launch(maxDragDistance);
            }
        }
    }

    void HandleTouchMovement()
    {
        // SlingshotVisual owns all touch before launch
        if (!isLaunched) return;

        var activeTouches = Touch.activeTouches;
        if (activeTouches.Count == 0) return;

        float screenMidX = Screen.width * 0.5f;
        float moveDir = 0f;

        foreach (var touch in activeTouches)
        {
            if (touch.screenPosition.x < screenMidX)
                moveDir = -1f;
            else
                moveDir = 1f;
        }

        transform.position = new Vector3(
            transform.position.x + moveDir * moveSpeed * Time.deltaTime,
            transform.position.y,
            0f
        );
    }

    // Called by SpeedManager when scroll speed hits zero.
    // Enables gravity so the player visually falls off the bottom of the screen.
    public void StartFalling()
    {
        enabled = false; // disable horizontal input during fall
        rb.gravityScale = fallGravityScale;

        // Freeze the camera so the player visually falls off the bottom of the screen.
        // Without this, CameraFollow tracks the falling player and game over never triggers.
        CameraFollow cf = Camera.main.GetComponent<CameraFollow>();
        if (cf != null) cf.enabled = false;
    }

    // Called by SlingshotVisual (touch/mouse) or keyboard input.
    // dragDistance: how far the slingshot was pulled in world units.
    public void Launch(float dragDistance)
    {
        isLaunched = true;
        // Normalize drag (0 → maxDragDistance) to speed (0 → maxLaunchSpeed)
        float t = Mathf.Clamp01(dragDistance / maxDragDistance);
        float speed = t * maxLaunchSpeed;
        SpeedManager.Instance.SetScrollSpeed(speed);
    }

    void ClampToScreen()
    {
        if (mainCamera == null) return;
        float halfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float clampedX = Mathf.Clamp(
            transform.position.x,
            -halfWidth + screenEdgeMargin,
             halfWidth - screenEdgeMargin
        );
        transform.position = new Vector3(clampedX, transform.position.y, 0f);
    }

    void OnDrawGizmos()
    {
        if (Camera.main == null) return;
        float halfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(-halfWidth + screenEdgeMargin, -20f, 0f),
            new Vector3(-halfWidth + screenEdgeMargin,  20f, 0f)
        );
        Gizmos.DrawLine(
            new Vector3(halfWidth - screenEdgeMargin, -20f, 0f),
            new Vector3(halfWidth - screenEdgeMargin,  20f, 0f)
        );
    }
}
