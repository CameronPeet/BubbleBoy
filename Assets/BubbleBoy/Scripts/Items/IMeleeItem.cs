using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IMeleeItem : ICollectable {

    protected bool active;
    public void SetActive(bool b)
    {
        active = b;
    }

}
