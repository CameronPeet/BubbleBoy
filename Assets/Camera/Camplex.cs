using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camplex : MonoBehaviour {

    [Tooltip("Camera will follow this transform")]
    public Transform target;

    [Tooltip("Camera will lock onto this transform by looking through the anchor point")]
    public Transform lockOnTarget;

    [Tooltip("Anchor offset from target position, camera looks at the anchor which follows the target + offset with smoothing")]
    public Vector3 anchorOffset;

    private Vector3 lockOnAnchorOffset;
    private Quaternion lockOnAnchorRot;

    public float minFollowDistance, maxFollowDistance;

    public float followSpeed = 10.0f;
    public float sensitivity = 3.0f;

    [Header("Collision Detection")]
    public LayerMask layer;
    public float margin = 1.0f;

    //Anchor point always lerps to target.position
    private Vector3 anchorPos;
    //Anchor rotation always lerps towards the input direction
    private Quaternion anchorRot;

    //Calculated each frame based as the lerped delta position 
    private Vector3 desiredAnchorPos;
    //Calculated each frame based on desiredAnchorPos, adjusted by collision detection.
    private Vector3 trueCameraPos;

    //Pitch and yaw of the camera
    private float pitch, yaw;

    private float distanceFromCameraToAnchor;
    private float followDistance
    {
        get
        {
            return distanceFromCameraToAnchor;
        }
        set
        {
            distanceFromCameraToAnchor = Mathf.Clamp(value, minFollowDistance, maxFollowDistance);
        }
    }

    private Transform proxy;

    /// <summary>
    /// Initialise the anchor pos, rot, yaw, pitch and following distance
    /// </summary>
    void Start()
    {
        anchorPos = target.position;
        anchorRot = target.rotation;

        yaw = target.eulerAngles.x;
        pitch = target.eulerAngles.y;

        followDistance = maxFollowDistance;

        proxy = new GameObject("proxy").transform;
    }


    float ax, ay;
    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    void Update()
    {
        if(lockOnTarget)
        {
            ax += Input.GetAxis("Mouse X") * sensitivity;
            ax += Input.GetAxis("LookHorizontal") * sensitivity;
            ay -= -Input.GetAxis("Mouse Y") * sensitivity;
            ay -= -Input.GetAxis("LookVertical") * sensitivity;

            ay = ClampAngle(ay, 0, 80);
            ax = ClampAngle(ax, -45, 45);

            proxy.position = anchorPos - Vector3.forward;
            proxy.rotation = anchorRot;
            proxy.RotateAround(anchorPos, Vector3.up, ax);
            
            //proxy.RotateAround(anchorPos, Vector3.right, -ay);
            Vector3 dir = anchorPos - proxy.position;
            dir = dir.normalized * 2.5f;

            lockOnAnchorOffset.x = dir.x;
            lockOnAnchorOffset.z = dir.z;

            proxy.position = anchorPos + Vector3.up;
            proxy.rotation = anchorRot;
            proxy.RotateAround(anchorPos, transform.right, ay);
            //proxy.RotateAround(anchorPos, Vector3.right, -ay);
            dir = anchorPos - proxy.position;
            dir = dir.normalized * 2.5f;

            lockOnAnchorOffset.y = dir.y;


            Collider collider = lockOnTarget.GetComponent<Collider>();

            Vector3 deltaPos;
            if(collider != null)
                deltaPos = transform.position - collider.bounds.center;
            else 
                deltaPos = transform.position - (Vector3.up * 0.35f);
                
            Quaternion rot = Quaternion.LookRotation(deltaPos);
            float p = rot.eulerAngles.x;
            yaw = rot.eulerAngles.y;
            //Adjust the pitch to be look at, not look away
            if(p < -180) pitch = p + 360;
            else if(p > 180) pitch = p - 360;
            else pitch = p;
        }

        else
        {
            lockOnAnchorOffset = Vector3.zero;
            //yaw += Input.GetAxis("LookHorizontal") * sensitivity;
            yaw += Input.GetAxis("Mouse X") * sensitivity;
            pitch -= -Input.GetAxis("Mouse Y") * sensitivity;
            //pitch -= -Input.GetAxis("LookVertical") * sensitivity;

            pitch = ClampAngle(pitch, -60, 80);
            yaw = ClampAngle(yaw, -360, 360);
        }
    }

    void FixedUpdate()
    {
        FreeCameraMovement();        
    }
	void FreeCameraMovement()
	{
		followDistance = Mathf.Lerp(followDistance, maxFollowDistance, followSpeed * Time.fixedDeltaTime);
	
		Vector3 forward = anchorRot * Vector3.forward;
		Vector3 right = anchorRot * Vector3.right;

		Vector3 p = new Vector3(0, anchorOffset.y, 0) + target.position + lockOnAnchorOffset;
		desiredAnchorPos = Vector3.Lerp(desiredAnchorPos, p, followSpeed * Time.fixedDeltaTime);

		trueCameraPos = desiredAnchorPos + new Vector3(0, anchorOffset.y, 0);
		//Collision Detection
	
		CollisionDetection();
		
		Vector3 lookAt = trueCameraPos + (right * Vector3.Dot(forward * followDistance, right));

		anchorPos = trueCameraPos;
		anchorRot = Quaternion.Slerp(anchorRot, Quaternion.Euler(pitch, yaw, 0), followSpeed * Time.fixedDeltaTime);

		transform.rotation = Quaternion.LookRotation(lookAt - transform.position);

		transform.position = anchorPos + (forward * followDistance);
	}

    private void CollisionDetection()
	{
		RaycastHit result;
		Vector3 pos = (anchorRot * Vector3.forward) * followDistance;
		ClipPlanePoints plane = GetComponent<Camera>().NearClipPlanePoints(trueCameraPos + pos, margin);

		if (Physics.Raycast(trueCameraPos, plane.LowerLeft - trueCameraPos, out result, followDistance + 0.2f, layer))
		{
			followDistance = result.distance;
		}
		if (Physics.Raycast(trueCameraPos, plane.LowerRight - trueCameraPos, out result, followDistance + 0.2f, layer))
		{
			if(followDistance > result.distance) followDistance = result.distance;
		}
		if (Physics.Raycast(trueCameraPos, plane.UpperLeft - trueCameraPos, out result, followDistance + 0.2f, layer))
		{
			if(followDistance > result.distance) followDistance = result.distance;
		}
		if (Physics.Raycast(trueCameraPos, plane.UpperRight - trueCameraPos, out result, followDistance + 0.2f, layer))
		{
			if(followDistance > result.distance) followDistance = result.distance;
		}
	}
    
    
	float ClampAngle(float angle, float min, float max)
	{
		do   
		{
			if (angle < -360)
				angle += 360;
			if (angle > 360)
				angle -= 360;
		} while (angle < -360 || angle > 360);

		return Mathf.Clamp(angle, min, max);
	}


    	/// <summary>
	/// Callback to draw gizmos that are pickable and always drawn.
	/// </summary>
	void OnDrawGizmos()
	{
		Gizmos.color = Color.green;

		Gizmos.DrawSphere(anchorPos, 0.2f);
		Gizmos.color = Color.blue;

		Gizmos.DrawSphere(desiredAnchorPos, 0.1f);
		
		Gizmos.color = Color.red;

		Gizmos.DrawSphere(trueCameraPos + ((anchorRot * Vector3.forward) * followDistance), 0.05f);

        Gizmos.DrawLine(anchorPos, trueCameraPos + (anchorRot * Vector3.forward) * followDistance);

        if(lockOnTarget)
        {
		    Gizmos.color = Color.cyan;
            Gizmos.DrawLine(anchorPos, lockOnTarget.position);
        }
	}

}
