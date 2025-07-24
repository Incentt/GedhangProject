using System.Collections;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;

public enum PlayerType
{
    Player1 = 1,
    Player2 = 2,
}

[CreateAssetMenu(fileName = "PlayerMovementStats", menuName = "ScriptableObjects/PlayerMovementStats", order = 1)]
public class PlayerStats : ScriptableObject
{
    [Header("LAYERS")]
    [Tooltip("Set this to the layer your player is on")]
    public LayerMask PlayerLayer;

    [Tooltip("Layers that the player can jump from. Only surfaces on these layers will allow jumping")]
    public LayerMask JumpableLayers;

    [Tooltip("Layers that the player can anchor on. Only surfaces on these layers will allow anchoring")]
    public LayerMask AnchorableLayers;

    [Header("INPUT")]
    [Tooltip("Makes all Input snap to an integer. Prevents gamepads from walking slowly. Recommended value is true to ensure gamepad/keybaord parity.")]
    public bool SnapInput = true;

    [Tooltip("Minimum input required before you mount a ladder or climb a ledge. Avoids unwanted climbing using controllers"), Range(0.01f, 0.99f)]
    public float VerticalDeadZoneThreshold = 0.3f;

    [Tooltip("Minimum input required before a left or right is recognized. Avoids drifting with sticky controllers"), Range(0.01f, 0.99f)]
    public float HorizontalDeadZoneThreshold = 0.1f;

    [Header("MOVEMENT")]
    [Tooltip("The top horizontal movement speed")]
    public float RunMaxSpeed = 25;

    [Tooltip("The player's capacity to gain horizontal speed")]
    public float Acceleration = 18;

    [Tooltip("The player's capacity to lose horizontal speed")]
    public float Deceleration = 20;

    [Tooltip("Acceleration multiplier when in air. Helps with air control")]
    public float InAirAccelerationMultiplier = 0.5f;

    [Tooltip("Deceleration multiplier when in air. Helps with air control")]
    public float InAirDecelerationMultiplier = 0.65f;

    [Header("Collision Detection")]

    [Tooltip("The detection distance for grounding and roof"), Range(0f, 0.5f)]
    public float GroundAndCeilingCheckDistance = 0.2f;

    [Tooltip("The detection padding for grounding and roof"), Range(0f, 0.5f)]
    public float GroundAndCeilingCheckSidePadding = 0.1f;

    [Header("Edge Detection")]
    [Tooltip("The detection offset for edge detection. Helps with ledge detection"), Range(0f, 0.5f)]
    public float EdgeDetectionOffset = 0.4f;

    [Tooltip("The detection distance for edge detection. Helps with ledge detection")]
    public float EdgeDetectionDistance = 0.65f;

    [Tooltip("The minimum vertical velocity the player can auto-jump. Helps with ledge detection")]
    public float EdgeDetectionVelYThreshold = -5f; // Minimum vertical velocity to consider edge detection

    [Tooltip("The impulse applied when jumping from edge detection")]
    public float EdgeUpImpulse = 20f; // Impulse applied when jumping from an edge

    [Header("JUMP")]
    [Tooltip("The immediate velocity applied when jumping")]
    public float JumpImpulse = 200;

    [Tooltip("The impulse downward added when jump is released early")]
    public float JumpEndEarlyImpulse = 100f;

    [Tooltip("The time before coyote jump becomes unusable. Coyote jump allows jump to execute even after leaving a ledge")]
    public float CoyoteTime = .15f;

    [Tooltip("The amount of time we buffer a jump. This allows jump input before actually hitting the ground")]
    public float JumpBuffer = .2f;

    [Tooltip("Multiplier for horizontal velocity when jumping")]
    public float JumpHorizontalVelocityMultiplier = 0.2f;

    [Tooltip("The maximum vertical movement speed")]
    public float MaxFallSpeed = 40;

    [Header("Gravity Scale")]

    [Tooltip("Gravity scale multiplier when grounded. Helps on slopes")]
    public float GroundingGravityScaleModifier = 3f;

    [Tooltip("The gravity multiplier added when floating upwards")]
    public float FloatUpGravityScaleModifier = 4f;

    [Tooltip("The gravity multiplier added when falling")]
    public float FallingGravityScaleModifier = 5f;

    [Tooltip("The gravity multiplier added when swinging")]
    public float SwingingGravityScaleModifier = 2f;

    [Header("Swinging")]

    [Tooltip("The speed at which the player swings")]
    public float SwingForce = 10f;

    [Tooltip("The maximum velocity the player can reach while swinging")]
    public float MaxSwingVelocity = 150f;

    [Tooltip("The constant downward force applied while swinging")]
    public float SwingBaseDownForce = 10f;

    public float SwingVelocityDownForceMultiplier = 1.2f;

    public float SwingEndImpulseMultiplier = 5f;

    public float SwingMomentumBonus = 1.2f;

    [Header("Rotation")]
    [Tooltip("Lerp amount for standing based on normal")]
    public float AlignRotationLerpAmount = 40f;


    [Tooltip("The minimum dot product value for the ground normal to be considered valid")]
    public float GroundNormalDotThreshold = 0.8f;

    [Header("Anchoring")]
    [Tooltip("Cooldown time after stopping anchoring before being able to anchor again")]
    public float AnchorCooldownTime = 0.2f;
}

