using UnityEngine;

[CreateAssetMenu(fileName = "EntityStatsData", menuName = "ScriptableObjects/EntityStatsData", order = 1)]
public class EntityStatsData : ScriptableObject
{
    public int health;
    public int attack;
    public int knockbackAttack = 500;
    public float invulnerabilityDuration = 1.0f;
}
