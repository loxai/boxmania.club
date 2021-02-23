using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HitChecker : MonoBehaviour
{
    public const int LOW_PASS_RANGE = 21090;
    public const int HIGH_PASS_RANGE = 2190;

    ParticleSystem[] particleSys;

    float lastLaneHit = -10;//init with value higher than maxTimeDelta
    int lastLaneDirection = -1;
    float beatBoxArrivalTime = -10;
    float trailArrivalTime = -10;
    BeatBox beatBoxArrival = null;

    float maxTimeDelta = 0.1f;//TODO this is the time margin to register a hit success, also used for snap on beat recording, should be in Settings

    //private float vibrationDuration = 0.08f;
    //private float vibrationStrength = 0.4f;
    //float lastVibrationTime;
    float leftVibrationTime;
    float rightVibrationTime;
    //float beatBoxTimeFrame = 0.1f;
    bool checkingTrail;
    Material mat;

    Transform handTransform = null;
    GameObject spawningBlock = null;
    Vector3 prevPosition;

    //bool recording = false;
    RegisteredBeat registeredBeat = new RegisteredBeat(0, SongRow.BoxType.NONE);
    RegisteredBeat copiedBeat = new RegisteredBeat(0, SongRow.BoxType.NONE);

    RegisteredMove registeredMove = null;
    bool triggeredByRightController;

    private AudioSource audioSource;
    private AudioLowPassFilter audioLPF;
    private AudioHighPassFilter audioHPF;
    GameObject audioProgress;
    //bool isLooping;
    //bool isLoopPlaying;
    BeatPlayMode beatPlayMode = BeatPlayMode.TRIGGER_IMMEDIATE;
    enum BeatPlayMode
    {
        TRIGGER_IMMEDIATE,//once and out of sync
        TRIGGER_TO_BEAT,//once in beat sync start
        TRIGGER_IN_BEAT,//once starts and ends at beat
        LOOP_TO_BEAT,//loop cutting audio to beat duration
        LOOP_TO_AUDIO//loop at audio duration
    }

    bool isAlreadyInside = false;

    //TODO put the enables in a state enum
    bool isEnabledLaneAudio = false;
    bool isEnabledModelling = false;
    bool wasAudioTriggered = false;

    GameObject laneInfoUI;
    Text label;

    float beatDuration = 0.5f;//calculated from selected bpm (120bpm = 0.5secs per beat)
    float currentBeatProgress;

    GameObject blockPrefab;

    //float addedBeatDuration = 0;
    //int beatCyclesWhileInside = 0;
    //int tmpBeatCyclesWhileInside = 0;

    bool audioModSelected;

    public class RegisteredBeat
    {
        public float timestamp;
        public SongRow.BoxType type;

        public RegisteredBeat(float timestamp, SongRow.BoxType type)
        {
            this.timestamp = timestamp;
            this.type = type;
        }
        public void clear()
        {
            timestamp = 0;
            type = SongRow.BoxType.NONE;
        }
    }
    public class RegisteredMove
    {
        public float timestamp;
        public Vector3 position;

        public RegisteredMove(float timestamp, Vector3 position)
        {
            this.timestamp = timestamp;
            this.position = position;
        }
    }
    //public List<RecordedBeat> recordedBeats = new List<RecordedBeat>();

    public KeyCode actionKey = KeyCode.None;
    //TODO we are using keys as modifiers, which means we cannot (or very hard to) have different box types (on different lanes, eg. pressing two buttons) at the same time cos the modifier will affect both


    void Awake()
    {
        mat = gameObject.GetComponent<Renderer>().material;
        audioSource = GetComponent<AudioSource>();
        audioProgress = transform.parent.Find("progress").gameObject;

        audioLPF = GetComponent<AudioLowPassFilter>();
        audioHPF = GetComponent<AudioHighPassFilter>();


        //label = transform.parent.GetComponentInChildren<Text>(true);
        //label.enabled = false;

        laneInfoUI = AssetManager.instance.getLaneInfoUI();
        laneInfoUI.transform.position = transform.position;// +Vector3.up * 0.9f;
#if !UNITY_EDITOR && !UNITY_STANDALONE
        laneInfoUI.transform.parent = transform.parent;
#endif

        laneInfoUI.GetComponentInChildren<LaneBoxUIButton>(true).gameObject.SetActive(true);

#if UNITY_STANDALONE || UNITY_EDITOR
        laneInfoUI.GetComponentInChildren<LaneBoxUIButton>(true).setHitChecker(this);
        transform.parent.GetComponentInChildren<ModSelectable>(true).gameObject.SetActive(false);
        laneInfoUI.GetComponentInChildren<OVRRaycaster>(true).enabled = false;
#else
        transform.parent.GetComponentInChildren<ModSelectable>(true).gameObject.SetActive(true);
        transform.parent.GetComponentInChildren<ModSelectable>(true).setHitChecker(this);
        //laneInfoUI.GetComponentInChildren<LaneBoxUIButton>(true).gameObject.SetActive(false);
        laneInfoUI.GetComponentInChildren<GraphicRaycaster>(true).enabled = false;
#endif
        laneInfoUI.SetActive(false);

        //laneInfoUI.transform.parent = transform;//WARNING: if I do this, the UI doesn't work!
        label = laneInfoUI.transform.Find("info/LaneBoxButton/Text").GetComponent<Text>();
        //setLabelText();

        particleSys = GetComponentsInChildren<ParticleSystem>();
    }
    public void reset()
    {
        registeredBeat = new RegisteredBeat(0, SongRow.BoxType.NONE);
        checkingTrail = false;
        beatBoxArrival = null;
        lastLaneHit = -10;//init with value higher than maxTimeDelta
        beatBoxArrivalTime = -10;
        trailArrivalTime = -10;
        handTransform = null;
        spawningBlock = null;
        prevPosition = transform.position;
        beatPlayMode = BeatPlayMode.TRIGGER_IMMEDIATE;
        audioLoopOffsetTotal = 0;
        //addedBeatDuration = 0;
        //beatCyclesWhileInside = 0;
        //tmpBeatCyclesWhileInside = 0;
        wasAudioTriggered = false;
    }
    float currentTime()
    {
        if (Spawner.instance != null)
                return Spawner.instance.getClipWithOffsetTimestamp();
        return -1;
    }
    float seekOffset()
    {
        if (Spawner.instance != null)
            return Spawner.instance.getSeekOffset();
        return -1;
    }

    private void startParticles()
    {
        foreach (ParticleSystem ps in particleSys)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            ps.Play(true);
        }
    }
    public void beatBoxArrived(BeatBox beatBox)
    {
        beatBoxArrivalTime = currentTime();// Time.time;
        beatBoxArrival = beatBox;

        trailArrivalTime = beatBox.getTrailTime();

        checkingTrail = false;

        if (Settings.instance.difficultyTouchToHit)
        {
            if (isAlreadyInside)
            {
                beatBoxArrival.hit(1);

                beatBoxArrival = null;//TODO currently not checking trail in touchToHit mode
                //if (trailArrivalTime <= 0)//no trail, we can discard the box
                //    beatBoxArrival = null;
                //else
                //    checkingTrail = true;
                startParticles();
            }
            else
            {
                beatBoxArrival.miss();
                beatBoxArrival = null;
                beatBoxArrivalTime = -10;
            }
        }
    }
    internal void setLaneColor(Color color)
    {
        mat.SetColor("_BaseColor", color);
        //if (particleSys.Length > 0)
        //    particleSys[0].GetComponentInChildren<Renderer>().material.SetColor("_BaseColor", new Color(color.r, color.g, color.b));
    }
    internal void setKey(KeyCode keyCode)
    {
        actionKey = keyCode;
    }
    Vector3 grabOffset = Vector3.zero;
    float audioLoopOffsetTotal = 0;
    float audioLoopOffset = 0;

    //bool skipBeat;
    bool isNewBeat()
    {
        bool result = false;
        currentBeatProgress += Time.deltaTime;
        if (currentBeatProgress >= beatDuration)
        {
            currentBeatProgress = 0;
            result = true;
        }
        return result;
    }
    void setBeatDuration(EventParam eventParam)
    {
        currentBeatProgress = 0;
        beatDuration = 60f / ((BeatData)eventParam).bpm;
    }
    void setCrossFade(EventParam eventParam)
    {
        if (audioSource != null)
        {
            float channelDecrease = 0.5f;// +Mathf.Abs(eventParam.float1 / 100) * 0.5f;
            if (triggeredByRightController)
                channelDecrease -= (eventParam.float1 / 100) / 2;
            else
                channelDecrease += (eventParam.float1 / 100) / 2;
            audioSource.volume = 1 - channelDecrease;
            //Debug.Log("Right? " + triggeredByRightController + ", volume " + audioSource.volume);
        }
    }
    //float elapsedDragTime = 0.01f;
    Vector3 lastDragPosition;
    void Update()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        if (Input.GetKeyDown(actionKey))
        {
            laneTriggerDown(Input.GetKey(KeyCode.RightShift));
        }
        if (Input.GetKeyUp(actionKey))
        {
            laneTriggerUp();
        }
