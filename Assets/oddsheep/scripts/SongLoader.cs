using RhythmTool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SongLoader : MonoBehaviour
{
    enum State
    {
        IDLE,
        PREPARING,
        LOADED,
        PLAYING,
        STOPPING
    }
    State state = State.IDLE;
    public delegate void SongReady(SongData songData);

    SongData songData;
    //public AudioSource audioSource;
    //AudioClip clip;

    //bool useBeats = true;

    //int totalLanes = 4;
    //string patternPath = null;
    //string pattern = SongData.AUTO4;
    SongData.PatternType patternType = SongData.PatternType.AUTO;
    int patternLanes = 4;
    private AudioImporter importer;
    RhythmAnalyzer rhythmAnalyzer;
    RhythmData rhythmData;

    List<Beat> beats = new List<Beat>();
    List<Onset> onsets = new List<Onset>();
    List<Chroma> chromaFeatures = new List<Chroma>();

    float processedData = 15;

    //Dictionary<SongRow.Type, List<SongRow>> rows = new Dictionary<SongRow.Type, List<SongRow>>();
    List<SongRow> rowsBox = new List<SongRow>();
    List<SongRow> rowsBpm = new List<SongRow>();
    List<SongRow> rowsBeat = new List<SongRow>();
    List<SongRow> rowsLanePositions = new List<SongRow>();
    
    Settings.PlayMode playMode;
    SongReady songReady;

    bool songFromFile = false;
    public bool isLoading;
    bool inited = false;
    //bool recordingMode = false;
    void Awake()
    {
#if (UNITY_EDITOR)
        importer = GetComponent<NAudioImporter>();
#else
        importer = GetComponent<MobileImporter>();
#endif

        rhythmAnalyzer = GetComponent<RhythmAnalyzer>();
        rhythmAnalyzer.Initialized += process;
        importer.Loaded += songLoaded;
    }
    void reset()
    {
        processedData = 15;
        rowsBox.Clear();
        rowsBpm.Clear();
        rowsBeat.Clear();
        rowsLanePositions.Clear();
        rhythmData = null;
        inited = false;
    }
    public void loadSong(SongData songData, SongReady songReady, int lanes, SongData.PatternType patternType, Settings.PlayMode playMode)
    {
        this.songData = songData;
        if (songData.isYoutube)
            loadYT(songData, songReady, lanes, patternType, playMode);
        else
            loadIncluded(songData, songReady, lanes, patternType, playMode);
    }
    //so PLAYMODE + REC pattern, REC pattern might not exist, we would want to load AUTO or CUstom selected pattern, without saving afterwards
    private void loadYT(SongData songData, SongReady songReady, int lanes, SongData.PatternType patternType, Settings.PlayMode playMode)// = Settings.PlayMode.PLAY)
    {
        patternType = SongData.PatternType.CUSTOM;
        Debug.Log("************* SONGLOADER.loadSong, youtube " + songData + ", " + patternType + " " + playMode);
        state = State.PREPARING;
        this.songReady = songReady;
        this.patternLanes = lanes;
        this.patternType = patternType;
        this.playMode = playMode;
        //this.recordingMode = playMode == Settings.PlayMode.RECORD_PATTERN;

        songFromFile = false;
        isLoading = true;

        isLoading = false;
        Dictionary<string, string> patternFilePaths = new Dictionary<string, string>();
        SongData.findPatterns(patternFilePaths, songData.name);
        string suffix = SongData.getPatternSuffix(patternLanes, patternType);
        Debug.Log("youtube songloader, suffix " + suffix);
        Debug.Log("youtube songloader, suffix " + suffix.Length + " " + "4_".Length + " " + (suffix.Length - 1));

        foreach (string p in patternFilePaths.Keys)
            Debug.Log("******* " + p + " " + patternFilePaths[p]);
        //TODO non hardcoded
        //aaaa
        //Debug.Log("******* " + "4_" + patternFilePaths[suffix.Substring("4_".Length)]);
        loadPattern(patternFilePaths[suffix]);
        //loadPattern("/boxmaniaResources/patterns/QmXotvtA_po_4_CUSTOM.ptn");

        
        Dictionary<SongRow.Type, List<SongRow>> rows = new Dictionary<SongRow.Type, List<SongRow>>();
        rows.Add(SongRow.Type.BPM, rowsBpm);
        rows.Add(SongRow.Type.BOX, rowsBox);
        rows.Add(SongRow.Type.BEAT, rowsBeat);
        rows.Add(SongRow.Type.LANE_POS, rowsLanePositions);

        songData.rows = rows;
        
        bool patternExists = patternFilePaths.ContainsKey(suffix);
        if (!patternExists)
            Debug.Log("No existing pattern (" + suffix + "), will create");

        if (!Settings.instance.patternOverwriteAuto && patternExists)
            songReady(songData);
        else
        {
            Debug.Log("TODO switch to record mode when no yt pattern found");
            songReady(songData);
        }
    }
    private void loadCustom(SongData songData, SongReady songReady, int lanes, SongData.PatternType patternType, Settings.PlayMode playMode)// = Settings.PlayMode.PLAY)
    {
        Debug.Log("************* SONGLOADER.loadSong, path " + songData + ", " + patternType + " " + playMode);
        state = State.PREPARING;
        this.songReady = songReady;
        this.patternLanes = lanes;
        this.patternType = patternType;
        this.playMode = playMode;
        //this.recordingMode = playMode == Settings.PlayMode.RECORD_PATTERN;

        songFromFile = true;
        isLoading = true;
        importer.Import(songData.path);
    }
    private void loadIncluded(SongData songData, SongReady songReady, int lanes, SongData.PatternType patternType, Settings.PlayMode playMode)// = Settings.PlayMode.PLAY)
    {
        Debug.Log("************* SONGLOADER.loadSong, clip " + songData + ", " + patternType + " " + playMode);
        state = State.PREPARING;
        this.songReady = songReady;
        this.patternLanes = lanes;
        this.patternType = patternType;
        this.playMode = playMode;
        //this.recordingMode = playMode == Settings.PlayMode.RECORD_PATTERN;
        
        songFromFile = false;
        isLoading = true;
        songLoaded(songData.getClip());
    }

    //TODO something wrong with layout comparison, using layout index somewhere, and layout totalLanes in other
    void songLoaded(AudioClip clip)
    {
        Debug.Log("************* SONGLOADER.SONGLOADED " + clip.name + " " + clip.length);
        Debug.Log("Errors? " + importer.error);
        reset();
        state = State.LOADED;

        songData.setClip(clip);

        if (clip.loadState == AudioDataLoadState.Unloaded)
        {
            Debug.Log("Clip not loaded, loading now");
            clip.LoadAudioData();
        }

        //string patternFilePath = getPatternPath();
        //bool exists = File.Exists(patternFilePath);
        //TODO are we loading custom patterns correctly?
        Dictionary<string, string> patternFilePaths = new Dictionary<string, string>();
        SongData.findPatterns(patternFilePaths, clip.name);

        //SongData.PatternType validTypesForLoading = SongData.PatternType.AUTO;
        //if (patternType == SongData.PatternType.CUSTOM)
        //    patternType = SongData.PatternType.CUSTOM;
        string suffix = SongData.getPatternSuffix(patternLanes, patternType);

        //Debug.Log("Patterns found:");
        //foreach (string s in patternFilePaths.Keys)
        //    Debug.Log(s + "->" + patternFilePaths[s]);

        bool patternExists = patternFilePaths.ContainsKey(suffix);
        if (!patternExists)
            Debug.Log("No existing pattern (" + suffix + "), will create");

        if (!Settings.instance.patternOverwriteAuto && patternExists)
        {
            //Debug.Log("Auto pattern files already exists, no need to create");
            loadPattern(patternFilePaths[suffix]);
            //string[] fileContent = File.ReadAllLines(patternFilePaths[suffix]);

            ////List<SongRow> rows = new List<SongRow>();

            //foreach (string rowStr in fileContent)
            //{
            //    SongRow row = SongRow.parse(rowStr);
            //    if (row != null)
            //    {
            //        if (row.type == SongRow.Type.BEAT)
            //        {
            //            rowsBeat.Add(row);
            //        }
            //        else
            //        {
            //            if (row.type == SongRow.Type.BPM)
            //                rowsBpm.Add(row);
            //            else
            //                if (row.type == SongRow.Type.BOX)
            //                    rowsBox.Add(row);
            //                else
            //                    if (row.type == SongRow.Type.LANE_POS)
            //                        rowsLanePositions.Add(row);
            //        }
            //    }
            //}
            isLoading = false;

            Debug.Log("SongLoader loaded pattern " + rowsBeat.Count + " beats, " + rowsBox.Count + " boxes, " + rowsBpm.Count + " bpms, " + rowsLanePositions.Count + " lanePositions");

            Dictionary<SongRow.Type, List<SongRow>> rows = new Dictionary<SongRow.Type, List<SongRow>>();
            rows.Add(SongRow.Type.BPM, rowsBpm);
            rows.Add(SongRow.Type.BOX, rowsBox);
            rows.Add(SongRow.Type.BEAT, rowsBeat);
            rows.Add(SongRow.Type.LANE_POS, rowsLanePositions);
            songData.rows = rows;
            songReady(songData);

            //lane pos format: first three for 4,6,8 lanes, then 4,6,8,timestamp if it changes during song
        }
        else
        {
            //TODO rhythmAnalyzer.Abort if stopping before finished... but that should never happen
            Debug.Log("************* SONGLOADER.ANALYZE " + clip.name);
            //rhythmAnalyzer.Analyze(audioSource.clip, (int)(audioSource.clip.length - 1));
            //clip.loadState == AudioDataLoadState.Loaded
            rhythmData = rhythmAnalyzer.Analyze(clip, (int)processedData);
        }
    }
    void loadPattern(string path)
    {
        Debug.Log("************* SONGLOADER.REUSEPATTERN " + path);
        string[] fileContent = File.ReadAllLines(path);

        //List<SongRow> rows = new List<SongRow>();

        foreach (string rowStr in fileContent)
        {
            SongRow row = SongRow.parse(rowStr);
            if (row != null)
            {
                if (row.type == SongRow.Type.BEAT)
                {
                    rowsBeat.Add(row);
                }
                else
                {
                    if (row.type == SongRow.Type.BPM)
                        rowsBpm.Add(row);
                    else
                        if (row.type == SongRow.Type.BOX)
                            rowsBox.Add(row);
                        else
                            if (row.type == SongRow.Type.LANE_POS)
                                rowsLanePositions.Add(row);
                }
            }
        }

    }
    public float getProgress()
    {
        return rhythmAnalyzer.progress;
    }
    string getPatternPath()
    {//TODO take into account difficulty level and AUTO/SLOTABC
        //TODO auto files should not be used for default setting? (ie. always process and play from memory)
        //return Path.Combine(Path.Combine(AssetManager.instance.getBaseFolder(), "patterns"), clip.name + "_" + pattern + ".ptn");
        return Path.Combine(Path.Combine(AssetManager.instance.getBaseFolder(), "patterns"), SongData.getPatternFileName(songData.getClipName(), patternLanes, patternType));
    }
    float lastTime = 0;
    void Update()
    {
        //TODO should enable first and disable after loading to avoid this update while running
        //processStepB(Time.deltaTime);
        if (lastTime < Time.time && rhythmAnalyzer.initialized && inited && rhythmData != null)
        {
            lastTime = Time.time + 5;
            Debug.Log("Song analysed " + rhythmAnalyzer.progress);//+ processedData + ", rows " + rows.Count + " "
            if (rhythmAnalyzer.isDone)
            {
                finish();
                inited = false;
            }
        }
    }
    void finish()
    {
        Debug.Log("Reviewing rhythm analysis");
        getSongRows(songData.getClipLength());

        switch (playMode)
        {
            case Settings.PlayMode.PLAY:
                SongData.storePattern(getPatternPath(), rowsBpm, rowsBeat, rowsBox, rowsLanePositions);
                break;
            case Settings.PlayMode.RECORD_PATTERN:
                SongData.storePattern(getPatternPath(), rowsBpm, rowsBeat, rowsBox, rowsLanePositions);
                break;
            case Settings.PlayMode.TEST_PATTERN_PLAY:
                break;
        }
        isLoading = false;

        Dictionary<SongRow.Type, List<SongRow>> rows = new Dictionary<SongRow.Type, List<SongRow>>(); 
        rows.Add(SongRow.Type.BPM, rowsBpm);
        rows.Add(SongRow.Type.BOX, rowsBox);
        rows.Add(SongRow.Type.BEAT, rowsBeat);

        //Debug.Log(rowsBpm.Count + " bpm rows, " + rowsBox.Count + " box rows");

        rows.Add(SongRow.Type.LANE_POS, rowsLanePositions);

        songData.rows = rows;
        songReady(songData);
    }
    void getSongRows(float songEnd)
    {
        beats.Clear();
        onsets.Clear();

        //RhythmData rData = rhythmAnalyzer.rhythmData;
        rhythmData.GetFeatures<Beat>(beats, 0, songEnd);//songDuration);
        rhythmData.GetFeatures<Onset>(onsets, 0, songEnd);
        float bpm = -1;

        List<Beat> doubleBeats = new List<Beat>();
        for (int i = 0; i < beats.Count - 1; i++)
        {
            Beat b = new Beat();
            b.timestamp = beats[i].timestamp;
            doubleBeats.Add(b);

            Beat b2 = new Beat();
            b2.timestamp = b.timestamp + (beats[i + 1].timestamp - b.timestamp) / 2;
            doubleBeats.Add(b2);
        }
        //beats = doubleBeats;

            Debug.Log("Beats in this song " + beats.Count);


        //int[] prevActiveLanes = new int[AssetManager.MAX_LANES];
        int[] finalActiveLanes = new int[AssetManager.MAX_LANES];
        //Debug.Log("PAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTERN " + Settings.instance.patternUseHold);
        if (!Settings.instance.autoPatternUseBeats)//use onsets instead
        {//use note starts as beat box spawn point (instead of beat time, shown at the else)
            rowsBpm.Add(new SongRow(0, 160));
            for (int i = 0; i < onsets.Count; i++)
            {
                Onset currentBeat = onsets[i];
                Onset nextBeat = null;
                if (Settings.instance.patternUseHold && i < beats.Count - 1)
                    nextBeat = onsets[i + 1];

        
                rowsBeat.Add(new SongRow(currentBeat.timestamp));

                int[] currentActiveLanes = getActiveLanes(currentBeat, patternLanes);//get beats with chroma feature

                if (Settings.instance.patternUseHold && nextBeat != null && nextBeat.timestamp - currentBeat.timestamp < Settings.instance.patternHoldMinTimestamp)
                {
                    //Debug.Log("*************************************grehgerthgthtr " + Settings.instance.patternUseHold);
                    int[] nextActiveLanes = getActiveLanes(nextBeat, patternLanes);

                    for (int l = 0; l < AssetManager.MAX_LANES; l++)
                    {
                        //we don't do holds in onset mode
                        if (currentActiveLanes[l] == 0)
                            finalActiveLanes[l] = 0;
                        else
                            if (currentActiveLanes[l] == 1)
                                finalActiveLanes[l] = 1;
                    }
                    //Debug.Log("BeatCurr comparison " + showActiveLanes(currentActiveLanes));
                    //Debug.Log("BeatNext comparison " + showActiveLanes(nextActiveLanes));
                }
                else
                    finalActiveLanes = currentActiveLanes;

                //Debug.Log("****BeatFinl comparison " + showActiveLanes(finalActiveLanes));
                rowsBox.Add(new SongRow(currentBeat.timestamp, (int[])finalActiveLanes.Clone()));
            }
        }
        else
        {
            for (int i = 0; i < beats.Count; i++)
            {
                Beat currentBeat = beats[i];

                if (currentBeat.bpm != bpm)
                {
                    bpm = currentBeat.bpm;
                    rowsBpm.Add(new SongRow(currentBeat.timestamp, bpm));
                }

                //if (recordingMode)
                rowsBeat.Add(new SongRow(currentBeat.timestamp));

            }
            for (int i = 0; i < doubleBeats.Count; i++)
            {
                Beat currentBeat = doubleBeats[i];
                Beat nextBeat = null;
                if (Settings.instance.patternUseHold && i < doubleBeats.Count - 1)
                    nextBeat = doubleBeats[i + 1];

                int[] currentActiveLanes = getActiveLanes(currentBeat, patternLanes);//get beats with chroma feature

                if (Settings.instance.patternUseHold && nextBeat != null && nextBeat.timestamp - currentBeat.timestamp < Settings.instance.patternHoldMinTimestamp)
                {
                    //Debug.Log("*************************************grehgerthgthtr " + Settings.instance.patternUseHold);
                    int[] nextActiveLanes = getActiveLanes(nextBeat, patternLanes);

                    for (int l = 0; l < AssetManager.MAX_LANES; l++)
                    {
                        if (currentActiveLanes[l] == 0)
                            finalActiveLanes[l] = 0;
                        else
                            if (finalActiveLanes[l] > 0 && currentActiveLanes[l] == 1)
                                finalActiveLanes[l] = 2;
                            else
                                if (currentActiveLanes[l] == 1 && nextActiveLanes[l] == 1)
                                    finalActiveLanes[l] = 2;//hold
                                else
                                    if (currentActiveLanes[l] == 1)
                                        finalActiveLanes[l] = 1;
                    }
                    //Debug.Log("BeatCurr comparison " + showActiveLanes(currentActiveLanes));
                    //Debug.Log("BeatNext comparison " + showActiveLanes(nextActiveLanes));
                }
                else
                    finalActiveLanes = currentActiveLanes;

                //Debug.Log("****BeatFinl comparison " + showActiveLanes(finalActiveLanes));
                rowsBox.Add(new SongRow(currentBeat.timestamp, (int[])finalActiveLanes.Clone()));
        }
        }
        
    }
    int[] getActiveLanes(Onset onset, int totalLanes)
    {
        int[] activeLanes = new int[AssetManager.MAX_LANES];

        chromaFeatures.Clear();
        rhythmData.GetIntersectingFeatures(chromaFeatures, onset.timestamp, onset.timestamp);

        //TODO this patternActive max maybe should be done to spawner, so that generation is always same regardless of setting value
        for (int n = 0; n < Math.Min(Settings.instance.patternMaxActiveLanesAtOnce, chromaFeatures.Count); n++)
        {
            int lane = (int)((int)chromaFeatures[n].note * totalLanes / 12f);
            activeLanes[lane] = 1;

            //chromaFeatures.Clear();
            //rhythmData.GetIntersectingFeatures(chromaFeatures, beat.timestamp, beat.timestamp);

            //for (int n = 0; n < Math.Min(Settings.instance.patternMaxActiveLanes, chromaFeatures.Count); n++)
            //{
            //    int lane = (int)((int)chromaFeatures[n].note * totalLanes / 12f);
            //    activeLanes[lane] = 1;
            //}
        }
        return activeLanes;
    }

    int[] getActiveLanes(Beat beat, int totalLanes)
    {
        int[] activeLanes = new int[AssetManager.MAX_LANES];

        chromaFeatures.Clear();
        //rhythmData.GetIntersectingFeatures(chromaFeatures, beat.timestamp - Settings.instance.beatChromaTimeDelta, beat.timestamp + Settings.instance.beatChromaTimeDelta);
        //rhythmData.GetIntersectingFeatures(chromaFeatures, beat.timestamp, beat.timestamp);
        rhythmData.GetIntersectingFeatures(chromaFeatures, beat.timestamp - 0.01f, beat.timestamp + 0.01f);



        for (int n = 0; n < Math.Min(Settings.instance.patternMaxActiveLanesAtOnce, chromaFeatures.Count); n++)
        {
            int lane = (int)((int)chromaFeatures[n].note * totalLanes / 12f);
            activeLanes[lane] = 1;

        }
        return activeLanes;
    }
    string showActiveLanes(int[] activeLanes)
    {
        string activeStr = "";
        for (int i = 0; i < activeLanes.Length; i++)
            activeStr += activeLanes[i] + ",";
        return activeStr;
    }
    public void process(RhythmData rData)//, float currentTimestamp = 0)
    {
        //TODO add more row types for secondary VFX features, like spectrum visualiser?
        //string patternFilePath = getPatternPath();
        //bool exists = File.Exists(patternFilePath);
        //float songDuration = audioSource.clip.length;
        //Debug.Log("************* SONGLOADER.PROCESS " + patternFilePath + " " + songDuration + " " + totalLanes);
        Debug.Log("************* SONGLOADER.COMPETED ANALYSIS " + songData);
        List<Beat> tmpBeats = new List<Beat>();
        rData.GetFeatures<Beat>(tmpBeats, 0, songData.getClipLength());
        //Debug.Log("beats detected " + tmpBeats.Count);
        rhythmData = rData;
        //getSegmentData(0, processedData);
        inited = true;
    }
}
