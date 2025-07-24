using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeDamageable : MonoBehaviour
{
    public LineRenderer lineRenderer;
    [SerializeField] private float RopeDamage;
    public float ColliderRadius = 0.1f;

    private void Start()
    {
        // Initialize RopeDamage if needed
        RopeDamage = GameManager.Instance.GetRopeDamage();
    }
    void Update()
    {
        HandleCollision();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // if (collision.CompareTag("Enemy"))
        // {
        //     EnemyAI enemyAI = collision.GetComponent<EnemyAI>();
        //     if (enemyAI != null)
        //     {
        //         enemyAI.TakeDamage(RopeDamage);
        //     }
        //     Debug.Log($"Enemy took {RopeDamage} damage from rope.");
        // }
    }
    private void HandleCollision()
    {
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            Vector3 point = lineRenderer.GetPosition(i);
            Collider2D[] colliders = Physics2D.OverlapCircleAll(point, ColliderRadius, LayerMask.GetMask("Enemy"));
            foreach (Collider2D collider in colliders)
            {

                // Check if it's an enemy by tag (removed isTrigger requirement)
                if (collider.CompareTag("Enemy"))
                {
                    EnemyAI enemyAI = collider.GetComponent<EnemyAI>();
                    if (enemyAI != null)
                    {
                        enemyAI.TakeDamage(RopeDamage);
                    }
                }
            }
        }
    }
    private void OnDrawGizmos()
    {
        // if (lineRenderer != null)
        // {
        //     for (int i = 0; i < lineRenderer.positionCount; i++)
        //     {
        //         Gizmos.color = Color.red;
        //         Gizmos.DrawSphere(lineRenderer.GetPosition(i), ColliderRadius);
        //     }
        // }
    }
}
