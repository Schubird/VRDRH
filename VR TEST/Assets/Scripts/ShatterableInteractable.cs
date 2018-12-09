using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using NobleMuffins.TurboSlicer;

public class ShatterableInteractable : MonoBehaviour
{
    public Action<float> OnShatterAction = delegate { };

    [Tooltip("InteractableObject that we can use.")]
    [SerializeField] private VRTK_InteractableObject interactableObject;
    [Tooltip("Object that gets shattered. Make this a CHILD.")]
    [SerializeField] private Sliceable sliceableObject;
    [Tooltip("Number of cuts for the shatter.")]
    [SerializeField] private int shatterSteps = 3;
    [Tooltip("Minimum impulse needed to shatter.")]
    [SerializeField] private float minimumImpulse = 5f;
    [Tooltip("Maximum impulse read for the shatter event.")]
    [SerializeField] private float maximumImpulse = 10f;
    [Tooltip("Must be currently grabbed to shatter.")]
    [SerializeField] private bool requireGrab = true;
    [Tooltip("Drop the object when shattered.")]
    [SerializeField] private bool dropOnShatter = true;
    [Tooltip("Set the interactable to ungrabbale when shattered.")]
    [SerializeField] private bool disableGrabOnShatter = true;
    [Tooltip("Enable Physics on scliceable when shatterd.")]
    [SerializeField] private bool enablePhysicsOnShatter = true;

    private bool _isShattered = false;

	void Start ()
    {
        if (interactableObject == null)
        {
            interactableObject = GetComponent<VRTK_InteractableObject>();
        }
        if (maximumImpulse < minimumImpulse)
        {
            maximumImpulse = minimumImpulse;
        }
	}

    void OnCollisionEnter(Collision coll)
    {
        if (_isShattered) // Dont do anything if already Shattered.
        {
            return;
        }
        if (interactableObject == null || sliceableObject == null) // Don't do anything if missing important references.
        {
            Debug.LogWarning("Missing important references!!!");
            return;
        }
        if (!requireGrab || interactableObject.IsGrabbed()) //Only shatter if grabbed.
        {
            if (CheckIfNotPlayer(coll.gameObject)) // Dont shatter if it hits the player.
            {
                float impulseMagnitude = coll.impulse.magnitude;
                if (impulseMagnitude > minimumImpulse) // shatter if enough impulse. 
                {
                    ShatterObject(Mathf.Clamp(impulseMagnitude, minimumImpulse, maximumImpulse));
                }
            }
        }
    }

    // Shatter the Object
    public void ShatterObject(float impulsePower)
    {
        if (_isShattered)
        {
            return;
        }
        if (dropOnShatter)
        {
            interactableObject.Ungrabbed();
        }
        if (disableGrabOnShatter)
        {
            interactableObject.isGrabbable = false;
        }
        if (enablePhysicsOnShatter)
        {
            Rigidbody rb = sliceableObject.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
            }
        }

        TurboSlicerSingleton.Instance.Shatter(sliceableObject.gameObject, shatterSteps);
        _isShattered = true;

        // NOTE: Use this if you want to get the impulse power between 0 and 1.
        //impulsePower = Mathf.InverseLerp(minimumImpulse, maximumImpulse, impulsePower);
        OnShatterAction(impulsePower);
    }

    // Check to make sure the object is NOT the player.
    private bool CheckIfNotPlayer(GameObject go)
    {
        // Improve this?
        return (!go.CompareTag("Player"));
    }

}
