using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public const string EVENT_BEAT = "EVENT_BEAT";
    public const string EVENT_BEAT_NOTE = "EVENT_BEAT_NOTE";
    public const string EVENT_BPM_CHANGE = "EVENT_BPM_CHANGE";
    public const string EVENT_BEATBOX_MISS = "EVENT_BEATBOX_MISS";
    public const string EVENT_BEATBOX_HIT = "EVENT_BEATBOX_HIT";
    public const string EVENT_SONG_SELECTED = "EVENT_SONG_SELECTED";
    //public const string EVENT_SONG_PLAY_STOP = "EVENT_SONG_PLAY_STOP";
    public const string EVENT_SONG_PLAY = "EVENT_SONG_PLAY";
    public const string EVENT_SONG_STOP = "EVENT_SONG_STOP";
    public const string EVENT_THEME_SELECTED = "EVENT_THEME_SELECTED";
    public const string EVENT_USER_LANE_LAYOUT_CHANGE = "EVENT_USER_LANE_LAYOUT_CHANGE";//refers to the event of dragging the lane ends with vr controllers
    public const string EVENT_LANE_LAYOUT_CHANGE = "EVENT_LANE_LAYOUT_CHANGE";//refers to ui event of choosing different layout
    public const string EVENT_LANE_SIZE_CHANGE = "EVENT_LANE_SIZE_CHANGE";
    public const string EVENT_LANE_COLOR_CHANGE = "EVENT_LANE_COLOR_CHANGE";
    public const string EVENT_COMBO_CHAIN = "EVENT_COMBO_CHAIN";
    public const string EVENT_DESTROY_BEATS = "EVENT_DESTROY_BEATS";
    public const string EVENT_END_OF_SONG = "EVENT_END_OF_SONG";
    public const string EVENT_CROSS_FADE_CHANGE = "EVENT_CROSS_FADE_CHANGE";
    public const string EVENT_SHOW_NOTIFICATION = "EVENT_SHOW_NOTIFICATION";
    
    //public const string EVENT_LOAD_PLATFORM = "EVENT_LOAD_PLATFORM";
    //public const string EVENT_LOAD_BEAT_BOX = "EVENT_LOAD_BEAT_BOX";
    

    private Dictionary<string, Action<EventParam>> eventDictionary;

    private static EventManager eventManager;

    public static EventManager instance
    {
        get
        {
            if (eventManager == null)
            {
                eventManager = FindObjectOfType(typeof(EventManager)) as EventManager;

                if (eventManager == null)
                {
                    Debug.LogError("There needs to be one active EventManger script on a GameObject in your scene.");
                }
                else
                {
                    eventManager.Init();
                }
            }
            return eventManager;
        }
    }

    void Init()
    {
        if (eventDictionary == null)
        {
            eventDictionary = new Dictionary<string, Action<EventParam>>();
        }
    }

    public static void StartListening(string eventName, Action<EventParam> listener)
    {
        Action<EventParam> thisEvent;
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            //Add more event to the existing one
            thisEvent += listener;

            //Update the Dictionary
            instance.eventDictionary[eventName] = thisEvent;
        }
        else
        {
            //Add event to the Dictionary for the first time
            thisEvent += listener;
            instance.eventDictionary.Add(eventName, thisEvent);
        }
    }

    public static void StopListening(string eventName, Action<EventParam> listener)
    {
        if (eventManager == null) return;
        Action<EventParam> thisEvent;
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            //Remove event from the existing one
            thisEvent -= listener;

            //Update the Dictionary
            instance.eventDictionary[eventName] = thisEvent;
        }
    }

    public static void TriggerEvent(string eventName, EventParam eventParam)
    {
        Action<EventParam> thisEvent = null;
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            if (thisEvent != null)
                thisEvent.Invoke(eventParam);
            // OR USE  instance.eventDictionary[eventName](eventParam);
        }
    }
}

//Re-usable structure/ Can be a class to. Add all parameters you need inside it
public class EventParam
{
    public float float1;
    public int int1;
    public string string1;
    public EventParam() { }
    public EventParam(int i)
    {
        int1 = i;
    }
    public EventParam(float f)
    {
        float1 = f;
    }
    public EventParam(string s)
    {
        string1 = s;
    }
    public EventParam(int i, string s)
    {
        int1 = i;
        string1 = s;
    }
}
public class SongParam : EventParam
{
    public string songPath;
    public int defaultSong = -1;
    public SongParam(string songPath)
    {
        this.songPath = songPath;
    }
    public SongParam(int songIndex)
    {
        defaultSong = songIndex;
    }
}
public class BeatData : EventParam
{
    public int lane;
    public float speed;
    public float bpm;

    public BeatData(int lane = -1, float speed = - 1, float bpm = -1)
    {
        this.lane = lane;
        this.speed = speed;
        this.bpm = bpm;
    }
}

//public class LayoutParam : EventParam
//{
//    public Vector3[] lanePos;
//    public LayoutParam(Vector3[] lanePos)
//    {
//        this.lanePos = lanePos;
//    }
//}
/*
public class BeatData : EventParam
{
    public int lane;
    public float speed;

    public BeatData(int lane, float speed)
    {
        this.lane = lane;
        this.speed = speed;
    }
}

public class ScoreData : EventParam
{
    public TimingFlag flag;
    public Judgement judgement;

    public ScoreData(Judgement judgement, TimingFlag flag)
    {
        this.judgement = judgement;
        this.flag = flag;
    }
}

public class LaneInputData : EventParam
{
    public int hand; // 0 for left, 1 for right

    public int lane; // lane index

    public LaneInputData(int hand, int lane)
    {
        this.hand = hand;
        this.lane = lane;
    }
}
*/