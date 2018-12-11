using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;
using NobleMuffins.TurboSlicer;
using UnityEngine.Events;

public class ShatterableInteractable : MonoBehaviour
{
    [Tooltip("InteractableObject that we can use.")]
    [SerializeField] private VRTK_InteractableObject interactableObject;
    [Header("Shatter Effect Control")]
    [Tooltip("Root object of the shattering object. (We hold this after shatter)")]
    [SerializeField] private GameObject shatterObjectRoot;
    [Tooltip("Object that gets shattered. Make this a CHILD.")]
    [SerializeField] private Sliceable[] shatterablePieces;
    [Tooltip("Objects that fall off but don't shatter.")]
    [SerializeField] private GameObject[] fullPieces;
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

    [Header("Wwise Event")]
    public AK.Wwise.Event shatterEvent;

    [Header("OnShatter Event")]
    public UnityEvent OnShatterUnityEvent;
    public Action<float> OnShatterAction = delegate { };

    private bool _isShattered = false;
    private List<GameObject> allFragments = new List<GameObject>();

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
        if (interactableObject == null) // Don't do anything if missing important references.
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
                    // Set impulsePower % between min and max.
                    float impulsePower = Mathf.InverseLerp(minimumImpulse, maximumImpulse, 
                        Mathf.Clamp(impulseMagnitude, minimumImpulse, maximumImpulse));

                    ShatterObject(impulsePower);
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
        _isShattered = true;
        allFragments = new List<GameObject>();


        // Play Wwise Event
        shatterEvent.Post(gameObject);

        // Emit Haptic Rumble.
        if (interactableObject.IsGrabbed())
        {
            VRTK_ControllerReference controllerReference = VRTK_ControllerReference.GetControllerReference(interactableObject.GetGrabbingObject());
            if (VRTK_ControllerReference.IsValid(controllerReference))
            {
                VRTK_ControllerHaptics.TriggerHapticPulse(controllerReference, pulseStrength, pulseDuration, pulseInterval);
            }
        }

        // Perform Shatter Effect
        PerformShatterEffect(impulsePower);

        // Release Object (if necessary)
        if (dropOnShatter)
        {
            interactableObject.Ungrabbed();
        }
        if (disableGrabOnShatter)
        {
            interactableObject.isGrabbable = false;
        }

        // Emit OnShatter event.
        OnShatterAction(impulsePower);
        OnShatterUnityEvent.Invoke();
    }

    // Actually shattering the object.
    private void PerformShatterEffect(float impulsePower)
    {
        // 1) Drop all full pieces
        foreach (GameObject piece in fullPieces)
        {
            DropPiece(piece, impulsePower);
        }

        // 2) Shatter all sliceable pieces
        foreach (Sliceable sliceable in shatterablePieces)
        {
            StartCoroutine(CoShatterPiece(sliceable, impulsePower));
        }
    }

    private void DropPiece(GameObject piece, float impulsePower)
    {
        piece.transform.SetParent(null);

        allFragments.Add(piece);

        if (enablePhysicsOnShatter)
        {
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
            }
            else
            {
                rb = piece.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = false;
            }
        }
    }

    // Shatter the Object
    private IEnumerator CoShatterPiece(Sliceable shatterPiece, float impulsePower)
    {
        Transform prevParent = shatterObjectRoot.transform.parent;
        Vector3 prevLocalPos = prevParent.InverseTransformPoint(shatterPiece.transform.position);
        //Transform prevParent = shatterPiece.transform.parent;
        //Vector3 prevLocalPos = shatterPiece.transform.localPosition;
        Vector3 prevGrabPos = prevLocalPos;

        // Determine whether we're going to try to hold a fragment.
        bool holdFragment = interactableObject.IsGrabbed() && !dropOnShatter && (shatterPiece.gameObject == shatterObjectRoot);

        if (interactableObject.IsGrabbed())
        {
            var attachPoint = interactableObject.GetPrimaryAttachPoint();
            prevGrabPos = prevParent.InverseTransformPoint(attachPoint.position);
        }

        bool addedRigidbody = false;
        if (enablePhysicsOnShatter)
        {
            Rigidbody rb = shatterPiece.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
            }
            else
            {
                rb = shatterPiece.gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                addedRigidbody = true;
            }
        }

        // Note: This gets stupid complicated because shattering is multithreaded, 
        // and we have no way of getting the result objects from the shatter. (I think...)

        // Put in temporary "Container" object.
        Transform container = new GameObject("[SLICE CONTAINER]").transform;
        container.SetParent(prevParent);
        container.localPosition = Vector3.zero;
        container.localRotation =  Quaternion.identity;
        container.localScale = Vector3.one;
        shatterPiece.gameObject.transform.SetParent(container);

        // IMPORTANT: Yielding here allow all the other pieces to move into containers 
        //  *BEFORE* before starting the shatter. 
        //  This prevents extra duplicates when shatter clones the object, 
        //  and null references from when shatter deletes the original.
        yield return null;

        // Perform Shatter.
        int preShatterChildCount = container.childCount;
        TurboSlicerSingleton.Instance.Shatter(shatterPiece.gameObject, shatterSteps);

        // Wait for each async "shatter" job to complete.
        for (int i = 0; i < shatterSteps+1; i++)
        {
            while (container.childCount == preShatterChildCount)
            {
                yield return null;
            }
            preShatterChildCount = container.childCount;
        }
        //yield return new WaitForEndOfFrame(); // (Might not be necessary?)

        // Get all of the fragments generated from the shatter.
        List<Transform> fragments = new List<Transform>();
        for (int i = 0; i < container.childCount; i++)
        {
            fragments.Add(container.GetChild(i));
            allFragments.Add(container.GetChild(i).gameObject);
        }

        // Shattering the object while it's still in your hand.
        if (holdFragment)
        {
            // Find the fragment we wan't to keep holding.
            // Find ONE fragment that's the largest and/or the closest to the original grabPos.
            Transform heldFragment = null;
            //float closestDist = float.MaxValue;
            float largestVolume = 0f;
            for (int i = 0; i < container.childCount; i++)
            {
                Transform child = container.GetChild(i);
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
                // Ignore children without meshrenderers...
            }

            // If we found a fragment to hold, re-parent it to the original parent.
            if (heldFragment != null)
            {
                heldFragment.SetParent(prevParent);
                fragments.Remove(heldFragment);
                allFragments.Remove(heldFragment.gameObject);

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

        // Remove all children from the container, then destroy it.
        container.DetachChildren();
        Destroy(container.gameObject);
        
    }

    // Check to make sure the object is NOT the player.
    private bool CheckIfNotPlayer(GameObject go)
    {
        // Improve this?
        return (!go.CompareTag("Player"));
    }

#if UNITY_EDITOR
    [ContextMenu("Shatter")]
    private void TestShatter()
    {
        ShatterObject(1f);
    }
#endif
}
