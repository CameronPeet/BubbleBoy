using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum LocomotionType
{
    FreeWithStrafe,
    OnlyStrafe,
    OnlyFree
}

[CreateAssetMenu(fileName = "CharacterInfo", menuName = "Character/CharacterInfo", order = 1)]
public class CharacterInfo : ScriptableObject
{


    [Header("--- Locomotion Setup ---")]

    public LocomotionType locomotionType = LocomotionType.FreeWithStrafe;
    //[Tooltip("The character Head will follow where you look at, UNCHECK if you are using TopDown or 2.5D")]
    //[SerializeField]
    //public bool headTracking = true;

    [Tooltip("Use this to rotate the character using the World axis, or false to use the camera axis - CHECK for Isometric Camera")]
    [SerializeField]
    public bool rotateByWorld = false;

    [Tooltip("Speed of the rotation on free directional movement")]
    [SerializeField]
    public float rotationSpeed = 8f;

    [Tooltip("Add extra speed for the locomotion movement, keep this value at 0 if you want to use only root motion speed.")]
    [SerializeField]
    public float extraMoveSpeed = 0f;

    [Tooltip("Add extra speed for the strafe movement, keep this value at 0 if you want to use only root motion speed.")]
    [SerializeField]
    public float extraStrafeSpeed = 0f;

    [Header("--- Grounded Setup ---")]
    [Tooltip("Distance to became not grounded")]
    [SerializeField]
    public float groundCheckDistance = 0.5f;
    public float groundDistance;
    public RaycastHit groundHit;

    [Tooltip("ADJUST IN PLAY MODE - Offset height limit for sters - GREY Raycast in front of the legs")]
    public float stepOffsetEnd = 0.45f;
    [Tooltip("ADJUST IN PLAY MODE - Offset height origin for sters, make sure to keep slight above the floor - GREY Raycast in front of the legs")]
    public float stepOffsetStart = 0.05f;
    [Tooltip("Higher value will result jittering on ramps, lower values will have difficulty on steps")]
    public float stepSmooth = 2f;

    [Tooltip("Max angle to walk")]
    [SerializeField]
    public float slopeLimit = 45f;

    [Tooltip("Apply extra gravity when the character is not grounded")]
    [SerializeField]
    public float extraGravity = 4f;

    [Tooltip("Select a VerticalVelocity to turn on Land High animation")]
    [SerializeField]
    public float landHighVel = -5f;

    [Tooltip("Turn the Ragdoll On when falling at high speed (check VerticalVelocity) - leave the value with 0 if you don't want this feature")]
    [SerializeField]
    public float ragdollVel = -8f;
    public float moveSet_ID;

}
