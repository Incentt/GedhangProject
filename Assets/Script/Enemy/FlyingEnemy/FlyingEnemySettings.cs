using UnityEngine;

[CreateAssetMenu(fileName = "FlyingEnemyAISettings", menuName = "ScriptableObjects/FlyingEnemyAISettings", order = 1)]
public class FlyingEnemySettings : EnemyAISettings
{
    [Header("Flying Enemy Settings")]
    public float attackRange = 5f;
    public float dashSpeed = 8f;
    public float dashCooldown = 2f;
    public float hoverHeight = 3f;
    public LayerMask playerLayerMask = -1;

    [Header("Patrol Settings")]
    public float patrolDistance = 3f;
    public float patrolPauseTime = 1f;

    [Header("Animation Settings")]
    public string isPatrolAnimationParameter = "isPatrol";
}