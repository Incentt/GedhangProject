using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public abstract class EnemyAI : MonoBehaviour
{
    [Header("Enemy Stats")]
    public EntityStatsData enemyStats;
    [Header("AI Configuration")]
    protected EnemyAISettings aiSettings;
    [SerializeField] protected EnemyState currentState = EnemyState.Idle;

    [Header("Movement")]
    [SerializeField] protected bool facingRight = true;
    protected Transform groundCheck;
    protected Transform wallCheck;

    [Header("Animation")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected SpriteRenderer spriteRenderer;

    // Components
    protected Rigidbody2D rb;
    protected EnemyHealth enemyHealth;

    // State management
    protected float stateTimer;
    protected float patrolTimer;
    protected Vector2 movement;
    protected bool shouldTurn = false;

    // Ground and wall detection
    protected bool isGrounded;
    protected bool isTouchingWall;
    protected bool isAtLedge;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<EnemyHealth>();

        if (animator == null)
            animator = GetComponent<Animator>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        InitializeAI();
    }


    protected virtual void InitializeAI()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        enemyHealth.SetStats(enemyStats);
    }

    private void Start()
    {
        SetState(EnemyState.Idle);
    }

    private void Update()
    {
        if (currentState == EnemyState.Die)
            return;

        stateTimer += Time.deltaTime;

        CheckEnvironment();
        HandleState();
        CheckForTurn();

        if (shouldTurn)
        {
            Turn();
            shouldTurn = false;
        }
    }

    private void FixedUpdate()
    {
        if (currentState == EnemyState.Die)
            return;

        rb.velocity = new Vector2(movement.x, rb.velocity.y);
    }

    protected virtual void CheckEnvironment()
    {

    }

    protected virtual void HandleState()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdleState();
                break;
            case EnemyState.Patrol:
                HandlePatrolState();
                break;
            case EnemyState.Die:
                HandleDieState();
                break;
        }
    }

    protected virtual void HandleIdleState()
    {
        movement = Vector2.zero;


    }

    protected virtual void HandlePatrolState()
    {

    }

    protected virtual void HandleDieState()
    {
        movement = Vector2.zero;


    }

    protected virtual void CheckForTurn()
    {

    }

    protected virtual void Turn()
    {
        facingRight = !facingRight;

        // Flip sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
        }
    }

    protected virtual void SetState(EnemyState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        stateTimer = 0f;

        // Handle state-specific setup
        switch (newState)
        {
            case EnemyState.Idle:
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    string walkParam = aiSettings.idleAnimation;
                    animator.SetTrigger(walkParam);
                }
                break;
            case EnemyState.Patrol:
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    string walkParam = aiSettings.patrolAnimation;
                    animator.SetTrigger(walkParam);
                }
                break;
            case EnemyState.Attack:
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    string attackParam = aiSettings.attackAnimation;
                    animator.SetTrigger(attackParam);
                }
                break;
            case EnemyState.Die:
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    // string deathParam = aiSettings != null ? aiSettings.deathAnimationParameter : "die";
                    // animator.SetTrigger(deathParam);
                }
                break;
        }
    }

    public virtual void Die()
    {
        SetState(EnemyState.Die);
    }

    protected virtual void OnDrawGizmosSelected()
    {

    }
}
