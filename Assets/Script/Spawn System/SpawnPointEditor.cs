using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpawnPointController))]
public class SpawnPointEditor : Editor
{
    private SpawnPointController spawnController;

    private void OnEnable()
    {
        spawnController = (SpawnPointController)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        // Quick link to window
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Spawn Point Tools", EditorStyles.boldLabel);

        if (GUILayout.Button("Open Spawn Point Manager Window", GUILayout.Height(30)))
        {
            SpawnPointWindow.ShowWindow();
        }

        EditorGUILayout.LabelField("Or use Window > Spawn Point Manager", EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();

        // Basic info
        if (spawnController.spawnPoints != null && spawnController.spawnPoints.Length > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Quick Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total Spawn Points: {spawnController.spawnPoints.Length}");
            EditorGUILayout.LabelField($"Current Index: {spawnController.GetCurrentSpawnIndex()}");
            Transform currentSpawn = spawnController.GetCurrentSpawnPoint();
            if (currentSpawn != null)
            {
                EditorGUILayout.LabelField($"Current Spawn: {currentSpawn.name}");
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void OnSceneGUI()
    {
        if (spawnController.spawnPoints == null) return;

        // Draw spawn point labels in scene view
        for (int i = 0; i < spawnController.spawnPoints.Length; i++)
        {
            if (spawnController.spawnPoints[i] == null) continue;

            Transform spawnPoint = spawnController.spawnPoints[i];

            // Draw label
            Handles.Label(spawnPoint.position + Vector3.up * 2f,
                         $"Spawn {i}" + (i == spawnController.GetCurrentSpawnIndex() ? " (Current)" : ""));

            // Draw player positions
            Vector3 player1Pos = spawnPoint.position + new Vector3(-spawnController.playerXOffset, 0, 0);
            Vector3 player2Pos = spawnPoint.position + new Vector3(spawnController.playerXOffset, 0, 0);

            Handles.color = Color.red;
            Handles.DrawWireCube(player1Pos, Vector3.one * 0.5f);
            Handles.Label(player1Pos + Vector3.up, "P1");

            Handles.color = Color.yellow;
            Handles.DrawWireCube(player2Pos, Vector3.one * 0.5f);
            Handles.Label(player2Pos + Vector3.up, "P2");
        }
    }
}