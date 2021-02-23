using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockBeat : MonoBehaviour
{
    public float factor = 1;
    public float duration = 0.3f;
    Vector3 baseScale = Vector3.one;
    Vector3 largeScale = Vector3.one * 2;

    float time;
    void Awake()
    {
        baseScale = transform.localScale;
        largeScale = baseScale * factor;
    }
    // Update is called once per frame
    void Update()
    {
        if (time > 0)
        {
            transform.localScale = Vector3.Lerp(baseScale, largeScale, time);
            time -= Time.deltaTime;
        }
    }

    internal void init(float duration, float factor, Vector3 baseScale)
    {
        this.duration = duration;
        this.factor = factor;
        this.baseScale = baseScale;
        this.largeScale = factor * baseScale;
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
