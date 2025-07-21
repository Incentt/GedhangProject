using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
#if UNITY_EDITOR

public class SpawnPointWindow : EditorWindow
{

    private SpawnPointController spawnController;
    private Vector2 scrollPosition;
    private int selectedSpawnIndex = 0;
    private string searchFilter = "";
    private bool autoRefresh = true;
    private bool showPlayerPreview = true;

    // GUI Styles
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private GUIStyle buttonStyle;
    private bool stylesInitialized = false;

    [MenuItem("Window/Spawn Point Manager")]
    public static void ShowWindow()
    {
        SpawnPointWindow window = GetWindow<SpawnPointWindow>("Spawn Point Manager");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private void OnEnable()
    {
        FindSpawnController();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
        {
            FindSpawnController();
        }
    }

    private void FindSpawnController()
    {
        spawnController = FindObjectOfType<SpawnPointController>();
        if (spawnController != null)
        {
            selectedSpawnIndex = spawnController.GetCurrentSpawnIndex();
        }
    }

    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(5, 5, 5, 5)
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitializeStyles();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Header
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Spawn Point Manager", headerStyle);
        EditorGUILayout.EndVertical();

        // Auto-refresh toggle
        EditorGUILayout.BeginHorizontal();
        autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);
        if (GUILayout.Button("Refresh", GUILayout.Width(100)))
        {
            FindSpawnController();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Check if SpawnPointController exists
        if (spawnController == null)
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.HelpBox("No SpawnPointController found in the scene!", MessageType.Warning);
            if (GUILayout.Button("Create SpawnPointController", buttonStyle))
            {
                CreateSpawnPointController();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            return;
        }

        // SpawnController info
        DrawSpawnControllerInfo();

        EditorGUILayout.Space();

        // Search filter
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            searchFilter = "";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Spawn points list
        DrawSpawnPointsList();

        EditorGUILayout.Space();

        // Current spawn point info
        DrawCurrentSpawnInfo();

        EditorGUILayout.Space();

        // Player actions
        DrawPlayerActions();

        EditorGUILayout.Space();

        EditorGUILayout.EndScrollView();

        // Auto-refresh
        if (autoRefresh)
        {
            Repaint();
        }
    }

    private void DrawSpawnControllerInfo()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Spawn Controller Info", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("GameObject:", GUILayout.Width(100));
        EditorGUILayout.ObjectField(spawnController, typeof(SpawnPointController), true);
        EditorGUILayout.EndHorizontal();

        int spawnCount = spawnController.spawnPoints != null ? spawnController.spawnPoints.Length : 0;
        EditorGUILayout.LabelField($"Total Spawn Points: {spawnCount}");
        EditorGUILayout.LabelField($"Current Index: {spawnController.GetCurrentSpawnIndex()}");

