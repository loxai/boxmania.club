using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Theme : MonoBehaviour
{
    public GameObject beatBox;
    public GameObject platform;
    public GameObject[] notifications;//0 miss, 5 best

    public Texture2D[] backgroundCubemap;

    public float backgroundExposure = 0.2f;
    public Color backgroundTint = Color.white;

    public Color[] beatBoxLanesTint = new Color[] { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan, Color.white, Color.black };
    public Color hitBoxColor = new Color(1, 1, 1, 0.3f);//new Color[] { Color.red, Color.green, Color.blue, Color.yellow };
    public Color hitBoxHighlightColor = new Color(1, 0, 0, 0.3f);//new Color[] { Color.red, Color.green, Color.blue, Color.yellow };
    public Color hitBoxLoopingColor = new Color(0.8f, 0.2f, 0.2f, 0.3f);//new Color[] { Color.red, Color.green, Color.blue, Color.yellow };
    
    public List<int> beatBoxTintMaterialIndex;

    public bool fog = false;
    public Color fogColor = Color.black;
    public float fogDensity = 0.001f;
    bool fromFile;

    string name = "default";
    public override string ToString()
    {
        return base.ToString() + name;
    }
    public static Theme load(string path, Transform parentTransform)
    {
        AssetManager.instance.destroyPlatformLocationChildren();
        Theme result = new Theme();
        result.fromFile = true;
        result.name = "/" + Path.GetFileNameWithoutExtension(path);
        Dictionary<string, string> themeParams = Utils.readDictionaryFile(path);

        string themePath = Path.GetDirectoryName(path);

        //Debug.Log("theme path " + themePath);

        loadLaneColor(result, 0, themeParams);
        loadLaneColor(result, 1, themeParams);
        loadLaneColor(result, 2, themeParams);
        loadLaneColor(result, 3, themeParams);
        loadLaneColor(result, 4, themeParams);
        loadLaneColor(result, 5, themeParams);
        loadLaneColor(result, 6, themeParams);
        loadLaneColor(result, 7, themeParams);

        if (themeParams.ContainsKey("hitBoxColor"))
        {
            result.hitBoxColor = Utils.readColor(themeParams["hitBoxColor"], 0.3f);
        }
        if (themeParams.ContainsKey("hitBoxHighlightColor"))
        {
            result.hitBoxHighlightColor = Utils.readColor(themeParams["hitBoxHighlightColor"], 0.6f);
        }
        if (themeParams.ContainsKey("beatBoxTintIndex"))
        {
            result.beatBoxTintMaterialIndex = new List<int>();

            string[] parts = themeParams["beatBoxTintIndex"].Split(',');
            foreach (string p in parts)
            {
                try
                {
                    result.beatBoxTintMaterialIndex.Add(int.Parse(p));
                }
                catch (Exception e)
                {
                    Debug.Log("Invalid tint index " + p);
                }
            }
        }
        result.beatBox = ObjectLoader.LoadObj(Path.Combine(themePath, themeParams["beatBoxObj"]), themeParams.ContainsKey("beatBoxMtl") ? Path.Combine(themePath, themeParams["beatBoxMtl"]) : null);
        if (themeParams.ContainsKey("beatBoxRotation"))
            result.beatBox.transform.eulerAngles = Utils.readVector(themeParams["beatBoxRotation"]);
        result.beatBox.SetActive(false);


        if (themeParams.ContainsKey("platformSet"))
        {
            Debug.Log("Loading platformSet");
            //result.platform = parentTransform.gameObject;//so this is platformLocation
            ObjectLoader.LoadObjSet(Path.Combine(themePath, themeParams["platformSet"]), parentTransform);
            //TODO clean up instances on destroy/switch theme
        }
        else
        {
            Debug.Log("Loading platformObj");
            result.platform = ObjectLoader.LoadObj(Path.Combine(themePath, themeParams["platformObj"]), themeParams.ContainsKey("platformMtl") ? Path.Combine(themePath, themeParams["platformMtl"]) : null);
        }

        if (result.platform != null)
        {
            if (themeParams.ContainsKey("platformRotation"))
                result.platform.transform.eulerAngles = Utils.readVector(themeParams["platformRotation"]);
            if (themeParams.ContainsKey("platformScale"))
                result.platform.transform.localScale = Utils.readVector(themeParams["platformScale"]);
            if (themeParams.ContainsKey("platformPosition"))
                result.platform.transform.position = Utils.readVector(themeParams["platformPosition"]);
            //result.platform.SetActive(false);
        }

        result.notifications = new GameObject[5];
        loadNotification(result, themePath, themeParams, 0, "notificationMiss");
        loadNotification(result, themePath, themeParams, 1, "notificationOk");
        loadNotification(result, themePath, themeParams, 2, "notificationGood");
        loadNotification(result, themePath, themeParams, 3, "notificationGreat");
        loadNotification(result, themePath, themeParams, 4, "notificationAwesome");

        result.backgroundCubemap = new Texture2D[6];
        if (themeParams.ContainsKey("backgroundFront"))
            result.backgroundCubemap[0] = Utils.LoadTexture(Path.Combine(themePath, themeParams["backgroundFront"]));
        if (themeParams.ContainsKey("backgroundBack"))
            result.backgroundCubemap[1] = Utils.LoadTexture(Path.Combine(themePath, themeParams["backgroundBack"]));
        //left and right are apparently wrong in Unity? ... or just how the scene is currently oriented... probably
        if (themeParams.ContainsKey("backgroundLeft"))
            result.backgroundCubemap[3] = Utils.LoadTexture(Path.Combine(themePath, themeParams["backgroundLeft"]));
        if (themeParams.ContainsKey("backgroundRight"))
            result.backgroundCubemap[2] = Utils.LoadTexture(Path.Combine(themePath, themeParams["backgroundRight"]));
        if (themeParams.ContainsKey("backgroundUp"))
            result.backgroundCubemap[4] = Utils.LoadTexture(Path.Combine(themePath, themeParams["backgroundUp"]));
        if (themeParams.ContainsKey("backgroundDown"))
            result.backgroundCubemap[5] = Utils.LoadTexture(Path.Combine(themePath, themeParams["backgroundDown"]));

        //Debug.Log("BACK " + result.backgroundCubemap[0]);

        if (themeParams.ContainsKey("backgroundTint"))
        {
            Vector3 colorVector = Utils.readVector(themeParams["backgroundTint"]);
            result.backgroundTint = new Color(colorVector.x, colorVector.y, colorVector.z);
        }
        if (themeParams.ContainsKey("backgroundExposure"))
            try
            {
                result.backgroundExposure = float.Parse(themeParams["backgroundExposure"]);
            }
            catch (Exception e)
            {
                Debug.Log("Invalid background exposure");
            }
        if (themeParams.ContainsKey("fog")){
            result.fog = bool.Parse(themeParams["fog"]);
        }
        if (themeParams.ContainsKey("fogColor")){
            result.fogColor = Utils.readColor(themeParams["fogColor"], 1);
        }
        if (themeParams.ContainsKey("fogDensity")){
            result.fogDensity = float.Parse(themeParams["fogDensity"]);
        }

        return result;
    }
    static void loadLaneColor(Theme instance, int index, Dictionary<string, string> themeParams)
    {
        string paramName = "beatBoxLane" + index + "Tint";
        if (themeParams.ContainsKey(paramName))
        {
            Vector3 colorVector = Utils.readVector(themeParams[paramName]);
            instance.beatBoxLanesTint[index] = new Color(colorVector.x, colorVector.y, colorVector.z);
        }

    }
    static void loadNotification(Theme instance, string themePath, Dictionary<string, string> themeParams, int index, string prefix)
    {
        if (!themeParams.ContainsKey(prefix + "Obj"))
            return;

        instance.notifications[index] = ObjectLoader.LoadObj(Path.Combine(themePath, themeParams[prefix + "Obj"]), themeParams.ContainsKey(prefix + "Mtl")? Path.Combine(themePath, themeParams[prefix + "Mtl"]) : null);

        if (instance.notifications[index] == null)
            return;
        if (themeParams.ContainsKey(prefix + "Rotation"))
            instance.notifications[index].transform.eulerAngles = Utils.readVector(themeParams[prefix + "Rotation"]);
        if (themeParams.ContainsKey(prefix + "Scale"))
            instance.notifications[index].transform.localScale = Utils.readVector(themeParams[prefix + "Scale"]);
        if (themeParams.ContainsKey(prefix + "Position"))
            instance.notifications[index].transform.position = Utils.readVector(themeParams[prefix + "Position"]);
        instance.notifications[index].SetActive(false);
    }
    internal void freeUp()
    {
        if (fromFile)
        {
            Debug.Log("Freeing up existing theme");
            Destroy(beatBox);
            //platform is scene platformLocation, just delete its children
            for (int i = 0; i < platform.transform.childCount; i++)
            {
                Destroy(platform.transform.GetChild(i));
            }
            //Destroy(platform);
            foreach (GameObject go in notifications)
                Destroy(go);
        }
        else
        {
            //for (int i = 0; i < platform.transform.childCount; i++)
            //{
            //    platform.transform.GetChild(i).gameObject.SetActive(false);
            //}
        }
        //TODO what else?
    }
}
