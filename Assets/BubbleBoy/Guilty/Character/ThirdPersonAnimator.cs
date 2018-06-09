using UnityEngine;
using System.Collections;

namespace GuiltyCharacter
{
    public abstract class ThirdPersonAnimator : Character
    {
        #region Variables
        // match cursorObject to help animation to reach their cursorObject
        [HideInInspector] public Transform matchTarget;
        // access the animator states (layers)
        [HideInInspector] public AnimatorStateInfo stateInfo, upperBodyInfo;
        [HideInInspector] public float oldSpeed;
        public float speedTime
        {
            get
            {
                var _speed = animator.GetFloat("Speed");
                var acceleration = (_speed - oldSpeed) / Time.fixedDeltaTime;
                oldSpeed = _speed;
                return Mathf.Round(acceleration);
            }
        }

        #endregion

        /// <summary>
        /// ANIMATOR - update animations at the animator controller (Mecanim)
        /// </summary>
        public void UpdateAnimator()
        {
            if (ragdolled)
                DisableActions();

            if (animator == null || !animator.enabled) return;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            QuickTurn180Animation();
            LandHighAnimation();
            ClimbUpAnimation();
            PushButtonAnimation();
            JumpAnimation();
            QuickStopAnimation();
            ExtraMoveSpeed();
            LocomotionAnimation();
        }

        /// <summary>
        /// CONTROL LOCOMOTION
        /// </summary>
        void LocomotionAnimation()
        {
            animator.SetBool("Strafing", strafing);
            animator.SetBool("Crouch", crouch);
            animator.SetBool("OnGround", onGround);
            animator.SetFloat("GroundDistance", ci.groundDistance);
            animator.SetFloat("VerticalVelocity", verticalVelocity);
            animator.SetFloat("MoveSet_ID", ci.moveSet_ID, 0.1f, Time.fixedDeltaTime);

            if (freeLocomotionConditions)
                // free directional movement get the directional angle
                animator.SetFloat("Direction", lockPlayer ? 0f : direction, 0.1f, Time.fixedDeltaTime);
            else
                // strafe movement get the input 1 or -1
                animator.SetFloat("Direction", lockPlayer ? 0f : direction, 0.15f, Time.fixedDeltaTime);

            animator.SetFloat("Speed", !stopMove || lockPlayer ? speed : 0f, 0.2f, Time.fixedDeltaTime);
        }

        /// <summary>
        /// EXTRA MOVE SPEED - apply extra speed for the the free directional movement or the strafe movement
        /// </summary>
        void ExtraMoveSpeed()
        {
            if (!inAttack)
            {
                if (stateInfo.IsName("Grounded.Strafing Movement") || stateInfo.IsName("Grounded.Strafing Crouch"))
                {
                    var newSpeed_Y = (ci.extraStrafeSpeed * speed);
                    var newSpeed_X = (ci.extraStrafeSpeed * direction);
                    newSpeed_Y = Mathf.Clamp(newSpeed_Y, -ci.extraStrafeSpeed, ci.extraStrafeSpeed);
                    newSpeed_X = Mathf.Clamp(newSpeed_X, -ci.extraStrafeSpeed, ci.extraStrafeSpeed);
                    transform.position += transform.forward * (newSpeed_Y * Time.fixedDeltaTime);
                    transform.position += transform.right * (newSpeed_X * Time.fixedDeltaTime);
                }
                else if (stateInfo.IsName("Grounded.Free Movement") || stateInfo.IsName("Grounded.Free Crouch"))
                {
                    var newSpeed = (ci.extraMoveSpeed * speed);
                    transform.position += transform.forward * (newSpeed * Time.fixedDeltaTime);
                }
            }
            else
            {
                speed = 0f;
            }
        }

