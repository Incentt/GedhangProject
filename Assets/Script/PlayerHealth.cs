using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : EntityHealth
{
    public override void Die()
    {

    }
    public void Respawn()
    {
        SetHealth(stats.health);
        GameManager.Instance.RespawnPlayersAtCurrentSpawn();
    }
    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);
        //Knocback
        Debug.Log("Player took damage: " + amount + ", Current Health: " + CurrentHealth);
    }

    public override void SetStats(EntityStatsData newStats)
    {
        base.SetStats(newStats);
        //Unused for now
    }
}
