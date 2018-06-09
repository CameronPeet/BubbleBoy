using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectableMelee : MonoBehaviour {

    public bool startUsing;
    public bool destroyOnDrop;
    public string message = "Pick up Weapon";
    public string handler = "handler@weaponName";

    private bool usingPhysics;
    SphereCollider _sphere;
    Collider _collider;
    Rigidbody _rigidbody;

    [HideInInspector] public IMeleeItem _meleeItem;

	// Use this for initialization
	void Start () {

        _meleeItem = GetComponent<IMeleeItem>();
        if(_meleeItem == null)
        {
            _meleeItem = GetComponentInChildren<IMeleeItem>();
        }
        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _sphere = GetComponent<SphereCollider>();

        if (startUsing)
            EnableMeleeWeapon();
        else
            DisableMeleeWeapon();
	}

    void Update()
    {
        if (_rigidbody.IsSleeping() && usingPhysics)
        {
            usingPhysics = false;
            _collider.enabled = false;
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
        }
    }

    public void EnableMeleeWeapon()
    {
        _sphere.enabled = false;
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _collider.enabled = false;
        _collider.isTrigger = true;
    }

    public void DisableMeleeWeapon()
    {
        _sphere.enabled = true;
        _collider.enabled = true;
        _collider.isTrigger = false;
        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
        usingPhysics = true;
        _meleeItem.SetActive(false);
    }
}
