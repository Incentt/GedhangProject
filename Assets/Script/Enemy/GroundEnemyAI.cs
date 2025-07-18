using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class GroundEnemyAI : EnemyAI
{
    [Header("AI Configuration")]
    [SerializeField] private new GroundEnemyAISettings aiSettings;

    // State management
    private bool isPatrolling = true;
    private bool isAlive = true;
    private float pauseTimer = 0f;
    private bool isPaused = false;

    // Properties for easy access
    private float PatrolSpeed => aiSettings.patrolSpeed;
    private float PauseTime => aiSettings.stayTime;
    private LayerMask GroundLayer => aiSettings.groundLayer;
    private LayerMask WallLayer => aiSettings.wallLayer;
    private bool TurnOnWallHit => aiSettings.turnOnWallHit;
    private bool TurnOnLedge => aiSettings.turnOnLedge;
    private bool PauseBeforeTurn => aiSettings.pauseBeforeTurn;

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

    private void InitializeAI()
    {
        base.InitializeAI();
        // Set up ground and wall check points if they don't exist
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = groundCheckObj.transform;
        }

        if (wallCheck == null)
        {
            GameObject wallCheckObj = new GameObject("WallCheck");
            wallCheckObj.transform.SetParent(transform);
            wallCheckObj.transform.localPosition = new Vector3(facingRight ? 0.5f : -0.5f, 0, 0);
            wallCheck = wallCheckObj.transform;
        }

        Debug.Log("Ground Enemy AI initialized - Patrol Only Mode");
    }

    private void Start()
    {
        StartPatrolling();
    }

    private void Update()
    {
        if (!isAlive)
            return;

        CheckEnvironment();
        HandleMovement();
    }

    private void FixedUpdate()
    {
        if (!isAlive)
            return;

        // Apply movement
        if (isPatrolling && !isPaused)
        {
            float moveSpeed = facingRight ? PatrolSpeed : -PatrolSpeed;
            rb.velocity = new Vector2(moveSpeed, rb.velocity.y);
        }
        else
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    private void CheckEnvironment()
    {
        if (groundCheck == null || wallCheck == null)
            return;

        // Check if grounded
        Vector2 groundCheckPos = groundCheck.position;
        isGrounded = Physics2D.OverlapCircle(groundCheckPos, aiSettings.groundCheckRadius, GroundLayer);

        // Check for ledge (only when grounded)
        if (isGrounded)
        {
            Vector2 ledgeCheckPos = groundCheckPos + Vector2.right * (facingRight ? aiSettings.detectionDistance : -aiSettings.detectionDistance);
            isAtLedge = !Physics2D.OverlapCircle(ledgeCheckPos, aiSettings.groundCheckRadius, GroundLayer);
        }
        else
        {
            isAtLedge = false;
        }

        // Check for wall
        Vector2 wallCheckPos = wallCheck.position;
        Vector2 wallCheckDirection = facingRight ? Vector2.right : Vector2.left;
        RaycastHit2D wallHit = Physics2D.Raycast(wallCheckPos, wallCheckDirection, aiSettings.wallCheckDistance, WallLayer);
        isTouchingWall = wallHit.collider != null;

        // Debug visualization
        Debug.DrawRay(wallCheckPos, wallCheckDirection * aiSettings.wallCheckDistance, isTouchingWall ? Color.red : Color.white);
        Debug.DrawLine(groundCheckPos, groundCheckPos + Vector2.down * aiSettings.groundCheckRadius, isGrounded ? Color.green : Color.red);

        if (isGrounded)
        {
            Vector2 ledgeCheckPos = groundCheckPos + Vector2.right * (facingRight ? aiSettings.detectionDistance : -aiSettings.detectionDistance);
            Debug.DrawLine(ledgeCheckPos, ledgeCheckPos + Vector2.down * aiSettings.groundCheckRadius, isAtLedge ? Color.red : Color.blue);
        }
    }

    private void HandleMovement()
    {
        if (isPaused)
        {
            pauseTimer += Time.deltaTime;
            if (pauseTimer >= PauseTime)
            {
                isPaused = false;
                pauseTimer = 0f;
                Turn();
            }
            return;
        }

        // Only check for turns if we're grounded and moving
        if (isGrounded && isPatrolling)
        {
            // Check if we need to turn around
            bool shouldTurnForWall = isTouchingWall && TurnOnWallHit;
            bool shouldTurnForLedge = isAtLedge && TurnOnLedge;

            if (shouldTurnForWall || shouldTurnForLedge)
            {
                if (PauseBeforeTurn)
                {
                    isPaused = true;
                    SetWalkingAnimation(false);
                }
                else
                {
                    Turn();
                }
            }
        }
        // If not grounded, stop moving but don't turn (let gravity handle it)
        else if (!isGrounded)
        {
            // Stop horizontal movement when in air
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
    }

    private void Turn()
    {
        facingRight = !facingRight;

        // Flip sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
        }

        // Update wall check position
        if (wallCheck != null)
        {
            Vector3 wallCheckPos = wallCheck.localPosition;
            wallCheckPos.x = facingRight ? Mathf.Abs(wallCheckPos.x) : -Mathf.Abs(wallCheckPos.x);
            wallCheck.localPosition = wallCheckPos;
        }

        // Resume walking animation if not paused
        if (!isPaused)
        {
            SetWalkingAnimation(true);
        }
    }

    private void StartPatrolling()
    {
        isPatrolling = true;
        SetWalkingAnimation(true);
    }

    private void StopPatrolling()
    {
        isPatrolling = false;
        SetWalkingAnimation(false);
    }

    private void SetWalkingAnimation(bool isWalking)
    {
        if (animator != null && !string.IsNullOrEmpty(aiSettings.walkingAnimationParameter))
        {
            animator.SetBool(aiSettings.walkingAnimationParameter, isWalking);
        }
    }

    public void Die()
    {
        isAlive = false;
        StopPatrolling();

        rb.velocity = Vector2.zero;
        // Play death animation
        if (animator != null && !string.IsNullOrEmpty(aiSettings.deathAnimationParameter))
        {
            animator.SetTrigger(aiSettings.deathAnimationParameter);
        }
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (aiSettings == null)
            return;

        // Draw ground check
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, aiSettings.groundCheckRadius);

            // Draw ledge check position
            Vector2 ledgeCheckPos = groundCheck.position + Vector3.right * (facingRight ? aiSettings.detectionDistance : -aiSettings.detectionDistance);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(ledgeCheckPos, aiSettings.groundCheckRadius);
        }

        // Draw wall check
        if (wallCheck != null)
        {
            Gizmos.color = Color.red;
            Vector3 wallCheckDirection = facingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawRay(wallCheck.position, wallCheckDirection * aiSettings.wallCheckDistance);
        }
    }
}