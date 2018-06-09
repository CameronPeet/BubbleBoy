using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeEquipmentManager : MonoBehaviour {

    [HideInInspector] public MeleeWeapon currentMeleeWeapon;
    [HideInInspector] public CollectableMelee currentCollectableWeapon;
    [HideInInspector] public bool changeWeapon;
    public List<Transform> weaponHandlers;

    private Transform bodyCenter;

    [HideInInspector] public bool inAttack;

    // Use this for initialization
    void Start () {
        var animator = GetComponent<Animator>();
        if (animator == null) Destroy(this);
        bodyCenter = GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Spine);
        SetMeleeWeapon(HumanBodyBones.RightHand);
        //SetMeleeShield(HumanBodyBones.LeftLowerArm);
    }


    public void SetMeleeWeapon(HumanBodyBones bodyPart)
    {
        var part = GetComponent<Animator>().GetBoneTransform(bodyPart);
        if (part)
        {
            var meleeWeapon = part.GetComponentInChildren<MeleeWeapon>();
            var collectWeapon = part.GetComponentInChildren<CollectableMelee>();
            currentMeleeWeapon = meleeWeapon;
            currentCollectableWeapon = collectWeapon;

            if (currentMeleeWeapon)
            {
            }
            changeWeapon = false;
        }
    }

    public void SetWeaponHandler(CollectableMelee weapon)
    {
        print("MessageReceived");

        if (!changeWeapon)
        {
            print("find handler");
            changeWeapon = true;
            var handler = weaponHandlers.Find(h => h.name.Equals(weapon.handler));

            if (handler)
            {
                DropWeapon();
                print("Attaching");
                weapon.transform.position = handler.position;
                weapon.transform.rotation = handler.rotation;
                weapon.transform.parent = handler;
                weapon.EnableMeleeWeapon();
                SetMeleeWeapon(HumanBodyBones.RightHand);
            }
            else
            {
                Debug.LogWarning("Missing " + weapon.name + " handler, please create and assign one at the MeleeWeaponManager");
            }
        }
    }

    public void DropWeapon()
    {
        if (currentCollectableWeapon != null)
        {
            currentCollectableWeapon.transform.parent = null;
            currentCollectableWeapon.DisableMeleeWeapon();
            if (currentCollectableWeapon.destroyOnDrop)
                Destroy(currentCollectableWeapon.gameObject);
            currentMeleeWeapon = null;
        }
    }


}
