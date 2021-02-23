using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitDetect : MonoBehaviour
{
    public float vibrationDuration;
    public float vibrationStrength;
    float lastVibrationTime;
    public OVRInput.Controller controller;


    //Vector3 lastPos;
    
    void Update()
    {
        if (vibrationDuration > 0 && lastVibrationTime > 0 && lastVibrationTime <= Time.time)
        {
            OVRInput.SetControllerVibration(1.0f, 0, controller);
            lastVibrationTime = 0;
        }
        //lastPos = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (vibrationDuration > 0)
        {
            if (other.name.StartsWith("End"))
            {
                OVRInput.SetControllerVibration(1.0f, vibrationStrength, controller);
                lastVibrationTime = Time.time + vibrationDuration;

                HitChecker hitChecker = other.GetComponent<HitChecker>();
                //hitChecker.hitDetected();
            }
                /*
            else
            {//assume any other collision is with a beatbox
                if (Vector3.Distance(lastPos, transform.position) > 0.01f){
                    OVRInput.SetControllerVibration(1.0f, vibrationStrength, controller);
                    lastVibrationTime = Time.time + vibrationDuration;
                    other.SendMessage("hit");
                    //float hitPrecision = Vector3.Distance(transform.position, other.transform.position);
                    //Debug.Log("Hit precision " + hitPrecision);
                    //EventManager.TriggerEvent(EventManager.EVENT_BEATBOX_HIT, null);
                }
            }
                 * */
        }
    }
}
