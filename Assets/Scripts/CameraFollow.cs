using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float bottomOffset = 3f; // distance from bottom of screen to player

    private float fixedX;
    private float fixedZ;

    void Start()
    {
        fixedX = transform.position.x;
        fixedZ = transform.position.z;
    }

    void LateUpdate()
    {
        // Get the bottom of the screen in world space
        float camHalfHeight = GetComponent<Camera>().orthographicSize;
        float targetY = player.position.y + camHalfHeight - bottomOffset;

        transform.position = new Vector3(fixedX, targetY, fixedZ);
    }
}