using UnityEngine;

[CreateAssetMenu(fileName = "GroundEnemyAISettings", menuName = "ScriptableObjects/GroundEnemyAISettings", order = 1)]
public class GroundEnemyAISettings : ScriptableObject
{
    [Header("Movement Settings")]
    [Tooltip("Speed at which the enemy patrols")]
    public float patrolSpeed = 2f;

    [Tooltip("Time to pause when PauseBeforeTurn is enabled")]
    public float stayTime = 1f;

    [Header("Detection Settings")]
    [Tooltip("Distance to check for ledges ahead")]
    public float detectionDistance = 1f;

    [Tooltip("Radius for ground detection")]
    public float groundCheckRadius = 0.1f;

    [Tooltip("Distance to check for walls")]
    public float wallCheckDistance = 0.3f;

    [Tooltip("Layers considered as ground")]
    public LayerMask groundLayer = 1;

    [Tooltip("Layers considered as walls")]
    public LayerMask wallLayer = 1;

    [Header("AI Behavior")]
    [Tooltip("Should the enemy turn when hitting a wall?")]
    public bool turnOnWallHit = true;

    [Tooltip("Should the enemy turn when reaching a ledge?")]
    public bool turnOnLedge = true;

    [Tooltip("Should the enemy pause before turning?")]
    public bool pauseBeforeTurn = true;

    [Header("Animation Settings")]
    [Tooltip("Animation parameter name for walking")]
    public string walkingAnimationParameter = "isWalking";

    [Tooltip("Animation parameter name for death")]
    public string deathAnimationParameter = "die";
}