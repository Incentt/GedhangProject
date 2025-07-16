using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerType
{
    Player1 = 1,
    Player2 = 2,
}

[CreateAssetMenu]
public class PlayerStats : ScriptableObject
{
    [Header("LAYERS")]
    [Tooltip("Set this to the layer your player is on")]
    public LayerMask PlayerLayer;

    [Tooltip("Layers that the player can jump from. Only surfaces on these layers will allow jumping")]
    public LayerMask JumpableLayers;

    [Header("INPUT")]
    [Tooltip("Makes all Input snap to an integer. Prevents gamepads from walking slowly. Recommended value is true to ensure gamepad/keybaord parity.")]
    public bool SnapInput = true;

    [Tooltip("Minimum input required before you mount a ladder or climb a ledge. Avoids unwanted climbing using controllers"), Range(0.01f, 0.99f)]
    public float VerticalDeadZoneThreshold = 0.3f;

    [Tooltip("Minimum input required before a left or right is recognized. Avoids drifting with sticky controllers"), Range(0.01f, 0.99f)]
    public float HorizontalDeadZoneThreshold = 0.1f;

    [Header("MOVEMENT")]
    [Tooltip("The top horizontal movement speed")]
    public float TargetSpeed = 25;

    [Tooltip("The player's capacity to gain horizontal speed at ground")]
    public float GroundAcceleration = 60;

    [Tooltip("The player's capacity to lose horizontal speed at ground")]
    public float GroundDeceleration = 120;

    [Tooltip("The player's capacity to gain horizontal speed at air")]
    public float InAirAcceleration = 60;

    [Tooltip("The player's capacity to lose horizontal speed at air")]
    public float InAirDeceleration = 120;

    [Tooltip("The power applied to the accel or deccel of the player horizontal velocity")]
    public float FrictionAmount = 0.8f;
    [Tooltip("The power applied to the accel or deccel of the player horizontal velocity")]
    public float AirResistance = 0.95f;

    [Tooltip("The power applied to the accel or deccel of the player horizontal velocity")]
    public float VelocityPower = 0.9f;

    [Tooltip("Gravity scale multiplier when grounded. Helps on slopes")]
    public float GroundingGravityScaleModifier = 5f;

    [Tooltip("The detection distance for grounding and roof detection"), Range(0f, 0.5f)]
    public float GrounderDistance = 0.2f;

    [Header("JUMP")]
    [Tooltip("The immediate velocity applied when jumping")]
    public float JumpPower = 36;

    [Tooltip("The maximum vertical movement speed")]
    public float MaxFallSpeed = 40;

    [Tooltip("The player's capacity to gain fall speed. a.k.a. In Air Gravity")]
    public float FallAcceleration = 20;

    [Tooltip("The gravity multiplier added when in air")]
    public float InAirGravityScaleModifier = 4f;

    [Tooltip("The gravity multiplier added when falling")]
    public float FallingGravityScaleModifier = 4f;

    [Tooltip("The gravity multiplier added when jump is released early")]
    public float JumpEndEarlyGravityScaleModifier = 8f;

    [Tooltip("The time before coyote jump becomes unusable. Coyote jump allows jump to execute even after leaving a ledge")]
    public float CoyoteTime = .15f;

    [Tooltip("The amount of time we buffer a jump. This allows jump input before actually hitting the ground")]
    public float JumpBuffer = .2f;
}

