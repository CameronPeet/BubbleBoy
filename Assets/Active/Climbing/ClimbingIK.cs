using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbingIK : MonoBehaviour {

    public Vector3 RayStart;
    public float RayLength;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        RaycastHit HitResult_A;
        Vector3 heightOffset = (transform.up * 0.5f);
        if (Physics.Raycast(transform.position + heightOffset, Vector3.forward,  out HitResult_A, RayLength))
        {
            print("hit");
        }

        RaycastHit HeightCheck;
        Vector3 height = transform.up * 4.0f;
        if(Physics.Raycast(transform.position + height + transform.forward * 0.5f, Vector3.down, out HeightCheck, 2.0f))
        {
            print("Ready to jump up");
            Animator anim = GetComponent<Animator>();
            anim.SetBool("CanLedgeGrab", true);
        }
        else
        {
            GetComponent<Animator>().SetBool("CanLedgeGrab", false);
        }

#if UNITY_EDITOR
        // helper to visualise the ground check ray in the scene view
        Debug.DrawLine(transform.position + heightOffset, transform.position + heightOffset + (transform.forward * RayLength));
        Debug.DrawLine(transform.position + height + transform.forward * 0.5f,  transform.position + height + (Vector3.down * 2.0f) + transform.forward * 0.5f);
#endif
    }
}
