using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

public class AssetManager : MonoBehaviour
{
    public const int MAX_LANES = 8;
    public const int POOL_SIZE = 80;
    string basePath;

    public static AssetManager _instance;
    public static AssetManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.Find("GameManager").GetComponent<AssetManager>();
            }

            return _instance;
        }
    }
    public Material skyboxMaterial;

    public AudioClip[] defaultSongs;
    public Theme defaultThemeA;
    public Theme defaultThemeB;
    Theme customTheme;

    Theme currentTheme;

    //GameObject mockPoolBeatBoxModel;
    //int selectedHandModel = 0;
    //int selectedSongClip = 0;

    public GameObject platformLocation;
    public GameObject notificationLocation;

    public Texture2D fallbackSkyboxTexture;
    //public GameObject fallbackPlatform;
    public GameObject fallbackNotification;
    //public Shader defaultColorShader;
    //public Shader defaultTextureShader;
    //public string defaultColorShaderName;
    //public string defaultTextureShaderName;
    //public Shader fallbackShader;
    public Material objMaterial;
    public GameObject rightControllerAnchor;
    public GameObject leftControllerAnchor;
    public GameObject[] handPrefabs;
    public GameObject ghostBoxPrefab;
    public GameObject laneInfoPrefab;
    public GameObject defaultBuildBlockPrefab;
    public GameObject directionPrefab;

    Dictionary<int, GameObject> notificationPool = new Dictionary<int, GameObject>();
    Dictionary<int, List<GameObject>> beatBoxPool = new Dictionary<int, List<GameObject>>();
    List<GameObject> ghostBoxPool = new List<GameObject>();
    List<GameObject> directionalPool = new List<GameObject>();
    

    public Material laneEndMaterial;
    public Material trailMaterial;

    public void Awake()
    {
        _instance = this;


        AudioClip[] songs = Resources.LoadAll<AudioClip>("songs");
        ArrayList tmp = new ArrayList();
        foreach (AudioClip clip in songs)
        {
            if (clip.name.Contains("TheFatRat"))
                tmp.Insert(0, clip);
            else
                tmp.Add(clip);
        }
        defaultSongs = (AudioClip[])tmp.ToArray(typeof(AudioClip));
        Debug.Log(defaultSongs.Length + " songs loaded");

        setupTheme(defaultThemeA, true);
        setHandModels(0);
    }
    public void setupTheme(Theme theme, bool destroyChildren)
    {
        if (currentTheme != null)
            currentTheme.freeUp();
        currentTheme = theme;
        Debug.Log("Setup theme " + currentTheme);

        skyboxMaterial.SetColor("_Tint", currentTheme.backgroundTint);
        skyboxMaterial.SetFloat("_Exposure", currentTheme.backgroundExposure);

        if (currentTheme.backgroundCubemap != null && currentTheme.backgroundCubemap.Length == 6)
        {
            skyboxMaterial.SetTexture("_FrontTex", currentTheme.backgroundCubemap[0]);
            skyboxMaterial.SetTexture("_BackTex", currentTheme.backgroundCubemap[1]);
            skyboxMaterial.SetTexture("_LeftTex", currentTheme.backgroundCubemap[2]);
            skyboxMaterial.SetTexture("_RightTex", currentTheme.backgroundCubemap[3]);
            skyboxMaterial.SetTexture("_UpTex", currentTheme.backgroundCubemap[4]);
            skyboxMaterial.SetTexture("_DownTex", currentTheme.backgroundCubemap[5]);
        }

        RenderSettings.fog = currentTheme.fog;
        RenderSettings.fogColor = currentTheme.fogColor;
        RenderSettings.fogDensity = currentTheme.fogDensity;

        setPlatform(destroyChildren);

        //laneEndMaterial.SetColor("_BaseColor", currentTheme.hitBoxColor);
        EventManager.TriggerEvent(EventManager.EVENT_LANE_COLOR_CHANGE, null);

        initNotificationPool();

        initBeatBoxPool();

        initGhostBeatBoxPool();

        initDirectionalPool();
        //poolBeatBoxes();
    }
    internal void setHandModels(int handModel){
        //remove existing models
        if (leftControllerAnchor.transform.childCount > 0)
            Destroy(leftControllerAnchor.transform.GetChild(0).gameObject);
        if (rightControllerAnchor.transform.childCount > 0)
            Destroy(rightControllerAnchor.transform.GetChild(0).gameObject);

        GameObject hand = Instantiate(handPrefabs[handModel]);
        hand.transform.position = leftControllerAnchor.transform.position;
        hand.transform.rotation = leftControllerAnchor.transform.rotation;
        hand.transform.Rotate(Vector3.right, 45);
        hand.transform.parent = leftControllerAnchor.transform;
        for (int i = 0; i < hand.transform.childCount; i++)
        {
            Transform child = hand.transform.GetChild(i);
            //if (child.GetComponent<Collider>() != null)
                child.tag = "Left";
        }

        hand = Instantiate(handPrefabs[handModel]);
        hand.transform.position = rightControllerAnchor.transform.position;
        hand.transform.rotation = rightControllerAnchor.transform.rotation;
        hand.transform.Rotate(Vector3.right, 45);
        //hand.transform.rotation
        hand.transform.parent = rightControllerAnchor.transform;
        for (int i = 0; i < hand.transform.childCount; i++)
        {
            Transform child = hand.transform.GetChild(i);
            //if (child.GetComponent<Collider>() != null)
            child.tag = "Right";
        }
        
    }
    internal void initNotificationPool()
    {
        for (int l = 0; l < notificationPool.Count; l++)
            if (notificationPool.ContainsKey(l))
                Destroy(notificationPool[l]);
        notificationPool.Clear();

        if (currentTheme.notifications == null || currentTheme.notifications.Length == 0)
            currentTheme.notifications = new GameObject[] { fallbackNotification };

        for (int i = 0; i < currentTheme.notifications.Length; i++)
        {
            if (currentTheme.notifications[i] == null)
                currentTheme.notifications[i] = fallbackNotification;

            GameObject notification = Instantiate(currentTheme.notifications[i]);
            notification.SetActive(false);
            notification.transform.position += notificationLocation.transform.position;
            notificationPool.Add(i, notification);
        }
    }

    internal void initDirectionalPool()
    {
        foreach (GameObject go in directionalPool)
            Destroy(go);
        directionalPool.Clear();

        for (int i = 0; i < POOL_SIZE; i++)
        {
            GameObject instance = Instantiate(directionPrefab);
            directionalPool.Add(instance);
            instance.SetActive(false);
        }

    }
    internal void initGhostBeatBoxPool()
    {
        foreach (GameObject go in ghostBoxPool)
            Destroy(go);
        ghostBoxPool.Clear();

        for (int i = 0; i < POOL_SIZE; i++)
        {
            GameObject instance = Instantiate(ghostBoxPrefab);
            instance.AddComponent<BeatBox>();
            ghostBoxPool.Add(instance);
            instance.SetActive(false);
        }

    }
    GameObject createBeatBoxInstance(int lane)
    {
        GameObject beatBox = Instantiate(currentTheme.beatBox);
        beatBox.SetActive(false);
        beatBox.AddComponent<BeatBox>();
        TrailRenderer trailRenderer = beatBox.AddComponent<TrailRenderer>();
        trailRenderer.enabled = false;
        trailRenderer.time = 0;

        Color color = currentTheme.beatBoxLanesTint[lane];
        Renderer[] renderers = beatBox.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            for (int n = 0; n < mats.Length; n++)
            {
                if (currentTheme.beatBoxTintMaterialIndex == null || currentTheme.beatBoxTintMaterialIndex.Contains(i + n))
                {
                    mats[n].SetColor("_BaseColor", color);
                    mats[n].SetColor("_Color", color);
                }
            }
        }
        return beatBox;
    }
    internal void initBeatBoxPool()//string p)
    {
        int maxLanes = currentTheme.beatBoxLanesTint.Length;//tint should always be init to max lanes (8)
        for (int l = 0; l < maxLanes; l++)
            if (beatBoxPool.ContainsKey(l))
                foreach (GameObject go in beatBoxPool[l])
                    Destroy(go);
        beatBoxPool.Clear();

        for (int l = 0; l < maxLanes; l++)
        {
            GameObject beatBox = createBeatBoxInstance(l);
            List<GameObject> list = new List<GameObject>();
            for (int p = 0; p < POOL_SIZE; p++)
            {
                GameObject go = Instantiate(beatBox);
                go.SetActive(false);


                list.Add(go);
            }
            list.Add(beatBox);//we add the template as well, why not?

            beatBoxPool.Add(l, list);
        }
    }
    internal GameObject getGhostBeatbox()
    {
        GameObject result;

        if (ghostBoxPool.Count > 0)
        {
            result = ghostBoxPool[0];
            ghostBoxPool.RemoveAt(0);
        }
        else
        {
            //Debug.Log("Not enough beatboxes pooled!");
            result = Instantiate(ghostBoxPrefab);
            result.AddComponent<BeatBox>();
        }
        result.SetActive(true);
        return result;
    }

    public GameObject getBeatbox(int lane)
    {
        GameObject result;
        List<GameObject> lanePool = beatBoxPool[lane];
        if (lanePool.Count > 0)
        {
            result = lanePool[0];
            lanePool.RemoveAt(0);
        }
        else
        {
            Debug.Log("Not enough beatboxes pooled!");
            result = createBeatBoxInstance(lane);
            //lanePool.Add(result);
        }
        result.SetActive(true);
        result.tag = lane.ToString();//saves keeping the lane num stored in the object when freeing
        return result;
    }
    public void freeBeatbox(GameObject beatbox)//free up uses the box tag (a number) to identify the lane pool it belongs to
    {
        beatbox.SetActive(false);
        int lane = -1;
        try
        {
            lane = int.Parse(beatbox.tag);
        }
        catch (Exception e)
        {
            //Debug.Log("Free beatbox tag is not a number, it's a ghost box!");
        }

        beatbox.transform.parent = null;
        if (lane >= 0)
            beatBoxPool[lane].Add(beatbox);
        else
            ghostBoxPool.Add(beatbox);
        //Destroy(beatbox);
    }
    public string getBaseFolder()
    {
        if (basePath == null)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            //Debug.Log("persistentDataPath " + Application.persistentDataPath);
            basePath = "/boxmaniaResources";
#else
            basePath = Application.persistentDataPath.Substring(0, Application.persistentDataPath.IndexOf("/Android")) + "/boxmaniaResources";
            //basePath = Path.Combine(Application.persistentDataPath, "boxmaniaResources");
#endif
        }

        //TODO add readme.txt with folder instructions

        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);
        if (!Directory.Exists(Path.Combine(basePath,"songs")))
            Directory.CreateDirectory(Path.Combine(basePath, "songs"));
        if (!Directory.Exists(Path.Combine(basePath, "patterns")))
            Directory.CreateDirectory(Path.Combine(basePath, "patterns"));
        if (!Directory.Exists(Path.Combine(basePath, "themes")))
            Directory.CreateDirectory(Path.Combine(basePath, "themes"));

        //Debug.Log("GetBasePath = " + basePath);
        return basePath;
    }

    internal GameObject getScoreNotification(int notificationLevel)
    {
        GameObject notification = notificationPool[notificationLevel];
        notification.SetActive(true);

        return notification;
    }

    internal void freeScoreNotification(GameObject currentNotification)
    {
        currentNotification.SetActive(false);
        //Destroy(currentNotification);
    }
    internal void destroyPlatformLocationChildren()
    {
        int childs = platformLocation.transform.childCount;
        for (int i = 0; i < childs; i++)
            Destroy(platformLocation.transform.GetChild(i).gameObject);

    }
    internal void setPlatform(bool destroyChildren)//string platform)
    {
        if (destroyChildren)
        {
            destroyPlatformLocationChildren();
        }


        if (currentTheme.platform != null)
        {
            Debug.Log("Creating platform instance from currentTheme " + currentTheme.name + " " + currentTheme.platform.name);
            GameObject platform = Instantiate(currentTheme.platform);
            platform.SetActive(true);
            platform.transform.position += platformLocation.transform.position;
            platform.transform.parent = platformLocation.transform;

        }

    }

    internal Color getHitBoxLoopColor()
    {
        return currentTheme.hitBoxLoopingColor;
    }
    internal Color getHitBoxColor(bool highlight)
    {
        if (highlight)
            return currentTheme.hitBoxHighlightColor;
        return currentTheme.hitBoxColor;
    }
    internal AudioClip getDefaultSong(int index)
    {
        //Debug.Log("************* ASSETMANAGER.getDefaultSong " + index);
        if (index > defaultSongs.Length)
            index = 0;
        return defaultSongs[index];
    }

    internal int defaultSongCount()
    {
        return defaultSongs.Length;
    }
    internal Color getLaneColor(int l)
    {
        return currentTheme.beatBoxLanesTint[l];
    }
    internal GameObject getLaneInfoUI()
    {
        GameObject result = GameObject.Instantiate(laneInfoPrefab);

#if UNITY_STANDALONE || UNITY_EDITOR
        result.GetComponentInChildren<OVRRaycaster>().enabled = false;
        result.GetComponentInChildren<GraphicRaycaster>().enabled = true;
#else
        result.GetComponentInChildren<GraphicRaycaster>().enabled = false;
        result.GetComponentInChildren<OVRRaycaster>().enabled = true;
#endif

        return result;
    }

    public AudioClip[] defaultLaneAudio;
    internal AudioClip getLaneAudio(int bank, int lane)
    {
        return defaultLaneAudio[lane];
    }

    internal GameObject getBuildBlock()
    {
        return Instantiate(defaultBuildBlockPrefab);
    }

    internal GameObject getDirectionalObject()
    {
        GameObject result;

        if (directionalPool.Count > 0)
        {
            result = directionalPool[0];
            directionalPool.RemoveAt(0);
        }
        else
        {
            //Debug.Log("Not enough beatboxes pooled!");
            result = Instantiate(directionPrefab);
        }
        result.SetActive(true);
        return result;
    }

    internal void freeDirectional(GameObject directional)
    {
        directional.transform.parent = null;
        directional.SetActive(false);

        directionalPool.Add(directional);
    }
}
