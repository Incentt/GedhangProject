using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Animations;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController : MonoBehaviour, IPlayerController
{
    public PlayerType PlayerType;
    [SerializeField] private PlayerStats _stats;
    private PlayerAnimatorController animController;

    private Rigidbody2D _rb;
    private CapsuleCollider2D _col;
    private FrameInput _frameInput;

    #region Interface
    public event Action<bool, float> GroundedChanged;

    private SpriteRenderer _spriteRenderer;
    public PlayerController otherPlayer { get; private set; }


    #endregion

    private float _time;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<CapsuleCollider2D>();
        animController = GetComponent<PlayerAnimatorController>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // // Set up position constraint
        // _positionConstraint = GetComponent<PositionConstraint>();
        // if (_positionConstraint == null)
        // {
        //     _positionConstraint = gameObject.AddComponent<PositionConstraint>();
        // }

        // // Make sure there’s at least one source
        // if (_positionConstraint.sourceCount == 0)
        // {
        //     // Create a default source
        //     var source = new ConstraintSource
        //     {
        //         sourceTransform = null, // This will be set later when anchoring
        //         weight = 1f
        //     };
        //     _positionConstraint.AddSource(source);
        // }

        // _positionConstraint.constraintActive = false;
    }
    void Start()
    {
        if (GameManager.Instance != null)
        {
            if (PlayerType == PlayerType.Player1)
            {
                otherPlayer = GameManager.Instance.currentPlayer2.GetComponent<PlayerController>();
            }
            else if (PlayerType == PlayerType.Player2)
            {
                otherPlayer = GameManager.Instance.currentPlayer1.GetComponent<PlayerController>();
            }
        }
    }
    private void Update()
    {
        _time += Time.deltaTime;
        GatherInput();
    }

    private void GatherInput()
    {
        string playerNumber = PlayerType == PlayerType.Player1 ? "1" : "2";

        _frameInput = new FrameInput
        {
            JumpDown = Input.GetButtonDown("Jump" + playerNumber),
            JumpHeld = Input.GetButton("Jump" + playerNumber),
            AnchorHeld = Input.GetButton("Anchor" + playerNumber),
            MoveHorizontal = Input.GetAxisRaw("Horizontal" + playerNumber)
        };

        if (_stats.SnapInput)
        {
            _frameInput.MoveHorizontal = Mathf.Abs(_frameInput.MoveHorizontal) < _stats.HorizontalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.MoveHorizontal);
        }

        if (_frameInput.JumpDown)
        {
            _jumpToConsume = true;
            _timeJumpWasPressed = _time;
        }
    }

    private void FixedUpdate()
    {
        CheckCollisions();
        HandlePlayerRotation();
        HandleJump();
        HandleEdgeDetection();
        HandleHorizontalMovementOnGroundAndAir();
        HandleGravityScale();
        HandleAnchoring();
        HandleSwinging();
        HandleMaxVelocity();

        Debug.DrawRay(transform.position, transform.right * 2f, Color.blue);

        if (PlayerType == PlayerType.Player1)
        {
            // Debug.Log("Grounded: " + _isGrounded + ", Is Anchored: " + _isAnchored + ", Is Swinging: " + _isSwinging + ", Jump Cut: " + _endedJumpEarly);
        }
    }


    #region Collision
    private float _frameLeftGrounded = float.MinValue;
    private RaycastHit2D _groundHit;
    private bool _isGrounded;
    private bool _onJumpableSurface;
    private bool _onAnchorableSurface;
    private bool _wasOnJumpableSurface;

    private void CheckCollisions()
    {
        // Ground and Ceiling
        // bool ceilingIsHit = Physics2D.CapsuleCast(new Vector2(_col.bounds.center.x, _col.bounds.center.y), _col.size, _col.direction, 0, Vector2.up, _stats.GroundAndCeilingCheckDistance, ~_stats.PlayerLayer);

        int currentPlayerLayer = gameObject.layer;
        int layerMask = ~(1 << currentPlayerLayer);

        RaycastHit2D leftGroundHit = Physics2D.Raycast(new Vector2(_col.bounds.min.x + _stats.GroundAndCeilingCheckSidePadding, _col.bounds.min.y), Vector2.down, _stats.GroundAndCeilingCheckDistance, layerMask);
        RaycastHit2D centerGroundHit = Physics2D.Raycast(new Vector2(_col.bounds.center.x, _col.bounds.min.y), Vector2.down, _stats.GroundAndCeilingCheckDistance, layerMask);
        RaycastHit2D rightGroundHit = Physics2D.Raycast(new Vector2(_col.bounds.max.x - _stats.GroundAndCeilingCheckSidePadding, _col.bounds.min.y), Vector2.down, _stats.GroundAndCeilingCheckDistance, layerMask);

        Debug.DrawRay(leftGroundHit.point, Vector2.down * _stats.GroundAndCeilingCheckDistance, Color.green);
        Debug.DrawRay(centerGroundHit.point, Vector2.down * _stats.GroundAndCeilingCheckDistance, Color.green);
        Debug.DrawRay(rightGroundHit.point, Vector2.down * _stats.GroundAndCeilingCheckDistance, Color.green);

        _groundHit = centerGroundHit.collider != null ? centerGroundHit : (leftGroundHit.collider != null ? leftGroundHit : rightGroundHit);
        bool groundIsHit = _groundHit.collider != null && _groundHit.collider != _col; // Ensure we're not detecting ourselves

        // Check if we're on a jumpable surface (excluding self)
        bool currentlyOnJumpableSurface = groundIsHit &&
                            _groundHit.collider != _col &&
                            (_stats.JumpableLayers.value & (1 << _groundHit.collider.gameObject.layer)) != 0;

        // Check if we're on a anchorable surface (excluding self)
        bool currentlyOnAnchorableSurface = groundIsHit &&
                              _groundHit.collider != _col &&
                              (_stats.AnchorableLayers.value & (1 << _groundHit.collider.gameObject.layer)) != 0;

        // Hit a Ceiling
        // if (ceilingIsHit) _rb.velocity = new Vector2(_rb.velocity.x, Mathf.Min(0, _rb.velocity.y));

        // Landed on the Ground
        if (!_isGrounded && groundIsHit)
        {
            _isGrounded = true;
            _coyoteUsable = true;
            _bufferedJumpUsable = true;
            _endedJumpEarly = false;

            _onJumpableSurface = currentlyOnJumpableSurface;
            _onAnchorableSurface = currentlyOnAnchorableSurface;

            GroundedChanged?.Invoke(true, Mathf.Abs(_rb.velocity.y));
            animController.PlayLandAnimation();
        }
        // Left the Ground
        else if (_isGrounded && !groundIsHit)
        {
            _isGrounded = false;
            _frameLeftGrounded = _time;
            _wasOnJumpableSurface = _onJumpableSurface;
            _onJumpableSurface = false;
            GroundedChanged?.Invoke(false, 0);
        }
        else if (_isGrounded && groundIsHit)
        {
            // Still grounded
            _onJumpableSurface = currentlyOnJumpableSurface;
            _onAnchorableSurface = currentlyOnAnchorableSurface;
        }

    }
    #endregion

    #region Rotation

    private void HandlePlayerRotation()
    {
        if (_isGrounded || _isAnchored)
        {
            AlignRotationToGroundNormal();
        }
        else if (_isSwinging)
        {
            AlignRotationToSwingDirection();
        }
        else
        {
            _rb.MoveRotation(0f);
        }

    }

    private void AlignRotationToGroundNormal()

    {
        Vector2 dir = new Vector2(_frameInput.MoveHorizontal * 0.5f, -1);
        float distanceFromPivot = GetComponent<Renderer>().bounds.size.y / 2 + _stats.GroundAndCeilingCheckDistance;
        RaycastHit2D groundHitFromPivot = Physics2D.Raycast(transform.position, dir, distanceFromPivot, ~_stats.PlayerLayer);
        Debug.DrawRay(transform.position, dir * distanceFromPivot, Color.red);

        Quaternion targetRot = Quaternion.identity;

        if (_groundHit.collider != null && groundHitFromPivot.collider != null && Vector2.Dot(groundHitFromPivot.normal, Vector2.up) > _stats.GroundNormalDotThreshold)
        {
            targetRot = Quaternion.FromToRotation(Vector3.up, groundHitFromPivot.normal);
        }

        float targetAngle = targetRot.eulerAngles.z;

        if (targetRot == Quaternion.identity)
        {
            _rb.MoveRotation(0f);
        }
        else
        {
            float lerpedAngle = Mathf.LerpAngle(transform.rotation.eulerAngles.z, targetAngle, Time.deltaTime * _stats.AlignRotationLerpAmount);
            _rb.MoveRotation(lerpedAngle);
        }
    }

    private void AlignRotationToSwingDirection()
    {
        if (!_isSwinging) return;

        Vector2 direction = otherPlayer.transform.position - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        float lerpedAngle = Mathf.LerpAngle(transform.rotation.eulerAngles.z, angle, Time.deltaTime * _stats.AlignRotationLerpAmount);
        _rb.MoveRotation(lerpedAngle);
    }

    #endregion

    #region Jumping

    private bool _jumpToConsume;
    private bool _bufferedJumpUsable;
    private bool _endedJumpEarly;
    private bool _coyoteUsable;
    private float _timeJumpWasPressed;

    private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
    private bool CanUseCoyote => _coyoteUsable && !_isGrounded && _time < _frameLeftGrounded + _stats.CoyoteTime && _wasOnJumpableSurface;
    private bool CanJump => (_isGrounded && _onJumpableSurface) || CanUseCoyote;

    private void HandleJump()
    {
        if (_isAnchored) return; // Skip jump handling while anchored

        if (!_endedJumpEarly && !_isGrounded && !_frameInput.JumpHeld && _rb.velocity.y > 0f)
        {
            _endedJumpEarly = true;
            ExecuteJumpCut();
        }

        if (!_jumpToConsume && !HasBufferedJump) return;

        if (CanJump)
        {
            ExecuteJump();
            animController.PlayJumpAnimation();
        }


        _jumpToConsume = false;
    }

    private void ExecuteJump()
    {
        _endedJumpEarly = false;
        _timeJumpWasPressed = 0;
        _bufferedJumpUsable = false;
        _coyoteUsable = false;
        _rb.velocity = new Vector2(_rb.velocity.x * _stats.JumpHorizontalVelocityMultiplier, 0); // Reset vertical velocity
        _rb.AddForce(new Vector2(0, _stats.JumpImpulse), ForceMode2D.Impulse);
    }

    private void ExecuteJumpCut()
    {

        // Stop upward movement
        _rb.velocity = new Vector2(_rb.velocity.x, 0);
        // Apply immediate downward force when ending jump early
        _rb.AddForce(new Vector2(0, -_stats.JumpEndEarlyImpulse), ForceMode2D.Impulse);

    }

    #endregion

    #region Edge Detection
    private void HandleEdgeDetection()
    {
        if (_isGrounded || _isAnchored || _isSwinging || _rb.velocity.y < _stats.EdgeDetectionVelYThreshold || Mathf.Abs(_frameInput.MoveHorizontal) < 0.1f) return; // Skip edge detection while grounded or anchored or falling

        // Edge detection
        float moveDirection = Mathf.Sign(_frameInput.MoveHorizontal);

        // Check for ground ahead of player's movement
        Vector2 wallCheckAheadPos = new Vector2(
            _col.bounds.center.x + (_col.bounds.size.x / 2 + _stats.EdgeDetectionDistance) * moveDirection,
            _col.bounds.min.y
        );

        // Check for wall ahead at player height
        Vector2 airCheckAheadPos = new Vector2(
            _col.bounds.center.x + (_col.bounds.size.x / 2 + _stats.EdgeDetectionDistance) * moveDirection,
            _col.bounds.min.y + _stats.EdgeDetectionOffset
        );


        RaycastHit2D wallCheckHit = Physics2D.Raycast(wallCheckAheadPos, Vector2.right * _frameInput.MoveHorizontal, _stats.EdgeDetectionDistance, ~_stats.PlayerLayer);
        RaycastHit2D airCheckHit = Physics2D.Raycast(airCheckAheadPos, Vector2.right * _frameInput.MoveHorizontal, _stats.EdgeDetectionDistance, ~_stats.PlayerLayer);

        Debug.DrawRay(wallCheckAheadPos, Vector2.right * _frameInput.MoveHorizontal * _stats.EdgeDetectionDistance, Color.green);
        Debug.DrawRay(airCheckAheadPos, Vector2.right * _frameInput.MoveHorizontal * _stats.EdgeDetectionDistance, Color.green);

        if (wallCheckHit.collider != null && airCheckHit.collider == null)
        {
            ExecuteEdgeUpImpulse();
        }
    }

    private void ExecuteEdgeUpImpulse()
    {
        _rb.AddForce(new Vector2(0, _stats.EdgeUpImpulse), ForceMode2D.Impulse);
    }
    #endregion

    #region Movement
    private void HandleHorizontalMovementOnGroundAndAir()
    {
        if (_isAnchored || _isSwinging) return; // Skip movement handling while anchored / swinging

        float acceleration = _isGrounded ? _stats.Acceleration : _stats.Acceleration * _stats.InAirAccelerationMultiplier;
        float deceleration = _isGrounded ? _stats.Deceleration : _stats.Deceleration * _stats.InAirDecelerationMultiplier;

        float targetSpeed = _frameInput.MoveHorizontal * _stats.RunMaxSpeed;
        float speedDifference = targetSpeed - _rb.velocity.x;
        float accelerationRate = Mathf.Abs(targetSpeed) < 0.01f ? deceleration : acceleration;

        float movementX = speedDifference * accelerationRate;

        if (_frameInput.MoveHorizontal == 0)
        {
            animController.PlayIdleAnimation();
        }
        else
        {
            _spriteRenderer.flipX = _frameInput.MoveHorizontal < 0;
            animController.PlayRunAnimation();
        }

        _rb.AddForce(movementX * transform.right);
    }

    private void HandleMaxVelocity()
    {
        if (_isSwinging)
        {
            if (_rb.velocity.magnitude > _stats.MaxSwingVelocity)
            {
                _rb.velocity = _rb.velocity.normalized * _stats.MaxSwingVelocity;
            }
        }
        else
        {
            if (_rb.velocity.x > _stats.RunMaxSpeed)
            {
                _rb.velocity = new Vector2(_stats.RunMaxSpeed, _rb.velocity.y);
            }

            if (_rb.velocity.y < -_stats.MaxFallSpeed)
            {
                _rb.velocity = new Vector2(_rb.velocity.x, -_stats.MaxFallSpeed);
            }
        }

    }

    #endregion

    #region Gravity Scale
    private void HandleGravityScale()
    {
        if (_isAnchored) return; // Skip gravity scale handling while anchored

        if (_isGrounded)
        {
            _rb.gravityScale = _stats.GroundingGravityScaleModifier;
        }
        else if (_isSwinging)
        {
            _rb.gravityScale = _stats.SwingingGravityScaleModifier;
        }
        else if (!_isGrounded && _rb.velocity.y < 0f)
        {
            _rb.gravityScale = _stats.FallingGravityScaleModifier;
        }
        else if (!_isGrounded && _rb.velocity.y > 0f)
        {
            _rb.gravityScale = _stats.FloatUpGravityScaleModifier;
        }
    }
    #endregion

    #region Anchoring
    private bool _isAnchored;
    // private Transform _originalParent;
    private RigidbodyType2D _originalBodyType;
    // private PositionConstraint _positionConstraint;
    private void HandleAnchoring()
    {
        if (_frameInput.AnchorHeld && _isGrounded && !_isAnchored && _onAnchorableSurface && !_isSwinging)
        {
            // Find the ground object to attach to
            GameObject groundObject = _groundHit.collider?.gameObject;

            if (groundObject != null)
            {
                StartAnchoring(groundObject);
            }

        }

        if (!_frameInput.AnchorHeld && _isAnchored || !_isGrounded && _isAnchored)
        {
            StopAnchoring();
        }
    }

    private void StartAnchoring(GameObject groundObject)
    {

        if (_isAnchored) return; // Already anchored
        if (groundObject == null) return; // No ground object to anchor on

        _isAnchored = true;

        // Store original state
        _originalBodyType = _rb.bodyType;

        // Store current position offset from ground
        // Vector3 offset = transform.position - groundObject.transform.position;

        // var cs = _positionConstraint.GetSource(0);
        // cs.sourceTransform = groundObject.transform;
        // _positionConstraint.SetSource(0, cs);

        // // Set the offset
        // _positionConstraint.translationOffset = offset;

        // // Enable constraint
        // _positionConstraint.constraintActive = true;
        // _positionConstraint.enabled = true;

        // Play crouch animation
        animController.PlayCrouchAnimation(true);

        // Make rigidbody kinematic
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.velocity = Vector2.zero;
    }

    public void StopAnchoring()
    {
        _isAnchored = false;

        // Disable constraint
        // _positionConstraint.enabled = false;
        // _positionConstraint.constraintActive = false;

        // Stop animation
        animController.PlayCrouchAnimation(false);

        // Restore original rigidbody type
        _rb.bodyType = _originalBodyType;
    }

    #endregion

    #region Swinging
    private bool _isSwinging;
    public bool isSwinging => _isSwinging;
    private bool _useInvertedSwingingDirection = false;

    private void HandleSwinging()
    {
        if (!_isGrounded && !_isAnchored && !_isSwinging && otherPlayer._isAnchored)
        {
            StartSwinging();
            Debug.Log("Started swinging");
        }

        if (_isGrounded && _isSwinging || !otherPlayer._isAnchored && _isSwinging)
        {
            StopSwinging();
            Debug.Log("Stopped swinging");
        }

        if (_isSwinging)
        {
            if (transform.position.y < otherPlayer.transform.position.y)
            {
                _useInvertedSwingingDirection = true;
            }
            else
            {
                _useInvertedSwingingDirection = false;
            }

            HandleHorizontalMovementWhileSwinging();
        }

    }

    private void StartSwinging()
    {
        _isSwinging = true;
    }

    private void StopSwinging()
    {
        _isSwinging = false;

        Vector2 anchorPoint = otherPlayer.transform.position;
        Vector2 ropeVector = (Vector2)transform.position - anchorPoint;
        Vector2 tangent = Vector2.Perpendicular(ropeVector).normalized;

        // Current swing velocity in tangent direction
        float swingVelocity = Vector2.Dot(_rb.velocity, tangent);

        // Apply impulse at the end of swing
        float impulse = swingVelocity * _stats.SwingEndImpulseMultiplier;
        _rb.AddForce(tangent * impulse, ForceMode2D.Impulse);
        otherPlayer._rb.AddForce(tangent * impulse, ForceMode2D.Impulse);
    }

    private void HandleHorizontalMovementWhileSwinging()
    {
        if (otherPlayer == null || !otherPlayer._isAnchored) return;

        Vector2 anchorPoint = otherPlayer.transform.position;
        Vector2 ropeVector = (Vector2)transform.position - anchorPoint;
        Vector2 tangent = Vector2.Perpendicular(ropeVector).normalized;

        // Current swing velocity in tangent direction
        float swingVelocity = Vector2.Dot(_rb.velocity, tangent);
        
        // Apply local downward force: base force + velocity-based force
        float baseDownForce = _stats.SwingBaseDownForce;
        float velocityDownForce = Mathf.Abs(swingVelocity) * _stats.SwingVelocityDownForceMultiplier;
        float totalDownForce = baseDownForce + velocityDownForce;
        
        Vector2 localDownForce = -transform.up * totalDownForce;
        _rb.AddForce(localDownForce, ForceMode2D.Force);
        
        float input = _frameInput.MoveHorizontal;
        if (Mathf.Abs(input) < 0.01f) return;

        _spriteRenderer.flipX = input < 0;

        // Momentum-based force: stronger when adding to existing momentum
        float momentumFactor = 1f;
        if (Mathf.Sign(input) == Mathf.Sign(swingVelocity))
        {
            momentumFactor = 1f + (Mathf.Abs(swingVelocity) / _stats.MaxSwingVelocity) * _stats.SwingMomentumBonus;
        }

        Vector2 swingForce = tangent * input * _stats.SwingForce * momentumFactor;
        _rb.AddForce(swingForce, ForceMode2D.Force);
    }



    #endregion
}

public struct FrameInput
{
    public bool JumpDown;
    public bool JumpHeld;
    public bool AnchorHeld;
    public float MoveHorizontal;
}

public interface IPlayerController
{
    public event Action<bool, float> GroundedChanged;
}