        /// <summary>
        /// QUICK TURN 180 ANIMATION
        /// </summary>
        void QuickTurn180Animation()
        {
            animator.SetBool("QuickTurn180", quickTurn180);

            // complete the 180 with matchTarget and disable quickTurn180 after completed
            if (stateInfo.IsName("Action.QuickTurn180"))
            {
                if (!animator.IsInTransition(0) && !ragdolled)
                    animator.MatchTarget(Vector3.one, cameraState.freeRotation, AvatarTarget.Root,
                                 new MatchTargetWeightMask(Vector3.zero, 1f),
                                 animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 0.9f);

                if (stateInfo.normalizedTime >= 0.9f)
                    quickTurn180 = false;
            }
        }

        /// <summary>
        /// QUICK STOP ANIMATION
        /// </summary>
        void QuickStopAnimation()
        {
            animator.SetBool("QuickStop", quickStop);

            bool quickStopConditions = !actions && onGround && !inAttack;

            // make a quickStop when release the key while running
            if (speedTime <= -3f && quickStopConditions)
                quickStop = true;

            // disable quickStop
            if (quickStop && input.sqrMagnitude >= 0.1f || quickTurn180 || inAttack)
                quickStop = false;
            else if (stateInfo.IsName("Action.QuickStop"))
            {
                if (stateInfo.normalizedTime > 0.9f || input.sqrMagnitude >= 0.1f || stopMove || inAttack)
                    quickStop = false;
            }
        }


        /// <summary>
        /// JUMP ANIMATION AND BEHAVIOUR
        /// </summary>
        void JumpAnimation()
        {
            animator.SetBool("Jump", jump);

            var jumpAirControl = actionsController.Jump.jumpAirControl;
            var jumpForce = actionsController.Jump.jumpForce;
            var jumpForward = actionsController.Jump.jumpForward;
            var newSpeed = (jumpForward * speed);

            isJumping = stateInfo.IsName("Action.Jump") || stateInfo.IsName("Action.JumpMove") || stateInfo.IsName("Airborne.FallingFromJump");
            animator.SetBool("IsJumping", isJumping);

            if (stateInfo.IsName("Action.Jump"))
            {
                // apply extra height to the jump
                if (stateInfo.normalizedTime < 0.85f)
                {
                    _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, jumpForce, _rigidbody.velocity.z);
                    transform.position += transform.up * (jumpForce * Time.fixedDeltaTime);
                }
                // apply extra speed forward
                if (stateInfo.normalizedTime >= 0.65f && jumpAirControl)
                    transform.position += transform.forward * (newSpeed * Time.fixedDeltaTime);
                else if (stateInfo.normalizedTime >= 0.65f && !jumpAirControl)
                    transform.position += transform.forward * Time.fixedDeltaTime;
                // end jump animation
                if (stateInfo.normalizedTime >= 0.6f || hitReaction)
                    jump = false;
            }

            if (stateInfo.IsName("Action.JumpMove"))
            {
                // apply extra height to the jump
                if (stateInfo.normalizedTime < 0.85f)
                {
                    _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, jumpForce, _rigidbody.velocity.z);
                    transform.position += transform.up * (jumpForce * Time.fixedDeltaTime);
                }
                // apply extra speed forward
                if (jumpAirControl)
                    //_rigidbody.velocity = transform.forward * (newSpeed * Time.fixedDeltaTime);
                    transform.position += transform.forward * (newSpeed * Time.fixedDeltaTime);
                else
                    transform.position += transform.forward * Time.fixedDeltaTime;

                // end jump animation
                if (stateInfo.normalizedTime >= 0.55f || hitReaction)
                    jump = false;
            }

