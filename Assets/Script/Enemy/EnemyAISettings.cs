using UnityEngine;

public class EnemyAISettings : ScriptableObject
{
    [Header("AI Settings")]
    public float patrolSpeed = 2f;
    public float stayTime = 1f;

    [Header("Animation Settings")]
    public string idleAnimation = "Idle";
    public string patrolAnimation = "Walk";
    public string attackAnimation = "Attack";
    public string dieAnimation = "Die";

    // Add more settings as needed
}
