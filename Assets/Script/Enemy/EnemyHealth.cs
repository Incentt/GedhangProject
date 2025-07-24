using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : EntityHealth
{
    private EnemyAI enemyAI;
    private GameObject ShatterParticlePrefab;
    private SpriteRenderer spriteRenderer;
    private Coroutine invulnerabilityFlashCoroutine;

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        ShatterParticlePrefab = Resources.Load<GameObject>("PlasticShatter");
    }

    public override void Die()
    {
        // Stop any ongoing flash effect
        if (invulnerabilityFlashCoroutine != null)
        {
            StopCoroutine(invulnerabilityFlashCoroutine);
        }

        // Notify the AI about death
        if (enemyAI != null)
        {
            enemyAI.Die();
        }
        Instantiate(ShatterParticlePrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
        //StartCoroutine(DestroyAfterDelay(2f));
    }

    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);
        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    protected override void StartInvulnerability()
    {
        base.StartInvulnerability();

        // Start visual feedback for invulnerability
        if (spriteRenderer != null && invulnerabilityFlashCoroutine == null)
        {
            invulnerabilityFlashCoroutine = StartCoroutine(InvulnerabilityFlash());
        }
    }

    private IEnumerator InvulnerabilityFlash()
    {
        Color originalColor = spriteRenderer.color;
        float flashDuration = 0.1f;

        while (isInvulnerable)
        {
            // Flash to semi-transparent
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f);
            yield return new WaitForSeconds(flashDuration);

            // Return to normal color
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        // Ensure we end with the original color
        spriteRenderer.color = originalColor;
        invulnerabilityFlashCoroutine = null;
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
