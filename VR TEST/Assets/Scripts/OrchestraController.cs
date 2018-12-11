using UnityEngine;
using System.Collections;

public class OrchestraController : MonoBehaviour
{
    [System.Serializable]
    public struct MusicSectionInfo
    {

    }

    public Instrument[] instruments;

    public AK.Wwise.Event playAllEvent;
    public AK.Wwise.RTPC sectionRtpc;
    

    //public float crossFadeTime = 0.1f;
    //public float crossFadeTime = 0.1f;
    //private float MAX_VOLUME = 10f;

    private void Start()
    {

    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartOrchastra();
            sectionRtpc.SetGlobalValue(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // Section A
            sectionRtpc.SetGlobalValue(5);
        }
        else if(Input.GetKeyDown(KeyCode.Alpha3))
        {
            // Section B
            sectionRtpc.SetGlobalValue(10);
        }
    }

    public void StartSection(int sectionNum)
    {
        switch (sectionNum)
        {
            case 0:
                sectionRtpc.SetGlobalValue(0);
                break;
            case 1:
                sectionRtpc.SetGlobalValue(5);
                break;
            case 2:
                sectionRtpc.SetGlobalValue(10);
                break;
            default:
                break;
        }
        // Section B
        sectionRtpc.SetValue(gameObject, 10);
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
