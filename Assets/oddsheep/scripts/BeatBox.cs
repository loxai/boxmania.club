using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatBox : MonoBehaviour
{
    float maxHoldTime = 0;//0.16f;
    float maxLeaveTime = 1;//0.64f;

    Vector3 leaveOffset = new Vector3(0, 0, -5);
    //float leaveTime = 0;
    //float holdTime = 0;
    Vector3 startPos;

    //float clipBeatTimestamp;
    public float timeToLaneEnd;
    float clipOffset;

    HitChecker destCheck;
    public bool hasArrived = false;
    float timeBeforeRemove;

    //TrailRendererCustom trail = null;
    TrailRenderer trail = null;

    Vector3 destPositionAtTimeEnd;

    bool reverseDirection = false;

    Color trailMissColor = new Color(0.8f, 0.8f, 0.8f, 0.1f);

    GameObject ghostMiss;

    GameObject directionObj;
    internal int direction;


    public void init(Vector3 startPos, HitChecker destCheck, float timeToLaneEnd, int direction, bool doGhostMiss = false, bool reverseDirection = false)//, Vector3 destPositionAtTimeEnd)
    {
        this.destCheck = destCheck;
        this.timeToLaneEnd = timeToLaneEnd;
        this.clipOffset = Spawner.instance.getClipWithOffsetTimestamp();
        this.startPos = startPos;
        //this.clipBeatTimestamp = timestampEnd;

        if (doGhostMiss)
        {
            this.ghostMiss = AssetManager.instance.getGhostBeatbox();
            ghostMiss.SetActive(false);
        }
        this.direction = direction;
        if (direction >= 0)
        {
            this.directionObj = AssetManager.instance.getDirectionalObject();
            directionObj.transform.eulerAngles = new Vector3(0, 0, direction * 45);
            directionObj.transform.position = transform.position;
            directionObj.transform.parent = transform;
            directionObj.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        }

        transform.position = startPos;
        //leaveTime = 0;
        //holdTime = 0;

        trail = GetComponent<TrailRenderer>();
        if (trail != null)
            trail.enabled = false;
            //trail.time = 0;

        gameObject.SetActive(true);
        hasArrived = false;
        timeBeforeRemove = 0;

        lastDestination = destCheck.transform.position;

        destPositionAtTimeEnd = Vector3.zero;

        this.reverseDirection = reverseDirection;

        GetComponent<Renderer>().enabled = true;
    }
    Vector3 lastDestination = Vector3.zero;
    int arrivalTrailPositionCount;
    Vector3[] positions = null;
    
    void Update()
    {
        if (!hasArrived)
        {//not arrived, keep moving
            float lerpTime = (Spawner.instance.getClipWithOffsetTimestamp() - clipOffset) / (timeToLaneEnd);

            float lerpTimeRevCheck = lerpTime;
            if (reverseDirection)
                lerpTimeRevCheck = 1 - lerpTime;

            if (destCheck == null)
                return;
            if (destCheck.transform.parent == null)
                Debug.Log("DESTCHECK.parent IS NULLL????");

            if (destPositionAtTimeEnd != Vector3.zero)
            {
                //Vector3 intermediateDestPos = Vector3.Slerp(destCheck.transform.parent.position, destPositionAtTimeEnd, lerpTimeRevCheck * 2);//multiply by 2 so that box targets final destination earlier (so last half is already straight to point)
                //transform.localPosition = Vector3.Lerp(startPos, intermediateDestPos, lerpTimeRevCheck);

                transform.position = Vector3.Lerp(startPos, destPositionAtTimeEnd, lerpTimeRevCheck);
            }
            else
                transform.position = Vector3.Lerp(startPos, destCheck.transform.parent.position, lerpTimeRevCheck);

            //if (transform.position == destCheck.transform.position) // reached destination
            if (lerpTime >= 1)
            {
                hasArrived = true;

                if (trail == null || !trail.enabled)
                    timeBeforeRemove = 2;// 2;


                if (!reverseDirection)
                {
                    destCheck.beatBoxArrived(this);
                    if (trail != null && trail.enabled)
                    {
                        arrivalTrailPositionCount = trail.positionCount;
                        transform.position = destCheck.transform.parent.position;
                        transform.parent = destCheck.transform.parent;
                    }
                    else
                        transform.position = new Vector3(1000, 1000, 1000);
                }

            }
            //if (trail != null && trail.enabled)
            //    trail.time -= Time.deltaTime;
        }
        else
        {
            timeBeforeRemove -= Time.deltaTime;
            if (timeBeforeRemove > 0)
            {
                if (false && trail.enabled)
                {
                    //TODO ah todo...
                    for (int i = 0; i < trail.positionCount; i++)
                    {
                        float alpha = ((float)i / trail.positionCount);
                        if (trail.positionCount <= arrivalTrailPositionCount)
                        {
                            int ind = i;

                            trail.SetPosition(i, trail.GetPosition(ind) + new Vector3(destCheck.transform.position.x, destCheck.transform.position.y, 0) * (1) - lastDestination);// * (alpha));
                        }
                        //else
                        //    trail.SetPosition(i, new Vector3(destCheck.transform.position.x, destCheck.transform.position.y, destCheck.transform.position.z) * (1) - lastDestination);// * (alpha));

                    }
                    lastDestination = new Vector3(destCheck.transform.position.x, destCheck.transform.position.y, 0);

                }

                if (ghostMiss != null)
                {
                    ghostMiss.transform.position += leaveOffset * Time.deltaTime;
                }

            }
            else
            {
                freeAssets();
            }
        }
        
    }
    public float getTrailTime()
    {
        if (trail == null || !trail.enabled)
            return 0;
        return trail.time;
    }
    
    public void holdTrail(float trailTimestamp, Color color)
    {
        if (!trail.enabled)
        {
            trail.material = AssetManager.instance.trailMaterial;
            trail.alignment = LineAlignment.View;//.TransformZ;
            trail.startWidth = Settings.instance.trailRendererWidth;
            trail.endWidth = Settings.instance.trailRendererWidth/5;
            trail.material.SetColor("_BaseColor", color);
            //trail.startColor = Color.yellow;
            //trail.endColor = Color.black;
            trail.time = trailTimestamp;
            trail.enabled = true;
            //Debug.Log("TRAIL CREATE trail.time " + trailTimestamp);
        }
        else
        {
            //Debug.Log("Extending trail " + trailTimestamp);
            //if (!hasArrived)
            trail.time += trailTimestamp;
            //trail.material.SetColor("_BaseColor", color);
            //Debug.Log("TRAIL ADD trail.time " + trailTimestamp);
        }

        //if (!hasArrived)
        timeBeforeRemove += trailTimestamp;// trail.time;
        //trail.time += Settings.instance.patternHoldMinTimestamp;

        //Debug.Log("Trail time " + trail.time + " added " + trailTimestamp + ", timeBeforeRemove " + timeBeforeRemove);
    }
    public void miss()
    {
        EventManager.TriggerEvent(EventManager.EVENT_BEATBOX_MISS, null);

        if (trail != null && trail.enabled)
        {

            trailMiss();
        }
                        
        //freeAssets();
        //Debug.Log("MIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIISS");
        if (ghostMiss != null)
        {
            ghostMiss.SetActive(true);
            ghostMiss.transform.position = destCheck.transform.parent.position;
            ghostMiss.transform.localScale = destCheck.transform.localScale * 0.2f;
        }
        if (directionObj != null)
        {
            directionObj.transform.parent = null;
            AssetManager.instance.freeDirectional(directionObj);
        }
        directionObj = null;
    }
    public void hit(float hitPrecision)
    {
        //float hitPrecision = Vector3.Distance(transform.position, endPos);
        //Debug.Log("Hit precision " + hitPrecision);
        EventManager.TriggerEvent(EventManager.EVENT_BEATBOX_HIT, new EventParam(hitPrecision));

        //AssetManager.instance.freeBeatbox(gameObject);
    }
    public void trailMiss()
    {
        if (trail.enabled)
        {
            //trail.startColor = trailMissColor;
            //trail.endColor = trailMissColor;
            trail.material.SetColor("_BaseColor", trailMissColor);
        }
        //EventManager.TriggerEvent(EventManager.EVENT_BEATBOX_MISS, null);
    }
    public void trailHit()
    {
        if (trail.enabled)
            trail.enabled = false;

        //just get extra points
        EventManager.TriggerEvent(EventManager.EVENT_BEATBOX_HIT, new EventParam(1f));
    }
    void destroy(EventParam eventParam)
    {
        if (trail != null && trail.enabled)
            trail.enabled = false;

        freeAssets();
    }
    void freeAssets()
    {
        if (directionObj != null)
        {
            directionObj.transform.parent = null;
            AssetManager.instance.freeDirectional(directionObj);
        }
        directionObj = null;
        if (ghostMiss != null)
        {
            ghostMiss.transform.parent = null;
            AssetManager.instance.freeBeatbox(ghostMiss);
        }
        ghostMiss = null;
        AssetManager.instance.freeBeatbox(gameObject);
    }
    void OnEnable()
    {
        EventManager.StartListening(EventManager.EVENT_DESTROY_BEATS, destroy);
    }

    void OnDisable()
    {
        EventManager.StopListening(EventManager.EVENT_DESTROY_BEATS, destroy);
    }

    internal void setFutureLanePosition(Vector3 futureFinalDest)
    {
        //destPosition is in localPosition, so we need to add the lane box parent as base position

        this.destPositionAtTimeEnd = destCheck.transform.parent.parent.position + futureFinalDest;
    }

    internal bool isTrailActive()
    {
        return trail != null && trail.enabled;
    }
}
