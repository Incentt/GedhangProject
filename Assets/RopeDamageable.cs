using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopeDamageable : MonoBehaviour
{
    [SerializeField] private float RopeDamage;

    private void Start()
    {
        // Initialize RopeDamage if needed
        RopeDamage = GameManager.Instance.GetRopeDamage();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            EnemyAI enemyAI = collision.GetComponent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.TakeDamage(RopeDamage);
            }
            Debug.Log($"Enemy took {RopeDamage} damage from rope.");
        }
    }

}
