using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Android;

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
    public event Action Jumped;
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
        HandleJump();
        HandleDirection();
        ApplyFriction();
        ApplyAirResistance();
        HandleGravityScale();
    }

    #region Collision
    private float _frameLeftGrounded = float.MinValue;
    private bool _grounded;
    private bool _onJumpableSurface;
    private bool _wasOnJumpableSurface;

    private void CheckCollisions()
    {
        // Ground and Ceiling
        bool ceilingIsHit = Physics2D.CapsuleCast(new Vector2(_col.bounds.center.x, _col.bounds.center.y + _stats.GroundAndCeilingCheckOffset), _col.size, _col.direction, 0, Vector2.up, _stats.GroundAndCeilingCheckDistance, ~_stats.PlayerLayer);

        RaycastHit2D leftGroundHit = Physics2D.Raycast(new Vector2(_col.bounds.min.x, _col.bounds.min.y - _stats.GroundAndCeilingCheckOffset), Vector2.down, _stats.GroundAndCeilingCheckDistance);
        RaycastHit2D centerGroundHit = Physics2D.Raycast(new Vector2(_col.bounds.center.x, _col.bounds.min.y - _stats.GroundAndCeilingCheckOffset), Vector2.down, _stats.GroundAndCeilingCheckDistance);
        RaycastHit2D rightGroundHit = Physics2D.Raycast(new Vector2(_col.bounds.max.x, _col.bounds.min.y - _stats.GroundAndCeilingCheckOffset), Vector2.down, _stats.GroundAndCeilingCheckDistance);

        Debug.DrawRay(leftGroundHit.point, Vector2.down * _stats.GroundAndCeilingCheckDistance, Color.green);
        Debug.DrawRay(centerGroundHit.point, Vector2.down * _stats.GroundAndCeilingCheckDistance, Color.green);
        Debug.DrawRay(rightGroundHit.point, Vector2.down * _stats.GroundAndCeilingCheckDistance, Color.green);

        float distanceFromPivot = GetComponent<Renderer>().bounds.size.y / 2 + _stats.GroundAndCeilingCheckOffset + _stats.GroundAndCeilingCheckDistance; // Using CapsuleCollider2D (your current setup)
        RaycastHit2D groundHitFromPivot = Physics2D.Raycast(transform.position, Vector2.down, distanceFromPivot, ~_stats.PlayerLayer);
        Debug.DrawRay(transform.position, Vector2.down * distanceFromPivot, Color.red);

        bool groundIsHit = centerGroundHit.collider != null || leftGroundHit.collider != null || rightGroundHit.collider != null;

        // Align the player's rotation to the ground normal
        AlignRotationToGroundNormal(groundHitFromPivot);

        // Check if we're on a jumpable surface
        bool jumpableGroundIsHit = Physics2D.CapsuleCast(new Vector2(_col.bounds.center.x, _col.bounds.center.y - _stats.GroundAndCeilingCheckOffset), _col.size, _col.direction, 0, Vector2.down, _stats.GroundAndCeilingCheckDistance, _stats.JumpableLayers);

        // Hit a Ceiling
        if (ceilingIsHit) _rb.velocity = new Vector2(_rb.velocity.x, Mathf.Min(0, _rb.velocity.y));

        // Update jumpable surface status
        _onJumpableSurface = jumpableGroundIsHit;

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

    private void AlignRotationToGroundNormal(RaycastHit2D groundHit)
    {

        // Get the target rotation
        Quaternion targetRot = Quaternion.identity;

        if (groundHit.collider != null)
        {
            targetRot = Quaternion.FromToRotation(Vector3.up, groundHit.normal);
        }

        // Apply rotation
        if (targetRot == Quaternion.identity)
        {
            transform.rotation = targetRot;
        }
        else
        {
            // Need to fix: jitter when in between 2 grounds normals
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * _stats.StandBasedOnNormalLerpAmount
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
        Jumped?.Invoke();
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
        if (!_grounded)
        {
            _rb.velocity = new Vector2(_rb.velocity.x * _stats.AirResistance, _rb.velocity.y);
        }
    }

    #endregion

    #region Horizontal
    private void HandleGravityScale()
    {
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
}

public struct FrameInput
{
    public bool JumpDown;
    public bool JumpHeld;
    public float MoveHorizontal;
}

public interface IPlayerController
{
    public event Action<bool, float> GroundedChanged;

    public event Action Jumped;
}
