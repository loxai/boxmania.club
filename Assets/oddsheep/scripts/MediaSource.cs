using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MediaSource : MonoBehaviour
{
    public AudioSource audioSource;
    public YTManager videoSource;
    bool isVideo = false;
    SongData songData;

    public delegate void MediaPrepared();

    override public string ToString()
    {
        return name + " " + songData.path + " " + (songData.isYoutube ? " YT" : "" + songData.getClipLength());
    }
    internal string getName()
    {
        return songData.getClipName();
    }

    internal bool unloadAudioData()
    {
        if (audioSource != null && audioSource.clip != null)
            return audioSource.clip.UnloadAudioData();
        return true;
    }

    internal void stop()
    {
        if (isVideo)
        {
            videoSource.stop();
        }
        else
        {
            audioSource.Stop();
            audioSource.time = 0;
        }
    }

    internal void setVolume(float p)
    {
        if (isVideo)
            videoSource.setVolume(p);
        else
            audioSource.volume = p;
    }

    internal void play()
    {
        if (isVideo)
            videoSource.play();
        else
        {
            audioSource.Play();
        }
    }

    internal void loadData(SongData songData, MediaPrepared mediaPrepared)
    {
        this.songData = songData;
        isVideo = songData.isYoutube;
        if (isVideo)
        {
            videoSource.init(songData.path, mediaPrepared);
        }
        else
        {
            audioSource.clip = songData.getClip();
            mediaPrepared();
        }
    }

    internal float getTime()
    {
        if (isVideo)
            return videoSource.getTime();

        return audioSource.time;
    }

    internal bool isPlaying()
    {
        if (isVideo)
            return videoSource.isPlaying();

        return audioSource.isPlaying;
    }

}
