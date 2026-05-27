using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

public class SlingshotVisual : MonoBehaviour
{
    [Header("Anchors")]
    public Transform leftAnchor;
    public Transform rightAnchor;
    public Transform player;

    [Header("Band Settings")]
    public LineRenderer leftBand;
    public LineRenderer rightBand;

    private PlayerController playerController;
    private Vector2 dragStartWorld;
    private bool isDragging = false;
    private float currentDragDistance = 0f;

    void Awake()
    {
        EnhancedTouchSupport.Enable();
    }

    void Start()
    {
        playerController = player.GetComponent<PlayerController>();
        HideBands();
    }

    void Update()
    {
        if (playerController.IsLaunched)
        {
            HideBands();
            return;
        }

#if UNITY_EDITOR
        HandleKeyboardVisual();
        HandleMouseVisual();
#else
        HandleTouchVisual();
#endif
    }

    void HandleKeyboardVisual()
    {
        if (Keyboard.current.downArrowKey.isPressed ||
            Keyboard.current.sKey.isPressed)
        {
            // Show a fixed visual stretch for keyboard (editor only)
            Vector3 pulledPos = player.position + Vector3.down * 3f;
            DrawBands(pulledPos);
        }
        else
        {
            HideBands();
        }
    }

    void HandleMouseVisual()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 worldPos = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue());

        if (mouse.leftButton.wasPressedThisFrame)
        {
            dragStartWorld = worldPos;
            isDragging = true;
        }
        else if (mouse.leftButton.isPressed && isDragging)
        {
            Vector2 drag = worldPos - dragStartWorld;

            if (drag.y < 0)
            {
                // Cap drag to max stretch distance
                if (drag.magnitude > playerController.maxDragDistance)
                    drag = drag.normalized * playerController.maxDragDistance;

                currentDragDistance = drag.magnitude;
                Vector3 pulledPos = player.position + (Vector3)drag;
                DrawBands(pulledPos);
            }
            else
            {
                currentDragDistance = 0f;
                HideBands();
            }
        }
        else if (mouse.leftButton.wasReleasedThisFrame && isDragging)
        {
            isDragging = false;
            HideBands();
            if (currentDragDistance > 0f)
                playerController.Launch(currentDragDistance);
            currentDragDistance = 0f;
        }
    }

    void HandleTouchVisual()
    {
        var activeTouches = Touch.activeTouches;
        if (activeTouches.Count == 0)
        {
            HideBands();
            return;
        }

        var touch = activeTouches[0];
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(touch.screenPosition);

        if (touch.phase == TouchPhase.Began)
        {
            dragStartWorld = worldPos;
            isDragging = true;
        }
        else if (touch.phase == TouchPhase.Moved && isDragging)
        {
            Vector2 drag = worldPos - dragStartWorld;

            if (drag.y < 0)
            {
                // Cap drag to max stretch distance
                if (drag.magnitude > playerController.maxDragDistance)
                    drag = drag.normalized * playerController.maxDragDistance;

                currentDragDistance = drag.magnitude;
                Vector3 pulledPos = player.position + (Vector3)drag;
                DrawBands(pulledPos);
            }
            else
            {
                currentDragDistance = 0f;
                HideBands();
            }
        }
        else if (touch.phase == TouchPhase.Ended)
        {
            isDragging = false;
            HideBands();
            if (currentDragDistance > 0f)
                playerController.Launch(currentDragDistance);
            currentDragDistance = 0f;
        }
    }

    void DrawBands(Vector3 pulledPosition)
    {
        leftBand.enabled = true;
        rightBand.enabled = true;

        leftBand.SetPosition(0, leftAnchor.position);
        leftBand.SetPosition(1, pulledPosition);

        rightBand.SetPosition(0, rightAnchor.position);
        rightBand.SetPosition(1, pulledPosition);
    }

    void HideBands()
    {
        leftBand.enabled = false;
        rightBand.enabled = false;
    }
}