using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Settings : MonoBehaviour
{
    public enum PlayMode
    {
        PLAY,
        RECORD_PATTERN,
        TEST_PATTERN_PLAY,
        RECORD_SONG,
        CREATE_BOSS//?
    }

    public static Settings _instance;
    public static Settings instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.Find("GameManager").GetComponent<Settings>();
            }

            return _instance;
        }
    }

    KeyCode[] laneKeyCodes = new KeyCode[] { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, };

    public string serverPatternSharing = @"http:\\boxmania.club\folder.php";

    public float vibrationDuration = 0.28f;
    public float vibrationStrength = 0.4f;

    public SongData.PatternType selectedPattern = SongData.PatternType.AUTO;
    public int difficultyLevel;

    internal bool difficultyTrailBoxes = false;
    internal bool difficultyPlusOneBox = false;
    //internal bool difficultyObstacles = false;
    internal bool difficultyDamage = false;
    internal bool difficultyTouchToHit = false;//when true, hitchecker only checks if it is currently triggered to accept a successful hit
    internal bool difficultyDirectional = true;

    bool songPatternOverridesLanePositions = true;//TODO songLoader to check for Vector3 lane positions. WARNING: after song finishes, should restore previous lane layout (default or custom)
    public bool isSongPatternOverridesLanePositions(){
        return songPatternOverridesLanePositions;
    }
    internal bool ghostBoxWithLaneColor = true;

    internal float beatBoxBaseSpeed = 0.05f;//0.025
    SongData selectedSong = null;
    int numLanesIndex;
    bool usingCustomLayouts;
    int size;
    int beater;
    int theme = 0;

    public float trailRendererWidth = 0.15f;
    //remember, these need to be changed in editor, not here
    public int patternMaxActiveLanesAtOnce = 2;//boxes spawning at the same time
    public bool patternUseHold = false;
    public bool patternUseOnsets = false;
    public bool patternUseMines = false;
    public float patternHoldMinTimestamp = 0.21f;
    public bool patternOverwriteAuto = true;
    public bool patternRecordSnapToBeat = false;


    public float patternMergeTimestampsDelta = 0.1f;

    public GameObject pcCamera;
    public GameObject vrCamera;
    public GameObject pcPointer;
    public GameObject vrPointer;
    public GameObject[] uiVRRaycasterRoot;

    public GameObject[] pcDisabledObjects;
    //internal bool PCMode = false;
    void Awake()
    {
        _instance = this;
        custom4LanesLayout = new Vector3[] { default4LanesLayout[0] * customLayoutFactor, default4LanesLayout[1] * customLayoutFactor, default4LanesLayout[2] * customLayoutFactor, default4LanesLayout[3] * customLayoutFactor };//new Vector3(111,111,111),
        custom6LanesLayout = new Vector3[] { default6LanesLayout[0] * customLayoutFactor, default6LanesLayout[1] * customLayoutFactor, default6LanesLayout[2] * customLayoutFactor, default6LanesLayout[3] * customLayoutFactor, default6LanesLayout[4] * customLayoutFactor, default6LanesLayout[5] * customLayoutFactor };
        custom8LanesLayout = new Vector3[] { default8LanesLayout[0] * customLayoutFactor, default8LanesLayout[1] * customLayoutFactor, default8LanesLayout[2] * customLayoutFactor, default8LanesLayout[3] * customLayoutFactor,
                                                            default8LanesLayout[4] * customLayoutFactor, default8LanesLayout[5] * customLayoutFactor, default8LanesLayout[6] * customLayoutFactor, default8LanesLayout[7] * customLayoutFactor };

#if UNITY_STANDALONE || UNITY_EDITOR
        pcCamera.SetActive(true);
        vrCamera.SetActive(false);
        pcPointer.SetActive(true);
        vrPointer.SetActive(false); 
        Camera.SetupCurrent(pcCamera.GetComponent<Camera>());

        //remove OVR references in PC mode for all UI
        foreach (GameObject go in uiVRRaycasterRoot)
        {
            OVRRaycaster[] raycasters = go.GetComponentsInChildren<OVRRaycaster>(true);
            foreach (OVRRaycaster raycaster in raycasters)
                raycaster.enabled = false;
        }
        //remove OVR references in PC mode for UI in lanes
        //raycasters = transform.parent.transform.Find("Locations").GetComponentsInChildren<OVRRaycaster>(true);
        //foreach (OVRRaycaster raycaster in raycasters)
        //    raycaster.enabled = false;

        foreach (GameObject go in pcDisabledObjects)
            go.SetActive(false);


#else
        pcCamera.SetActive(false);
        vrCamera.SetActive(true);
        pcPointer.SetActive(false);
        vrPointer.SetActive(true);
#endif
    }

    internal SongData getSelectedSong()//null means it is a default song
    {
        if (selectedSong == null)
        {
            //int rnd = (int)(Random.value * (AssetManager.instance.defaultSongCount() - 1));
            int rnd = (int)(Random.value * 14);
            AudioClip clip = AssetManager.instance.getDefaultSong(rnd);
            selectedSong = new SongData(clip.name, rnd);
        }
        return selectedSong;
    }
    internal void setSelectedSong(SongData songData)
    {
        this.selectedSong = songData;
    }
    internal Vector3[] getLayout()
    {
        if (selectedLayout == null)
            setNumLanesIndex(getNumLanesIndex());
        return selectedLayout;
    }
    private Vector3[] default4LanesLayout = new Vector3[] { new Vector3(-0.5f, -0.211f, 0.593f), new Vector3(-0.166f, -0.211f, 0.593f), new Vector3(0.166f, -0.211f, 0.593f), new Vector3(0.5f, -0.211f, 0.593f) };//new Vector3(111,111,111),
    //private Vector3[] default6LanesLayout = new Vector3[] { new Vector3(-0.6f, -0.2f, 0.3f), new Vector3(-0.4f, -0.1f, 0.4f), new Vector3(-0.2f, 0, 0.5f), new Vector3(0.2f, 0, 0.5f), new Vector3(0.4f, -0.1f, 0.4f), new Vector3(0.6f, -0.2f, 0.3f)};
    private Vector3[] default6LanesLayout = new Vector3[] { new Vector3(-0.6f, -0.3f,0f), new Vector3(-0.25f, 0f, 0.2f), new Vector3(-0.4f, 0.4f, -0.05f), new Vector3(0.4f, 0.4f, -0.05f), new Vector3(0.25f, 0f, 0.2f), new Vector3(0.6f, -0.3f, 0f) };
    //private Vector3[] default8LanesLayout = new Vector3[] { new Vector3(-0.6f, -0.211f, 0.4f), new Vector3(-0.3f, -0.211f, 0.6f), new Vector3(0.3f, -0.211f, 0.6f), new Vector3(0.6f, -0.211f, 0.4f), new Vector3(-0.5f, 0.05f, 0.4f), new Vector3(-0.15f, 0.05f, 0.52f), new Vector3(0.15f, 0.05f, 0.52f), new Vector3(0.5f, 0.05f, 0.4f) };
    private Vector3[] default8LanesLayout = new Vector3[] { new Vector3(-0.7f, 0.1f, 0.2f), new Vector3(-0.25f, 0.15f, 0.4f), new Vector3(0.25f, 0.15f, 0.4f), new Vector3(0.7f, 0.1f, 0.2f),
                                                            new Vector3(-0.65f, -0.25f, 0.2f), new Vector3(-0.25f, -0.25f, 0.52f), new Vector3(0.15f, -0.25f, 0.52f), new Vector3(0.65f, -0.25f, 0.2f) };

    //private Vector3[] custom4LanesLayout = new Vector3[] { new Vector3(-0.65f, -0.2f, 0.6f), new Vector3(-0.166f, -0.2f, 0.6f), new Vector3(0.166f, -0.2f, 0.6f), new Vector3(0.65f, -0.2f, 0.6f) };//new Vector3(111,111,111),
    //private Vector3[] custom6LanesLayout = new Vector3[] { new Vector3(-0.7f, -0.2f, 0.2f), new Vector3(-0.4f, -0.2f, 0.4f), new Vector3(-0.2f, 0, 0.5f), new Vector3(0.2f, 0, 0.5f), new Vector3(0.4f, -0.2f, 0.4f), new Vector3(0.7f, -0.2f, 0.2f) };
    //private Vector3[] custom8LanesLayout = new Vector3[] { new Vector3(-0.6f, -0.3f, 0.4f), new Vector3(-0.3f, -0.3f, 0.6f), new Vector3(0.3f, -0.3f, 0.6f), new Vector3(0.6f, -0.3f, 0.4f), new Vector3(-0.5f, 0.25f, 0.4f), new Vector3(-0.15f, 0.25f, 0.52f), new Vector3(0.15f, 0.25f, 0.52f), new Vector3(0.5f, 0.25f, 0.4f) };

    const float customLayoutFactor = 1.2f;
    private Vector3[] custom4LanesLayout = null;
    private Vector3[] custom6LanesLayout = null;
    private Vector3[] custom8LanesLayout = null;

    Vector3[] selectedLayout = null;
    public static float distanceToDetectDrag = 0.025f;
    internal float lowPassFilterResonance = 3;
    internal float highPassFilterResonance = 3;
    public bool autoPatternUseBeats = true;//changing this means stored auto patterns are outdated and need to be redone
    internal float minTimeBetweenRegisteredBeats = 0.1f;
    //public float beatChromaTimeDelta = 0.05f;//set a timeframe by which beat and chroma feature can intersect
    //public float laneDragTimeLapse = 0.1f;//the sampling time lapse between registering lane position drag
    internal Vector3[] setLanesPosition(bool customLayouts)
    {
        //Debug.Log("SetLanes " + customLayouts + " num lanes " + getNumLanes());
        this.usingCustomLayouts = customLayouts;

        switch (numLanesIndex)
        {
            case 0:
                selectedLayout = customLayouts ? custom4LanesLayout : default4LanesLayout;
                break;
            case 1:
                selectedLayout = customLayouts ? custom6LanesLayout : default6LanesLayout;
                break;
            case 2:
                selectedLayout = customLayouts ? custom8LanesLayout : default8LanesLayout;
                break;
        }
        return selectedLayout;
    }
    internal void setNumLanesIndex(int numLanesIndex)//numLanes is 4, 6 or 8
    {
        //Debug.Log("Set Num lanes index " + numLanesIndex);
        this.numLanesIndex = numLanesIndex;
        setLanesPosition(usingCustomLayouts);//just to refresh selectedLayout array

        EventManager.TriggerEvent(EventManager.EVENT_LANE_LAYOUT_CHANGE, new EventParam(numLanesIndex));
    }
    internal void setNumLanes(int numLanes)
    {
        int num = 0;
        switch (numLanes)
        {
            case 4:
                num = 0;
                break;
            case 6:
                num = 1;
                break;
            case 8:
                num = 2;
                break;
        }
        setNumLanesIndex(num);
    }
    internal int getNumLanesIndex()
    {
        return numLanesIndex;
    }
    internal int getNumLanes()
    {
        switch (numLanesIndex)
        {
            case 0:
                return 4;
            case 1:
                return 6;
            case 2:
                return 8;
        }
        return 4;
    }
    
    internal void setHitBoxSize(int size)
    {
        this.size = size;
        EventManager.TriggerEvent(EventManager.EVENT_LANE_SIZE_CHANGE, new EventParam(size));
    }
    internal void setBeater(int beater)
    {
        this.beater = beater;
        AssetManager.instance.setHandModels(beater);
    }


    internal KeyCode getLaneKeyCode(int totalLanes, int lane)
    {
        Debug.Log("BUUUUUUUUUUG " + totalLanes + " lane " + lane + " laneKeyCodes.lentgh " + laneKeyCodes.Length);
        return laneKeyCodes[lane];
    }


    internal int getBeater()
    {
        return beater;
    }

    internal int getHitBoxSize()
    {
        return size;
    }
    internal int getTheme()
    {
        return theme;
    }
    internal void setDefaultThemeA()
    {
        theme = 0;
        AssetManager.instance.setupTheme(AssetManager.instance.defaultThemeA, true);
    }
    internal void setDefaultThemeB()
    {
        theme = 1;
        AssetManager.instance.setupTheme(AssetManager.instance.defaultThemeB, true);
    }
    internal void setDefaultThemeC()
    {
        theme = 2;
        //AssetManager.instance.setupTheme(AssetManager.instance.defaultThemeC);
    }
    internal bool setCustomTheme()
    {
        bool result = false;
        theme = 3;
        string path = Path.Combine(AssetManager.instance.getBaseFolder(), "themes/customTheme/settings.thm");

        if (result = File.Exists(path))
        {
//            AssetManager.instance.destroyPlatformLocationChildren();
            Theme loadedTheme = Theme.load(path, AssetManager.instance.platformLocation.transform);//this theme loading instances its own platform stuff, so we need to chear the platform first
            AssetManager.instance.setupTheme(loadedTheme, false);//don't delete platform location content since we instanced it in the custom theme loading process
        }

        return result;
    }

    internal bool isUsingCustomLayouts()
    {
        return usingCustomLayouts;
    }
    internal void setUsingCustomLayouts(bool customLayouts)
    {
        Debug.Log("setUsingCustomLayouts to " + customLayouts);
        this.usingCustomLayouts = customLayouts;
    }
    internal void setCustomLayout(Vector3[] pos)
    {
        switch (pos.Length)
        {
            case 4:
                custom4LanesLayout = pos;
                break;
            case 6:
                custom6LanesLayout = pos;
                break;
            case 8:
                custom8LanesLayout = pos;
                break;
        }
    }
}
