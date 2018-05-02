using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKClimber : MonoBehaviour {

    public bool ikActive;

    public bool leftHandIK;
    public bool rightHandIK;

    public float ArmSpace = 1.0f;

    public Vector3 leftHandPos;
    public Vector3 rightHandPos;

    public Quaternion leftHandRot;
    public Quaternion rightHandRot;

    private Animator anim;
    private Rigidbody rigidBody;

    bool Jumped = false;


	// Use this for initialization
	void Start () {
        anim = GetComponent<Animator>();
        rigidBody = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void FixedUpdate () {

        RaycastHit Forward;
        RaycastHit LHit;
        RaycastHit RHit;

        float upHeight = 2.0f;

        float forwardDistance = 0.0f;
        if(Physics.Raycast(transform.position + new Vector3(0.0f, 1.5f, 0.0f), transform.forward, out Forward, 1.0f))
        {
            forwardDistance = Forward.distance + 0.1f;
        }
        else
        {
            return;
        }

        Vector3 pos = transform.position;
        Vector3 forward = transform.forward * forwardDistance;
        Vector3 up = transform.up * upHeight;
        Vector3 armOffset = transform.right * ArmSpace;

        if(Physics.Raycast(pos + forward + up - armOffset, -transform.up - (transform.right * 0.5f),  out LHit, 1.0f))
        {
            leftHandIK = true;
            leftHandPos = LHit.point;
            leftHandRot = Quaternion.FromToRotation(Vector3.forward, LHit.normal);
        }
        else
        {
            leftHandIK = false;
        }

        if (Physics.Raycast(pos + forward + up + armOffset, -transform.up + (transform.right * 0.5f), out RHit, 1.0f))
        {
            rightHandIK = true;
            rightHandPos = RHit.point;
            rightHandRot = Quaternion.FromToRotation(Vector3.forward, RHit.normal);

        }
        else
        {
            rightHandIK = false;
        }

        if(rightHandIK && leftHandIK)
        {
            if(LHit.distance < 0.5f)
            {
                float jump = anim.GetFloat("Jump");
                bool grounded = anim.GetBool("OnGround");
                if (jump < 0.0f && !grounded)
                {
                    rigidBody.isKinematic = true;
                    anim.SetBool("IsClimbing", true);
                    GetComponent<ThirdPersonUserController>().SetClimbMode(true);
                }
                else
                {
                    Jumped = false;
                }
            }
        }


#if UNITY_EDITOR
        // helper to visualise the ground check ray in the scene view
        Debug.DrawRay(pos + forward + up + armOffset, -transform.up - (transform.right * 0.5f), new Color(0, 1, 0));
        Debug.DrawRay(pos + forward + up - armOffset, -transform.up + (transform.right * 0.5f));
#endif
    }

    public void ClimbUp()
    {
        rigidBody.isKinematic = false;
        anim.SetBool("IsClimbing", false);
        GetComponent<ThirdPersonUserController>().SetClimbMode(false);
        Jumped = false;


        RaycastHit Hit;
        if(Physics.Raycast(anim.GetBoneTransform(HumanBodyBones.Spine).position, Vector3.down, out Hit, 5.0f, LayerMask.NameToLayer("Player")))
        {
            transform.position = Hit.point;
        }


        print("Hello");
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if(ikActive)
        {
            float weight = 1.0f;
            if (Jumped)
            {
                AnimatorStateInfo animationState = anim.GetCurrentAnimatorStateInfo(0);
                AnimatorClipInfo[] myAnimatorClip = anim.GetCurrentAnimatorClipInfo(0);
                weight = myAnimatorClip[0].clip.length * animationState.normalizedTime;
            }

            if (leftHandIK)
            {
               

                anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight);
                anim.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPos);

                anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, weight);
                anim.SetIKRotation(AvatarIKGoal.LeftHand, leftHandRot);
            }
            if (rightHandIK)
            {
                anim.SetIKPositionWeight(AvatarIKGoal.RightHand, weight);
                anim.SetIKPosition(AvatarIKGoal.RightHand, rightHandPos);


                anim.SetIKRotationWeight(AvatarIKGoal.RightHand, weight);
                anim.SetIKRotation(AvatarIKGoal.RightHand, rightHandRot);
            }
        }
    }

    public bool HandleJump()
    {

        Vector3 pos = transform.position;
        Vector3 forward = transform.forward * 1.0f;
        Vector3 up = transform.up * 4.0f;

        RaycastHit result;
        Debug.DrawRay(pos + forward + up, -up, Color.red);
        if (Physics.Raycast(pos + forward + up, -transform.up, out result, 2.5f))
        {
            anim.Play("Idle To Braced Hang");
            Jumped = true;
            return true;
        }

        return false;
    }


}
