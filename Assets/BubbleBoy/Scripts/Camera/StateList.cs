using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
[CreateAssetMenu(fileName = "CameraStateList", menuName = "Character/CameraStateList", order = 1)]
public class StateList : ScriptableObject
{
    [SerializeField] public string Name;
    [SerializeField] public List<CameraState> tpCameraStates;

    public StateList()
    {
        tpCameraStates = new List<CameraState>();
        tpCameraStates.Add(new CameraState("Default"));
    }
}