            // apply extra speed forward when falling
            if (stateInfo.IsName("Airborne.FallingFromJump") && jumpAirControl)
                transform.position += transform.forward * (newSpeed * Time.fixedDeltaTime);
            else if (stateInfo.IsName("Airborne.FallingFromJump") && !jumpAirControl)
                transform.position += transform.forward * Time.fixedDeltaTime;
        }

        /// <summary>
        /// HARD LANDING ANIMATION
        /// </summary>
        void LandHighAnimation()
        {
            animator.SetBool("LandHigh", landHigh);

            // if the character fall from a great height, landhigh animation
            if (!onGround && verticalVelocity <= ci.landHighVel && ci.groundDistance <= 0.5f)
                landHigh = true;

            if (landHigh && stateInfo.IsName("Airborne.LandHigh"))
            {
                quickStop = false;
                if (stateInfo.normalizedTime >= 0.1f && stateInfo.normalizedTime <= 0.2f)
                {
                }

                if (stateInfo.normalizedTime > 0.9f || hitReaction)
                {
                    landHigh = false;
                }
            }
        }

        /// <summary>
        /// CLIMB ANIMATION
        /// </summary>
        void PushButtonAnimation()
        {
            animator.SetBool("PushButton", pushButton);

            if (stateInfo.IsName("Action.pushButton"))
            {
                if (stateInfo.normalizedTime > 0.1f && stateInfo.normalizedTime < 0.3f)
                {
                    //_rigidbody.useGravity = false;
                    //_capsuleCollider.isTrigger = true;
                }

                // we are using matchtarget to find the correct height of the object
                if (!animator.IsInTransition(0))
                {
                    animator.MatchTarget(matchTarget.position, matchTarget.rotation, AvatarTarget.RightHand,
                        new MatchTargetWeightMask(new Vector3(1, 0, 1), 0), 0f, 0.2f);
                }

                if (crouch)
                {
                    if (stateInfo.normalizedTime >= 0.7f || hitReaction)
                    {
                        _capsuleCollider.isTrigger = false;
                        _rigidbody.useGravity = true;
                        pushButton = false;
                    }
                }
                else
                {
                    if (stateInfo.normalizedTime >= 0.9f || hitReaction)
                    {
                        _capsuleCollider.isTrigger = false;
                        _rigidbody.useGravity = true;
                        pushButton = false;


                    }
                }
            }
        }

        /// <summary>
        /// CLIMB ANIMATION
        /// </summary>
        void ClimbUpAnimation()
        {
            animator.SetBool("ClimbUp", climbUp);

            if (stateInfo.IsName("Action.ClimbUp"))
            {
                if (stateInfo.normalizedTime > 0.1f && stateInfo.normalizedTime < 0.3f)
                {
                    _rigidbody.useGravity = false;
                    _capsuleCollider.isTrigger = true;
                }

                // we are using matchtarget to find the correct height of the object
                if (!animator.IsInTransition(0))
                {
                    //animator.MatchTarget(matchTarget.position, matchTarget.rotation, AvatarTarget.LeftHand,
                    //    new MatchTargetWeightMask(new Vector3(0, 1, 1), 0), 0f, 0.2f);

                    animator.MatchTarget(matchTarget.position, matchTarget.rotation, AvatarTarget.LeftHand,
                        new MatchTargetWeightMask(new Vector3(0, 1, 1), 0), 0f, 0.2f);
                }
                    

                if (crouch)
                {
                    if (stateInfo.normalizedTime >= 0.7f || hitReaction)
                    {
                        _capsuleCollider.isTrigger = false;
                        _rigidbody.useGravity = true;
                        climbUp = false;
                    }
                }
                else
                {
                    if (stateInfo.normalizedTime >= 0.9f || hitReaction)
                    {
                        _capsuleCollider.isTrigger = false;
                        _rigidbody.useGravity = true;
                        climbUp = false;
                    }
                }
            }
        }

        public static Vector3 NormalizeAngle(Vector3 eulerAngle)
        {
            var delta = eulerAngle;

            if (delta.x > 180) delta.x -= 360;
            else if (delta.x < -180) delta.x += 360;

            if (delta.y > 180) delta.y -= 360;
            else if (delta.y < -180) delta.y += 360;

            if (delta.z > 180) delta.z -= 360;
            else if (delta.z < -180) delta.z += 360;

            return new Vector3(delta.x, delta.y, delta.z);//round values to angle;
        }

        /// <summary>
        /// Get look at point based on bounds center of lockTarget
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        Vector3 lookPoint(float distance)
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            Vector3 point = ray.GetPoint(distance);
            if(tpCamera != null && tpCamera.lockTarget != null)
            {
                Transform target = tpCamera.lockTarget;
                float _heightOffset = 0.2f;

                var bounds = target.GetComponent<Collider>().bounds;
                var middle = bounds.center;
                var height = Vector3.Distance(bounds.min, bounds.max);

                point = middle + new Vector3(0, height * _heightOffset, 0);
            }
            return point;
        }


        void OnAnimatorIK()
        {
            float rhWeight = animator.GetFloat("RightHand");

            if (rhWeight > 0.0f)
            {
                animator.SetIKPosition(AvatarIKGoal.RightHand, matchTarget.position);
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rhWeight);
            }

            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.0f);

            }
        }
    }


}


