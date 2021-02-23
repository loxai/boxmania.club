using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    enum State
    {
        INIT,
        SEEK_TO_PLAY,
        //READY_TO_PLAY,
        PLAYING,
        STOPPED,
        MODELLING
    }
    State state = State.INIT;

    Settings.PlayMode playMode;

    //AudioSource audioSource;
    public MediaSource mediaSource;
    public SongData songData;
    //public Vector3[] laneEndSizes = new Vector3[] { new Vector3(0.1f, 0.1f, 0.1f), new Vector3(0.7f, 0.7f, 0.7f), new Vector3(1.2f, 1.2f, 1.2f) };// = new Vector3(0.1f, 0.1f, 0.1f);
    //Vector3[] laneEndSizes = new Vector3[] { new Vector3(0.15f, 0.15f, 0.1f), new Vector3(0.3f, 0.3f, 0.3f), new Vector3(0.6f, 0.6f, 0.6f) };// = new Vector3(0.1f, 0.1f, 0.1f);
    Vector3[] laneEndSizes = new Vector3[] { new Vector3(0.1f, 0.1f, 0.1f), new Vector3(0.2f, 0.2f, 0.2f), new Vector3(0.3f, 0.3f, 0.3f) };// = new Vector3(0.1f, 0.1f, 0.1f);

    GameObject[] lanesStart;
    HitChecker[] lanesEnd;
    public GameObject[] laneContainers;

    //TODO handle lane layout and size somewhere more generic?
    int selectedLaneContainer = 0;
    int selectedLaneEndSize = 0;

    float frameSeekOffset = 0;
    float seekOffset = 0;
    float[] laneSpeed = null;//new float[4];
    float[] timeToLaneEnd = null;//new float[4];
    Vector3 laneStartOffset = new Vector3(0, 5, 30);//new Vector3(0, 25, 40);//new Vector3(0, 5, 10);

    float bpm = -1;

    HitChecker.RegisteredBeat[] prevRegisteredBeats = null;
    Vector3[] currentLaneDestination;
    float totalDuration;
    float lanePosChangeProgressTime;

    float playStartOffset = 0;
    //Vector3 dragControlCenterOffset = Vector3.zero;

    int bpmRowIndex;
    int boxRowIndex;
    int beatRowIndex;
    int lanPosRowIndex;
    //List<SongRow> recordedRows = new List<SongRow>();
    List<SongRow> processedRecordedBeats = new List<SongRow>();
    List<SongRow> lanePosChanges = new List<SongRow>();

    bool laneAudio;

    //int currentRow = 0;
    //List<SongRow> songRows;
    //Dictionary<SongRow.Type, List<SongRow>> songRows;
    //AudioSource audioSource;
    float firstBeatTimestamp;
    int totalLanes = 4;

    int activatedLaneIndex = 0;//a 'randomizing' counter that does sum of all previous activated lane index to decide ghost box lane placement

    internal static Spawner instance;

    bool recordingStartFlag = true;


    //TODO integrate these two into single array (used to check if trail needs extension, and for +1 beat difficulty)
    BeatBox[] lastHoldBox = new BeatBox[AssetManager.MAX_LANES];
    BeatBox[] lastRegularBox = new BeatBox[AssetManager.MAX_LANES];

    void Awake()
    {
        instance = this;
        //initLanes();
    }
    void Start()
    {
        initLanes();
    }

    internal Settings.PlayMode getPlayMode()
    {
        return playMode;
    }
    void setLaneColor(EventParam eventParam)
    {
        if (lanesEnd != null)
        for (int i = 0; i < lanesEnd.Length; i++)
        {
            lanesEnd[i].setLaneColor(AssetManager.instance.getHitBoxColor(false));
            //StringWriter result = new StringWriter();
            string letters = "QWERHJKL";
//            result.Write(letters[i-1];
//            aaa

            Debug.Log("Setting key for lane " + i + " " + (char)letters[(int)Math.Max(0, i - 1)]);
            //lanesEnd[i].setKey(Settings.instance.getLaneKeyCode(lanesEnd.Length, (char)letters[(int)Math.Max(0, i - 1)]));
            lanesEnd[i].setKey(Settings.instance.getLaneKeyCode(lanesEnd.Length, i));
        }
    }

    void setLaneLayout(EventParam eventParam)
    {
        //Debug.Log("Set Lane layout " + laneContainers[selectedLaneContainer] + " " + selectedLaneContainer);
        if (laneContainers[selectedLaneContainer] != null)
            laneContainers[selectedLaneContainer].SetActive(false);
        int i = eventParam.int1;
        if (i > laneContainers.Length)
            i = 0;

        HitChecker[] deactivateHC = laneContainers[selectedLaneContainer].GetComponentsInChildren<HitChecker>();
        foreach (HitChecker hc in deactivateHC)
            hc.reset();

        selectedLaneContainer = i;

        initLanes();
    }
    void setLaneSize(EventParam eventParam)
    {
        int i = eventParam.int1;
        if (i > laneEndSizes.Length)
            i = 0;
        selectedLaneEndSize = i;
        initLanes();
    }
    internal void initLanes()
    {
        laneContainers[selectedLaneContainer].SetActive(true);
        lanesEnd = laneContainers[selectedLaneContainer].GetComponentsInChildren<HitChecker>();
        int laneCount = lanesEnd.Length;

        if (lanesStart != null && lanesStart.Length > 0)
            foreach (GameObject g in lanesStart)
                Destroy(g);

        lanesStart = new GameObject[laneCount];

        for (int i = 0; i < lanesEnd.Length; i++)
        {
            lanesStart[i] = new GameObject("laneStart" + i);
            lanesStart[i].transform.position = lanesEnd[i].transform.position + laneStartOffset;
            lanesStart[i].transform.parent = lanesEnd[i].transform;

            lanesEnd[i].transform.parent.localScale = laneEndSizes[selectedLaneEndSize];
            //lanesEnd[i].transform.localScale = laneEndSizes[selectedLaneEndSize];

        }

        laneSpeed = new float[laneCount];
        timeToLaneEnd = new float[laneCount];
        totalLanes = lanesStart.Length;

        setLaneColor(null);

        setLanesPosition(Settings.instance.getLayout(), false);

        setLaneAudio(isEnabledLaneAudio());
        //Debug.Log("Lanes initialised");
    }
    internal void setLanesPosition(Vector3[] selectedLayout, bool changeNumLanes)
    {
        if (changeNumLanes && selectedLayout.Length != Settings.instance.getNumLanes() && Settings.instance.isSongPatternOverridesLanePositions())
        {
            Debug.Log("Song number of lanes (in lane_pos) is not matching current number of lanes, so switching");
            Settings.instance.setNumLanes(selectedLayout.Length);
            initLanes();
        }

        //Debug.Log("Setting lane positions " + selectedLayout.Length);
        for (int lane = 0; lane < selectedLayout.Length; lane++)
        {
            //Debug.Log("********************************** From " + lanesEnd[lane].transform.localPosition + " to " + selectedLayout[lane]);
            //lanesEnd[lane].transform.parent.position = selectedLayout[lane];
            lanesEnd[lane].moveLane(selectedLayout[lane]);
            //Debug.Log("Lane " + lane + " " + selectedLayout[lane]);
        }
    }
    internal void setLanesPosition(Vector3[] selectedLayout, float lerpTime)
    {
        for (int lane = 0; lane < selectedLayout.Length; lane++)
        {
            //Debug.Log("********************************** From " + lanesEnd[lane].transform.parent.localPosition + " to " + selectedLayout[lane] + " at " + lerpTime);
            lanesEnd[lane].transform.parent.localPosition = Vector3.Lerp(lanesEnd[lane].transform.parent.localPosition, selectedLayout[lane], lerpTime);
            //Debug.Log("Lane " + lane + " " + selectedLayout[lane]);
        }
    }
    internal Vector3[] getLanesPosition()
    {
        Vector3[] lanePos = new Vector3[lanesEnd.Length];
        for (int lane = 0; lane < lanePos.Length; lane++)
        {
            lanePos[lane] = lanesEnd[lane].transform.parent.localPosition;
            //Debug.Log("Lane " + lane + " " + selectedLayout[lane]);
        }

        return lanePos;
    }

    int getNextRowIndex(SongRow.Type type, float beforeTimestamp, int startIndex)
    {
        int result = -1;

        int index = startIndex;

        if (index >= 0 && index < songData.rows[type].Count) {
            SongRow songRow = songData.rows[type][index];
            if (songRow.timestamp >= 0 && songRow.timestamp < beforeTimestamp)
            {
                result = index;
            }
        }
        //Debug.Log("Next row " + type + " " + result + " " + songRows[type][index]);

        return result;
    }
    void calculateTimeToLaneEnd(float newBpm)
    {
        bpm = newBpm;
        EventManager.TriggerEvent(EventManager.EVENT_BPM_CHANGE, new BeatData(-1, -1, bpm));

        for (int i = 0; i < lanesEnd.Length; i++)
        {
            float laneDistance = Vector3.Distance(lanesEnd[i].transform.position, lanesStart[i].transform.position);
            laneSpeed[i] = Settings.instance.beatBoxBaseSpeed * bpm;// *bpm;// bpm* BEATBOX_BASE_SPEED;
            timeToLaneEnd[i] = laneDistance / laneSpeed[i];
            //Debug.Log("laneDist: " + laneDistance + " timeToLane: " + timeToLaneEnd[i] + " calcSpeed: " + laneSpeed[i]);
        }
    }
    public bool hasRecordedPattern()
    {
        return processedRecordedBeats.Count > 0;
    }
    public void saveRecording()//List<SongRow> rows)
    {
        if (processedRecordedBeats.Count > 0)
        {
            string user = "unknown";//OVRManager.profile.name
            string[] header = new string[] { "//generated by boxmania.club v" + Utils.gameVersion, "version=0", "description=Created by " + user + " for " + mediaSource.getName() };

            string fileName = SongData.getPatternFileName(mediaSource.getName(), lanesEnd.Length, SongData.PatternType.REC);
            string patternFilePath = Path.Combine(AssetManager.instance.getBaseFolder(), Path.Combine("patterns", fileName));
            SongData.storePattern(patternFilePath, songData.rows[SongRow.Type.BPM], songData.rows[SongRow.Type.BEAT], processedRecordedBeats, lanePosChanges);


            //UIManager.showNotification(10, "Saved song pattern", "Details" + mediaSource.ToString());


            EventManager.TriggerEvent(EventManager.EVENT_SHOW_NOTIFICATION, new EventParam(0,"Saved song pattern //TODO Share online as " + Utils.macToNicknameShort()));
            //uiManager.showNotification(10, "Saved song pattern //TODO Share online as " + Utils.macToNicknameShort(), "Details" + mediaSource.ToString());
            

        }
    }
    internal SongRow mergeRows(SongRow a, SongRow b)
    {
        for (int lane = 0; lane < a.activeLanes.Length; lane++)
        {
            int aLane = a.activeLanes[lane];
            int bLane = b.activeLanes[lane];
            int mLane = 0;

            if (aLane == 2 && bLane != 2)//exception for holding box
                mLane = bLane;
            else
            if (aLane > bLane)
                mLane = aLane;
            else
                mLane = bLane;

            a.activeLanes[lane] = mLane;
        }
        return a;
    }
    public void stop()
    {
        state = State.STOPPED;
        if (playMode == Settings.PlayMode.RECORD_PATTERN)
        {
                       foreach (SongRow row in processedRecordedBeats)
            {
                Debug.Log(Utils.showLanes(row.activeLanes) + " " + row.timestamp);
            }
            //Debug.Log("ENDSHOW");
            //TODO review all rows for maybe_holds. if maybe_hold, check subsequent row/lane until not a maybe_hold, then change all previous rows to the new value
            for (int i = 0; i < processedRecordedBeats.Count; i++)
            {
                int[] currentLanes = processedRecordedBeats[i].activeLanes;
                for (int lane = 0; lane < lanesEnd.Length; lane++)
                {
                    if (currentLanes[lane] == (int)SongRow.BoxType.MAYBE_HOLD)
                    {
                        int ind = i + 1;
                        if (ind < processedRecordedBeats.Count)
                        {
                            int[] nextLanes = processedRecordedBeats[ind].activeLanes;
                            while (nextLanes[lane] == (int)SongRow.BoxType.MAYBE_HOLD)//find end of maybe_hold
                            {
                                ind++;
                                if (ind < processedRecordedBeats.Count)
                                    nextLanes = processedRecordedBeats[ind].activeLanes;
                                else
                                    break;
                            }
                            for (int n = i; n < ind; n++)//found end, assign value to currentLane, lanes between i and ind need to be fixed too
                            {
                                processedRecordedBeats[n].activeLanes[lane] = nextLanes[lane];
                            }
                                //currentLanes[lane] = nextLanes[lane];
                            nextLanes[lane] = (int)SongRow.BoxType.NONE;//clear the ending row, now all previous lanes have been correctly set...?
                        }
                    }
                }
            }
            //TODO remove empty lanes that repeat timestamp

            Debug.Log("Recorded beats " + processedRecordedBeats.Count);
            //saveRecording(processedRecordedBeats);
            
        }


        if (mediaSource != null)
        {
            mediaSource.stop();
            if (mediaSource.unloadAudioData())
                Debug.Log("Clip data unloaded");

            //mediaSource.time = 0;
            Debug.Log("Stop playing " + mediaSource);
        } else
            Debug.Log("SPAWNER.STOP no source to stop");

        if (songData.rows != null && songData.rows[SongRow.Type.LANE_POS] != null && songData.rows[SongRow.Type.LANE_POS].Count > 0 && Settings.instance.isSongPatternOverridesLanePositions())//if song has lane positions x,y,z;x1,y1,z1;... and settings override is on,restore
            setLanesPosition(Settings.instance.getLayout(), true);//restore lane positions after we played a song with custom ones

        EventManager.TriggerEvent(EventManager.EVENT_DESTROY_BEATS, null);
    }
    public void setLaneAudio(bool laneAudio)//Settings.PlayMode playMode)
    {
        this.laneAudio = laneAudio;
        //foreach (HitChecker hc in lanesEnd)
        for (int lane = 0; lane < lanesEnd.Length; lane++)
        {
            if (laneAudio)
            {
                lanesEnd[lane].setAudioClips(new AudioClip[]{AssetManager.instance.getLaneAudio(0, lane),
                AssetManager.instance.getLaneAudio(1, lane)});
            }
            else
                lanesEnd[lane].setAudioClips(null);
        }
    }
    public bool isEnabledLaneAudio()
    {
        return laneAudio;
    }
    public void beginSongRecord()
    {
        this.playMode = Settings.PlayMode.RECORD_SONG;
    }
    public void previewSelectedSong(SongData songData = null)
    {
        if (songData == null)
            songData = Settings.instance.getSelectedSong();
        if (songData != null)// && songData.isDefaultSong())
        {
            mediaSource.loadData(songData, null);
            mediaSource.setVolume(0.3f);
            mediaSource.play();
        }
        //TODO preview non default songs...
        //else
        //    audioSource.clip = AssetManager.instance.getDefaultSong(0);
    }
    public void mediaPrepared()
    {
        //TODO Settings.instance.isSongPatternOverridesLanePositions() ignore lane_POS if not doing override
        //TODO if overrides, who handles number of lanes... should be user set, but if set to 4 and lane_pos has 6 pos, should we switch numlanes... mmm, yeah, if override, check first lane_pos length to change ui set
        Debug.Log("Media prepared");
        if (playMode == Settings.PlayMode.RECORD_PATTERN)
        {
            processedRecordedBeats.Clear();
            lanePosChanges.Clear();
        }

        if (playMode == Settings.PlayMode.TEST_PATTERN_PLAY)
        {//only thing needed to play test is replace loaded box type items to recordedBeats, then act as regular play
            Debug.Log("Skipping loaded BOXes (" + songData.rows[SongRow.Type.BOX].Count + ") to use previously recorded pattern (" + processedRecordedBeats.Count + ")");
            songData.rows[SongRow.Type.BOX] = processedRecordedBeats;
            songData.rows[SongRow.Type.LANE_POS] = lanePosChanges;//THIS IS RELATED TO THE FUTURE POS ISSUE
            playMode = Settings.PlayMode.PLAY;
        }

        Debug.Log("Spawner.play " + mediaSource.getName());
        Debug.Log(songData.rows[SongRow.Type.BEAT].Count + " beats, " + songData.rows[SongRow.Type.BOX].Count + " boxes, " + songData.rows[SongRow.Type.BPM].Count + " bpms, " + songData.rows[SongRow.Type.LANE_POS].Count + " lanePositions");

        foreach (HitChecker hc in lanesEnd)
        {
            hc.reset();
        }

        int firstBPMIndex = 0;// getFirstRowIndex(SongRow.Type.BPM);
        int firstBeatIndex = 0;// getFirstRowIndex(SongRow.Type.BOX);

        if (songData.rows[SongRow.Type.BPM].Count == 0)
            songData.rows[SongRow.Type.BPM].Add(new SongRow(0, 160));//set default bpm if there's none in the pattern (should not happen but...)
        //if (firstBPMIndex < 0 || firstBeatIndex < 0){
        if (songData.rows[SongRow.Type.BPM].Count == 0 || songData.rows[SongRow.Type.BOX].Count == 0)
        //if (songRows[SongRow.Type.BOX].Count == 0)
        {
            Debug.LogError("Can't play song, BOX types missing " + firstBPMIndex + " " + firstBeatIndex);
            state = State.STOPPED;
        }
        else
        {
            //SongRow firstBpm = songRows[firstBPMIndex];
            //SongRow firstBpm = songRows[SongRow.Type.BPM][0];

            float maxBpm = 80;
            foreach (SongRow row in songData.rows[SongRow.Type.BPM])
            {
                if (row.bpm > maxBpm)
                    maxBpm = row.bpm;
            }
            calculateTimeToLaneEnd(Mathf.Min(maxBpm, 240));
            //working assumption that first beat will reach lane end first (it won't if other lanes timeToEnd + second beat timestamp < first beat)
            //TODO a thourough check to find which songRow and lane is the first to end

            float soonestTimeToEnd = 100000;
            //SongRow firstBeat = songRows[firstBeatIndex];
            SongRow firstBeat = songData.rows[SongRow.Type.BOX][0];
            firstBeatTimestamp = firstBeat.timestamp;

            //TODO problem here is a beat on L8 is not registering on a layout 4 config
            for (int lane = 0; lane < timeToLaneEnd.Length; lane++)
                //if (firstBeat.activeLanes[lane] > 0 && soonestTimeToEnd > timeToLaneEnd[lane])
                //if (firstBeat.activeLanes[lane] > 0 && soonestTimeToEnd > timeToLaneEnd[lane])
                if (soonestTimeToEnd > timeToLaneEnd[lane])
                    soonestTimeToEnd = timeToLaneEnd[lane];

            Debug.Log("Soonest time to end " + soonestTimeToEnd + " " + firstBeat + " frameSeekOffset: " + frameSeekOffset);

            seekOffset = soonestTimeToEnd;
            state = State.SEEK_TO_PLAY;
        }

        if (state == State.SEEK_TO_PLAY)
        {
            if (songData.rows[SongRow.Type.LANE_POS].Count > 0 && Settings.instance.isSongPatternOverridesLanePositions())
            {//if first lane_pos row exists and has timestamp 0, and song pattern overrides lane pos, we init lane positions with its parameter
                SongRow firstLanePos = songData.rows[SongRow.Type.LANE_POS][0];
                if (firstLanePos.timestamp <= 0)
                {
                    //Debug.Log("7777777777777777777777777777777777 TO " + firstLanePos.lanePositions);
                    setLanesPosition(firstLanePos.lanePositions, true);
                }
            }
        }

        prevRegisteredBeats = new HitChecker.RegisteredBeat[lanesEnd.Length];
        for (int i = 0; i < lanesEnd.Length; i++)
            prevRegisteredBeats[i] = new HitChecker.RegisteredBeat(0, SongRow.BoxType.NONE);

        //audioSource.PlayDelayed(seekOffset);

        //return state == State.SEEK_TO_PLAY;
    }
    public bool begin(SongData songData, Settings.PlayMode playMode)// = Settings.PlayMode.PLAY)
    {
        state = State.INIT;
        this.playMode = playMode;
        this.songData = songData;
        //currentRow = 0;
        frameSeekOffset = 0;
        mediaSource.stop();
        bpmRowIndex = 0;
        boxRowIndex = 0;
        beatRowIndex = 0;
        lanPosRowIndex = 0;// 1;//skip the first lane defining lane pose changes
        recordingStartFlag = true;
        activatedLaneIndex = 0;
        lastHoldBox = new BeatBox[AssetManager.MAX_LANES];
        lastRegularBox = new BeatBox[AssetManager.MAX_LANES];
        playStartOffset = 0;

        mediaSource.loadData(songData, mediaPrepared);
        
        return true;
    }
    void readyToPlay()
    {
        state = State.PLAYING;
    }
    void Update()
    {

        float audioTime = mediaSource.getTime();// -playStartOffset;//TODO necessary to add playStartOffset? let's go with no, but it does affect the boxes that were spawned before clip start, so should add the offset once available, to existing boxes
        //Debug.Log("... " + frameSeekOffset + " " + seekOffset + " " + state);
        if (state != State.MODELLING)
        {
            //we check user lane pos drag in all states, for different use cases (eg. while idle just switch custom layouts, for recording save the new pos, etc.)
            bool lanePosChanged = false;
            //Vector3[] changedLanes = new Vector3[lanesEnd.Length];
            for (int lane = 0; lane < lanesEnd.Length; lane++)
            {
                HitChecker.RegisteredMove move = lanesEnd[lane].getRegisteredMove();
                if (move != null)
                {
                    lanePosChanged = true;
                    if (playMode == Settings.PlayMode.RECORD_PATTERN)
                    {
                        lanePosChanges.Add(new SongRow(getLanesPosition(), audioTime));
                        //Debug.Log("Recorded lane pos change at " + audioTime);
                    }
                    //changedLanes[lane] = move.position;
                }
                //else//non changing lanes we set to current position (this is saved as custom positions in UIManager)
                //    changedLanes[lane] = lanesEnd[lane].transform.position;
            }
            if (lanePosChanged && (state != State.PLAYING && state != State.SEEK_TO_PLAY))//only broadcast lane change in idle states, this changes the ui and settings, no need to do while playing/recording
                EventManager.TriggerEvent(EventManager.EVENT_USER_LANE_LAYOUT_CHANGE, null);//new LayoutParam(changedLanes));
        }

        if (state != State.SEEK_TO_PLAY && state != State.PLAYING)
            return;


        if (state == State.SEEK_TO_PLAY){//spawn the beats that have timestamp shorter than timeToLaneEnd
            if (recordingStartFlag && playMode == Settings.PlayMode.RECORD_PATTERN)
            {//spawn colored boxes when the song begins (well, seekOffset time before it begins)
                spawn(-1);
                recordingStartFlag = false;

                //TODO setting this initial position messes up the resulting pattern lanes pos?!?!

                //Vector3[] lanePos = Settings.instance.getLayout();//we also save current layout as starting one for the pattern
                lanePosChanges.Add(new SongRow(getLanesPosition(), 0));
            }
            frameSeekOffset += Time.deltaTime;
            //Debug.Log("... " + frameSeekOffset + " " + seekOffset);
            if (seekOffset <= frameSeekOffset)
            {
                Debug.Log("TimeToEnd offset is complete, starting clip. Offset " + seekOffset + " " + playMode);
                mediaSource.setVolume(0.9f);
                mediaSource.play();
                //state = State.READY_TO_PLAY;
                state = State.PLAYING;
                //TODO to keep things simple, make sure all lanes in a layout always have same distance
                frameSeekOffset = seekOffset;// timeToLaneEnd[0];//this works, as long as all lanes have same distance

            }
        }
        if (state == State.PLAYING){
            if (!mediaSource.isPlaying())
            {
                Debug.Log("Song end, stopping");
                EventManager.TriggerEvent(EventManager.EVENT_END_OF_SONG, null);
                stop();
                return;
            }
            //if (audioSource.time == 0)
            //    elapsedPlayTime += Time.deltaTime;
        }


        if (playMode == Settings.PlayMode.RECORD_PATTERN)
        {
            //TODO spawn line instead of ghostboxes
            /*
            int index = getNextRowIndex(SongRow.Type.BEAT, audioTime + frameSeekOffset, beatRowIndex);
            while (index >= 0)
            {
                //Debug.Log("BEAT at " + songRows[SongRow.Type.BEAT][index].timestamp + " " + time + " clip time " + audioSource.time);
                spawn(songRows[SongRow.Type.BEAT][index].timestamp);
                beatRowIndex = index + 1;
                index = getNextRowIndex(SongRow.Type.BEAT, audioTime + frameSeekOffset, beatRowIndex);

            }
             * */

            //lane pos change caused by switch button
            //TODO let's deactivate this, it's enough with drag?
            if (InputUtils.isCustomLayoutSwitchDown())
            {
                //Settings.instance.setCustomLayouts(!Settings.instance.isCustomLayouts());//already done in uimanager.update()
                //setLanesPosition(lanePos);//UIManager already takes care of position changes, just need to save the new pos for the recording
                Vector3[] lanePos = Settings.instance.getLayout();
                lanePosChanges.Add(new SongRow(lanePos, audioTime));
            }

            int activeLaneChanges = 0;

            //take hitcheckers registered beats and save/spawn
            float laneBoxAverageTime = 0;
            for (int lane = 0; lane < lanesEnd.Length; lane++)
            {
                HitChecker.RegisteredBeat beat = lanesEnd[lane].getRegisteredBeat();

                if (beat != null && beat.type != SongRow.BoxType.NONE)// && (prevRegisteredBeats[lane] == null || prevRegisteredBeats[lane].type == SongRow.BoxType.NONE))// || beat.timestamp > prevRegisteredBeats[lane].timestamp + Settings.instance.minTimeBetweenRegisteredBeats))//avoid duplicates
                {
                    //if (prevRegisteredBeats[lane] != null)
                    //    Debug.Log(beat.timestamp + " " + prevRegisteredBeats[lane].timestamp + " " + prevRegisteredBeats[lane].type);
                    //if (prevRegisteredBeats[lane].type != SongRow.BoxType.NONE && beat.timestamp < prevRegisteredBeats[lane].timestamp + Settings.instance.minTimeBetweenRegisteredBeats)
                    if (beat.timestamp < prevRegisteredBeats[lane].timestamp + Settings.instance.minTimeBetweenRegisteredBeats)
                    {
                        Debug.Log("Ignoring box!!!");
                        continue;
                    }

                    laneBoxAverageTime += beat.timestamp;
                    prevRegisteredBeats[lane].type = beat.type;
                    prevRegisteredBeats[lane].timestamp = beat.timestamp;
                    activeLaneChanges++;

                    int direction = SongRow.getDirection(beat.type);

                    if (direction >= 0)
                        spawn(lane, false, true, direction);
                    else
                    switch (beat.type)
                    {
                        case SongRow.BoxType.REGULAR:
                            spawn(lane, false, true);
                            break;
                        case SongRow.BoxType.HOLD:
                            //TODO keep holds array to extend trail
                            //use lastHoldBox[lane]?
                            BeatBox holdBeatBox = spawn(lane, false, true);//TODO spawn recording trails and stuff (currently just ghostbox + regular box)
                            holdBeatBox.holdTrail(1.5f, Color.grey);
                            break;
                    }
                }
                else
                {
                    prevRegisteredBeats[lane].type = SongRow.BoxType.NONE;
                    //prevRegisteredBeats[lane].timestamp = beat.timestamp;
                }
            }
            if (activeLaneChanges > 0)
            {
                //if (laneBoxAverageTime == 0)
                //    Debug.Log("0 TIMESTAMP!!!!!!!!");
                if (laneBoxAverageTime != 0)//ignore timestamp zeros, those are from... release? TODO, wtf?
                {
                    processedRecordedBeats.Add(new SongRow(laneBoxAverageTime / activeLaneChanges, prevRegisteredBeats));
                }
            }
        }

        lanePosChangeProgressTime += Time.deltaTime;
        if (mediaSource.isPlaying() && playMode == Settings.PlayMode.PLAY && mediaSource.getTime() <= 0)
        {
            playStartOffset += Time.deltaTime;
            Debug.Log("not playing yet?!?!?!?!?!?!?!?!? " + playStartOffset);
        }
        else
            if (playMode == Settings.PlayMode.PLAY || playMode == Settings.PlayMode.TEST_PATTERN_PLAY)
            {

                //adjust lane position smoothly
                if (lanePosChangeProgressTime <= totalDuration)
                    setLanesPosition(currentLaneDestination, lanePosChangeProgressTime / totalDuration);

                //check updated lane pos to set smooth change
                int index = getNextRowIndex(SongRow.Type.LANE_POS, audioTime, lanPosRowIndex);
                if (index >= 0)
                {
                    if (songData.rows[SongRow.Type.LANE_POS].Count > index + 1)
                    {
                        totalDuration = songData.rows[SongRow.Type.LANE_POS][index + 1].timestamp - songData.rows[SongRow.Type.LANE_POS][index].timestamp;
                        //Debug.Log("Total duration " + totalDuration);
                        lanePosChangeProgressTime = 0;
                        currentLaneDestination = songData.rows[SongRow.Type.LANE_POS][index].lanePositions;
                    }
                    else
                        setLanesPosition(songData.rows[SongRow.Type.LANE_POS][index].lanePositions, totalDuration);

                    lanPosRowIndex = index + 1;
                }

                //beat BPM change checks. using just for event notification to animators
                //TODO songRow types for animations (new segment?)...
                index = getNextRowIndex(SongRow.Type.BPM, audioTime + frameSeekOffset, bpmRowIndex);
                if (index >= 0)
                {
                    float bpm = songData.rows[SongRow.Type.BPM][index].bpm;
                    //calculateTimeToLaneEnd(Math.Max(bpm, 120));//doing this messes up the pattern (fast beats overtaking slow ones)
                    EventManager.TriggerEvent(EventManager.EVENT_BPM_CHANGE, new BeatData(-1, -1, bpm));
                    bpmRowIndex = index + 1;
                }

                //beat with chroma check
                int activatedLaneCount = 0;
                float searchAheadBeatTime = (audioTime + frameSeekOffset);

                index = getNextRowIndex(SongRow.Type.BOX, searchAheadBeatTime, boxRowIndex);
                if (index >= 0)
                {
                    //EventManager.TriggerEvent(EventManager.EVENT_BEAT, new BeatData(-1, -1, bpm));

                    SongRow songRow = songData.rows[SongRow.Type.BOX][index];
                    boxRowIndex = index + 1;
                    //int[] activeLanes = songRow.activeLanes;
                    for (int lane = 0; lane < timeToLaneEnd.Length; lane++)
                    {
                        if (songRow.activeLanes[lane] > 0)
                        {
                            //finds out if the lane will be at a different location when beat arrives, which should be songRow.timestamp + timeToLaneEnd
                            //TODO review the timing for this
                            //look at where the lane end might be at the time the box arrives (now + timeToLaneEnd)
                            int validFutureLanePosIndex = -1;
                            //int futureLanePosIndex = getNextRowIndex(SongRow.Type.LANE_POS, time - seekOffset + timeToLaneEnd[lane], lanPosRowIndex);
                            int futureLanePosIndex = getNextRowIndex(SongRow.Type.LANE_POS, songRow.timestamp, lanPosRowIndex);
                            while (futureLanePosIndex >= 0)
                            {
                                validFutureLanePosIndex = futureLanePosIndex;
                                //futureLanePosIndex = getNextRowIndex(SongRow.Type.LANE_POS, time - seekOffset + timeToLaneEnd[lane], futureLanePosIndex + 1);
                                futureLanePosIndex = getNextRowIndex(SongRow.Type.LANE_POS, songRow.timestamp, futureLanePosIndex + 1);
                            }

                            if (validFutureLanePosIndex >= 0){
                                Debug.Log("Beat should arrive at " + (songRow.timestamp + timeToLaneEnd[lane]));
                                Debug.Log("Closest position update  " + songData.rows[SongRow.Type.LANE_POS][validFutureLanePosIndex] + " index " + validFutureLanePosIndex + " at " + songData.rows[SongRow.Type.LANE_POS][validFutureLanePosIndex].timestamp);
                            }
                            Vector3 futureLanePos = Vector3.zero;
                            if (validFutureLanePosIndex >= 0)
                                futureLanePos = songData.rows[SongRow.Type.LANE_POS][validFutureLanePosIndex].lanePositions[lane];

                            //validFutureLanePosIndex = -1;
                            //validFutureLanePosIndex = -1;
                            //spawning a hold or regular note, with lane position at time of arrival (if it changes)
                            //notTODO it's not a bug, it's a feature: hold lanes get restarted at init of other lane changes. fixed (it was a timestamping, index fetching, nut sucking problem)
                            bool isHold = Settings.instance.difficultyTrailBoxes && songRow.activeLanes[lane] == 2;
                            if (isHold)
                            {
                                //we create new hold
                                if (lastHoldBox[lane] == null || !lastHoldBox[lane].isTrailActive())
                                {//don't feed trails to boxes that are already done!
                                    if (validFutureLanePosIndex >= 0)
                                        lastHoldBox[lane] = spawn(lane, futureLanePos);
                                    else
                                        lastHoldBox[lane] = spawn(lane, false, false);
                                    activatedLaneIndex += 2;

                                    Color laneColor = AssetManager.instance.getLaneColor(lane);
                                    laneColor.a = 0.2f;
                                    //we add the initial trail to the box
                                    if (songData.rows[SongRow.Type.BOX].Count > index + 1 && songData.rows[SongRow.Type.BOX][index + 1].activeLanes[lane] == (int)SongRow.BoxType.HOLD)
                                        lastHoldBox[lane].holdTrail(songData.rows[SongRow.Type.BOX][index + 1].timestamp - songRow.timestamp, laneColor);
                                    //else
                                    //    lastHoldBox[lane].holdTrail(audioSource.clip.length - songRow.timestamp, laneColor);//last hold with no other box to set finish time
                                }
                                else//we add trail to existing hold
                                {//index-1 should always be inside bounds, since it is a trail for previous row
                                    //if (songRow.timestamp - songRows[SongRow.Type.BOX][index - 1].timestamp > 0)//hmm...
                                    //{
                                        //lastHoldBox[lane].holdTrail(songRow.timestamp - songRows[SongRow.Type.BOX][index - 1].timestamp, AssetManager.instance.getLaneColor(lane));
                                    if (songData.rows[SongRow.Type.BOX].Count > index + 1 && songData.rows[SongRow.Type.BOX][index + 1].activeLanes[lane] == (int)SongRow.BoxType.HOLD)
                                        {
                                            //problem here is that last segment of trail addition addst time after trail should have endend
                                            lastHoldBox[lane].holdTrail(songData.rows[SongRow.Type.BOX][index + 1].timestamp - songRow.timestamp, Color.white);//AssetManager.instance.getLaneColor(lane) - new Color(0,0,0,0.6f));
                                            //lastHoldBox[lane].holdTrail(songRow.timestamp + songRows[SongRow.Type.BOX][index - 1].timestamp, Color.white);//AssetManager.instance.getLaneColor(lane) - new Color(0,0,0,0.6f));
                                            activatedLaneIndex += 3;
                                        } 
                                        //else
                                        //    lastHoldBox[lane] = null;//we can finish the trail here?
                                    //}
                                }
                                //TODO current//problem is that trail finishes at the right time, but beatbox still stays in place... should disable trail
                            }
                            else
                            {//non hold box
                                if (lastHoldBox[lane] != null)
                                    activatedLaneIndex += 4;
                                lastHoldBox[lane] = null;
                                
                                int direction = SongRow.getDirection(songRow.activeLanes[lane]);

                                if (validFutureLanePosIndex >= 0)
                                    lastRegularBox[lane] = spawn(lane, futureLanePos, direction);
                                else
                                    lastRegularBox[lane] = spawn(lane, false, false, direction);

                                //Debug.Log("Spawning note (test note at 3.5s) note.ts" + songRow.timestamp + " audiotime+offset" + time + " audiotime" + audioSource.time + " audiotime+offset+seekoffset " + (time + seekOffset) + "  with playOffset " + (audioSource.time + frameSeekOffset + playStartOffset));
                                //Debug.Log("Spawning note frameOffset " + frameSeekOffset + " seekOffset" + seekOffset);// + " audiotime" + audioSource.time + " audiotime+offset+seekoffset " + (time + seekOffset) + " -playOffset " + (audioSource.time - seekOffset + playStartOffset));

                                EventManager.TriggerEvent(EventManager.EVENT_BEAT_NOTE, new BeatData(lane, -1, bpm));

                                activatedLaneIndex += lane;
                                activatedLaneCount++;
                                //activatedTimestamp = songRow.timestamp;
                            }
                        }
                        else
                        {
                            if (lastHoldBox[lane] != null)
                            {//this lane is currently inactive, so no more trail to add to current trail box, next trail will be added to new box
                                lastHoldBox[lane] = null;
                                activatedLaneIndex += 5;
                            }
                            else
                                activatedLaneIndex += 6;
                        }
                    }

                    //index = getNextRowIndex(SongRow.Type.BOX, searchAheadBeatTime, boxRowIndex);
                }

                index = getNextRowIndex(SongRow.Type.BEAT, searchAheadBeatTime, beatRowIndex);
                if (index >= 0)
                {
                    EventManager.TriggerEvent(EventManager.EVENT_BEAT, new BeatData(-1, -1, bpm));
                    //difficulty +1 code
                    if (playMode == Settings.PlayMode.PLAY && Settings.instance.difficultyPlusOneBox)//ignore the option +1 when testing
                    {
                        //if (activatedLaneCount < Settings.instance.patternMaxActiveLanesAtOnce)
                        //if (activatedLaneCount == 0)
                        //{
                            int plusOneLane = (activatedLaneIndex + activatedLaneCount) % lanesEnd.Length;
                            //while (lastHoldBox[plusOneLane] != null)
                            //    plusOneLane = (++activatedLaneIndex + activatedLaneCount) % lanesEnd.Length;

                            if (lastRegularBox[plusOneLane] != null && searchAheadBeatTime - lastRegularBox[plusOneLane].timeToLaneEnd > 0.5f)
                                plusOneLane++;

                            if (plusOneLane < lanesEnd.Length)
                                spawn(plusOneLane, !Settings.instance.ghostBoxWithLaneColor);
                        //}
                    }
                    //EventManager.TriggerEvent(EventManager.EVENT_BEAT, new BeatData(-1, -1, bpm));
                    beatRowIndex = index + 1;
                    //index = getNextRowIndex(SongRow.Type.BEAT, audioTime, beatRowIndex);
                }

            }
    }

    public float getClipWithOffsetTimestamp()
    {
        if (mediaSource != null && mediaSource.isPlaying())
            return mediaSource.getTime() + seekOffset;
        else
            return frameSeekOffset;//-1
    }
    public float getClipTimestamp()
    {
        if (mediaSource != null && mediaSource.isPlaying())
            return mediaSource.getTime();
        else
            return 0;//-1
    }
    //spawn (ghost?) boxes to all lanes
    void spawn(int colorFromLane = -1)
    {//spawns 'ghost' boxes on all lanes to signify a beat (used for pattern creation)

        for (int lane = 0; lane < lanesEnd.Length; lane++)
        {

            Transform destTransform = lanesEnd[lane].transform;

            GameObject beatbox = null;
            if (colorFromLane < 0)
                beatbox = AssetManager.instance.getGhostBeatbox();
            else
                beatbox = AssetManager.instance.getBeatbox(colorFromLane);

            //HISTORIC_TODO bug in resize causing tiny scales on reused pool boxes -- that was due to the trails messing up the bounds
            Utils.resizeToMatch(beatbox, destTransform.gameObject);

            if (beatbox.transform.localScale.x < 0.05)
            {
                Debug.Log("Unexpected shrinking boxes!!!!");
                Debug.Log(beatbox.transform.localScale + " " + beatbox.transform.name + " " + destTransform.gameObject.transform.localScale);
            }
            //beatbox.transform.localScale *= 0.5f;
            beatbox.transform.position = lanesStart[lane].transform.position;

            BeatBox boxScript = beatbox.GetComponent<BeatBox>();
            //boxScript.init(laneStart[lane].transform.position, laneEnd[lane].transform.position, speed, timestamp);
            boxScript.init(lanesStart[lane].transform.position, lanesEnd[lane], timeToLaneEnd[lane], -1, false);

            //EventManager.TriggerEvent(EventManager.EVENT_BEAT, new BeatData(lane, -1, bpm));
        }
    }
    BeatBox spawn(int lane, Vector3 futureFinalDest, int direction = -1)
    {
        //Debug.Log("Spawning with futureLaneDest " + futureFinalDest);
        BeatBox box = spawn(lane, false, false, direction);
        box.setFutureLanePosition(futureFinalDest);
        return box;
    }
    BeatBox spawn(int lane, bool ghostBox = false, bool reverseDirection = false, int direction = -1)
    {//spawns beatbox in right lane with right color

        bool doGhostMiss = !ghostBox && !reverseDirection;

        Transform destTransform = lanesEnd[lane].transform;
        //Debug.Log("Instancing beat at lane " + lane + " timestamp " + arrivalTimestamp + " T2E" + timeToLaneEnd[lane]);

        GameObject beatbox = null;
        if (ghostBox)
            beatbox = AssetManager.instance.getGhostBeatbox();
        else
            beatbox = AssetManager.instance.getBeatbox(lane); 

        //ODO bug in resize causing tiny scales on reused pool boxes -- that was due to the trails messing up the bounds. still some Unexpected shrinking boxes traces
        Utils.resizeToMatch(beatbox, destTransform.gameObject);

        if (beatbox.transform.localScale.x < 0.05)
        {
            Debug.Log("Unexpected shrinking boxes!!!!");
            Debug.Log(beatbox.transform.localScale + " " + beatbox.transform.name + " " + destTransform.gameObject.transform.localScale);
        }
        //beatbox.transform.localScale *= 0.5f;
        beatbox.transform.position = lanesStart[lane].transform.position;

        BeatBox boxScript = beatbox.GetComponent<BeatBox>();
        //boxScript.init(laneStart[lane].transform.position, laneEnd[lane].transform.position, speed, timestamp);

        if (Settings.instance.difficultyDirectional)
            boxScript.init(lanesStart[lane].transform.position, lanesEnd[lane], timeToLaneEnd[lane], direction, doGhostMiss, reverseDirection);
        else
            boxScript.init(lanesStart[lane].transform.position, lanesEnd[lane], timeToLaneEnd[lane], -1, doGhostMiss, reverseDirection);

        //EventManager.TriggerEvent(EventManager.EVENT_BEAT, new BeatData(lane, -1, bpm));
        return boxScript;
    }

    void OnEnable()
    {
        //EventManager.StartListening(EventManager.EVENT_SONG_PLAY, playStop);
        //EventManager.StartListening(EventManager.EVENT_SONG_STOP, playStop);
        EventManager.StartListening(EventManager.EVENT_LANE_COLOR_CHANGE, setLaneColor);
        EventManager.StartListening(EventManager.EVENT_LANE_LAYOUT_CHANGE, setLaneLayout);
        EventManager.StartListening(EventManager.EVENT_LANE_SIZE_CHANGE, setLaneSize);
    }

    void OnDisable()
    {
        //EventManager.StopListening(EventManager.EVENT_SONG_PLAY, playStop);
        //EventManager.StopListening(EventManager.EVENT_SONG_STOP, playStop);
        EventManager.StopListening(EventManager.EVENT_LANE_COLOR_CHANGE, setLaneColor);
        EventManager.StopListening(EventManager.EVENT_LANE_LAYOUT_CHANGE, setLaneLayout);
        EventManager.StopListening(EventManager.EVENT_LANE_SIZE_CHANGE, setLaneSize);
    }

    internal float getSeekOffset()
    {
        return frameSeekOffset;
    }


    internal void setModelMode(bool p)
    {
        if (p)
            state = State.MODELLING;
        else
            state = State.STOPPED;

        foreach(HitChecker hc in lanesEnd){
            if (p)
                hc.setBlocks(new GameObject[]{AssetManager.instance.defaultBuildBlockPrefab});
            else
                hc.setBlocks(null);
        }
    }

    internal Settings.PlayMode getState()
    {
        return playMode;
    }
}
