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
    }

    public void RegenerateHealth(float amount)
    {
        SetHealth(CurrentHealth + amount);
    }
}