///// <summary>
///// STEP UP ANIMATION
///// </summary>
//void StepUpAnimation()
//{
//    animator.SetBool("StepUp", stepUp);

//    if (stateInfo.IsName("Action.StepUp"))
//    {
//        if (stateInfo.normalizedTime > 0.1f && stateInfo.normalizedTime < 0.3f)
//        {
//            _capsuleCollider.isTrigger = true;
//            _rigidbody.useGravity = false;
//        }

//        // we are using matchtarget to find the correct height of the object                
//        if (!animator.IsInTransition(0))
//            animator.MatchTarget(matchTarget.position, matchTarget.rotation,
//                        AvatarTarget.LeftHand, new MatchTargetWeightMask
//                        (new Vector3(0, 1, 1), 0), 0f, 0.5f);

//        if (stateInfo.normalizedTime > 0.9f || hitReaction)
//        {
//            _capsuleCollider.isTrigger = false;
//            _rigidbody.useGravity = true;
//            stepUp = false;
//        }
//    }
//}

///// <summary>
///// JUMP OVER ANIMATION
///// </summary>
//void JumpOverAnimation()
//{
//    animator.SetBool("JumpOver", jumpOver);

//    if (stateInfo.IsName("Action.JumpOver"))
//    {
//        quickTurn180 = false;
//        if (stateInfo.normalizedTime > 0.1f && stateInfo.normalizedTime < 0.3f)
//        {
//            _rigidbody.useGravity = false;
//            _capsuleCollider.isTrigger = true;
//        }

//        // we are using matchtarget to find the correct height of the object
//        if (!animator.IsInTransition(0))
//            animator.MatchTarget(matchTarget.position, matchTarget.rotation,
//                        AvatarTarget.LeftHand, new MatchTargetWeightMask
//                        (new Vector3(0, 1, 1), 0), 0.1f * (1 - stateInfo.normalizedTime), 0.3f * (1 - stateInfo.normalizedTime));

//        if (stateInfo.normalizedTime >= 0.7f || hitReaction)
//        {
//            _rigidbody.useGravity = true;
//            _capsuleCollider.isTrigger = false;
//            jumpOver = false;
//        }
//    }
//}




///// <summary>
///// ROLL ANIMATION
///// </summary>
//void RollAnimation()
//{
//    animator.SetBool("Roll", roll);

//    // rollFwd
//    if (stateInfo.IsName("Action.Roll"))
//    {
//        lockPlayer = true;
//        _rigidbody.useGravity = false;

//        // prevent the character to rolling up 
//        if (verticalVelocity >= 1)
//            _rigidbody.velocity = Vector3.ProjectOnPlane(_rigidbody.velocity, groundHit.normal);

//        // reset the rigidbody a little ealier to the character fall while on air
//        if (stateInfo.normalizedTime > 0.3f)
//            _rigidbody.useGravity = true;

//        // transition back if the character is not crouching
//        if (!crouch && stateInfo.normalizedTime > 0.85f)
//        {
//            lockPlayer = false;
//            roll = false;
//        }
//        // transition back if the character is crouching
//        else if (crouch && stateInfo.normalizedTime > 0.75f)
//        {
//            lockPlayer = false;
//            roll = false;
//        }
//    }
//}






///// <summary>
///// LADDER ANIMATION
///// </summary>
//void LadderAnimation()
//{
//    // resume the states of the ladder in one bool 
//    usingLadder =
//        stateInfo.IsName("Ladder.EnterLadderBottom") ||
//        stateInfo.IsName("Ladder.ExitLadderBottom") ||
//        stateInfo.IsName("Ladder.ExitLadderTop") ||
//        stateInfo.IsName("Ladder.EnterLadderTop") ||
//        stateInfo.IsName("Ladder.ClimbLadder");

