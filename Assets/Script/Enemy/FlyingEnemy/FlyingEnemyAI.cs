using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEnemyAI : EnemyAI
{
    [Header("Flying Enemy Specific")]
    [SerializeField] private FlyingEnemySettings flyingSettings;

    // Player detection and targeting
    private Transform player;
    private bool playerInRange = false;
    private bool isDashing = false;
    private bool dashOnCooldown = false;

    // Attack timing
    private float lastDashTime;
    private float dashStartTime; // Track when dash started
    private Vector2 dashDirection;
    private Vector2 dashTargetPosition; // Store the target position when dash starts
    private Vector2 originalPosition;

    // Patrol movement
    private bool movingRight = true;
    private bool isPaused = false;
    private Vector2 leftBound;
    private Vector2 rightBound;

    // Add hysteresis to prevent jittering
    private float attackRangeBuffer = 0.5f; // Buffer zone to prevent rapid state switching
    private float lastPlayerCheckTime = 0f;
    private float playerCheckInterval = 0.1f; // Check player less frequently

    protected override void Awake()
    {
        base.Awake();
        aiSettings = flyingSettings;

        // Find player using layer mask and get the closest one
        FindClosestPlayer();

        // Store original position for hovering
        originalPosition = transform.position;

        // Set up patrol bounds based on original Y position
        leftBound = new Vector2(originalPosition.x - flyingSettings.patrolDistance, originalPosition.y);
        rightBound = new Vector2(originalPosition.x + flyingSettings.patrolDistance, originalPosition.y);
    }

    protected override void Turn()
    {
        // Use sprite renderer flip instead of transform rotation
        facingRight = !facingRight;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
        }
    }

    private void FindClosestPlayer()
    {
        Transform closestPlayer = null;
        float closestDistance = Mathf.Infinity;

        // First try to find by tag (more reliable)
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");

        if (playerObjects.Length > 0)
        {
            foreach (GameObject playerObj in playerObjects)
            {
                if (playerObj != null && playerObj.activeInHierarchy)
                {
                    float distance = Vector2.Distance(transform.position, playerObj.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPlayer = playerObj.transform;
                    }
                }
            }
        }

        // If no player found by tag, try layer mask as backup
        if (closestPlayer == null && flyingSettings != null)
        {
            Collider2D[] playerColliders = Physics2D.OverlapCircleAll(transform.position, 50f, flyingSettings.playerLayerMask);

            foreach (Collider2D collider in playerColliders)
            {
                if (collider != null && collider.gameObject.activeInHierarchy)
                {
                    float distance = Vector2.Distance(transform.position, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPlayer = collider.transform;
                    }
                }
            }
        }

        // Only update player reference if we found someone closer or current player is invalid
        if (closestPlayer != null && (player == null || !player.gameObject.activeInHierarchy || closestDistance < Vector2.Distance(transform.position, player.position)))
        {
            // Debug info when switching targets
            if (player != closestPlayer)
            {
                Debug.Log($"Flying Enemy switching target to: {closestPlayer.name} (distance: {closestDistance:F2})");
            }
            player = closestPlayer;
        }
        else if (player != null && !player.gameObject.activeInHierarchy)
        {
            player = null;
        }
    }

    protected override void InitializeAI()
    {
        base.InitializeAI();

        // Ensure rigidbody settings for flying enemy
        if (rb != null)
        {
            rb.gravityScale = 0f; // Flying enemy shouldn't be affected by gravity
            rb.isKinematic = false; // Make sure it's not kinematic
        }
        else
        {
            Debug.LogError("No Rigidbody2D found on flying enemy!");
        }
    }

    private void Start()
    {
        FindClosestPlayer();
        SetState(EnemyState.Patrol); // Start with patrol instead of idle
    }

    protected override void SetState(EnemyState newState)
    {
        if (currentState == newState)
            return;

        EnemyState previousState = currentState;
        currentState = newState;
        stateTimer = 0f;

        // Handle flying enemy specific state transitions
        switch (newState)
        {
            case EnemyState.Attack:

                break;
            case EnemyState.Patrol:
                transform.rotation = Quaternion.identity;
                if (rb != null)
                {
                    rb.velocity = Vector2.zero; // Reset any velocity
                }
                break;
            case EnemyState.Idle:
                currentState = EnemyState.Patrol;
                break;
        }
    }

    protected override void Update()
    {
        if (currentState == EnemyState.Die)
            return;

        stateTimer += Time.deltaTime;

        CheckPlayerInRange();
        CheckEnvironment();
        HandleState();

        // Update dash cooldown
        if (dashOnCooldown && Time.time - lastDashTime >= flyingSettings.dashCooldown)
        {
            dashOnCooldown = false;
        }
    }

    private void CheckPlayerInRange()
    {
        if (Time.time - lastPlayerCheckTime < playerCheckInterval)
            return;

        lastPlayerCheckTime = Time.time;

        // Always refresh closest player to detect new players or if current player is no longer closest
        FindClosestPlayer();

        if (player == null)
        {
            playerInRange = false;
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        float effectiveAttackRange = (currentState == EnemyState.Attack)
            ? flyingSettings.attackRange + attackRangeBuffer
            : flyingSettings.attackRange;

        bool wasPlayerInRange = playerInRange;
        playerInRange = distanceToPlayer <= effectiveAttackRange;

        // Enhanced debug information
        Debug.DrawLine(transform.position, player.position, playerInRange ? Color.red : Color.green);
    }

    protected override void HandleState()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                // Redirect Idle to Patrol - flying enemies should always be patrolling
                SetState(EnemyState.Patrol);
                break;
            case EnemyState.Patrol:
                HandlePatrolState();
                break;
            case EnemyState.Attack:
                HandleAttackState();
                break;
            case EnemyState.Die:
                HandleDieState();
                break;
            default:
                Debug.LogError($"Unknown state: {currentState}");
                break;
        }
    }

    protected override void HandleIdleState()
    {
        HandlePatrolMovement();

        // Set patrol animation and reset rotation
        if (animator != null)
        {
            animator.SetBool(flyingSettings.isPatrolAnimationParameter, true);
        }

        // Reset rotation to 0 during patrol
        transform.rotation = Quaternion.identity;

        // Check if player is in range to attack
        if (playerInRange && !dashOnCooldown && player != null)
        {
            SetState(EnemyState.Attack);
        }
    }

    protected override void HandlePatrolState()
    {
        HandlePatrolMovement();

        // Set patrol animation and reset rotation
        if (animator != null)
        {
            animator.SetBool(flyingSettings.isPatrolAnimationParameter, true);
        }

        // Reset rotation to 0 during patrol
        transform.rotation = Quaternion.identity;

        // Check if player enters range - only switch if not on cooldown
        if (playerInRange && !dashOnCooldown && player != null)
        {
            SetState(EnemyState.Attack);
        }
    }

    private void HandlePatrolMovement()
    {
        if (isPaused)
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= flyingSettings.patrolPauseTime)
            {
                isPaused = false;
                patrolTimer = 0f;
            }
            return;
        }

        // Determine target bound and movement direction
        Vector2 targetBound = movingRight ? rightBound : leftBound;
        Vector2 currentPos = transform.position;

        // Calculate actual movement direction based on target
        Vector2 directionToTarget = (targetBound - currentPos).normalized;

        // Update facing direction based on actual movement direction
        bool shouldFaceRight = directionToTarget.x > 0;

        if (shouldFaceRight != facingRight)
        {
            facingRight = shouldFaceRight;
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !facingRight;
            }
        }

        // Move towards target bound
        transform.position = Vector2.MoveTowards(currentPos, targetBound, aiSettings.patrolSpeed * Time.deltaTime);

        // Check if reached the bound
        if (Vector2.Distance(transform.position, targetBound) < 0.1f)
        {
            movingRight = !movingRight;
            isPaused = true;
            patrolTimer = 0f;
        }
    }

    private void HandleAttackState()
    {
        if (player == null)
        {
            SetState(EnemyState.Patrol);
            return;
        }

        // Set attack animation (disable patrol animation)
        if (animator != null)
        {
            animator.SetBool(flyingSettings.isPatrolAnimationParameter, false);
        }

        if (!isDashing && !dashOnCooldown)
        {
            StartDashAttack();
        }

        if (isDashing)
        {
            // Check if dash has exceeded maximum duration (5 seconds)
            if (Time.time - dashStartTime >= 5f)
            {
                isDashing = false;
                SetState(EnemyState.Patrol);
                return;
            }

            // Move in straight line using the stored dash direction
            Vector2 currentPos = transform.position;
            Vector2 targetPos = currentPos + dashDirection * flyingSettings.dashSpeed * Time.deltaTime;
            transform.position = targetPos;

            // Exit early to prevent any rotation updates during dash
            return;
        }

        // Only do rotation and sprite updates when NOT dashing and NOT on cooldown
        if (!isDashing && !dashOnCooldown)
        {
            // Only calculate direction and rotate when NOT dashing (preparing for attack)
            Vector2 directionToPlayer = (player.position - transform.position).normalized;

            // Set sprite flipping based on horizontal direction
            bool shouldFaceRight = directionToPlayer.x >= 0;

            if (shouldFaceRight != facingRight)
            {
                facingRight = shouldFaceRight;
                if (spriteRenderer != null)
                {
                    spriteRenderer.flipX = !facingRight;
                }
            }

            // Use LookAt approach for rotation towards player
            Vector3 lookDirection = player.position - transform.position;

            if (lookDirection != Vector3.zero)
            {
                // For 2D, we want to rotate around the Z axis
                float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

                // When sprite is flipped, we need to adjust the rotation
                if (!facingRight)
                {
                    // Flip both X and Y components when sprite is flipped
                    angle = Mathf.Atan2(-lookDirection.y, -lookDirection.x) * Mathf.Rad2Deg;
                }

                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        // Only return to patrol if player is out of range AND we're not currently dashing
        if (!playerInRange && !isDashing)
        {
            SetState(EnemyState.Patrol);
        }
    }

    private void StartDashAttack()
    {
        if (player == null)
        {
            Debug.LogError("Cannot start dash attack - player is null!");
            return;
        }

        isDashing = true;
        dashOnCooldown = true;
        lastDashTime = Time.time;
        dashStartTime = Time.time; // Record when dash started

        // Capture the player's position ONCE when dash starts - this won't change during dash
        dashTargetPosition = player.position;

        // Calculate direction to the captured target position
        dashDirection = (dashTargetPosition - (Vector2)transform.position).normalized;

        // Set sprite flipping based on horizontal direction
        facingRight = dashDirection.x >= 0;
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
        }

        // Use LookAt to rotate towards the target position
        Vector3 lookDirection = dashTargetPosition - (Vector2)transform.position;

        // Use LookAt for rotation (Unity's LookAt points the forward axis toward target)
        if (lookDirection != Vector3.zero)
        {
            // For 2D, we want to rotate around the Z axis
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

            // When sprite is flipped, we need to adjust the rotation
            if (!facingRight)
            {
                // Flip both X and Y components when sprite is flipped
                angle = Mathf.Atan2(-lookDirection.y, -lookDirection.x) * Mathf.Rad2Deg;
            }

            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        // Stop dashing and attacking when colliding with anything while dashing
        if (isDashing)
        {
            isDashing = false;

            // If it's the player, deal damage and knockback
            if (collision.gameObject.CompareTag("Player"))
            {
                PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
                Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();

                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(enemyStats.attack);
                }

                // Apply knockback to player
                if (playerRb != null)
                {
                    Vector2 knockbackDirection = (collision.transform.position - transform.position).normalized;
                    float knockbackForce = enemyStats.knockbackAttack;
                    playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
                }
            }

            // Return to patrol state immediately after any collision
            SetState(EnemyState.Patrol);
        }

        // Call base collision handler for other collision types
        base.OnCollisionEnter2D(collision);
    }

    protected override void HandleDieState()
    {
        base.HandleDieState();
        isDashing = false;
        rb.velocity = Vector2.zero;

        // Reset rotation on death
        transform.rotation = Quaternion.identity;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Draw attack range
        if (flyingSettings != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, flyingSettings.attackRange);

            // Draw hysteresis buffer zone
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, flyingSettings.attackRange + attackRangeBuffer);

            // Draw hover height indicator
            Gizmos.color = Color.blue;
            Vector3 hoverPos = new Vector3(transform.position.x, transform.position.y + flyingSettings.hoverHeight, transform.position.z);
            Gizmos.DrawWireCube(hoverPos, Vector3.one * 0.5f);

            // Draw patrol bounds
            Gizmos.color = Color.cyan;
            Vector3 leftBoundPos = new Vector3(originalPosition.x - flyingSettings.patrolDistance, originalPosition.y, transform.position.z);
            Vector3 rightBoundPos = new Vector3(originalPosition.x + flyingSettings.patrolDistance, originalPosition.y, transform.position.z);
            Gizmos.DrawWireCube(leftBoundPos, Vector3.one * 0.3f);
            Gizmos.DrawWireCube(rightBoundPos, Vector3.one * 0.3f);
            Gizmos.DrawLine(leftBoundPos, rightBoundPos);
        }

        // Draw dash direction if dashing
        if (isDashing)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, dashDirection * 3f);
        }
    }
}