using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : EntityHealth
{
    private EnemyAI enemyAI;
    private GameObject ShatterParticlePrefab;

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        ShatterParticlePrefab = Resources.Load<GameObject>("PlasticShatter");
    }

    public override void Die()
    {
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

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