//    // just to prevent any wierd blend between this animations
//    if (usingLadder)
//    {
//        jump = false;
//        quickTurn180 = false;
//    }

//    // make sure to lock the player when entering or exiting a ladder
//    var lockOnLadder =
//        stateInfo.IsName("Ladder.EnterLadderBottom") ||
//        stateInfo.IsName("Ladder.ExitLadderBottom") ||
//        stateInfo.IsName("Ladder.ExitLadderTop") ||
//        stateInfo.IsName("Ladder.EnterLadderTop");

//    lockPlayer = lockOnLadder;

//    LadderBottom();
//    LadderTop();
//}

//void LadderBottom()
//{
//    animator.SetBool("EnterLadderBottom", enterLadderBottom);
//    animator.SetBool("ExitLadderBottom", exitLadderBottom);

//    // enter ladder from bottom
//    if (stateInfo.IsName("Ladder.EnterLadderBottom"))
//    {
//        _capsuleCollider.isTrigger = true;
//        _rigidbody.useGravity = false;

//        // we are using matchtarget to find the correct X & Z to start climb the ladder
//        // this information is provided by the cursorObject on the object, that use the script TriggerAction 
//        // in this state we are sync the position based on the AvatarTarget.Root, but you can use leftHand, left Foot, etc.
//        if (!animator.IsInTransition(0))
//            animator.MatchTarget(matchTarget.position, matchTarget.rotation,
//                       AvatarTarget.Root, new MatchTargetWeightMask
//                        (new Vector3(1, 1, 1), 1), 0.25f, 0.9f);

//        if (stateInfo.normalizedTime >= 0.75f || hitReaction)
//            enterLadderBottom = false;
//    }

//    // exit ladder bottom
//    if (stateInfo.IsName("Ladder.ExitLadderBottom") || hitReaction)
//    {
//        _capsuleCollider.isTrigger = false;
//        _rigidbody.useGravity = true;

//        if (stateInfo.normalizedTime >= 0.4f || hitReaction)
//        {
//            exitLadderBottom = false;
//            usingLadder = false;
//        }
//    }
//}

//void LadderTop()
//{
//    animator.SetBool("EnterLadderTop", enterLadderTop);
//    animator.SetBool("ExitLadderTop", exitLadderTop);

//    // enter ladder from top            
//    if (stateInfo.IsName("Ladder.EnterLadderTop"))
//    {
//        _capsuleCollider.isTrigger = true;
//        _rigidbody.useGravity = false;

//        // we are using matchtarget to find the correct X & Z to start climb the ladder
//        // this information is provided by the cursorObject on the object, that use the script TriggerAction 
//        // in this state we are sync the position based on the AvatarTarget.Root, but you can use leftHand, left Foot, etc.
//        if (stateInfo.normalizedTime < 0.25f && !animator.IsInTransition(0))
//            animator.MatchTarget(matchTarget.position, matchTarget.rotation,
//                        AvatarTarget.Root, new MatchTargetWeightMask
//                        (new Vector3(1, 0, 0.1f), 1), 0f, 0.25f);
//        else if (!animator.IsInTransition(0))
//            animator.MatchTarget(matchTarget.position, matchTarget.rotation,
//                        AvatarTarget.Root, new MatchTargetWeightMask
//                        (new Vector3(1, 1, 1), 1), 0.25f, 0.7f);

//        if (stateInfo.normalizedTime >= 0.7f || hitReaction)
//            enterLadderTop = false;
//    }

//    // exit ladder top
//    if (stateInfo.IsName("Ladder.ExitLadderTop") || hitReaction)
//    {
//        if (stateInfo.normalizedTime >= 0.85f || hitReaction)
//        {
//            _capsuleCollider.isTrigger = false;
//            _rigidbody.useGravity = true;
//            exitLadderTop = false;
//            usingLadder = false;
//        }
//    }
//}