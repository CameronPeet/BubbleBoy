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
        }

        void LateUpdate()
        {
            // handle input from controller, keyboard&mouse or mobile touch 
            InputHandle();                              
        }

        void InputHandle()
        {
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