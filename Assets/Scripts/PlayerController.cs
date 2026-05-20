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
    public float launchMultiplier = 12f;
    public float maxDragDistance = 3f;
    public float minDragToLaunch = 0.5f;

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
            Launch(Vector2.up * maxDragDistance);
        }
    }
    }

    void SwitchLane(int direction)
    {
        currentLane = Mathf.Clamp(currentLane + direction, 0, lanePositions.Length - 1);
    }

    void Launch(Vector2 direction)
    {
        isLaunched = true;
        rb.gravityScale = 2f;
        rb.linearVelocity = new Vector2(0f, direction.y * launchMultiplier);
    }

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