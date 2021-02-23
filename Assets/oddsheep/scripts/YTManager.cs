using Evereal.YoutubeDLPlayer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class YTManager : MonoBehaviour
{
    public GameObject playerObject;
    YTDLPlayer ytdlPlayer;
    YTDLCore ytdlCore;

    VideoPlayer videoPlayer;

    
    Dictionary<SongRow.Type, List<SongRow>> songRows;

    MediaSource.MediaPrepared mediaPrepared;

    void Awake()
    {
    }
    void Start()
    {
        //360 vid
        //https://www.youtube.com/watch?v=SsFa5JhSDH4
        //setAndPlay("www.youtube.com/watch?v=Os2Ui3-OvbQ");//doommetal
        //setAndPlay("https://www.youtube.com/watch?v=QmXotvtA_po");//beatsabermario
    }

    public void init(string url, MediaSource.MediaPrepared mediaPrepared)
    {
        this.mediaPrepared = mediaPrepared;

        transform.Find("YTDLPlayer_360").gameObject.SetActive(false);
        transform.Find("YTDLPlayer").gameObject.SetActive(false);
        playerObject.SetActive(true);

        string playerStr = "YTDLPlayer";
        if (url.EndsWith("360"))
        {
            playerStr = "YTDLPlayer_360";
            url = url.Substring(0, url.Length - "360".Length);
            Debug.Log("33333333333333333333360 YOUTUBE VIDEO TEST " + url);
        }
        GameObject sink = transform.Find(playerStr).gameObject;

        ytdlPlayer = sink.GetComponentInChildren<YTDLPlayer>();
        ytdlCore = sink.GetComponentInChildren<YTDLCore>();
        videoPlayer = ytdlCore.GetComponent<VideoPlayer>();

        videoPlayer.started += videoPlayerStarted;

        //ytdlPlayer.SetVideoUrl(url);
        ytdlPlayer.url = url;

        ytdlPlayer.parseCompleted += parseCompleted;
        ytdlPlayer.Parse(false);
    }

    private void parseCompleted(YTDLPlayer instance, VideoInfo videoInfo)
    {
        ytdlPlayer.prepareCompleted += prepareCompleted;
        bool preparation = ytdlPlayer.Prepare();
        Debug.Log("YT preparation " + preparation);
    }

    private void prepareCompleted(YTDLPlayer instance)
    {
        Debug.Log("video prepared");
        if (mediaPrepared != null)
            mediaPrepared();
    }

    public void play()
    {
        ytdlPlayer.Play();
    }
    public void stop()
    {
        ytdlPlayer.Stop();
    }
    public float getTime()
    {
        return (float)ytdlPlayer.time;
    }

    //void prepareCompleted(VideoPlayer source)
    //{
    //    Debug.Log("video prepared");
    //    if (mediaPrepared != null)
    //        mediaPrepared();
    //}
    void videoPlayerStarted(VideoPlayer source)
    {
        Debug.Log("video started");
        if (mediaPrepared != null)
            mediaPrepared();
        
        //Debug.Log("audio " + source.GetComponent<AudioSource>().clip.name);
    }

    // Update is called once per frame
    //void Update()
    //{
    //    if (videoPlayer != null && videoPlayer.isPlaying)
    //    {
    //        //TODO spawn boxes to
    //        //videoPlayer.time
    //    }
    //}


    //internal void begin(string url, Dictionary<SongRow.Type, List<SongRow>> songRows, Settings.PlayMode playMode)
    //{
    //    this.songRows = songRows;
    //    init(url);
    //    play();
    //}

    internal void setVolume(float p)
    {
        //Debug.Log("TODO vid volume");
        ytdlPlayer.SetAudioVolume(0, p);
    }

    internal bool isPlaying()
    {
        return ytdlPlayer.isPlaying;//todo getting ready time?
    }
}
