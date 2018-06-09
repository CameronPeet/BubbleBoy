using UnityEngine;
using System.Collections;

namespace GuiltyCharacter
{
    public class ThirdPersonController : ThirdPersonAnimator
    {
        private static ThirdPersonController _instance;
        public static ThirdPersonController instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GameObject.FindObjectOfType<ThirdPersonController>();
                }
                return _instance;
            }
        }

        private bool isAxisInUse;

        void Awake()
        {
            StartCoroutine("UpdateRaycast");// limit raycasts calls for better performance            
        }

        void Start()
        {
            // setup the basic information, created on Character.cs	
            Initialise();					
            Cursor.visible = false;
        }

        void FixedUpdate()
        {
            UpdateMotor();
            UpdateAnimator();
            ControlCameraState();
        }

        void LateUpdate()
        {
            // handle input from controller, keyboard&mouse or mobile touch 
            InputHandle();                              
        }

        void InputHandle()
        {
            CameraInput();
            if (!lockPlayer && !ragdolled)
            {
                ControllerInput();

                // we have mapped the 360 controller as our Default gamepad, 
                // you can change the keyboard inputs by chaging the Alternative Button on the InputManager.                
                InteractInput();
                JumpInput();
            }
            else
                LockPlayer();
        }

        void LockPlayer()
        {
            input = Vector2.zero;
            speed = 0f;
            canSprint = false;
        }


        /// <summary>
        /// CAMERA STATE - you can change the CameraState here, the bool means if you want lerp of not, make sure to use the same CameraState String that you named on ListData
        /// </summary>
        void ControlCameraState()
        {
            if (tpCamera == null)
                return;

            
            if (cameraState.changeCameraState && !strafing)
                tpCamera.ChangeState(cameraState.customCameraState, cameraState.customlookAtPoint, cameraState.smoothCameraState);
            else if (crouch)
                tpCamera.ChangeState("Crouch", true);
            else if (strafing)
                tpCamera.ChangeState("Strafing", true);
            else
                tpCamera.ChangeState("Default", true);
        }

        /// <summary>
        /// Camera Input
        /// </summary>
        void CameraInput()
        {
            if(tpCamera == null)
            { print("Hello"); return;  }

            //if (inputType == InputType.Mobile)
            //    tpCamera.RotateCamera(CrossPlatformInputManager.GetAxis("Mouse X"), CrossPlatformInputManager.GetAxis("Mouse Y"));
            //else if (inputType == InputType.MouseKeyboard)
            //{
                tpCamera.RotateCamera(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                tpCamera.Zoom(Input.GetAxis("Mouse ScrollWheel"));
            //}
            //else if (inputType == InputType.Controler)
            //    tpCamera.RotateCamera(Input.GetAxis("RightAnalogHorizontal"), Input.GetAxis("RightAnalogVertical"));

            RotateWithCamera();
        }


        /// <summary>
        /// AIMING INPUT
        /// </summary>
        void LockOnInput()
        {
            if (!actionsController.LockOn.use) return;
            var _input = actionsController.LockOn.input.ToString();

            if (!ci.locomotionType.Equals(LocomotionType.OnlyFree))
            {
                //if (inputType == InputType.Mobile)
                //{
                //    if (CrossPlatformInputManager.GetButtonDown(_input) && !actions)
                //    {
                //        animator.SetFloat("Direction", 0f);
                //        strafing = !strafing;
                //        tpCamera.gameObject.SendMessage("UpdateLockOn", strafing, SendMessageOptions.DontRequireReceiver);
                //    }
                //}
                //else
                {
                    if (Input.GetButtonDown(_input) && !actions)
                    {
                        animator.SetFloat("Direction", 0f);
                        strafing = !strafing;
                        tpCamera.gameObject.SendMessage("UpdateLockOn", strafing, SendMessageOptions.DontRequireReceiver);
                    }
                }
            }

            if (tpCamera.lockTarget)
            {
                // Switch between targets using Keyboard
                //if (inputType == InputType.MouseKeyboard)
                {
                    if (Input.GetKey(KeyCode.X))
                        tpCamera.gameObject.SendMessage("ChangeTarget", 1, SendMessageOptions.DontRequireReceiver);
                    else if (Input.GetKey(KeyCode.Z))
                        tpCamera.gameObject.SendMessage("ChangeTarget", -1, SendMessageOptions.DontRequireReceiver);
                }
                //// Switch between targets using GamePad
                //else if (inputType == InputType.Controler)
                //{
                //    var value = Input.GetAxisRaw("RightAnalogHorizontal");
                //    if (value == 1)
                //        tpCamera.gameObject.SendMessage("ChangeTarget", 1, SendMessageOptions.DontRequireReceiver);
                //    else if (value == -1f)
                //        tpCamera.gameObject.SendMessage("ChangeTarget", -1, SendMessageOptions.DontRequireReceiver);
                //}
            }
        }

        /// <summary>
        /// UPDATE RAYCASTS - handles a separate update for better performance
        /// </summary>
        public IEnumerator UpdateRaycast()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();

                StopMove();
            }
        }
        
        void ControllerInput()
        {
            input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        }

        void JumpInput()
        {
            if (!actionsController.Jump.use) return;
            var _input = actionsController.Jump.input.ToString();

            bool staminaConditions = true;
            bool jumpConditions = !crouch && onGround && !actions && staminaConditions && !strafing;

            {
                if (Input.GetKey(KeyCode.Space) && jumpConditions)
                {
                    jump = true;
                }
            }
        }


        void InteractInput()
        {
            if (!actionsController.Interact.use) return;
            var _input = actionsController.Interact.input.ToString();

            var hitObject = CheckActionObject();
            if (hitObject != null)
            {
                try
                {
                    if (hitObject.CompareTag("ClimbUp"))
                        DoAction(hitObject, ref climbUp, _input);
                    else if (hitObject.CompareTag("PushButton"))
                    {
                        DoAction(hitObject, ref pushButton, _input);
                    }
                }
                catch (UnityException e)
                {
                    Debug.LogWarning(e.Message);
                }
            }
        }

        /// <summary>
        /// DO ACTION - execute a action when press the action button, use with TriggerAction script
        /// </summary>
        /// <param name="hitObject"> gameobject with the component TriggerAction</param>
        /// <param name="action"> action bool </param>
        void DoAction(GameObject hitObject, ref bool action, string _input)
        {
            var triggerAction = hitObject.transform.GetComponent<TriggerAction>();
            if (!triggerAction)
            {
                Debug.LogWarning("Missing TriggerAction Component on " + hitObject.transform.name + "Object");
                return;
            }
            {
                if (Input.GetKey(KeyCode.E) && !actions || triggerAction.autoAction && !actions)
                {
                    // turn the action bool true and call the animation
                    action = true;

                    // disable the text and sprite 
                    // find the cursorObject height to match with the character animation
                    matchTarget = triggerAction.target;
                    // align the character rotation with the object rotation
                    var rot = hitObject.transform.rotation;
                    transform.rotation = rot;
                }
            }
        }
    }
}