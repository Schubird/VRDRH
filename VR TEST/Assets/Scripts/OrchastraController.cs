﻿using UnityEngine;
using System.Collections;

public class OrchastraController : MonoBehaviour
{
    public Instrument[] instruments;

    AK.Wwise.Event playAllEvent;
    AK.Wwise.RTPC crossFadeRtpc;

    //public float crossFadeTime = 0.1f;
    //public float crossFadeTime = 0.1f;
    private float MAX_VOLUME = 10f;

    private void Start()
    {

    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartOrchastra();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {

        }
        else if(Input.GetKeyDown(KeyCode.Alpha1))
        {

        }
    }

    public void StartOrchastra()
    {
        foreach (Instrument instrument in instruments)
        {
            instrument.StartPlaying();
        }
    }

    public void StopOrchastra()
    {
        foreach (Instrument instrument in instruments)
        {
            instrument.StopPlaying();
        }
    }

    public void CrossFade()
    {

    }

    private IEnumerator CoCrossfade(float duration)
    {
        float timer = 0f;
        
        while(timer < duration)
        {
            timer += Time.deltaTime;


            yield return null;
        }

    }
}
