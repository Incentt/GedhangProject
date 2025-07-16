using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointController : MonoBehaviour
{
    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Current Spawn Settings")]
    [SerializeField] private int currentSpawnIndex = 0;
    [SerializeField] private Transform currentSpawnPoint;

    [Header("Player Settings")]
    public float playerXOffset = 1.5f;

    private void Start()
    {
        ValidateSpawnPoints();
        SetCurrentSpawnPoint(currentSpawnIndex);
    }

    private void ValidateSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points assigned to SpawnPointController!");
            return;
        }

        // Remove null references
        List<Transform> validSpawnPoints = new List<Transform>();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                validSpawnPoints.Add(spawnPoints[i]);
            }
            else
            {
                Debug.LogWarning($"Spawn point at index {i} is null!");
            }
        }
        spawnPoints = validSpawnPoints.ToArray();
    }

    public void SetCurrentSpawnPoint(int index)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points available!");
            return;
        }

        if (index < 0 || index >= spawnPoints.Length)
        {
            Debug.LogWarning($"Spawn index {index} out of range! Using index 0.");
            index = 0;
        }

        currentSpawnIndex = index;
        currentSpawnPoint = spawnPoints[index];
        Debug.Log($"Current spawn point set to: {currentSpawnPoint.name} at position {currentSpawnPoint.position}");
    }

    public Transform GetCurrentSpawnPoint()
    {
        return currentSpawnPoint;
    }

    public int GetCurrentSpawnIndex()
    {
        return currentSpawnIndex;
    }

    public void SpawnPlayersAtCurrent()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SpawnPlayersAtSpawnPoint(currentSpawnPoint, playerXOffset);
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }
    }

    public void TeleportPlayersToSpawnPoint(int index)
    {
        SetCurrentSpawnPoint(index);
        TeleportPlayersToCurrentSpawn();
    }

    public void TeleportPlayersToCurrentSpawn()
    {
        if (currentSpawnPoint == null)
        {
            Debug.LogWarning("No current spawn point set!");
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TeleportPlayersToSpawnPoint(currentSpawnPoint, playerXOffset);
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }
    }

    public void RespawnPlayersAtCurrent()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RespawnPlayersAtSpawnPoint(currentSpawnPoint, playerXOffset);
        }
        else
        {
            Debug.LogError("GameManager instance not found!");
        }
    }

    // Utility methods for cycling through spawn points
    public void NextSpawnPoint()
    {
        int nextIndex = (currentSpawnIndex + 1) % spawnPoints.Length;
        SetCurrentSpawnPoint(nextIndex);
    }

    public void PreviousSpawnPoint()
    {
        int prevIndex = (currentSpawnIndex - 1 + spawnPoints.Length) % spawnPoints.Length;
        SetCurrentSpawnPoint(prevIndex);
    }

    // Get spawn point names for UI
    public string[] GetSpawnPointNames()
    {
        if (spawnPoints == null) return new string[0];

        string[] names = new string[spawnPoints.Length];
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            names[i] = spawnPoints[i] != null ? spawnPoints[i].name : $"Spawn Point {i}";
        }
        return names;
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;

            // Draw spawn point gizmo
            Gizmos.color = (i == currentSpawnIndex) ? Color.green : Color.blue;
            Gizmos.DrawWireSphere(spawnPoints[i].position, 0.5f);

            // Draw player positions
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(spawnPoints[i].position + new Vector3(-playerXOffset, 0, 0), Vector3.one * 0.3f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(spawnPoints[i].position + new Vector3(playerXOffset, 0, 0), Vector3.one * 0.3f);
        }
    }
}