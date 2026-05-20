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
    public float maxDragDistance = 3f;

    private PlayerController playerController;
    private Vector2 dragStartWorld;
    private bool isDragging = false;

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
#else
        HandleTouchVisual();
#endif
    }

    void HandleKeyboardVisual()
    {
        if (Keyboard.current.downArrowKey.isPressed ||
            Keyboard.current.sKey.isPressed)
        {
            Vector3 pulledPos = player.position + Vector3.down * maxDragDistance;
            DrawBands(pulledPos);
        }
        else
        {
            HideBands();
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
                Vector2 clampedDrag = Vector2.ClampMagnitude(drag, maxDragDistance);
                Vector3 pulledPos = player.position + (Vector3)clampedDrag;
                DrawBands(pulledPos);
            }
            else
            {
                HideBands();
            }
        }
        else if (touch.phase == TouchPhase.Ended)
        {
            isDragging = false;
            HideBands();
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