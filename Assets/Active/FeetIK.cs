using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeetIK : MonoBehaviour {

    protected Animator animator;

    public bool ikActive = false;
    public float ikWeight = 1.0f;

    [Header("Feet")]

    public float ForwardStretch = 0.05f;
    public float HeightStretch = 0.05f;
    public float LegSpace = 0.05f;

    public float VerticalOffset = 0.0f;

    Vector3 lFPos;
    Vector3 rFPos;

    Quaternion lFRot;
    Quaternion rFRot;

    float lFWeight = 0.0f;
    float rFWeight = 0.0f;

    Transform leftFoot;
    Transform rightFoot;
   

    void Start()
    {
        animator = GetComponent<Animator>();

        leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
    }

    private void Update()
    {
        RaycastHit leftResult;
        RaycastHit rightResult;

        Vector3 worldLeft = leftFoot.TransformPoint(Vector3.zero);
        Vector3 worldRight = rightFoot.TransformPoint(Vector3.zero);

        if (Physics.Raycast(worldLeft + (transform.forward * ForwardStretch) + (transform.up * HeightStretch) - (transform.right * LegSpace), -Vector3.up, out leftResult, LayerMask.NameToLayer("Player")))
        {

            lFPos = leftResult.point;
            lFRot = Quaternion.FromToRotation(transform.up, leftResult.normal) * transform.rotation;
        }

        if (Physics.Raycast(worldRight + (transform.forward * ForwardStretch) + (transform.up * HeightStretch) + (transform.right * LegSpace), -Vector3.up, out rightResult, 1))
        {
            rFPos = rightResult.point;
            rFRot = Quaternion.FromToRotation(transform.up, rightResult.normal) * transform.rotation;
        }
    }
    //a callback for calculating IK
    void OnAnimatorIK()
    {
        if (animator)
        {
            lFWeight = animator.GetFloat("LeftFoot");
            rFWeight = animator.GetFloat("RightFoot");

            lFWeight = ikWeight;
            rFWeight = ikWeight;

            //if the IK is active, set the position and rotation directly to the goal. 
            if (ikActive)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, lFWeight);
                animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rFWeight);

                animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, lFWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, rFWeight);

                animator.SetIKPosition(AvatarIKGoal.LeftFoot, lFPos + new Vector3(0.0f, VerticalOffset, 0.0f));
                animator.SetIKPosition(AvatarIKGoal.RightFoot, rFPos + new Vector3(0.0f, VerticalOffset, 0.0f));

                animator.SetIKRotation(AvatarIKGoal.LeftFoot, lFRot);
                animator.SetIKRotation(AvatarIKGoal.RightFoot, rFRot);
            }

            //if the IK is not active, set the position and rotation of the hand and head back to the original position
            else
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
                animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
                animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
                animator.SetLookAtWeight(0);
            }
        }
    }
}
