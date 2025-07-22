using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Prefabs")]
    public GameObject player1Prefab;
    public GameObject player2Prefab;

    [Header("Current Players")]
    public GameObject currentPlayer1;
    public GameObject currentPlayer2;

    private SpawnPointController spawnPointController;
    private RopeManager ropeManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        spawnPointController = FindAnyObjectByType<SpawnPointController>();
        ropeManager = FindAnyObjectByType<RopeManager>();
    }

    private void Start()
    {
        InitializeGame();
    }

    public void InitializeGame()
    {
        SpawnPlayers();
        if (ropeManager != null)
        {
            ropeManager.InitializeRope();
        }
    }

    public void SpawnPlayers()
    {
        if (spawnPointController != null)
        {
            Transform spawnPoint = spawnPointController.GetCurrentSpawnPoint();
            SpawnPlayersAtSpawnPoint(spawnPoint, spawnPointController.playerXOffset);
        }
        else
        {
            Debug.LogError("SpawnPointController not found!");
        }
    }

    public void SpawnPlayersAtSpawnPoint(Transform spawnPoint, float xOffset)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point is null!");
            return;
        }

        if (player1Prefab != null && player2Prefab != null)
        {
            // Destroy existing players if they exist
            if (currentPlayer1 != null)
                Destroy(currentPlayer1);
            if (currentPlayer2 != null)
                Destroy(currentPlayer2);

            // Spawn new players
            Vector3 player1Position = spawnPoint.position + new Vector3(-xOffset, 0, 0);
            Vector3 player2Position = spawnPoint.position + new Vector3(xOffset, 0, 0);

            currentPlayer1 = Instantiate(player1Prefab, player1Position, spawnPoint.rotation);
            currentPlayer2 = Instantiate(player2Prefab, player2Position, spawnPoint.rotation);

            // Set names for identification
            currentPlayer1.name = "Player1";
            currentPlayer2.name = "Player2";

            Debug.Log($"Players spawned at {spawnPoint.name}");
        }
        else
        {
            Debug.LogError("Player prefabs are not assigned in GameManager.");
        }
    }

    public void TeleportPlayersToSpawnPoint(Transform spawnPoint, float xOffset)
    {
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point is null!");
            return;
        }

        if (currentPlayer1 != null && currentPlayer2 != null)
        {
            Vector3 player1Position = spawnPoint.position + new Vector3(-xOffset, 0, 0);
            Vector3 player2Position = spawnPoint.position + new Vector3(xOffset, 0, 0);

            currentPlayer1.transform.position = player1Position;
            currentPlayer1.transform.rotation = spawnPoint.rotation;

            currentPlayer2.transform.position = player2Position;
            currentPlayer2.transform.rotation = spawnPoint.rotation;

            Debug.Log($"Players teleported to {spawnPoint.name}");
        }
        else
        {
            Debug.LogWarning("Players not found! Spawning new players instead.");
            SpawnPlayersAtSpawnPoint(spawnPoint, xOffset);
        }
        if (ropeManager != null)
        {
            ropeManager.DestroyRope();
            ropeManager.InitializeRope();
        }
    }

    public void RespawnPlayersAtSpawnPoint(Transform spawnPoint, float xOffset)
    {
        // This method destroys current players and spawns new ones
        SpawnPlayersAtSpawnPoint(spawnPoint, xOffset);

        // Reinitialize rope if it exists
        if (ropeManager != null)
        {
            ropeManager.DestroyRope();
            ropeManager.InitializeRope();
        }
    }

    public void RespawnPlayersAtCurrentSpawn()
    {
        if (spawnPointController != null)
        {
            Transform currentSpawn = spawnPointController.GetCurrentSpawnPoint();
            TeleportPlayersToSpawnPoint(currentSpawn, spawnPointController.playerXOffset);
        }
        else
        {
            Debug.LogError("SpawnPointController not found!");
        }
    }

    public SpawnPointController GetSpawnPointController()
    {
        return spawnPointController;
    }
    public float GetRopeDamage()
    {
        return currentPlayer1.GetComponent<PlayerHealth>().stats.attack;
    }
}