#endif

        if (beatBoxArrival != null)
        {
            playBeatBox(currentTime());
        }
        if (isAlreadyInside)//allow some recoding inputs while inside (hold, directional, regular through directional press?
        {
            if (InputUtils.getDirectionalDown(triggeredByRightController))
            {
                int direction = InputUtils.getDirectionalPress(triggeredByRightController);
                if (direction >= 0)
                {
                    //boxType = SongRow.BoxType.DIR_N + direction;
                    registeredBeat.type = SongRow.BoxType.DIR_N + direction;
                }
                else
                    registeredBeat.type = SongRow.BoxType.REGULAR;
            }
            if (InputUtils.isHoldKeyDown(triggeredByRightController))
            {
                registeredBeat.type = SongRow.BoxType.MAYBE_HOLD;
                registeredBeat.timestamp = currentTime() - seekOffset();
            }
        }
        if (isEnabledLaneAudio)
        {
            if ((isAlreadyInside || audioModSelected) && InputUtils.isLoopKeyDown(triggeredByRightController))
                switchLooping();

            if (isAlreadyInside || audioModSelected)
            {
                bool audioModifierKeys = InputUtils.isEffectBKeyPressed(triggeredByRightController) || InputUtils.isEffectAKeyPressed(triggeredByRightController);
                if (audioModifierKeys)
                    applyAudioMods();
            }
            float growth = audioSource.time / audioSource.clip.length;
            audioProgress.transform.localScale = new Vector3(growth, growth, growth);

            if (beatPlayMode == BeatPlayMode.TRIGGER_IMMEDIATE && wasAudioTriggered)
            {
                audioSource.Play();
                wasAudioTriggered = false;
            } else
            if (isNewBeat())
            {//in sync with bpm, so all samples start at the same time
                //instead of isLooping, 3 states: idle, loopToBeat, loopToAudio
                //also, trigger mode to ignore beat sync and play once at once

                //TODO option to set lane skip beats (loop clip stops and doesn't restart until n skips)
                //variable skips can create useful effects for drums/snares,...?
                //skipBeat = !skipBeat;
                //if (skipBeat)
                //{
                //    if (beatPlayMode == BeatPlayMode.LOOP_TO_BEAT)
                //    {
                //        audioSource.Stop();
                //    }
                //}
                //else
                switch (beatPlayMode)
                {
                    case BeatPlayMode.TRIGGER_IN_BEAT:
                        if (audioSource.isPlaying)//this syncs audio end with next beat
                            audioSource.Stop();
                        if (wasAudioTriggered)
                        {
                            audioSource.Play();
                            wasAudioTriggered = false;
                        }
                        break;
                    case BeatPlayMode.TRIGGER_TO_BEAT:
                        if (wasAudioTriggered)
                        {
                            audioSource.Play();
                            wasAudioTriggered = false;
                        }
                        break;
                    case BeatPlayMode.LOOP_TO_BEAT:
                        if (wasAudioTriggered)
                            audioSource.Play();
                        else
                            audioSource.Stop();
                        break;
                    case BeatPlayMode.LOOP_TO_AUDIO:
                        if (wasAudioTriggered && !audioSource.isPlaying)
                            audioSource.Play();
                        break;
                }

            }

        }

        //TODO?: this is so that boxes can be recorded while already inside the lane end (triggered)

        if (isEnabledModelling && spawningBlock != null && !InputUtils.isDragPressed(triggeredByRightController))
        {
            //save final position
            spawningBlock.transform.parent = null;
            spawningBlock.transform.eulerAngles = Vector3.zero;
            spawningBlock = null;
        }
        //checking user dragging the lane (or creating a build block)
        if (handTransform != null)
        {
            if (isEnabledModelling)
            {
                if (isAlreadyInside && InputUtils.isDragDown(triggeredByRightController))
                {
                    spawningBlock = Instantiate(blockPrefab);//FurnitureSpawner.spawn(f, blockPrefab);
                    spawningBlock.transform.position = handTransform.position;
                    spawningBlock.transform.parent = handTransform;
                    Utils.resizeToMatch(spawningBlock, gameObject);
                }
            }
            else
            {
                if (InputUtils.isDragDown(triggeredByRightController))
                {
                    grabOffset = -(transform.parent.position - handTransform.position) / 2;
                    lastDragPosition = Vector3.zero;
                }

                //if (grabOffset != Vector3.zero && InputUtils.isDragPressed(triggeredByRightController))
                if (grabOffset != Vector3.zero)
                    if (InputUtils.isDragPressed(triggeredByRightController))
                    {
                        //elapsedDragTime -= Time.deltaTime;
                        //if (true ||Vector3.Distance(transform.parent.position, movingTransform.position - grabOffset) > Settings.distanceToDetectDrag)//0.25f)
                        if (Vector3.Distance(lastDragPosition, handTransform.position - grabOffset) > Settings.distanceToDetectDrag)
                        {
                            registeredMove = new RegisteredMove(currentTime(), handTransform.position - grabOffset);
                            //Debug.Log("Registering move to " + (movingTransform.position - grabOffset));
                            //elapsedDragTime = Settings.instance.laneDragTimeLapse;
                            lastDragPosition = handTransform.position - grabOffset;
                        }
                        transform.parent.position = handTransform.position - grabOffset;
                    }
                    else
                    {
                        handTransform = null;
                        grabOffset = Vector3.zero;
                    }
            }
        }

        if (Settings.instance.vibrationDuration > 0 && rightVibrationTime > 0 && rightVibrationTime <= Time.time)
        {
            OVRInput.SetControllerVibration(1.0f, 0, OVRInput.Controller.RTouch);
            rightVibrationTime = 0;
        }
        if (Settings.instance.vibrationDuration > 0 && leftVibrationTime > 0 && leftVibrationTime <= Time.time)
        {
            OVRInput.SetControllerVibration(1.0f, 0, OVRInput.Controller.LTouch);
            leftVibrationTime = 0;
        }
    }

    void playBeatBox(float clipTime)
    {
        if (beatBoxArrival != null)
        {
            if (lastLaneHit > 0)//if there was a hit triggered in the lane (by key or beater)
            {
                float timeDelta = Mathf.Abs(lastLaneHit - beatBoxArrivalTime);
                //Debug.Log("Checking hit beatboxarrivaltime " + beatBoxArrivalTime + " vs lastLaneHit " + lastLaneHit + " delta " + timeDelta);
                if (timeDelta <= maxTimeDelta)
                {
                    if (beatBoxArrival.direction >= 0)
                    {
                        //Debug.Log(":::::::::lastLaneDirection " + lastLaneDirection + " beatBoxArrival.direction " + beatBoxArrival.direction);
                        if (lastLaneDirection != beatBoxArrival.direction)
                        {
                            beatBoxArrival.miss();
                            beatBoxArrival = null;
                            beatBoxArrivalTime = -10;
                        }
                        else
                        {
                            beatBoxArrival.hit(timeDelta / maxTimeDelta);

                            startParticles();
                        }
                    }
                    else
                    {
                        beatBoxArrival.hit(timeDelta / maxTimeDelta);
                        startParticles();
                    }

                    if (trailArrivalTime <= 0)//no trail, we can discard the box
                        beatBoxArrival = null;
                    else
                        checkingTrail = true;
                }
                lastLaneHit = -10;
            }

            if (!checkingTrail && beatBoxArrival != null && beatBoxArrivalTime < clipTime - maxTimeDelta )//past due time and has no trail, it's a miss
            {
                //Debug.Log("MISS! " + beatBoxArrivalTime + " " + (clipTime - maxTimeDelta));
                beatBoxArrival.miss();
                beatBoxArrival = null;
                beatBoxArrivalTime = -10;
            }
        }
        if (lastLaneHit > 0 && lastLaneHit < clipTime - maxTimeDelta)
        {
            lastLaneHit = -10;
        }
        

        if (checkingTrail)//this needs to be checked after main checks above
        {
            //at this point, controller should stay inside checker, otherwise ontriggerexit would cause a miss()
            trailArrivalTime -= Time.deltaTime;
            if (trailArrivalTime <= 0)
            {
                beatBoxArrival.trailHit();
                beatBoxArrival = null;
                checkingTrail = false;
            }
        }
    }
    private void setVibration(bool rightController)
    {
        OVRInput.SetControllerVibration(1.0f, Settings.instance.vibrationDuration, rightController ? OVRInput.Controller.RTouch : OVRInput.Controller.LTouch);
        if (rightController)
            rightVibrationTime = Time.time + Settings.instance.vibrationDuration;
        else
            leftVibrationTime = Time.time + Settings.instance.vibrationDuration;
    }
    internal RegisteredMove getRegisteredMove()
    {
        RegisteredMove tmp = registeredMove;
        registeredMove = null;
        return tmp;
    }
    internal RegisteredBeat getRegisteredBeat()
    {
        copiedBeat.timestamp = registeredBeat.timestamp;
        copiedBeat.type = registeredBeat.type;

        if (registeredBeat.type != SongRow.BoxType.HOLD)//we clear beat flag if it's not a hold type
            registeredBeat.clear();

        return copiedBeat;
    }
    void applyAudioMods()
    {
        float effectBLevel = -InputUtils.getEffectBKeyLevel(triggeredByRightController);
        float angleB = transform.eulerAngles.y;

        angleB += 90 * effectBLevel * Time.deltaTime;

        if (angleB < 0)
            angleB = 0;
        if (angleB > 45)
            angleB = 45;

        float effectALevel = -InputUtils.getEffectAKeyLevel(triggeredByRightController);
        float angleA = transform.eulerAngles.x;

        angleA += 90 * effectALevel * Time.deltaTime;

        if (angleA < 0)
            angleA = 0;
        if (angleA > 45)
            angleA = 45;

        //Debug.Log("EffectA " + effectBLevel + " angle " + yaw);
        transform.eulerAngles = new Vector3(angleA, angleB, transform.eulerAngles.z);

        audioLPF.cutoffFrequency = 22000 - LOW_PASS_RANGE * (angleB / 45);
        //Debug.Log(angleA)
        audioHPF.cutoffFrequency = 10 + HIGH_PASS_RANGE * ((angleA / 45));

        ////TODO effect B to automate effect A (sinus wave lfo)
        //audioSource.pitch = effectAValue;
        ////TODO bpm slider that sets sound loop max duration/length
    }
    void laneTriggerUp()
    {
        if (checkingTrail)
        {
            //Debug.Log("Trigger exit, missed trail, remaining trailArrivalTime " + trailArrivalTime + " " + beatBoxArrival.getTrailTime());
            beatBoxArrival.trailMiss();
            beatBoxArrival = null;
        }
        float time = currentTime() - seekOffset();

        checkingTrail = false;
        if (beatPlayMode != BeatPlayMode.LOOP_TO_AUDIO && beatPlayMode != BeatPlayMode.LOOP_TO_BEAT)
            mat.SetColor("_BaseColor", AssetManager.instance.getHitBoxColor(false));
        else
            mat.SetColor("_BaseColor", AssetManager.instance.getHitBoxLoopColor());

        if (registeredBeat.type == SongRow.BoxType.HOLD)
            registeredBeat.clear();
        //TODO maybe_hold should be deprecated, alway use action to enable hold, regular, directional, ...
        if (registeredBeat.type == SongRow.BoxType.MAYBE_HOLD)
        {
            if (time - registeredBeat.timestamp > 0.5f)
                registeredBeat.type = SongRow.BoxType.HOLD;
            else
                registeredBeat.type = SongRow.BoxType.REGULAR;
        }

        

        isAlreadyInside = false;

    }
    void switchLooping()
    {
        //currentBeatProgress = 0;
        if (beatPlayMode + 1 <= BeatPlayMode.LOOP_TO_AUDIO)
            beatPlayMode++;
        else
            beatPlayMode = 0;//BeatPlayMode.TRIGGER_IMMEDIATE;

        if (beatPlayMode != BeatPlayMode.LOOP_TO_AUDIO && beatPlayMode != BeatPlayMode.LOOP_TO_BEAT)
            mat.SetColor("_BaseColor", AssetManager.instance.getHitBoxColor(false));
        else
            mat.SetColor("_BaseColor", AssetManager.instance.getHitBoxLoopColor());

        setLabelText();
    }
    void laneTriggerDown(bool rightController)
    {
        if (isAlreadyInside)
            return;
        triggeredByRightController = rightController;

        isAlreadyInside = true;
        mat.SetColor("_BaseColor", AssetManager.instance.getHitBoxColor(true));
        //lastLaneHit = currentTime(false);
        lastLaneHit = currentTime();

        //Debug.Log("laneTriggerDown at " + lastLaneHit);

        SongRow.BoxType boxType = SongRow.BoxType.REGULAR;

        //TODO quite problematic to do calculation for hold... let's drop it for now and require action key
        if (InputUtils.isHoldKeyPressed(rightController))
            boxType = SongRow.BoxType.HOLD;

        int direction = InputUtils.getDirectionalPress(rightController);
        if (direction >= 0)
        {
            boxType = SongRow.BoxType.DIR_N + direction;
        }
        //if (InputUtils.isBlockHKeyPressed(rightController))
        //    boxType = SongRow.BoxType.BLOCK_H;
        //if (InputUtils.isBlockVKeyPressed(rightController))
        //    boxType = SongRow.BoxType.BLOCK_V;
        //if (InputUtils.isBombKeyPressed(rightController))
        //    boxType = SongRow.BoxType.BOMB;

        //TODO if recording beats...
        registeredBeat.timestamp = currentTime() - seekOffset();
        registeredBeat.type = boxType;

        //Debug.Log("TRIGGER DOWN " + registeredBeat.timestamp + " " + registeredBeat.type);
        if (isEnabledLaneAudio)
        {
            if (wasAudioTriggered)//triggered acting as a switch (for loop)
                wasAudioTriggered = false;
            else
                wasAudioTriggered = true;

            if (InputUtils.isLoopKeyPressed(rightController))
                switchLooping();

            
        }
    }
    public void setBlocks(GameObject[] gameObjects)
    {
        if (gameObjects != null)
        {
            blockPrefab = gameObjects[0];
        }
        isEnabledModelling = gameObjects != null;
        laneInfoUI.SetActive(gameObjects != null);
#if !UNITY_STANDALONE && !UNITY_EDITOR
        transform.parent.GetComponentInChildren<ModSelectable>(true).gameObject.SetActive(gameObjects != null);
#endif
        if (laneInfoUI.activeSelf)
        {
            setLabelText();
        }
    }
    //TODO handle array for different bank samples
    public void setAudioClips(AudioClip[] clips)
    {
        if (audioSource.clip != null)
            audioSource.clip.UnloadAudioData();

        if (clips != null)
        {
            audioSource.clip = clips[0];
            audioSource.clip.LoadAudioData();

            audioLPF.lowpassResonanceQ = Settings.instance.lowPassFilterResonance;
            audioHPF.highpassResonanceQ = Settings.instance.highPassFilterResonance;
        }

        isEnabledLaneAudio = clips != null;
        audioSource.enabled = isEnabledLaneAudio;
        audioHPF.enabled = isEnabledLaneAudio;
        audioLPF.enabled = isEnabledLaneAudio;
        laneInfoUI.SetActive(isEnabledLaneAudio);
#if !UNITY_STANDALONE && !UNITY_EDITOR
        transform.parent.GetComponentInChildren<ModSelectable>(true).gameObject.SetActive(isEnabledLaneAudio);
#endif
        if (laneInfoUI.activeSelf)
        {
            setLabelText();
        }
    }
    void setLabelText()
    {
        if (isEnabledLaneAudio && audioSource.clip != null)
            label.text = audioSource.clip.name + System.Environment.NewLine + beatPlayMode;

        if (isEnabledModelling && blockPrefab != null)
            label.text = blockPrefab.name;
    }
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Trigger enter " + other.tag);
        if (!isAlreadyInside)
        if (other.tag == "Right" || other.tag == "Left")
        {
            //Debug.Log("TRIGGERENTER " + other.tag + " " + other);
            //bool rightController = other.tag == "Right";
            triggeredByRightController = other.tag == "Right";
            setVibration(triggeredByRightController);
            laneTriggerDown(triggeredByRightController);

            Vector2 vec2 = new Vector2(transform.position.x - other.transform.position.x, transform.position.y - other.transform.position.y).normalized;

            lastLaneDirection = InputUtils.getDirectionalPress(vec2);
            //float hitAngle = Vector2.SignedAngle(Vector2.up, vec2);
            
            //lastLaneDirection = (int)(hitAngle / 8);
            //Debug.Log("ANGLE:::::::::::::::::::::::" + hitAngle + " " + lastLaneDirection);
            //Debug.Log("ANGLE:::::::::::::::::::::::B " + Vector2.Angle(Vector2.zero, vec2) + " " + Vector2.SignedAngle(Vector2.up, vec2) + " " + vec2);

                handTransform = other.transform;
        }
        else
        if (other.name == "main")//touch by another lane end (while moving)
        {
            //Debug.Log("Triggered by " + other.name);
            Vector3 force = transform.parent.position - other.transform.position;
            force.Normalize();
            transform.parent.position += force * Vector3.Distance(transform.parent.position, other.transform.position) / 2;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (isAlreadyInside)
        if (other.tag == "Right" || other.tag == "Left")
        {

            laneTriggerUp();

            handTransform = null;
        }
    }



    internal void moveLane(Vector3 pos)
    {
        transform.parent.localPosition = pos;
        laneInfoUI.transform.position = transform.position;

#if !UNITY_EDITOR && !UNITY_STANDALONE
        laneInfoUI.transform.parent = transform.parent;
#endif
        //   lanesEnd[lane].transform.parent.position = selectedLayout[lane];
    }

    internal void audioModsSelect(bool rightController)
    {
        triggeredByRightController = rightController;
        audioModSelected = true;
    }

    internal void audioModsDeselect()
    {
        audioModSelected = false;
    }
    void OnEnable()
    {
        EventManager.StartListening(EventManager.EVENT_BPM_CHANGE, setBeatDuration);
        EventManager.StartListening(EventManager.EVENT_CROSS_FADE_CHANGE, setCrossFade);
    }
    void OnDisable()
    {
        EventManager.StopListening(EventManager.EVENT_BPM_CHANGE, setBeatDuration);
        EventManager.StopListening(EventManager.EVENT_CROSS_FADE_CHANGE, setCrossFade);
    }
}
