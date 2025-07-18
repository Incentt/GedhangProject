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
    private PlayerController otherPlayer;


    #endregion

    private float _time;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<CapsuleCollider2D>();
        animController = GetComponent<PlayerAnimatorController>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // Set up position constraint
        _positionConstraint = GetComponent<PositionConstraint>();
        if (_positionConstraint == null)
        {
            _positionConstraint = gameObject.AddComponent<PositionConstraint>();
        }

        // Make sure there’s at least one source
        if (_positionConstraint.sourceCount == 0)
        {
            // Create a default source
            var source = new ConstraintSource
            {
                sourceTransform = null, // This will be set later when anchoring
                weight = 1f
            };
            _positionConstraint.AddSource(source);
        }

        _positionConstraint.constraintActive = false;
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
        HandleDirection();
        ApplyFriction();
        ApplyAirResistance();
        HandleGravityScale();
        HandleAnchoring();

        if (PlayerType == PlayerType.Player1)
        {
            //print("Grounded: " + _grounded + ", Is Anchored: " + _isAnchored);
        }
    }


    #region Collision
    private float _frameLeftGrounded = float.MinValue;
    private RaycastHit2D _groundHit;
    private bool _grounded;
    private bool _onJumpableSurface;
    private bool _onAnchorableSurface;
    private bool _wasOnJumpableSurface;

    private void CheckCollisions()
    {
        // Ground and Ceiling
        bool ceilingIsHit = Physics2D.CapsuleCast(new Vector2(_col.bounds.center.x, _col.bounds.center.y), _col.size, _col.direction, 0, Vector2.up, _stats.GroundAndCeilingCheckDistance, ~_stats.PlayerLayer);

        int currentPlayerLayer = gameObject.layer;
        int layerMask = ~(1 << currentPlayerLayer);

        RaycastHit2D leftGroundHit = Physics2D.Raycast(new Vector2(_col.bounds.min.x, _col.bounds.min.y), Vector2.down, _stats.GroundAndCeilingCheckDistance, layerMask);
        RaycastHit2D centerGroundHit = Physics2D.Raycast(new Vector2(_col.bounds.center.x, _col.bounds.min.y), Vector2.down, _stats.GroundAndCeilingCheckDistance, layerMask);
        RaycastHit2D rightGroundHit = Physics2D.Raycast(new Vector2(_col.bounds.max.x, _col.bounds.min.y), Vector2.down, _stats.GroundAndCeilingCheckDistance, layerMask);

        Debug.DrawRay(leftGroundHit.point, Vector2.down * _stats.GroundAndCeilingCheckDistance, Color.green);
        Debug.DrawRay(centerGroundHit.point, Vector2.down * _stats.GroundAndCeilingCheckDistance, Color.green);
        Debug.DrawRay(rightGroundHit.point, Vector2.down * _stats.GroundAndCeilingCheckDistance, Color.green);

        _groundHit = centerGroundHit.collider != null ? centerGroundHit : (leftGroundHit.collider != null ? leftGroundHit : rightGroundHit);
        bool groundIsHit = _groundHit.collider != null && _groundHit.collider != _col; // Ensure we're not detecting ourselves

        // Check if we're on a jumpable surface (excluding self)
        _onJumpableSurface = groundIsHit &&
                            _groundHit.collider != _col &&
                            (_stats.JumpableLayers.value & (1 << _groundHit.collider.gameObject.layer)) != 0;

        // Check if we're on a pivotable surface (excluding self)
        _onAnchorableSurface = groundIsHit &&
                              _groundHit.collider != _col &&
                              (_stats.AnchorableLayers.value & (1 << _groundHit.collider.gameObject.layer)) != 0;

        // Hit a Ceiling
        if (ceilingIsHit) _rb.velocity = new Vector2(_rb.velocity.x, Mathf.Min(0, _rb.velocity.y));

        // Landed on the Ground
        if (!_grounded && groundIsHit)
        {
            _grounded = true;
            _coyoteUsable = true;
            _bufferedJumpUsable = true;
            _endedJumpEarly = false;

            GroundedChanged?.Invoke(true, Mathf.Abs(_rb.velocity.y));
            animController.PlayLandAnimation();
        }
        // Left the Ground
        else if (_grounded && !groundIsHit)
        {
            _grounded = false;
            _frameLeftGrounded = _time;
            _wasOnJumpableSurface = _onJumpableSurface;
            _onJumpableSurface = false;
            GroundedChanged?.Invoke(false, 0);
        }

    }
    #endregion

    #region PlayerRotation

    private void HandlePlayerRotation()
    {
        if (_grounded || _isAnchored)
        {
            AlignRotationToGroundNormal();
        }
        else
        {
            transform.rotation = Quaternion.identity; // Reset rotation when not grounded or anchored
        }

    }

    private void AlignRotationToGroundNormal()
    {

        float distanceFromAnchor = GetComponent<Renderer>().bounds.size.y / 2 + _stats.GroundAndCeilingCheckDistance; // Using CapsuleCollider2D (your current setup)
        RaycastHit2D groundHitFromAnchor = Physics2D.Raycast(transform.position, Vector2.down, distanceFromAnchor, ~_stats.PlayerLayer);
        Debug.DrawRay(transform.position, Vector2.down * distanceFromAnchor, Color.red);

        // Get the target rotation
        Quaternion targetRot = Quaternion.identity;

        if (_groundHit.collider != null && groundHitFromAnchor.collider != null)
        {
            targetRot = Quaternion.FromToRotation(Vector3.up, groundHitFromAnchor.normal);
        }

        // Apply rotation
        if (targetRot == Quaternion.identity)
        {
            transform.rotation = targetRot; // Save resources
        }
        else
        {
            // Need to fix: jitter when in between 2 grounds normals
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * _stats.AlignRotationToGroundNormalLerpAmount
            );
        }
    }
    #endregion

    #region Jumping

    private bool _jumpToConsume;
    private bool _bufferedJumpUsable;
    private bool _endedJumpEarly;
    private bool _coyoteUsable;
    private float _timeJumpWasPressed;

    private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
    private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime && _wasOnJumpableSurface;
    private bool CanJump => (_grounded && _onJumpableSurface) || CanUseCoyote;

    private void HandleJump()
    {
        if (_isAnchored) return; // Skip jump handling while anchored

        if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.velocity.y > 0) _endedJumpEarly = true;

        if (!_jumpToConsume && !HasBufferedJump) return;

        if (CanJump) ExecuteJump();
        animController.PlayJumpAnimation();

        _jumpToConsume = false;
    }

    private void ExecuteJump()
    {
        _endedJumpEarly = false;
        _timeJumpWasPressed = 0;
        _bufferedJumpUsable = false;
        _coyoteUsable = false;
        _rb.AddForce(new Vector2(_rb.velocity.x, _stats.JumpPower), ForceMode2D.Impulse);
    }

    #endregion

    #region Movement
    private void HandleDirection()
    {
        // if (_frameInput.MoveHorizontal == 0)
        // {
        //     animController.PlayIdleAnimation();
        //     var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
        //     _rb.velocity = new Vector2(Mathf.MoveTowards(_rb.velocity.x, 0, deceleration * Time.fixedDeltaTime), _rb.velocity.y);
        // }
        // else
        // {
        //     _spriteRenderer.flipX = _frameInput.MoveHorizontal < 0;
        //     animController.PlayRunAnimation();
        //     _rb.velocity = new Vector2(Mathf.MoveTowards(_rb.velocity.x, _frameInput.MoveHorizontal * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime), _rb.velocity.y);
        // }
        if (_isAnchored) return; // Skip movement handling while anchored

        float acceleration = _grounded ? _stats.GroundAcceleration : _stats.InAirAcceleration;
        float deceleration = _grounded ? _stats.GroundDeceleration : _stats.InAirDeceleration;

        float targetSpeed = _frameInput.MoveHorizontal * _stats.TargetSpeed;
        float speedDifference = targetSpeed - _rb.velocity.x;
        float accelerationRate = Mathf.Abs(targetSpeed) < 0.01f ? deceleration : acceleration;
        float movementX = Mathf.Pow(Mathf.Abs(speedDifference) * accelerationRate, _stats.VelocityPower) * Mathf.Sign(speedDifference);

        if (_frameInput.MoveHorizontal == 0)
        {
            animController.PlayIdleAnimation();
        }
        else
        {
            _spriteRenderer.flipX = _frameInput.MoveHorizontal < 0;
            animController.PlayRunAnimation();
        }

        _rb.AddForce(movementX * Vector2.right);

    }

    private void ApplyFriction()
    {
        // if (_grounded && Mathf.Abs(_frameInput.MoveHorizontal) < 0.01f)
        // {
        //     float amount = Mathf.Min(Mathf.Abs(_rb.velocity.x), Mathf.Abs(_stats.FrictionAmount));
        //     amount *= Mathf.Sign(_rb.velocity.x);
        //     _rb.AddForce(Vector2.right * -amount, ForceMode2D.Impulse);
        // }
    }

    private void ApplyAirResistance()
    {
        if (_isAnchored) return; // Skip air resistance while anchored

        if (!_grounded)
        {
            _rb.velocity = new Vector2(_rb.velocity.x * _stats.AirResistance, _rb.velocity.y);
        }
    }

    #endregion

    #region Gravity Scale
    private void HandleGravityScale()
    {
        if (_isAnchored) return; // Skip gravity scale handling while anchored

        if (_grounded)
        {
            _rb.gravityScale = _stats.GroundingGravityScaleModifier;
        }
        else if (!_grounded && _rb.velocity.y < 0f)
        {
            _rb.gravityScale = _stats.FallingGravityScaleModifier;
        }
        else if (!_grounded && _rb.velocity.y > 0f)
        {
            _rb.gravityScale = _stats.InAirGravityScaleModifier;
        }

        if (_endedJumpEarly)
        {
            // If the jump was ended early, apply a stronger gravity scale
            _rb.gravityScale = _stats.JumpEndEarlyGravityScaleModifier;
        }
    }
    #endregion

    #region Anchoring
    private bool _isAnchored;
    // private Transform _originalParent;
    private RigidbodyType2D _originalBodyType;
    private PositionConstraint _positionConstraint;
    private void HandleAnchoring()
    {
        if (_frameInput.AnchorHeld && _grounded && !_isAnchored && _onAnchorableSurface)
        {
            //print("Attempting to anchor...");
            // Find the ground object to attach to
            GameObject groundObject = _groundHit.collider?.gameObject;

            if (groundObject != null)
            {
                StartAnchoring(groundObject);
            }

        }

        if (!_frameInput.AnchorHeld && _isAnchored)
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
        Vector3 offset = transform.position - groundObject.transform.position;

        var cs = _positionConstraint.GetSource(0);
        cs.sourceTransform = groundObject.transform;
        _positionConstraint.SetSource(0, cs);

        // Set the offset
        _positionConstraint.translationOffset = offset;

        // Enable constraint
        _positionConstraint.constraintActive = true;
        _positionConstraint.enabled = true;

        // Make rigidbody kinematic
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.velocity = Vector2.zero;

        // Play animation anchoring here

    }

    private void StopAnchoring()
    {
        _isAnchored = false;

        // Disable constraint
        _positionConstraint.enabled = false;
        _positionConstraint.constraintActive = false;

        // Restore original rigidbody type
        _rb.bodyType = _originalBodyType;
    }

    #endregion

    #region Swinging

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
