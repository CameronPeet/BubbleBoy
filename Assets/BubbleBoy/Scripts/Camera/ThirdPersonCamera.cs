using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GuiltyCharacter;

public class ThirdPersonCamera : MonoBehaviour
{

    private static ThirdPersonCamera _instance;

    [HideInInspector] public Transform currentTarget;
    [HideInInspector] public Transform lockTarget;
    public CameraState currentState;
    public StateList CameraStateList;
    [HideInInspector] public string currentStateName;
    [HideInInspector] public int indexList, indexLookPoint;
    [HideInInspector] public float offSetPlayerPivot;

    #region Inspector

    public Transform playerTarget;
    public float xMouseSensitivity = 3f;
    public float yMouseSensitivity = 3f;
    public bool lockCamera;

    public float smoothBetweenState = 0.05f;
    public float smoothCameraRotation = 12f;
    public float scrollSpeed = 10f;

    [Tooltip("What layer will be culled")]
    public LayerMask cullingLayer;
    [Tooltip("Change this value If the camera pass through the wall")]
    public float clipPlaneMargin;

    #endregion

    #region Variables
    private CameraState lerpState;
    private Transform targetLookAt;
    private Vector3 currentTargetPos;
    private Vector3 lookPoint;
    private Vector3 cPos;
    private Vector3 oldTargetPos;
    private Camera _camera;
    private float distance = 5f;
    private float mouseY = 0f;
    private float mouseX = 0f;
    private float targetHeight;
    private float currentZoom;
    private float desiredDistance;
    private float oldDistance;
    private bool useSmooth;
    #endregion

    public void SetTarget(Transform target)
    {
        this.currentTarget = target;
    }

