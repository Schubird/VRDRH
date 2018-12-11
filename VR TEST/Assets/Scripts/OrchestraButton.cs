using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VRTK.Controllables;

public class OrchestraButton : MonoBehaviour
{
    public VRTK_BaseControllable controllable;
    public OrchestraController orchestra;
    public int sectionIndex;

    private bool pressedOnce = false;
    public UnityEvent OnButtonPressed;

    protected virtual void OnEnable()
    {
        controllable = (controllable == null ? GetComponent<VRTK_BaseControllable>() : controllable);
        controllable.ValueChanged += ValueChanged;
        controllable.MaxLimitReached += MaxLimitReached;
        controllable.MinLimitReached += MinLimitReached;
    }

    protected virtual void ValueChanged(object sender, ControllableEventArgs e)
    {
    }

    protected virtual void MaxLimitReached(object sender, ControllableEventArgs e)
    {
        if(!pressedOnce)
        {
            pressedOnce = true;
            return;
        }
        if(orchestra)
            orchestra.StartSection(sectionIndex);
        OnButtonPressed.Invoke();
    }
    
    protected virtual void MinLimitReached(object sender, ControllableEventArgs e)
    {

    }
}
