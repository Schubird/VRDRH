using UnityEngine;
using NobleMuffins.TurboSlicer;

void OnCollisionEnter(Collision coll)
{
    if (!currentlyGrabbed) return;
    if (coll.gameObject.tag != "Player")
    {
        if (coll.impulse.magnitude > shatterThreshold)
        {
            TurboSlicerSingleton.Instance.Shatter(meshGameObject, shatterSteps);

            ReleaseGrab();
        }
    }
}