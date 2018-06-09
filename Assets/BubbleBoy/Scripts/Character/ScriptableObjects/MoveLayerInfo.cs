using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "MoveLayerInfo", menuName = "Character/MoveLayerInfo", order = 2)]
public class MoveLayerInfo : ScriptableObject {
    [Header("Layers")]
    public LayerMask groundLayer = 1 << 0;
    [SerializeField]
    public LayerMask stopMoveLayer;
    public float stopMoveDistance = 0.5f;
}
