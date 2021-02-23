using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hallway : MonoBehaviour
{
    public float factor = 1;
    public float duration = 5f;
    Vector3 startPos = Vector3.zero;
    Vector3 endPos = Vector3.one;
    public Vector3 endPosOffset = Vector3.back;

    public bool dontWaitBeat = true;

    float time;
    void Awake()
    {
        startPos = transform.localPosition;
        //Debug.Log("HALLWAY AWAKE " + startPos + " " + endPos + " " + duration);
        time = -1;
        //endPos = startPos + endPosOffset;
        //Debug.Log("END POS " + endPos);
        //time = duration;
        //endPos = startPos + Vector3.back * 100 * factor;
    }
    // Update is called once per frame
    void Update()
    {
        if (time >= 0)
        {
            transform.localPosition = Vector3.Lerp(startPos, endPos, 1 - (time / duration));
            time -= Time.deltaTime;

            if (dontWaitBeat && time < 0)
            {
                time = duration;
                endPos = startPos + endPosOffset;
            }
        }
    }

    internal void init(float duration, float factor, Vector3 endPosOffset)
    {
        this.duration = duration;
        this.factor = factor;
        this.startPos = transform.localPosition;
        this.endPosOffset = endPosOffset;
        this.endPos = startPos + endPosOffset;
        //Debug.Log("INIT " + startPos + " " + endPos + " " + endPosOffset + " " + duration);
    }
    void beat(EventParam eventParam)
    {
        if (time < 0)
        {
            time = duration;
            endPos = startPos + endPosOffset;
            //Debug.Log("HALLWAY " + startPos + " " + endPos + " " + duration);
        }
    }

    void OnEnable()
    {
        EventManager.StartListening(EventManager.EVENT_BEAT, beat);
    }

    void OnDisable()
    {
        EventManager.StopListening(EventManager.EVENT_BEAT, beat);
    }
}
