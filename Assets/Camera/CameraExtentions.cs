using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CameraExtentions 
{
	///Source https://www.youtube.com/watch?v=wFdvPuia5VM
	public static ClipPlanePoints NearClipPlanePoints(this Camera camera, Vector3 pos, float clipPlaneMargin)
	{
		var clipPlanePoints = new ClipPlanePoints();

		var transform = camera.transform;
		var halfFOV = (camera.fieldOfView / 2) * Mathf.Deg2Rad;
		var aspect = camera.aspect;
		var distance = camera.nearClipPlane;
		var height = distance * Mathf.Tan(halfFOV);
		var width = height * aspect;
		height *= 1 + clipPlaneMargin;
		width *= 1 + clipPlaneMargin;
		clipPlanePoints.LowerRight = pos + transform.right * width;
		clipPlanePoints.LowerRight -= transform.up * height;
		clipPlanePoints.LowerRight += transform.forward * distance;

		clipPlanePoints.LowerLeft = pos - transform.right * width;
		clipPlanePoints.LowerLeft -= transform.up * height;
		clipPlanePoints.LowerLeft += transform.forward * distance;

		clipPlanePoints.UpperRight = pos + transform.right * width;
		clipPlanePoints.UpperRight += transform.up * height;
		clipPlanePoints.UpperRight += transform.forward * distance;

		clipPlanePoints.UpperLeft = pos - transform.right * width;
		clipPlanePoints.UpperLeft += transform.up * height;
		clipPlanePoints.UpperLeft += transform.forward * distance;

		return clipPlanePoints;
	}

	public static bool ClipPlaneCast(Vector3 from, ClipPlanePoints _to, out RaycastHit hitInfo, float distance, LayerMask cullingLayer)
	{

		if (Physics.Raycast(from, _to.LowerLeft - from, out hitInfo, distance, cullingLayer)
        	|| (Physics.Raycast(from, _to.LowerRight - from, out hitInfo, distance, cullingLayer))
            || (Physics.Raycast(from, _to.UpperLeft - from, out hitInfo, distance, cullingLayer))
        	|| (Physics.Raycast(from, _to.UpperRight - from, out hitInfo, distance, cullingLayer))
			)
			return true;

			return false;
	}

	public static Vector3 RotateAround(this Quaternion rotation, Vector3 pos, Vector3 center, Vector3 axis, float angle) {
		Quaternion rot = Quaternion.AngleAxis(angle, axis); // get the desired rotation
		Vector3 dir = pos - center; // find current direction relative to center
		dir = rot * dir; // rotate the direction
		rotation *= rot; // rotate object to keep looking at the center
		return pos + dir;
	}
}


[System.Serializable]
public struct CameraTarget
{
	public	Transform target;

	public float lookAtHeightOffset;

	public float freeMoveRadius;

	public float maxCameraDistance;

	public float minCameraDistance;
	[HideInInspector]	public float cameraDistance;
	[HideInInspector]	public float prevCameraDistance;
}

public struct ClipProtection
{
	public Vector3 position;
	public Vector3 oldPosition;

	public float desiredDistance;

	public float protectionMargin;

}

[System.Serializable]
public struct TraceTarget
{
	public Transform target;
	public Vector3 position;
}

public struct CameraZoom
{
	public float zoom;
}

public struct Smoothings
{
	public float rotation;
	public float transition;
	public float zoom;
}

public struct ClipPlanePoints
{
	public Vector3 UpperLeft;
	public Vector3 UpperRight;
	public Vector3 LowerLeft;
	public Vector3 LowerRight;
}



