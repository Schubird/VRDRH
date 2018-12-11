using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Instrument : MonoBehaviour
{
    public AK.Wwise.Event[] sectionEvents;

    public AK.Wwise.Event startEvent;
    public AK.Wwise.Event stopEvent;
    public AK.Wwise.RTPC volumeRtpc;
    private bool isPlaying = false;

    private float MAX_VOLUME = 10f;

    public void StartPlaying()
    {
        if (isPlaying == false)
        {
            foreach (AK.Wwise.Event e in sectionEvents)
            {
                e.Post(gameObject);
            }
            startEvent.Post(gameObject);
            isPlaying = true;
        }
        SetVolume(MAX_VOLUME);
    }

    public void StopPlaying()
    {
        if(isPlaying)
        {
            stopEvent.Post(gameObject);
            isPlaying = false;
        }
        SetVolume(0f);
    }

    public void SetVolume(float volume)
    {
        volumeRtpc.SetValue(gameObject, volume);
    }

}
