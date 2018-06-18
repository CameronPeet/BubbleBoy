using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyCam : MonoBehaviour {

	public Transform target;
	public Vector3 targetOffset;
	Quaternion proxyRot;
	Vector3 proxyPos;
	Vector3 curPos;
	Vector3 colPos;
	Vector3 targetPos;

	public Transform lockOnTarget;
	
	private float followDistance
	{
		get
		{
			return d;
		}
		set
		{
			d = Mathf.Clamp(value, minFollowDistance, maxFollowDistance);
		}
	}
	private float d;
	public float maxFollowDistance = 8.0f;
	public float medFollowDistance =  6.5f;
	public float minFollowDistance = 1.5f;

	public float followSpeed = 10.0f;

	public float sensitivity = 12.0f;
	private float pitch, yaw;

	[Header("Collision Detection")]
	public LayerMask layer;
	public float margin;
	public int maxIterations;
	private int iterations;

    delegate void movementTypePointer();
    movementTypePointer CameraMovementHandler;

	// Use this for initialization
	void Start () {
		proxyPos = target.position;
		proxyRot = target.rotation;
		//targetPos = target.position + new Vector3(0, targetOffset.y, 0);

		yaw = target.eulerAngles.x;
		pitch = target.eulerAngles.y;

		followDistance = medFollowDistance;

		CameraMovementHandler = FreeCameraMovement;

	}


	void Update()
	{
		if(lockOnTarget)
		{
			Collider collider = lockOnTarget.GetComponent<Collider>();   

			if(collider == null) return;

			Vector3 deltaPos = transform.position - collider.bounds.center;
			Quaternion rot = Quaternion.LookRotation(deltaPos);
			float p = rot.eulerAngles.x;
			yaw = rot.eulerAngles.y;
			if(p < -180)
				pitch = p + 360;
			else if(p > 180)
				pitch = p - 360;
			else
				pitch = p;
		}

		else
		{
			yaw += Input.GetAxis("LookHorizontal") * sensitivity;
			yaw += Input.GetAxis("Mouse X") * sensitivity;
			pitch -= -Input.GetAxis("Mouse Y") * sensitivity;
			pitch -= -Input.GetAxis("LookVertical") * sensitivity;

			pitch = ClampAngle(pitch, -60, 80);
			yaw = ClampAngle(yaw, -360, 360);
		}
	}
	/// <summary>
	/// This function is called every fixed framerate frame, if the MonoBehaviour is enabled.
	/// </summary>
	void FixedUpdate()
	{
		CameraMovementHandler();
	}	

	void FreeCameraMovement()
	{
		followDistance = Mathf.Lerp(followDistance, medFollowDistance, followSpeed * Time.fixedDeltaTime);
	
		Vector3 forward = proxyRot * Vector3.forward;
		Vector3 right = proxyRot * Vector3.right;

		Vector3 p = new Vector3(0, targetOffset.y, 0) + target.position;
		curPos = Vector3.Lerp(curPos, p, followSpeed * Time.fixedDeltaTime);

		colPos = curPos + new Vector3(0, targetOffset.y, 0);
		//Collision Detection
	
		CollisionDetection();
		
		Vector3 lookAt = colPos + (right * Vector3.Dot(forward * followDistance, right));

		proxyPos = colPos;
		proxyRot = Quaternion.Slerp(proxyRot, Quaternion.Euler(pitch, yaw, 0), followSpeed * Time.fixedDeltaTime);

		transform.rotation = Quaternion.LookRotation(lookAt - transform.position);

		transform.position = proxyPos + (forward * followDistance);
	}

	private void CollisionDetection()
	{
		RaycastHit result;
		Vector3 pos = (proxyRot * Vector3.forward) * followDistance;
		ClipPlanePoints plane = GetComponent<Camera>().NearClipPlanePoints(colPos + pos, margin);

		if (Physics.Raycast(colPos, plane.LowerLeft - colPos, out result, followDistance + 0.2f, layer))
		{
			followDistance = result.distance;
		}
		if (Physics.Raycast(colPos, plane.LowerRight - colPos, out result, followDistance + 0.2f, layer))
		{
			if(followDistance > result.distance) followDistance = result.distance;
		}
		if (Physics.Raycast(colPos, plane.UpperLeft - colPos, out result, followDistance + 0.2f, layer))
		{
			if(followDistance > result.distance) followDistance = result.distance;
		}
		if (Physics.Raycast(colPos, plane.UpperRight - colPos, out result, followDistance + 0.2f, layer))
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

		Gizmos.DrawSphere(proxyPos, 0.2f);


		Gizmos.color = Color.blue;

		Gizmos.DrawSphere(curPos, 0.1f);
		
		Gizmos.color = Color.red;

		Gizmos.DrawSphere(colPos + ((proxyRot * Vector3.forward) * followDistance), 0.05f);


	}
}


