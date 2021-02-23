using System;
    using System.IO;
    using UnityEngine;
    using UnityEngine.Networking;
    using System.Collections;
using System.Collections.Generic;


public class AudioRecorder : MonoBehaviour
{
    public const string DEFAULT_FILENAME = "record";
    public const string FILE_EXTENSION = ".wav";

    public bool IsRecording { get { return recOutput; } }

    private int bufferSize;
    private int numBuffers;
    private int outputRate;
    private int headerSize = 44; //default for uncompressed wav
    private String fileName;
    private bool recOutput = false;
    private AudioClip newClip;
    private FileStream fileStream;
    private AudioClip[] audioClips;
    //private AudioSource[] audioSources;
    public int currentSlot;
    //float[] tempDataSource;

    public static AudioRecorder instance;

    List<float[]> dataBuffer = new List<float[]>();

    void Awake()
    {
        outputRate = AudioSettings.outputSampleRate;
        instance = this;
    }

    void Start()
    {
        AudioSettings.GetDSPBufferSize(out bufferSize, out numBuffers);
        //audioSources = new AudioSource[3];
        //audioSources[0] = GameObject.FindWithTag("RecSlot1").GetComponent<AudioSource>();
        //audioSources[1] = GameObject.FindWithTag("RecSlot2").GetComponent<AudioSource>();
        //audioSources[2] = GameObject.FindWithTag("RecSlot3").GetComponent<AudioSource>();
    }
    void Update()
    {
        if (recOutput || readyToStopRecording)
        {
            if (dataBuffer.Count > 0)
            {
                float[] data = dataBuffer[0];
                dataBuffer.RemoveAt(0);
                ConvertAndWrite(data);
                //Debug.Log("Saving first " + dataBuffer.Count);
            } else
                if (readyToStopRecording)
                {
                    Debug.Log("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXClosing file");
                    WriteHeader();
                    enabled = false;
                }
        }
    }

    public void StartRecording(string recordFileName)
    {
        enabled = true;
        readyToStopRecording = false;
        dataBuffer.Clear();
        //fileName = Path.GetFileNameWithoutExtension(recordFileName) + FILE_EXTENSION;

        Debug.Log("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXStart recording to " + recordFileName);
        Debug.Log("Using audiolistener " + GetComponent<AudioListener>() + ", " + GetComponent<Camera>() + " gameObjectName " + name);

        if (!recOutput)
        {
            StartWriting(recordFileName);
            recOutput = true;
        }
        else
        {
            Debug.LogError("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXRecording is in progress already");
        }
    }

    bool readyToStopRecording = false;
    public void StopRecording()
    {
        Debug.Log("XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXStop recording " + dataBuffer.Count);

        readyToStopRecording = true;
        recOutput = false;
    }


    private void StartWriting(String name)
    {
        fileStream = new FileStream(name, FileMode.Create);

        var emptyByte = new byte();
        for (int i = 0; i < headerSize; i++) //preparing the header
        {
            fileStream.WriteByte(emptyByte);
        }
    }
    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (recOutput)
        {
            dataBuffer.Add(data);
            //ConvertAndWrite(data); //audio data is interlaced
        }
    }

    private void ConvertAndWrite(float[] dataSource)
    {

        var intData = new Int16[dataSource.Length];
        //converting in 2 steps : float[] to Int16[], //then Int16[] to Byte[]

        var bytesData = new Byte[dataSource.Length * 2];
        //bytesData array is twice the size of
        //dataSource array because a float converted in Int16 is 2 bytes.

        //var rescaleFactor = 32767; //to convert float to Int16
        float rescaleFactor = 32767;

        for (var i = 0; i < dataSource.Length; i++)
        {
            intData[i] = (Int16)(dataSource[i] * rescaleFactor);
            var byteArr = new Byte[2];
            byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        fileStream.Write(bytesData, 0, bytesData.Length);
    }

    private void WriteHeader()
    {

        fileStream.Seek(0, SeekOrigin.Begin);

        var riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
        fileStream.Write(riff, 0, 4);

        var chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
        fileStream.Write(chunkSize, 0, 4);

        var wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
        fileStream.Write(wave, 0, 4);

        var fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
        fileStream.Write(fmt, 0, 4);

        var subChunk1 = BitConverter.GetBytes(16);
        fileStream.Write(subChunk1, 0, 4);

        UInt16 two = 2;
        UInt16 one = 1;

        var audioFormat = BitConverter.GetBytes(one);
        fileStream.Write(audioFormat, 0, 2);

        var numChannels = BitConverter.GetBytes(two);
        fileStream.Write(numChannels, 0, 2);

        var sampleRate = BitConverter.GetBytes(outputRate);
        fileStream.Write(sampleRate, 0, 4);

        var byteRate = BitConverter.GetBytes(outputRate * 4);

        fileStream.Write(byteRate, 0, 4);

        UInt16 four = 4;
        var blockAlign = BitConverter.GetBytes(four);
        fileStream.Write(blockAlign, 0, 2);

        UInt16 sixteen = 16;
        var bitsPerSample = BitConverter.GetBytes(sixteen);
        fileStream.Write(bitsPerSample, 0, 2);

        var dataString = System.Text.Encoding.UTF8.GetBytes("data");
        fileStream.Write(dataString, 0, 4);

        var subChunk2 = BitConverter.GetBytes(fileStream.Length - headerSize);
        fileStream.Write(subChunk2, 0, 4);

        Debug.Log("Closing stream");
        fileStream.Close();

    }

    //void UpdateClip()
    //{
    //    StartCoroutine(GetAudioClip());

    //}

    //IEnumerator GetAudioClip()
    //{
    //    using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + Application.dataPath + "myRecord1.wav", AudioType.WAV))
    //    {
    //        yield return www.Send();

    //        if (www.isNetworkError)
    //        {
    //            Debug.Log(www.error);
    //        }
    //        else
    //        {
    //            AudioClip newClip = DownloadHandlerAudioClip.GetContent(www);

    //            Debug.Log(newClip.name + "name    " + newClip.length);
    //            keyboardScript.audioClip = newClip;
    //        }
    //    }
    //}
}