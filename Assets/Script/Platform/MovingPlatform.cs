using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform platform;
    public Transform[] waypoints;
    public float speed = 2f;
    private int currentWaypointIndex = 0;
    private List<PlayerController> playersOnPlatform = new List<PlayerController>();
    private Vector3 lastPosition;

    private void Start()
    {
        if (waypoints.Length == 0)
        {
            Debug.LogError("No waypoints assigned to the MovingPlatform.");
            enabled = false;
        }
        if (waypoints.Length > 0 && platform != null)
        {
            platform.transform.position = waypoints[0].position;
        }
        lastPosition = platform.transform.position;
    }

    void Update()
    {
        if (waypoints.Length == 0) return;

        Vector3 previousPosition = transform.position;

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        Vector3 direction = (targetWaypoint.position - platform.transform.position).normalized;
        platform.transform.position += direction * speed * Time.deltaTime;

        if (Vector3.Distance(platform.transform.position, targetWaypoint.position) < 0.1f)
        {
            platform.transform.position = targetWaypoint.position;
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            Debug.Log($"Moving platform reached waypoint {currentWaypointIndex - 1}, moving to waypoint {currentWaypointIndex}");
        }
    }

    void FixedUpdate()
    {
        Vector3 movementDelta = platform.transform.position - lastPosition;

        if (movementDelta.magnitude > 0.001f)
        {
            MovePlayersOnPlatform(movementDelta);
        }

        CheckForPlayersOnPlatform();

        lastPosition = platform.transform.position;
    }

    private void CheckForPlayersOnPlatform()
    {
        PlayerController[] allPlayers = FindObjectsOfType<PlayerController>();

        foreach (PlayerController player in allPlayers)
        {
            if (player == null) continue;

            bool isOnPlatform = IsPlayerOnPlatform(player);

            if (isOnPlatform && !playersOnPlatform.Contains(player))
            {
                playersOnPlatform.Add(player);
            }
            else if (!isOnPlatform && playersOnPlatform.Contains(player))
            {
                playersOnPlatform.Remove(player);
            }
        }
    }
    private bool IsPlayerOnPlatform(PlayerController player)
    {
        if (platform == null) return false;

        Collider2D platformCollider = platform.GetComponent<Collider2D>();
        if (platformCollider == null) return false;

        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null) return false;

        float tolerance = 0.2f;
        bool isAbovePlatform = player.transform.position.y >= platformCollider.bounds.max.y - tolerance;
        bool isWithinXBounds = player.transform.position.x >= platformCollider.bounds.min.x - tolerance &&
                              player.transform.position.x <= platformCollider.bounds.max.x + tolerance;

        return isAbovePlatform && isWithinXBounds;
    }

    private void MovePlayersOnPlatform(Vector3 movementDelta)
    {
        playersOnPlatform.RemoveAll(player => player == null);

        foreach (PlayerController player in playersOnPlatform)
        {
            if (player != null)
            {
                player.transform.position += movementDelta;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null && !playersOnPlatform.Contains(player))
        {
            playersOnPlatform.Add(player);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null && playersOnPlatform.Contains(player))
        {
            playersOnPlatform.Remove(player);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.gameObject.GetComponent<PlayerController>();
        if (player != null && !playersOnPlatform.Contains(player))
        {
            playersOnPlatform.Add(player);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.gameObject.GetComponent<PlayerController>();
        if (player != null && playersOnPlatform.Contains(player))
        {
            playersOnPlatform.Remove(player);
        }
    }
}
