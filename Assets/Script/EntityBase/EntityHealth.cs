using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class EntityHealth : MonoBehaviour
{
    public EntityStatsData stats;
    [SerializeField] private float currentHealth;

    public float CurrentHealth
    {
        get { return currentHealth; }
        private set { currentHealth = Mathf.Clamp(value, 0, stats.health); }
    }
    private void Start()
    {
        if (stats == null)
        {
            return;
        }
        SetHealth(stats.health);
    }
    public virtual void TakeDamage(float amount)
    {
        CurrentHealth -= amount;
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }
    public void SetHealth(float amount)
    {
        CurrentHealth = amount;
    }

    public virtual void Die()
    {
        //Dont modified this here, it should be overridden in derived classes CEK PlayerHealth
    }
    public virtual void SetStats(EntityStatsData newStats)
    {
        stats = newStats;
        SetHealth(stats.health);
    }
}
