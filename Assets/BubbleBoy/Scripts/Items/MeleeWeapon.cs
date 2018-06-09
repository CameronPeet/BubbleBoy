using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : IMeleeItem {

    #region variabes
    public int ATK_ID;
    public int MoveSet_ID;

    public bool isActive { get { return active; } }
#endregion
}
