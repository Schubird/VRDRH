using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Instrument : MonoBehaviour
{
    public AK.Wwise.Event[] sectionStartEvents;
    public AK.Wwise.Event[] sectionStopEvents;

    public AK.Wwise.RTPC volumeRtpc;

    private float MAX_VOLUME = 10f;
    private bool isPlaying = false;

    public void StartPlaying()
    {
        if (isPlaying == false)
        {
            foreach (AK.Wwise.Event e in sectionStartEvents)
            {
                e.Post(gameObject);
            }
            //startEvent.Post(gameObject);
            isPlaying = true;
        }
        SetVolume(MAX_VOLUME);
    }

    public void StopPlaying()
    {
        if(isPlaying)
        {
            foreach (AK.Wwise.Event e in sectionStopEvents)
            {
                e.Post(gameObject);
            }
            //stopEvent.Post(gameObject);
            isPlaying = false;
        }
        SetVolume(0f);
    }

    public void SetVolume(float volume)
    {
        volumeRtpc.SetValue(gameObject, volume);
    }

}