        Transform currentSpawn = spawnController.GetCurrentSpawnPoint();
        if (currentSpawn != null)
        {
            EditorGUILayout.LabelField($"Current Spawn: {currentSpawn.name}");
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSpawnPointsList()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Spawn Points", EditorStyles.boldLabel);

        if (spawnController.spawnPoints == null || spawnController.spawnPoints.Length == 0)
        {
            EditorGUILayout.HelpBox("No spawn points assigned!", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        for (int i = 0; i < spawnController.spawnPoints.Length; i++)
        {
            Transform spawnPoint = spawnController.spawnPoints[i];
            if (spawnPoint == null) continue;

            // Filter by search
            if (!string.IsNullOrEmpty(searchFilter) &&
                !spawnPoint.name.ToLower().Contains(searchFilter.ToLower()))
            {
                continue;
            }

            DrawSpawnPointItem(i, spawnPoint);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSpawnPointItem(int index, Transform spawnPoint)
    {
        bool isCurrent = index == spawnController.GetCurrentSpawnIndex();
        Color originalColor = GUI.backgroundColor;

        if (isCurrent)
        {
            GUI.backgroundColor = Color.green;
        }

        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.BeginHorizontal();

        // Index and name
        EditorGUILayout.LabelField($"{index}:", GUILayout.Width(30));
        EditorGUILayout.ObjectField(spawnPoint, typeof(Transform), true);

        // Actions
        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
            spawnController.SetCurrentSpawnPoint(index);
            selectedSpawnIndex = index;
        }

        if (GUILayout.Button("Focus", GUILayout.Width(60)))
        {
            FocusOnSpawnPoint(spawnPoint);
        }

        if (GUILayout.Button("Teleport", GUILayout.Width(70)))
        {
            if (Application.isPlaying)
            {
                spawnController.TeleportPlayersToSpawnPoint(index);
            }
            else
            {
                EditorUtility.DisplayDialog("Play Mode Required", "Teleport requires Play Mode", "OK");
            }
        }

        EditorGUILayout.EndHorizontal();

        // Position info
        EditorGUILayout.LabelField($"Position: {spawnPoint.position}");

        // Player preview positions
        if (showPlayerPreview)
        {
            float xOffset = spawnController.playerXOffset;
            Vector3 player1Pos = spawnPoint.position + new Vector3(-xOffset, 0, 0);
            Vector3 player2Pos = spawnPoint.position + new Vector3(xOffset, 0, 0);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Player 1: {player1Pos}", GUILayout.Width(200));
            EditorGUILayout.LabelField($"Player 2: {player2Pos}");
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();

        GUI.backgroundColor = originalColor;
    }

    private void DrawCurrentSpawnInfo()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Current Spawn Point", EditorStyles.boldLabel);

        Transform currentSpawn = spawnController.GetCurrentSpawnPoint();
        if (currentSpawn != null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:", GUILayout.Width(50));
            EditorGUILayout.LabelField(currentSpawn.name);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Index:", GUILayout.Width(50));
            EditorGUILayout.LabelField(spawnController.GetCurrentSpawnIndex().ToString());
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Position:", GUILayout.Width(50));
            EditorGUILayout.LabelField(currentSpawn.position.ToString());
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("No current spawn point set!", MessageType.Warning);
        }

        // Navigation buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Previous", buttonStyle))
        {
            spawnController.PreviousSpawnPoint();
        }
        if (GUILayout.Button("Next", buttonStyle))
        {
            spawnController.NextSpawnPoint();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawPlayerActions()
    {
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("Player Actions", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Player actions require Play Mode", MessageType.Info);
        }

        EditorGUI.BeginDisabledGroup(!Application.isPlaying);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn at Current", buttonStyle))
        {
            spawnController.SpawnPlayersAtCurrent();
        }
        if (GUILayout.Button("Teleport to Current", buttonStyle))
        {
            spawnController.TeleportPlayersToCurrentSpawn();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Respawn at Current", buttonStyle))
        {
            spawnController.RespawnPlayersAtCurrent();
        }
        if (GUILayout.Button("Focus on Players", buttonStyle))
        {
            FocusOnPlayers();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();

        // Player status
        if (Application.isPlaying && GameManager.Instance != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Player Status:", EditorStyles.boldLabel);

            if (GameManager.Instance.currentPlayer1 != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Player 1:", GUILayout.Width(60));
                EditorGUILayout.ObjectField(GameManager.Instance.currentPlayer1, typeof(GameObject), true);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("Player 1: Not spawned");
            }

            if (GameManager.Instance.currentPlayer2 != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Player 2:", GUILayout.Width(60));
                EditorGUILayout.ObjectField(GameManager.Instance.currentPlayer2, typeof(GameObject), true);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("Player 2: Not spawned");
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void CreateSpawnPointController()
    {
        GameObject spawnControllerObj = new GameObject("SpawnPointController");
        spawnControllerObj.AddComponent<SpawnPointController>();
        spawnController = spawnControllerObj.GetComponent<SpawnPointController>();
        Selection.activeGameObject = spawnControllerObj;
    }

    private void CreateNewSpawnPoint()
    {
        GameObject newSpawnPoint = new GameObject($"Spawn Point {spawnController.spawnPoints.Length}");
        newSpawnPoint.transform.SetParent(spawnController.transform);

        // Position it in front of the scene camera
        if (SceneView.lastActiveSceneView != null)
        {
            Camera cam = SceneView.lastActiveSceneView.camera;
            newSpawnPoint.transform.position = cam.transform.position + cam.transform.forward * 5f;
        }

        // Add to spawn points array
        System.Array.Resize(ref spawnController.spawnPoints, spawnController.spawnPoints.Length + 1);
        spawnController.spawnPoints[spawnController.spawnPoints.Length - 1] = newSpawnPoint.transform;

        Selection.activeGameObject = newSpawnPoint;
        EditorUtility.SetDirty(spawnController);
    }

    private void RemoveNullReferences()
    {
        if (spawnController.spawnPoints == null) return;

        List<Transform> validSpawnPoints = new List<Transform>();
        for (int i = 0; i < spawnController.spawnPoints.Length; i++)
        {
            if (spawnController.spawnPoints[i] != null)
            {
                validSpawnPoints.Add(spawnController.spawnPoints[i]);
            }
        }

        spawnController.spawnPoints = validSpawnPoints.ToArray();
        EditorUtility.SetDirty(spawnController);
    }

    private void FocusOnSpawnPoint(Transform spawnPoint)
    {
        if (spawnPoint != null && SceneView.lastActiveSceneView != null)
        {
            Selection.activeGameObject = spawnPoint.gameObject;
            SceneView.lastActiveSceneView.pivot = spawnPoint.position;
            SceneView.lastActiveSceneView.Repaint();
        }
    }

    private void FocusOnPlayers()
    {
        if (GameManager.Instance != null && GameManager.Instance.currentPlayer1 != null)
        {
            Selection.activeGameObject = GameManager.Instance.currentPlayer1;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.pivot = GameManager.Instance.currentPlayer1.transform.position;
                SceneView.lastActiveSceneView.Repaint();
            }
        }
    }
}
#endif
