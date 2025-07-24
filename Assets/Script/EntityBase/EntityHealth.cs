using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class EntityHealth : MonoBehaviour
{
    public EntityStatsData stats;
    [SerializeField] private float currentHealth;

    // Invulnerability system
    protected bool isInvulnerable = false;
    protected float invulnerabilityTimer = 0f;

    public float CurrentHealth
    {
        get { return currentHealth; }
        private set { currentHealth = Mathf.Clamp(value, 0, stats.health); }
    }

    public bool IsInvulnerable => isInvulnerable;

    private void Start()
    {
        if (stats == null)
        {
            return;
        }
        SetHealth(stats.health);
    }

    protected virtual void Update()
    {
        // Handle invulnerability timer
        if (isInvulnerable)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0f)
            {
                isInvulnerable = false;
            }
        }
    }

    public virtual void TakeDamage(float amount)
    {
        // Check if invulnerable
        if (isInvulnerable)
        {
            return; // Ignore damage while invulnerable
        }

        CurrentHealth -= amount;

        // Start invulnerability period after taking damage
        if (stats != null && stats.invulnerabilityDuration > 0f)
        {
            StartInvulnerability();
        }

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void StartInvulnerability()
    {
        isInvulnerable = true;
        invulnerabilityTimer = stats.invulnerabilityDuration;
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
