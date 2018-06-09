using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GuiltyCharacter;


//Contain the base variables for character component setup and initialise them
namespace GuiltyCharacter
{
    public class CharacterBase : MonoBehaviour
    {

        #region BaseVariables
        [HideInInspector] public ThirdPersonCamera tpCamera;
        // get the animator component of character
        [HideInInspector] public Animator animator;
        // physics material
        [HideInInspector] public PhysicMaterial frictionPhysics, slippyPhysics;
        // get capsule collider information
        [HideInInspector] public CapsuleCollider _capsuleCollider;
        // storage capsule collider extra information
        [HideInInspector] public float colliderRadius, colliderHeight;
        // storage the center of the capsule collider info
        [HideInInspector] public Vector3 colliderCenter;
        // access the rigidbody component
        [HideInInspector] public Rigidbody _rigidbody;
        // generate input for the controller
        [HideInInspector] public Vector2 input;
        // lock all the character locomotion 
        [HideInInspector] public bool lockPlayer;
        // general variables to the locomotion
        [HideInInspector] public float speed, direction, verticalVelocity;
        // create a offSet base on the character hips 
        [HideInInspector] public float offSetPivot;
        // know if the character is ragdolled or not
        [HideInInspector] public bool ragdolled { get; set; }

        [HideInInspector] public ActionsController actionsController;

        #endregion

        public Transform cameraTransform
        {
            get
            {
                Transform camera = transform;
                if(Camera.main != null)
                {
                    camera = Camera.main.transform;
                }
                return camera;
            }
        }

        public void Initialise()
        {
            animator = GetComponent<Animator>();

            tpCamera = ThirdPersonCamera.instance;

            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            offSetPivot = Vector3.Distance(transform.position, hips.position);

            // prevents the collider from slipping on ramps
            frictionPhysics = new PhysicMaterial();
            frictionPhysics.name = "frictionPhysics";
            frictionPhysics.staticFriction = 1f;
            frictionPhysics.dynamicFriction = 1f;

            // default physics 
            slippyPhysics = new PhysicMaterial();
            slippyPhysics.name = "slippyPhysics";
            slippyPhysics.staticFriction = 0f;
            slippyPhysics.dynamicFriction = 0f;

            // rigidbody info
            _rigidbody = GetComponent<Rigidbody>();

            // capsule collider 
            _capsuleCollider = GetComponent<CapsuleCollider>();

            // save your collider preferences 
            colliderCenter = GetComponent<CapsuleCollider>().center;
            colliderRadius = GetComponent<CapsuleCollider>().radius;
            colliderHeight = GetComponent<CapsuleCollider>().height;

            if (tpCamera != null)
            {
                tpCamera.offSetPlayerPivot = offSetPivot;
                tpCamera.playerTarget = transform;
            }

            cameraTransform.SendMessage("Init", SendMessageOptions.DontRequireReceiver);

        }


        public void ResetRagdoll()
        {
            tpCamera.offSetPlayerPivot = offSetPivot;
            tpCamera.SetTarget(this.transform);
            lockPlayer = false;
            verticalVelocity = 0f;
            ragdolled = false;
        }

        public void RagdollGettingUp()
        {
            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
            _capsuleCollider.enabled = true;
        }

        public void EnableRagdoll()
        {
            tpCamera.offSetPlayerPivot = 0f;
            tpCamera.SetTarget(animator.GetBoneTransform(HumanBodyBones.Hips));
            ragdolled = true;
            _capsuleCollider.enabled = false;
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            lockPlayer = true;
        }
    }
}
