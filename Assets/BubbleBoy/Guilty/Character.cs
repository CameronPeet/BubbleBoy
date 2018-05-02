using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiltyCharacter
{
    public abstract class Character : CharacterBase
    {

        #region Actions
        //Boolean values that define movement within these scripts  
        protected bool
            onGround, stopMove, autoCrouch,
            quickStop, quickTurn180, canSprint,
            crouch, strafing, landHigh,
            jump, isJumping, sliding;

        // actions bools, used to turn on/off actions animations *check ThirdPersonAnimator*	        
        protected bool
            jumpOver,
            stepUp,
            climbUp,
            pushButton,
            roll,
            enterLadderBottom,
            enterLadderTop,
            usingLadder,
            exitLadderBottom,
            exitLadderTop,
            inAttack,
            blocking,
            hitReaction,
            hitRecoil;

        // return true if any animation actions value is true
        protected bool actions
        {
            get
            {
                return jumpOver || stepUp || climbUp || roll || usingLadder || quickStop || quickTurn180 || hitReaction || hitRecoil || pushButton;
            }
        }

        /// <summary>
        ///  DISABLE ACTIONS - call this method in case you need to turn every action bool false
        /// </summary>
        protected void DisableActions()
        {
            inAttack = false;
            blocking = false;
            quickTurn180 = false;
            hitReaction = false;
            hitRecoil = false;
            quickStop = false;
            canSprint = false;
            strafing = false;
            landHigh = false;
            jumpOver = false;
            roll = false;
            stepUp = false;
            crouch = false;
            jump = false;
            isJumping = false;
            pushButton = false;
        }
        #endregion

        #region Character Variables
        public enum LocomotionType
        {
            FreeWithStrafe,
            OnlyStrafe,
            OnlyFree
        }

        [Header("--- Locomotion Setup ---")]

        public LocomotionType locomotionType = LocomotionType.FreeWithStrafe;
        //[Tooltip("The character Head will follow where you look at, UNCHECK if you are using TopDown or 2.5D")]
        //[SerializeField]
        //protected bool headTracking = true;

        [Tooltip("Use this to rotate the character using the World axis, or false to use the camera axis - CHECK for Isometric Camera")]
        [SerializeField]
        protected bool rotateByWorld = false;

        [Tooltip("Speed of the rotation on free directional movement")]
        [SerializeField]
        protected float rotationSpeed = 8f;

        [Tooltip("Add extra speed for the locomotion movement, keep this value at 0 if you want to use only root motion speed.")]
        [SerializeField]
        protected float extraMoveSpeed = 0f;

        [Tooltip("Add extra speed for the strafe movement, keep this value at 0 if you want to use only root motion speed.")]
        [SerializeField]
        protected float extraStrafeSpeed = 0f;

        [Header("--- Grounded Setup ---")]
        [Tooltip("Distance to became not grounded")]
        [SerializeField]
        protected float groundCheckDistance = 0.5f;
        protected float groundDistance;
        public RaycastHit groundHit;

        [Tooltip("ADJUST IN PLAY MODE - Offset height limit for sters - GREY Raycast in front of the legs")]
        public float stepOffsetEnd = 0.45f;
        [Tooltip("ADJUST IN PLAY MODE - Offset height origin for sters, make sure to keep slight above the floor - GREY Raycast in front of the legs")]
        public float stepOffsetStart = 0.05f;
        [Tooltip("Higher value will result jittering on ramps, lower values will have difficulty on steps")]
        public float stepSmooth = 2f;

        [Tooltip("Max angle to walk")]
        [SerializeField]
        protected float slopeLimit = 45f;

        [Tooltip("Apply extra gravity when the character is not grounded")]
        [SerializeField]
        protected float extraGravity = 4f;

        [Tooltip("Select a VerticalVelocity to turn on Land High animation")]
        [SerializeField]
        protected float landHighVel = -5f;

        [Tooltip("Turn the Ragdoll On when falling at high speed (check VerticalVelocity) - leave the value with 0 if you don't want this feature")]
        [SerializeField]
        protected float ragdollVel = -8f;



        protected float moveSet_ID;
        protected GameObject currentCollectable;
        #endregion


        #region Camera Variables
        // generic string to change the CameraState
        [HideInInspector] public string customCameraState;
        // generic string to change the CameraPoint of the Fixed Point Mode
        [HideInInspector] public string customlookAtPoint;
        // generic bool to change the CameraState
        [HideInInspector] public bool changeCameraState;
        // generic bool to know if the state will change with or without lerp
        [HideInInspector] public bool smoothCameraState;
        // generic variables to find the correct direction 
        [HideInInspector] public Quaternion freeRotation;
        [HideInInspector] public bool keepDirection;
        [HideInInspector] public Vector2 oldInput;
        [HideInInspector] public Vector3 cameraForward;
        [HideInInspector] public Vector3 cameraRight;
        #endregion

        public void UpdateMotor()
        {
            CheckGround();
            CheckRagdoll();
            ControlHeight();
            ControlLocomotion();
        }

        void ControlLocomotion()
        {
            //Return if player movement is locked
            if (lockPlayer) return;


            // free directional movement
            if (freeLocomotionConditions)
            {
                // set speed to both vertical and horizontal inputs
                speed = Mathf.Abs(input.x) + Mathf.Abs(input.y);
                speed = Mathf.Clamp(speed, 0, 1);

                // add 0.5f on sprint to change the animation on animator
                if (canSprint) speed += 0.5f;

                //Cull speed if stopped
                if (stopMove) speed = 0f;

                // Handle no input 
                if (input == Vector2.zero && !quickTurn180)
                {
                    direction = Mathf.Lerp(direction, 0f, 20f * Time.fixedDeltaTime);
                }

                //If no current action even taking place, allow free rotational movement
                if ((!actions || quickTurn180 || quickStop) && !inAttack)
                {
                    FreeRotationMovement();
                }
            }

            // handle strafe movement
            else
            {
                speed = input.y;
                direction = input.x;
            }
        }

        /// <summary>
        /// Conditions for Free Directional Movement
        /// </summary>
        public bool freeLocomotionConditions
        {
            get
            {
                if (locomotionType.Equals(LocomotionType.OnlyStrafe)) strafing = true;
                return !strafing && !usingLadder && !landHigh && !locomotionType.Equals(LocomotionType.OnlyStrafe) || locomotionType.Equals(LocomotionType.OnlyFree);
            }
        }


        /// <summary>
        /// ACTIVATE RAGDOLL - check your verticalVelocity and assign a value on the variable RagdollVel
        /// </summary>
        void CheckRagdoll()
        {
            if (ragdollVel == 0) return;

            if (verticalVelocity <= ragdollVel && groundDistance <= 0.1f)
                transform.SendMessage("ActivateRagdoll", SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// CAPSULE COLLIDER HEIGHT CONTROL - controls height, position and radius of CapsuleCollider
        /// </summary>
        void ControlHeight()
        {
            if (crouch || roll)
            {
                _capsuleCollider.center = colliderCenter / 1.4f;
                _capsuleCollider.height = colliderHeight / 1.4f;
            }
            else if (usingLadder)
            {
                _capsuleCollider.radius = colliderRadius / 1.25f;
            }
            else
            {
                // back to the original values
                _capsuleCollider.center = colliderCenter;
                _capsuleCollider.radius = colliderRadius;
                _capsuleCollider.height = colliderHeight;
            }
        }

        /// <summary>
        /// GROUND CHECKER - check if the character is grounded or not	
        /// </summary>	
        void CheckGround()
        {
            CheckGroundDistance();

            // change the physics material to very slip when not grounded
            _capsuleCollider.material = (onGround && GroundAngle() < slopeLimit) ? frictionPhysics : slippyPhysics;
            // we don't want to stick the character grounded if one of these bools is true
            bool groundStickConditions = !jumpOver && !stepUp && !climbUp && !usingLadder && !hitReaction && !pushButton;

            if (groundStickConditions)
            {
                var onStep = StepOffset();

                if (groundDistance <= 0.05f)
                {
                    onGround = true;
                    // keeps the character grounded and prevents bounceness on ramps
                    //if (!onStep) _rigidbody.velocity = Vector3.ProjectOnPlane(_rigidbody.velocity, groundHit.normal);
                    Sliding();
                }
                else
                {
                    if (groundDistance >= groundCheckDistance)
                    {
                        onGround = false;
                        // check vertical velocity
                        verticalVelocity = _rigidbody.velocity.y;
                        // apply extra gravity when falling
                        if (!onStep && !roll)
                            transform.position -= Vector3.up * (extraGravity * Time.deltaTime);
                    }
                    else if (!onStep && !roll && !jump)
                        transform.position -= Vector3.up * (extraGravity * Time.deltaTime);
                }
            }
        }

        void Sliding()
        {
            var onStep = StepOffset();
            var groundAngleTwo = 0f;
            RaycastHit hitinfo;
            Ray ray = new Ray(transform.position, -transform.up);

            if (Physics.Raycast(ray, out hitinfo, Mathf.Infinity, groundLayer))
                groundAngleTwo = Vector3.Angle(Vector3.up, hitinfo.normal);

            if (GroundAngle() > slopeLimit + 1f && GroundAngle() <= 85 &&
                groundAngleTwo > slopeLimit + 1f && groundAngleTwo <= 85 &&
                groundDistance <= 0.05f && !onStep)
            {
                sliding = true;
                onGround = false;
                var slideVelocity = (GroundAngle() - slopeLimit) * 5f;
                slideVelocity = Mathf.Clamp(slideVelocity, 0, 10);
                _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, -slideVelocity, _rigidbody.velocity.z);
            }
            else
            {
                sliding = false;
                onGround = true;
            }
        }

        /// <summary>
        /// GROUND DISTANCE	- get the distance between the middle of the character to the ground
        /// </summary>
        void CheckGroundDistance()
        {
            if (_capsuleCollider != null)
            {
                // radius of the SphereCast
                float radius = _capsuleCollider.radius * 0.9f;
                var dist = Mathf.Infinity;
                // position of the SphereCast origin starting at the base of the capsule
                Vector3 pos = transform.position + Vector3.up * (_capsuleCollider.radius);
                
                // raycast for check the ground distance
                if (Physics.Raycast(transform.position + new Vector3(0, colliderHeight / 2, 0), Vector3.down, out groundHit, Mathf.Infinity, groundLayer))
                {
                    dist = transform.position.y - groundHit.point.y;
                }

                // ray for SphereCast
                Ray ray2 = new Ray(pos, -Vector3.up);
                // sphere cast around the base of the capsule to check the ground distance
                if (Physics.SphereCast(ray2, radius, out groundHit, Mathf.Infinity, groundLayer))
                {
                    // check if sphereCast distance is small than the ray cast distance
                    if (dist > (groundHit.distance - _capsuleCollider.radius * 0.1f))
                        dist = (groundHit.distance - _capsuleCollider.radius * 0.1f);
                }

                groundDistance = dist;
            }
        }

        /// <summary>
        /// STEP OFFSET LIMIT - check the height of the object ahead, control by stepOffSet
        /// </summary>
        /// <returns></returns>
        bool StepOffset()
        {
            if (input.sqrMagnitude < 0.1 || !onGround) return false;

            var hit = new RaycastHit();
            Ray rayStep = new Ray((transform.position + new Vector3(0, stepOffsetEnd, 0) + transform.forward * ((_capsuleCollider).radius + 0.05f)), Vector3.down);

            if (Physics.Raycast(rayStep, out hit, stepOffsetEnd - stepOffsetStart, groundLayer))
            {
                if (!stopMove && hit.point.y >= (transform.position.y) && hit.point.y <= (transform.position.y + stepOffsetEnd))
                {
                    var heightPoint = new Vector3(transform.position.x, hit.point.y + 0.1f, transform.position.z);
                    transform.position = Vector3.Lerp(transform.position, heightPoint, (speed * stepSmooth) * Time.fixedDeltaTime);
                    //var heightPoint = new Vector3(_rigidbody.velocity.x, hit.point.y + 0.1f, _rigidbody.velocity.z);
                    //_rigidbody.velocity = heightPoint * 10f * Time.fixedDeltaTime;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// CHECK GROUND ANGLE 
        /// </summary>
        /// <returns></returns>
        float GroundAngle()
        {
            var groundAngle = Vector3.Angle(groundHit.normal, Vector3.up);
            return groundAngle;
        }

        /// <summary>
        /// STOP MOVE - stop the character if hits a wall and apply slope limit to ramps
        /// </summary>
        public void StopMove()
        {
            if (input.sqrMagnitude < 0.1 || !onGround) return;

            RaycastHit hitinfo;
            Ray ray = new Ray(transform.position + new Vector3(0, colliderHeight / 3, 0), transform.forward);

            if (Physics.Raycast(ray, out hitinfo, _capsuleCollider.radius + stopMoveDistance, stopMoveLayer) && !usingLadder)
            {
                var hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);

                if (hitinfo.distance <= stopMoveDistance && hitAngle > 85)
                    stopMove = true;
                else if (hitAngle >= slopeLimit + 1f && hitAngle <= 85)
                    stopMove = true;
            }
            else if (Physics.Raycast(ray, out hitinfo, 1f, groundLayer) && !usingLadder)
            {
                var hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);
                if (hitAngle >= slopeLimit + 1f && hitAngle <= 85)
                    stopMove = true;
            }
            else
                stopMove = false;
        }


        /// <summary>
        /// ACTIONS - raycast to check if there is anything interactable ahead
        /// </summary>
        public GameObject CheckActionObject()
        {
            bool checkConditions = onGround && !landHigh && !actions && !inAttack;
            GameObject _object = null;

            if (checkConditions)
            {
                RaycastHit hitInfoAction;
                Vector3 yOffSet = new Vector3(0f, -0.5f, 0f);
                Vector3 fwd = transform.TransformDirection(Vector3.forward);

                if (Physics.Raycast(transform.position - yOffSet, fwd, out hitInfoAction, distanceOfRayActionTrigger))
                {
                    _object = hitInfoAction.transform.gameObject;
                }
            }
            return currentCollectable != null ? currentCollectable : _object;
        }

        public float distanceOfRayActionTrigger
        {
            get
            {
                if (_capsuleCollider == null) return 0f;
                var dist = _capsuleCollider.radius + 0.1f;
                return dist;
            }
        }

        /// <summary>
        /// FREE ROTATION - handle the character rotation when on a free directional movement, also activate the turn180 animations.
        /// </summary>
        void FreeRotationMovement()
        {
            if (input != Vector2.zero && !quickTurn180 && !lockPlayer && targetDirection.magnitude > 0.1f)
            {
                freeRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
                Vector3 velocity = Quaternion.Inverse(transform.rotation) * targetDirection.normalized;
                direction = Mathf.Atan2(velocity.x, velocity.z) * 180.0f / 3.14159f;

                // activate quickTurn180 based on the directional angle
                var quickTurn180Conditions = !crouch && direction >= 165 && !jump && onGround
                                           || !crouch && direction <= -165 && !jump && onGround;
                if (quickTurn180Conditions)
                    quickTurn180 = true;

                // apply free directional rotation while not turning180 animations
                if ((!quickTurn180 && !isJumping) || (isJumping && actionsController.Jump.jumpAirControl))
                {
                    Vector3 lookDirection = targetDirection.normalized;
                    freeRotation = Quaternion.LookRotation(lookDirection, transform.up);
                    var euler = new Vector3(0, freeRotation.eulerAngles.y, 0);
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(euler), rotationSpeed * Time.fixedDeltaTime);
                }
                if (!keepDirection)
                    oldInput = input;
                if (Vector2.Distance(oldInput, input) > 0.9f && keepDirection)
                    keepDirection = false;
            }
        }

        /// <summary>
        /// Find the correct direction based on the camera direction
        /// </summary>
        public Vector3 targetDirection
        {
            get
            {
                Vector3 refDir = Vector3.zero;
                cameraForward = keepDirection ? cameraForward : cameraTransform.TransformDirection(Vector3.forward);
                cameraForward.y = 0;

                //if (tpCamera == null || !tpCamera.currentState.cameraMode.Equals(TPCameraMode.FixedAngle) || !rotateByWorld)
                if(!rotateByWorld)
                {
                    //cameraForward = tpCamera.transform.TransformDirection(Vector3.forward);
                    cameraForward = keepDirection ? cameraForward : cameraTransform.TransformDirection(Vector3.forward);
                    cameraForward.y = 0; //set to 0 because of camera rotation on the X axis

                    //get the right-facing direction of the camera
                    cameraRight = keepDirection ? cameraRight : cameraTransform.TransformDirection(Vector3.right);

                    // determine the direction the player will face based on input and the camera's right and forward directions
                    refDir = input.x * cameraRight + input.y * cameraForward;
                }
                else
                {
                    refDir = new Vector3(input.x, 0, input.y);
                }
                return refDir;
            }
        }
    }
}


