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
    [Tooltip("Multiplier converting drag distance (world units) to launch speed.")]
    public float launchSpeedMultiplier = 6f;
    [Tooltip("Maximum launch speed regardless of how far the slingshot is pulled.")]
    public float maxLaunchSpeed = 20f;

    [Header("Acceleration")]
    [Tooltip("Upward velocity added per second after launch.")]
    public float acceleration = 2f;
    [Tooltip("Maximum upward speed the player can reach via acceleration.")]
    public float maxSpeed = 40f;

    private Rigidbody2D rb;
    private Camera mainCamera;
    private bool isLaunched = false;
    public bool IsLaunched => isLaunched;

    private PlayerInputActions inputActions;

    void Awake()
    {
        inputActions = new PlayerInputActions();
        EnhancedTouchSupport.Enable();
    }

    void OnEnable() => inputActions.Player.Enable();
    void OnDisable() => inputActions.Player.Disable();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        mainCamera = Camera.main;
    }

    void Update()
    {
#if UNITY_EDITOR
        HandleKeyboardInput();
#else
        HandleTouchMovement();
#endif

        // Gradually accelerate upward after launch
        if (isLaunched && rb.linearVelocity.y > 0f)
        {
            float newSpeed = Mathf.Min(rb.linearVelocity.y + acceleration * Time.deltaTime, maxSpeed);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, newSpeed);
        }

        ClampToScreen();
    }

    void HandleKeyboardInput()
    {
        // Horizontal movement — hold to move continuously
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

        // Launch on S / Down arrow release
        if (!isLaunched)
        {
            if (Keyboard.current.downArrowKey.wasReleasedThisFrame ||
                Keyboard.current.sKey.wasReleasedThisFrame)
            {
                Launch(maxLaunchSpeed / launchSpeedMultiplier);
            }
        }
    }

    void HandleTouchMovement()
    {
        // Movement input only active after launch —
        // before launch, SlingshotVisual owns all touch input.
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

    // Called by SlingshotVisual (touch/mouse) or keyboard input.
    // dragDistance: how far the slingshot was pulled in world units.
    public void Launch(float dragDistance)
    {
        isLaunched = true;
        rb.gravityScale = 2f;
        float speed = Mathf.Min(dragDistance * launchSpeedMultiplier, maxLaunchSpeed);
        rb.linearVelocity = new Vector2(0f, speed);
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