    public static ThirdPersonCamera instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ThirdPersonCamera>();
            }

            return _instance;
        }
    }
    private void Start()
    {
        Initialise();
    }

    public void Initialise()
    {
        //Cursor.visible = false;
        if (playerTarget == null)
            return;

        _camera = GetComponent<Camera>();
        currentTarget = playerTarget;
        currentTargetPos = new Vector3(currentTarget.position.x, currentTarget.position.y + offSetPlayerPivot, currentTarget.position.z);
        targetLookAt = new GameObject("targetLookAt").transform;
        targetLookAt.position = currentTarget.position;
        targetLookAt.hideFlags = HideFlags.HideInHierarchy;
        targetLookAt.rotation = currentTarget.rotation;
        // initialize the first camera state
        mouseY = currentTarget.eulerAngles.x;
        mouseX = currentTarget.eulerAngles.y;

        ChangeState("Default", false);
        currentZoom = currentState.defaultDistance;
        distance = currentState.defaultDistance;
        targetHeight = currentState.height;
        useSmooth = true;
    }


    private void FixedUpdate()
    {
        CameraMovement();

        if (playerTarget == null || targetLookAt == null || currentState == null || lerpState == null) return;

        switch (currentState.cameraMode)
        {
            case CameraMode.Free:
                break;
            case CameraMode.FixedAngle:
                CameraMovement();
                break;
            case CameraMode.FixedPoint:
                //CameraFixed();
                break;
        }
    }

    /// <summary>
    /// Change CameraState
    /// </summary>
    /// <param name="stateName"></param>
    /// <param name="Use smoth"></param>
    public void ChangeState(string stateName, bool hasSmooth)
    {
        if (currentState != null && currentState.Name.Equals(stateName)) return;
        // search for the camera state string name
        var state = CameraStateList.tpCameraStates.Find(delegate (CameraState obj) { return obj.Name.Equals(stateName); });

        if (state != null)
        {
            currentStateName = stateName;
            currentState.cameraMode = state.cameraMode;
            lerpState = state; // set the state of transition (lerpstate) to the state finded on the list
            // in case there is no smooth, a copy will be make without the transition values
            if (currentState != null && !hasSmooth)
                CopyState(currentState, state);
        }
        else
        {
            // if the state choosed if not real, the first state will be set up as default
            if (CameraStateList.tpCameraStates.Count > 0)
            {
                state = CameraStateList.tpCameraStates[0];
                currentStateName = state.Name;
                currentState.cameraMode = state.cameraMode;
                lerpState = state;
                if (currentState != null && !hasSmooth)
                    CopyState(currentState, state);
            }
        }
        // in case a list of states does not exist, a default state will be created
        if (currentState == null)
        {
            currentState = new CameraState("Null");
            currentStateName = currentState.Name;
        }

        indexList = CameraStateList.tpCameraStates.IndexOf(state);
        currentZoom = state.defaultDistance;
        currentState.fixedAngle = new Vector3(mouseX, mouseY);
        useSmooth = hasSmooth;
        indexLookPoint = 0;
    }

    /// <summary>
    /// Change State using look at point if the cameraMode is FixedPoint  
    /// </summary>
    /// <param name="stateName"></param>
    /// <param name="pointName"></param>
    /// <param name="hasSmooth"></param>
    public void ChangeState(string stateName, string pointName, bool hasSmooth)
    {
        useSmooth = hasSmooth;
        if (!currentState.Name.Equals(stateName))
        {
            // search for the camera state string name
            var state = CameraStateList.tpCameraStates.Find(delegate (CameraState obj)
            {
                return obj.Name.Equals(stateName);
            });

            if (state != null)
            {
                currentStateName = stateName;
                currentState.cameraMode = state.cameraMode;
                lerpState = state; // set the state of transition (lerpstate) to the state finded on the list
                                   // in case there is no smooth, a copy will be make without the transition values
                if (currentState != null && !hasSmooth)
                    CopyState(currentState, state);
            }
            else
            {
                // if the state choosed if not real, the first state will be set up as default
                if (CameraStateList.tpCameraStates.Count > 0)
                {
                    state = CameraStateList.tpCameraStates[0];
                    currentStateName = state.Name;
                    currentState.cameraMode = state.cameraMode;
                    lerpState = state;
                    if (currentState != null && !hasSmooth)
                        CopyState(currentState, state);
                }
            }
            // in case a list of states does not exist, a default state will be created
            if (currentState == null)
            {
                currentState = new CameraState("Null");
                currentStateName = currentState.Name;
            }

            indexList = CameraStateList.tpCameraStates.IndexOf(state);
            currentZoom = state.defaultDistance;
            currentState.fixedAngle = new Vector3(mouseX, mouseY);
            indexLookPoint = 0;
        }

        if (currentState.cameraMode == CameraMode.FixedPoint)
        {
            var point = currentState.lookPoints.Find(delegate (LookPoint obj)
            {
                return obj.pointName.Equals(pointName);
            });
            if (point != null)
            {
                indexLookPoint = currentState.lookPoints.IndexOf(point);
            }
            else
            {
                indexLookPoint = 0;
            }
        }
    }
    void CameraMovement()
    {
        if (currentTarget == null)
            return;

        if (useSmooth)
            Slerp(currentState, lerpState, smoothBetweenState);
        else
            CopyState(currentState, lerpState);

        if (currentState.useZoom)
        {
            currentZoom = Mathf.Clamp(currentZoom, currentState.minDistance, currentState.maxDistance);
            distance = useSmooth ? Mathf.Lerp(distance, currentZoom, 2f * Time.fixedDeltaTime) : currentZoom;
        }
        else
        {
            distance = useSmooth ? Mathf.Lerp(distance, currentState.defaultDistance, 2f * Time.fixedDeltaTime) : currentState.defaultDistance;
            currentZoom = distance;
        }

        desiredDistance = distance;
        var camDir = (currentState.forward * targetLookAt.forward) + (currentState.right * targetLookAt.right);
        camDir = camDir.normalized;

        var targetPos = new Vector3(currentTarget.position.x, currentTarget.position.y + offSetPlayerPivot, currentTarget.position.z);
        currentTargetPos = useSmooth ? Vector3.Lerp(currentTargetPos, targetPos, lerpState.smoothFollow * Time.fixedDeltaTime) : targetPos;
        cPos = currentTargetPos + new Vector3(0, targetHeight, 0);
        oldTargetPos = targetPos + new Vector3(0, currentState.height, 0);

        #region ClippingHit
        RaycastHit hitInfo;
        ClipPlanePoints planePoints = NearClipPlanePoints(_camera, cPos + (camDir * (distance)), clipPlaneMargin);
        ClipPlanePoints oldPoints = NearClipPlanePoints(_camera, oldTargetPos + (camDir * oldDistance), clipPlaneMargin);
        if (CullingRayCast(cPos, planePoints, out hitInfo, distance + 0.2f, cullingLayer)) distance = desiredDistance;

        if (CullingRayCast(oldTargetPos, oldPoints, out hitInfo, oldDistance + 0.2f, cullingLayer))
        {
            var t = distance - 0.2f;
            t -= currentState.cullingMinDist;
            t /= (distance - currentState.cullingMinDist);
            targetHeight = Mathf.Lerp(currentState.cullingHeight, currentState.height, Mathf.Clamp(t, 0.0f, 1.0f));
            cPos = currentTargetPos + new Vector3(0, targetHeight, 0);
        }
        else
        {
            oldDistance = useSmooth ? Mathf.Lerp(oldDistance, distance, 2f * Time.fixedDeltaTime) : distance;
            targetHeight = useSmooth ? Mathf.Lerp(targetHeight, currentState.height, 2f * Time.fixedDeltaTime) : currentState.height;
        }
        #endregion

        var lookPoint = cPos;
        lookPoint += (targetLookAt.right * Vector3.Dot(camDir * (distance), targetLookAt.right));
        targetLookAt.position = cPos;
        Quaternion newRot = Quaternion.Euler(mouseY, mouseX, 0);
        targetLookAt.rotation = useSmooth ? Quaternion.Slerp(targetLookAt.rotation, newRot, smoothCameraRotation * Time.fixedDeltaTime) : newRot;
        transform.position = cPos + (camDir * (distance));
        //transform.LookAt(lookPoint);
        var rotation = Quaternion.LookRotation(lookPoint - transform.position);
        rotation.eulerAngles += currentState.rotationOffSet;
        transform.rotation = rotation;
    }

    void CameraFixedPointMovement()
    {
    }

    /// <summary>
    /// Camera Rotation behaviour
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void RotateCamera(float x, float y)
    {
        if (currentState.cameraMode.Equals(CameraMode.FixedPoint)) return;
        if(!currentState.cameraMode.Equals(CameraMode.FixedAngle))
        {

            //Rotate camera
            if(lockTarget)
            {
                CalculateLockOnPoint();
            }
            else
            {
                // free rotation 
                mouseX += x * xMouseSensitivity;
                mouseY -= y * yMouseSensitivity;
                if (!lockCamera)
                {
                    mouseY = ClampAngle(mouseY, currentState.yMinLimit, currentState.yMaxLimit);
                    mouseX = ClampAngle(mouseX, currentState.xMinLimit, currentState.xMaxLimit);
                }
                else
                {
                    mouseY = currentTarget.root.localEulerAngles.x;
                    mouseX = currentTarget.root.localEulerAngles.y;
                }
            }
        }
        else
        {
            // fixed rotation
            mouseX = currentState.fixedAngle.x;
            mouseY = currentState.fixedAngle.y;
        }
    }

    /// <summary>    
    /// Zoom baheviour 
    /// </summary>
    /// <param name="scroolValue"></param>
    /// <param name="zoomSpeed"></param>
    public void Zoom(float scroolValue)
    {
        currentZoom -= scroolValue * scrollSpeed;
    }


    private float ClampAngle(float angle, float min, float max)
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
    /// Copy of CameraStates
    /// </summary>
    /// <param name="to"></param>
    /// <param name="from"></param>
    private void CopyState(CameraState to, CameraState from)
    {
        to.Name = from.Name;
        to.forward = from.forward;
        to.right = from.right;
        to.defaultDistance = from.defaultDistance;
        to.maxDistance = from.maxDistance;
        to.minDistance = from.minDistance;
        to.height = from.height;
        to.fixedAngle = from.fixedAngle;
        to.lookPoints = from.lookPoints;
        to.smoothFollow = from.smoothFollow;
        to.yMinLimit = from.yMinLimit;
        to.yMaxLimit = from.yMaxLimit;
        to.xMinLimit = from.xMinLimit;
        to.xMaxLimit = from.xMaxLimit;
        to.rotationOffSet = from.rotationOffSet;
        to.cullingHeight = from.cullingHeight;
        to.cullingMinDist = from.cullingMinDist;
        to.cameraMode = from.cameraMode;
        to.useZoom = from.useZoom;
    }

    private void Slerp(CameraState to, CameraState from, float time)
    {
        to.Name = from.Name;
        to.forward = Mathf.Lerp(to.forward, from.forward, time);
        to.right = Mathf.Lerp(to.right, from.right, time);
        to.defaultDistance = Mathf.Lerp(to.defaultDistance, from.defaultDistance, time);
        to.maxDistance = Mathf.Lerp(to.maxDistance, from.maxDistance, time);
        to.minDistance = Mathf.Lerp(to.minDistance, from.minDistance, time);
        to.height = Mathf.Lerp(to.height, from.height, time);
        to.fixedAngle = Vector2.Lerp(to.fixedAngle, from.fixedAngle, time);
        to.smoothFollow = Mathf.Lerp(to.smoothFollow, from.smoothFollow, time);
        to.yMinLimit = Mathf.Lerp(to.yMinLimit, from.yMinLimit, time);
        to.yMaxLimit = Mathf.Lerp(to.yMaxLimit, from.yMaxLimit, time);
        to.xMinLimit = Mathf.Lerp(to.xMinLimit, from.xMinLimit, time);
        to.xMaxLimit = Mathf.Lerp(to.xMaxLimit, from.xMaxLimit, time);
        to.rotationOffSet = Vector3.Lerp(to.rotationOffSet, from.rotationOffSet, time);
        to.cullingHeight = Mathf.Lerp(to.cullingHeight, from.cullingHeight, time);
        to.cullingMinDist = Mathf.Lerp(to.cullingMinDist, from.cullingMinDist, time);
        to.cameraMode = from.cameraMode;
        to.useZoom = from.useZoom;
        to.lookPoints = from.lookPoints;
    }

    void CalculateLockOnPoint()
    {
        if (currentState.cameraMode.Equals(CameraMode.FixedAngle) && lockTarget) return; // check if angle of camera is fixed         
        var collider = lockTarget.GetComponent<Collider>();                 // collider to get center of bounds

        if (collider == null)
        {
            // ClearTargetLockOn();
            //lockTarget = null;
            return;
        }

        var point = collider.bounds.center;
        Vector3 relativePos = point - transform.position;                   // get position relative to transform
        Quaternion rotation = Quaternion.LookRotation(relativePos);         // convert to rotation
                                                                            // convert angle (360 to 180)
        if (rotation.eulerAngles.x < -180)
            mouseY = rotation.eulerAngles.x + 360;
        else if (rotation.eulerAngles.x > 180)
            mouseY = rotation.eulerAngles.x - 360;
        else
            mouseY = rotation.eulerAngles.x;
        mouseX = rotation.eulerAngles.y;
    }

    bool CullingRayCast(Vector3 from, ClipPlanePoints _to, out RaycastHit hitInfo, float distance, LayerMask cullingLayer)
    {
        bool value = false;
        //if (showGizmos)
        //{
        //    Debug.DrawRay(from, _to.LowerLeft - from);
        //    Debug.DrawLine(_to.LowerLeft, _to.LowerRight);
        //    Debug.DrawLine(_to.UpperLeft, _to.UpperRight);
        //    Debug.DrawLine(_to.UpperLeft, _to.LowerLeft);
        //    Debug.DrawLine(_to.UpperRight, _to.LowerRight);
        //    Debug.DrawRay(from, _to.LowerRight - from);
        //    Debug.DrawRay(from, _to.UpperLeft - from);
        //    Debug.DrawRay(from, _to.UpperRight - from);
        //}
        if (Physics.Raycast(from, _to.LowerLeft - from, out hitInfo, distance, cullingLayer))
        {
            value = true;
            desiredDistance = hitInfo.distance;
        }

        if (Physics.Raycast(from, _to.LowerRight - from, out hitInfo, distance, cullingLayer))
        {
            value = true;
            if (desiredDistance > hitInfo.distance) desiredDistance = hitInfo.distance;
        }

        if (Physics.Raycast(from, _to.UpperLeft - from, out hitInfo, distance, cullingLayer))
        {
            value = true;
            if (desiredDistance > hitInfo.distance) desiredDistance = hitInfo.distance;
        }

        if (Physics.Raycast(from, _to.UpperRight - from, out hitInfo, distance, cullingLayer))
        {
            value = true;
            if (desiredDistance > hitInfo.distance) desiredDistance = hitInfo.distance;
        }

        return value;
    }

    ClipPlanePoints NearClipPlanePoints(Camera camera, Vector3 pos, float clipPlaneMargin)
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
}

public enum CameraMode
{
    Free,
    FixedAngle,
    FixedPoint
}


public struct ClipPlanePoints
{
    public Vector3 UpperLeft;
    public Vector3 UpperRight;
    public Vector3 LowerLeft;
    public Vector3 LowerRight;
}

