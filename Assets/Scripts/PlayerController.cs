using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class PlayerController : MonoBehaviour
{
    [Header("Lanes")]
    public float[] lanePositions = { -2f, 0f, 2f };
    public int currentLane = 1;
    public float laneSwitchSpeed = 15f;

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
    }

    void Update()
    {
        // Always allow lane switching regardless of launched state
        Vector3 target = new Vector3(lanePositions[currentLane], transform.position.y, 0f);
        transform.position = new Vector3(
            Mathf.Lerp(transform.position.x, target.x, laneSwitchSpeed * Time.deltaTime),
            transform.position.y,
            0f
        );

    #if UNITY_EDITOR
        HandleKeyboardInput();
    #else
        HandleTouchInput();
    #endif

        // Gradually accelerate upward after launch
        if (isLaunched && rb.linearVelocity.y > 0f)
        {
            float newSpeed = Mathf.Min(rb.linearVelocity.y + acceleration * Time.deltaTime, maxSpeed);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, newSpeed);
        }

        // Reset when player falls back down
        if (isLaunched && transform.position.y <= -4f)
            ResetPlayer();
    }

    void HandleKeyboardInput()
    {
    // Always allow lane switching
    if (Keyboard.current.rightArrowKey.wasPressedThisFrame ||
        Keyboard.current.dKey.wasPressedThisFrame)
    {
        SwitchLane(1);
    }
    else if (Keyboard.current.leftArrowKey.wasPressedThisFrame ||
             Keyboard.current.aKey.wasPressedThisFrame)
    {
        SwitchLane(-1);
    }

    // Only launch when not already in the air
    if (!isLaunched)
    {
        if (Keyboard.current.downArrowKey.wasReleasedThisFrame ||
            Keyboard.current.sKey.wasReleasedThisFrame)
        {
            Launch(maxLaunchSpeed / launchSpeedMultiplier); // keyboard simulates a full pull
        }
    }
    }

    void SwitchLane(int direction)
    {
        currentLane = Mathf.Clamp(currentLane + direction, 0, lanePositions.Length - 1);
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

    // Touch launching is handled by SlingshotVisual.
    void HandleTouchInput() { }

    void ResetPlayer()
    {
        isLaunched = false;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        transform.position = new Vector3(lanePositions[currentLane], -4f, 0f);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (float x in lanePositions)
            Gizmos.DrawLine(new Vector3(x, -10f, 0f), new Vector3(x, 10f, 0f));
    }
}