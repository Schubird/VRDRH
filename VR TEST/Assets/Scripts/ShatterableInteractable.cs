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
    [Header("Shatter Haptics")]
    [SerializeField] private float pulseStrength = 1f;
    [SerializeField] private float pulseDuration = 0.2f;
    [SerializeField] private float pulseInterval = 0.05f;

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

                    AkSoundEngine.PostEvent("Play_Wood_Crash_1", gameObject);
                    AkSoundEngine.PostEvent("Stop_LobbyGuitar", gameObject);
                }
            }
        }
    }

    public void ShatterObject(float impulsePower)
    {
        if (_isShattered)
        {
            return;
        }

        StartCoroutine(CoShatterObject(impulsePower));

        VRTK_ControllerReference controllerReference = VRTK_ControllerReference.GetControllerReference(interactableObject.GetGrabbingObject());
        if (VRTK_ControllerReference.IsValid(controllerReference))
        {
            //Debug.Log("VALID REF");
            //VRTK_ControllerHaptics.TriggerHapticPulse(controllerReference, pulseStrength);
            VRTK_ControllerHaptics.TriggerHapticPulse(controllerReference, pulseStrength, pulseDuration, pulseInterval);
        }
    }

    // Shatter the Object
    private IEnumerator CoShatterObject(float impulsePower)
    {
        //interactableObject.GetComponent<Rigidbody>().isKinematic = true;
        //interactableObject.

        Transform prevParent = sliceableObject.transform.parent;
        Vector3 prevLocalPos = sliceableObject.transform.localPosition;
        Vector3 prevGrabPos = prevLocalPos;
        if (interactableObject.IsGrabbed())
        {
            var attachPoint = interactableObject.GetPrimaryAttachPoint();
            prevGrabPos = prevParent.InverseTransformPoint(attachPoint.position);
        }

        if (dropOnShatter)
        {
            interactableObject.Ungrabbed();
            sliceableObject.transform.SetParent(null);
        }
        if (disableGrabOnShatter)
        {
            interactableObject.isGrabbable = false;
        }
        bool addedRigidbody = false;
        if (enablePhysicsOnShatter)
        {
            Rigidbody rb = sliceableObject.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
            }
            else
            {
                rb = sliceableObject.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                addedRigidbody = true;
            }
        }

        int preShatterChildCount = prevParent.childCount;
        TurboSlicerSingleton.Instance.Shatter(sliceableObject.gameObject, shatterSteps);

        if (dropOnShatter == false)
        {
            for (int i = 0; i < shatterSteps; i++)
            {
                while (prevParent.childCount == preShatterChildCount)
                {
                    yield return null;
                }
                preShatterChildCount = prevParent.childCount;
                //Debug.Log("Children: " + preShatterChildCount);
                //TurboSlicerSingleton.Instance.
            }
            yield return new WaitForEndOfFrame();

            List<Transform> children = new List<Transform>();
            for (int i = 0; i < prevParent.childCount; i++)
            {
                children.Add(prevParent.GetChild(i));
            }

            //Reparent ONE child thats closest to the original grabPos.
            Transform heldFragment = null;
            //float closestDist = float.MaxValue;
            float largestVolume = 0f;
            for (int i = 0; i < prevParent.childCount; i++)
            {
                Transform child = prevParent.GetChild(i);
                MeshRenderer mr = child.GetComponent<MeshRenderer>();
                if (mr)
                {
                    float volume = Mathf.Abs(mr.bounds.size.x * mr.bounds.size.y * mr.bounds.size.z);

                    // Bounds are in world space.
                    //Vector3 diff = prevParent.InverseTransformPoint(mr.bounds.center) - prevGrabPos;
                    //float dist = diff.magnitude;
                    //if (dist < closestDist)
                    if (volume > largestVolume)
                    {
                        heldFragment = child;
                        //closestDist = dist;
                        largestVolume = volume;
                    }
                }
                // Ignore children without meshrenderers
            }

            prevParent.DetachChildren();

            if (heldFragment != null)
            {
                heldFragment.SetParent(prevParent);

                if (enablePhysicsOnShatter) // Disable physics on this new object.
                {
                    Rigidbody rb = heldFragment.gameObject.GetComponent<Rigidbody>();
                    if (addedRigidbody)
                    {
                        Destroy(rb);
                    }
                    else
                    {
                        rb.isKinematic = true;
                    }
                }
            }
        }

        //interactableObject.GetComponent<Rigidbody>().isKinematic = false;

        //sliceableObject.transform.SetParent(prevParent);
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
