using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockRotate : MonoBehaviour
{
    public float factor = 1;
    public float duration = 0.3f;
    public Vector3 axis = Vector3.one;

    float time;
    // Update is called once per frame
    void Update()
    {
        if (duration > 0)
        {
            if (time > 0)
            {
                transform.Rotate(axis, factor * time);
                time -= Time.deltaTime;
            }
        }
        else
            transform.Rotate(axis, factor * Time.deltaTime);
    }

    internal void init(float duration, float factor)
    {
        this.duration = duration;
        this.factor = factor;
    }
    void beat(EventParam eventParam)
    {
        time = duration;
    }
    //void beatNote(EventParam eventParam)
    //{
    //    //TODO
    //    time = 1;
    //}
    void OnEnable()
    {
        EventManager.StartListening(EventManager.EVENT_BEAT, beat);
        EventManager.StartListening(EventManager.EVENT_BEAT_NOTE, beat);
    }

    void OnDisable()
    {
        EventManager.StopListening(EventManager.EVENT_BEAT, beat);
        EventManager.StopListening(EventManager.EVENT_BEAT_NOTE, beat);
    }

}
