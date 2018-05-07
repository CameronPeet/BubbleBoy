using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{

    private static ThirdPersonCamera _instance;

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

    }


    private void FixedUpdate()
    {
        CameraMovement();
    }

    void CameraMovement()
    {
    }

    void CameraFixedPointMovement()
    {
    }
}

public enum CameraMode
{
    Free,
    FixedAngle,
    FixedPoint
}
