using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Main : MonoBehaviour
{
    //public HandManager leftHand;
    //public HandManager rightHand;

    //public GameObject platformLocation;

    // Start is called before the first frame update
    void Start()
    {
        //rightHand.setHandleLength(0.3f);
        //leftHand.setHandleLength(0.3f);

        //AssetManager.instance.setupTheme(AssetManager.instance.defaultThemeB);
        //AssetManager.instance.setupTheme(Theme.load(Path.Combine(AssetManager.instance.getBaseFolder(), "sampleTheme/sampleTheme.thm")));

//#if !UNITY_EDITOR
//        EventManager.TriggerEvent(EventManager.EVENT_SONG_PLAY_STOP, null);//new SongParam(Path.Combine(AssetManager.instance.getBaseFolder(), "Pixelland.mp3")));
//#endif
    }
    /*
    void loadPlatform(EventParam eventParam)
    {
        int childs = platformLocation.transform.childCount;
        for (int i = 0; i < childs; i++)
            Destroy(platformLocation.transform.GetChild(i).gameObject);

        GameObject platform = AssetManager.instance.getPlatform(eventParam.string1);
        platform.transform.position = platformLocation.transform.position;
        platform.transform.parent = platformLocation.transform;
    }
    void loadBeatBox(EventParam eventParam)
    {
        AssetManager.instance.poolBeatBoxes(eventParam.string1);
    }
    void setFog(EventParam eventParam)
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.white;
        RenderSettings.fogDensity = 0.06f;
    }
     
    void setCubeMap(EventParam eventParam)
    {
        //byte[] binFile = ConfigScript.getInstance().getSelectedBackgroundImage();
        //skyboxTexture = new Texture2D(2, 2);
        //skyboxTexture.LoadImage(binFile);

        //skyboxMaterial.SetTexture("_Tex", skyboxTexture);
        //skyboxMaterial.SetColor("_Color", Color.cyan);
    }
     * */
    /*
    void OnEnable()
    {
        EventManager.StartListening(EventManager.EVENT_LOAD_PLATFORM, loadPlatform);
        EventManager.StartListening(EventManager.EVENT_LOAD_BEAT_BOX, loadBeatBox);
        
    }

    void OnDisable()
    {
        EventManager.StopListening(EventManager.EVENT_LOAD_PLATFORM, loadPlatform);
        EventManager.StopListening(EventManager.EVENT_LOAD_BEAT_BOX, loadBeatBox);
    }
     * */
}
