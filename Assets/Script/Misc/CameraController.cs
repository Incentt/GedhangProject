using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Player References")]
    public Transform player1;
    public Transform player2;

    [Header("Camera Settings")]
    public float minZoom = 3f;
    public float maxZoom = 10f;
    public float zoomBorder = 2f; // Extra border around players
    public float smoothTime = 0.3f; // Lerp smoothing time
    public Vector2 offset = Vector2.zero; // Manual offset from center point

    [Header("Advanced Settings")]
    public bool useOrthographic = true;
    public float perspectiveZoomMultiplier = 10f; // For perspective cameras

    private Camera cam;
    private Vector3 velocity = Vector3.zero;
    private float targetOrthographicSize;
    private float zoomVelocity = 0f;
    private PlayerController playerController1;
    private PlayerController playerController2;

    void Start()
    {
        cam = GetComponent<Camera>();

        // Set initial camera mode
        cam.orthographic = useOrthographic;

        // Initialize target zoom
        targetOrthographicSize = cam.orthographicSize;

        // Auto-find players if not assigned
        if (player1 == null || player2 == null)
        {
            FindPlayers();
        }

        playerController1 = player1?.GetComponent<PlayerController>();
        playerController2 = player2?.GetComponent<PlayerController>();
    }

    void LateUpdate()
    {
        if (player1 == null || player2 == null)
        {
            FindPlayers();
            Debug.LogWarning("CameraController: Player references are missing!, Finding Players again.");
            return;
        }

        // Calculate target position (midpoint between players)
        Vector3 targetPosition = GetCenterPoint();
        float requiredZoom;
        bool isSwinging = (playerController1 != null && playerController1.isSwinging) || (playerController2 != null && playerController2.isSwinging);
        if (isSwinging)
        {
            requiredZoom = maxZoom;
        }
        else
        {
            requiredZoom = GetRequiredZoom();
        }

        // Smooth camera movement
        SmoothCameraMovement(targetPosition, requiredZoom);
    }

    void FindPlayers()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            player1 = gameManager.currentPlayer1?.transform;
            player2 = gameManager.currentPlayer2?.transform;
        }

        if (player1 == null || player2 == null)
        {
            Debug.LogError("CameraController: Players not found! Please assign them in the inspector.");
        }
    }

    Vector3 GetCenterPoint()
    {
        Vector3 centerPoint = (player1.position + player2.position) / 2f;

        // Apply offset before constraints
        centerPoint.x += offset.x;
        centerPoint.y += offset.y;
        // Keep original Z position
        centerPoint.z = transform.position.z;

        return centerPoint;
    }

    float GetRequiredZoom()
    {
        // Calculate distance between players
        float distance = Vector3.Distance(player1.position, player2.position);

        // Calculate required zoom to fit both players with border
        float requiredZoom;

        if (useOrthographic)
        {
            requiredZoom = (distance / 2f) + zoomBorder;
            requiredZoom = Mathf.Clamp(requiredZoom, minZoom, maxZoom);
        }
        else
        {
            // For perspective cameras, adjust based on distance from camera
            requiredZoom = distance * perspectiveZoomMultiplier;
            requiredZoom = Mathf.Clamp(requiredZoom, minZoom, maxZoom);
        }

        return requiredZoom;
    }

    void SmoothCameraMovement(Vector3 targetPosition, float requiredZoom)
    {
        // Smooth position movement using SmoothDamp
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        // Smooth zoom transition
        if (useOrthographic)
        {
            targetOrthographicSize = requiredZoom;
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetOrthographicSize, ref zoomVelocity, smoothTime);
        }
        else
        {
            // For perspective cameras, adjust the Z position instead
            Vector3 currentPos = transform.position;
            currentPos.z = Mathf.SmoothDamp(currentPos.z, -requiredZoom, ref zoomVelocity, smoothTime);
            transform.position = currentPos;
        }
    }

    // Public methods for runtime adjustments
    public void SetPlayers(Transform newPlayer1, Transform newPlayer2)
    {
        player1 = newPlayer1;
        player2 = newPlayer2;
    }

    public void SetZoomLimits(float newMinZoom, float newMaxZoom)
    {
        minZoom = newMinZoom;
        maxZoom = newMaxZoom;
    }

    public void SetSmoothTime(float newSmoothTime)
    {
        smoothTime = Mathf.Max(0.1f, newSmoothTime);
    }

    // Utility method to instantly snap to target (useful for scene transitions)
    public void SnapToTarget()
    {
        if (player1 == null || player2 == null) return;

        Vector3 targetPosition = GetCenterPoint();
        float requiredZoom = GetRequiredZoom();

        transform.position = targetPosition;

        if (useOrthographic)
        {
            cam.orthographicSize = requiredZoom;
        }
        else
        {
            Vector3 pos = transform.position;
            pos.z = -requiredZoom;
            transform.position = pos;
        }

        // Reset velocities
        velocity = Vector3.zero;
        zoomVelocity = 0f;
    }
}
