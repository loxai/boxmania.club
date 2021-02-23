using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.Android;

public class Utils
{
    public static string gameVersion = "0.1";
    internal const char itemSplitChar = '|';
    internal const char subItemSplitChar = ';';
    internal const char vectorSplitChar = ',';
    internal const char songNameSplitChar = '_';

    public static bool hasPermission()
    {
        return Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead) && Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite);
    }
    public static void requestPermission(){
        if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Debug.Log("Authorised external storage read");
        }
        else
        {
            Debug.Log("Request permission for external storage read");
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
        }
        if (Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Debug.Log("Authorised external storage write");
        }
        else
        {
            Debug.Log("Request permission for external storage write");
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
    }
    public static string postChartToWeb(string chartName, string chartFilePath, string nick = null)
    {
        Debug.Log("TODO");
        return null;
    }
    public static string getChartFromWeb(string chartName, string nick = null)
    {
        Debug.Log("TODO");
        return null;
    }
    public static string listChartsFromWeb(string chartName, string nick = null)
    {
        Debug.Log("TODO");
        return null;
    }
    public static string macToNicknameShort()
    {
        StringWriter result = new StringWriter();
        string nick = macToNickname();
        int count = 0;
        foreach (char c in nick)
        {
            if (count++ % 4 == 0)
            {//first char
                string letters = "BCDFGJLKMNPSRTXZ6";
                result.Write(letters[Math.Max(0, (c % letters.Length) - 1)]);
                string vowels = "AEIOU6";
                result.Write(vowels[Math.Max(0, (c % vowels.Length) - 1)]);
            }
        }
        Debug.Log(">>>>>>>>>>>>>>>>>>>macToNicknameShort " + result.ToString());
        return result.ToString();
    }
    public static string macToNickname()
    {
        StringWriter result = new StringWriter();
        string mac = getMacAddress();
        int count = 0;
        foreach (char c in mac)
        {
            if (count++ % 2 == 0)
            {//first char
                string letters = "BCDFGJKLMNPRSTXZ";
                result.Write(letters[Math.Max(0,(c % letters.Length) - 1)]);
            }
            else
            {//second char (a vowel)
                //AEIOU
                string letters = "AEIOU";
                result.Write(letters[Math.Max(0, (c % letters.Length) - 1)]);
            }

        }
        Debug.Log(">>>>>>>>>>>>>>>>>>>macToNickname " + result.ToString());
        return result.ToString();
    }
    public static string getMacAddress()
    {
        string physicalAddress = "";

        NetworkInterface[] nice = NetworkInterface.GetAllNetworkInterfaces();

        foreach (NetworkInterface adaper in nice)
        {

            //Debug.Log(adaper.Description);

            if (adaper.Description == "en0")
            {
                physicalAddress = adaper.GetPhysicalAddress().ToString();
                break;
            }
            else
            {
                physicalAddress = adaper.GetPhysicalAddress().ToString();

                if (physicalAddress != "")
                {
                    break;
                };
            }
        }

        return physicalAddress;
    }
    public static Dictionary<string, string> readDictionaryFile(string path)
    {
        //Debug.Log("Read dictionary file at " + path);
        StreamReader reader = new StreamReader(path);
        Dictionary<string, string> result = new Dictionary<string, string>();
        string line = null;

        while ((line = reader.ReadLine()) != null)
        {
            string[] parts = line.Split('=');
            if (parts.Length == 2)
            {
                result.Add(parts[0], parts[1]);
            }
        }

        reader.Close();
        return result;
    }
    public static void resizeToMatch(GameObject source, GameObject target)//, GameObject attachTo = null, bool setCollider = false)
    {
        Quaternion sourceLocalRotations = source.transform.localRotation;//it's important to remove rotations to have bounds on the same coordinates space
        Quaternion targetLocalRotations = target.transform.localRotation;
        source.transform.localRotation = Quaternion.identity;
        target.transform.localRotation = Quaternion.identity;
        Quaternion sourceRotations = source.transform.rotation;
        Quaternion targetRotations = target.transform.rotation;
        source.transform.rotation = Quaternion.identity;
        target.transform.rotation = Quaternion.identity;

        Bounds bounds = new Bounds();
        Collider[] colliders = source.GetComponentsInChildren<Collider>();
        Renderer[] renderers = source.GetComponentsInChildren<Renderer>();

        if (false && colliders != null && colliders.Length > 0)
        {
            bounds = colliders[0].bounds;
            foreach (var c in colliders)
            {
                bounds.Encapsulate(c.bounds);
            }
        }
        else
        {
            bounds = renderers[0].bounds;
            foreach (var c in renderers)
            {
                if (!(c is TrailRenderer))//TrailRenderers break the calculation and are not relevant, so skip those
                    bounds.Encapsulate(c.bounds);
            }
        }
        Vector3 extentsTarget = target.GetComponent<Collider>().bounds.extents;
        Vector3 extentsSource = new Vector3(bounds.extents.x / source.transform.localScale.x, bounds.extents.y / source.transform.localScale.y, bounds.extents.z / source.transform.localScale.z);

        //Debug.Log("Source bounds " + bounds.extents + " target bounds " + extentsTarget);
        //extentsSource.Scale(source.transform.localScale);

        // find minimum scale out of the bunch - it will be the proper one to keep everything in "scale" 
        var min = Mathf.Min(Mathf.Min(extentsTarget.x / extentsSource.x, extentsTarget.y / extentsSource.y), extentsTarget.z / extentsSource.z);
        //var min = Mathf.Min(Mathf.Min(extentsSource.x / extentsTarget.x, extentsSource.y / extentsTarget.y), extentsSource.z / extentsTarget.z);
        source.transform.localScale = new Vector3(min, min, min);

        //if (min < 0.05f)
            //Debug.Log("VERY SMALL extentsSource " + extentsSource + " extentsTarget" + extentsTarget);

        //if (setCollider)
        //{
        //    SphereCollider collider = source.AddComponent<SphereCollider>();

        //    renderers = source.GetComponentsInChildren<Renderer>();
        //    bounds = renderers[0].bounds;

        //    foreach (var c in renderers)
        //    {
        //        bounds.Encapsulate(c.bounds);
        //    }
        //    collider.center = bounds.center;
        //    collider.radius = (bounds.size.x + bounds.size.y + bounds.size.z) / 3;
        //}

        source.transform.rotation = sourceRotations;
        target.transform.rotation = targetRotations;
        source.transform.localRotation = sourceLocalRotations;
        target.transform.localRotation = targetLocalRotations;

        //// reparent should always happen after resize
        //if (attachTo != null)
        //{
        //    source.transform.SetParent(attachTo.transform, false);
        //    //TODO when attaching (in order to sort out source objects that defined center doesn't match render bounds center), we should make sure that child is centered inside parent, so offset position by difference of renderer bounds?

        //    // source.transform.position = target.transform.position;
        //    // source.transform.parent = target.transform;
        //}
    }

    internal static Vector3 readVector(string p, char splitChar = vectorSplitChar)
    {
        Vector3 result = Vector3.zero;

        if (p != null)
        {
            string[] parts = p.Split(splitChar);
            if (parts.Length == 3)
            {
                try
                {
                    result = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
                }
                catch (Exception e)
                {
                    Debug.Log("Wrong vector params " + p);
                }
            }
        }

        return result;
    }
    internal static Texture2D LoadTexture(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        Texture2D dlTex = new Texture2D(2, 2);
        dlTex.LoadImage(File.ReadAllBytes(path));
        return dlTex;
    }

    internal static Color readColor(string p, float defaultAlpha, char splitChar = ',')
    {
        Color result = Color.white;

        if (p != null)
        {
            string[] parts = p.Split(splitChar);
            if (parts.Length == 3)
            {
                try
                {
                    result = new Color(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), defaultAlpha);
                }
                catch (Exception e)
                {
                    Debug.Log("Wrong vector params " + p);
                }
            } else
            if (parts.Length == 4)
            {
                try
                {
                    result = new Color(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
                }
                catch (Exception e)
                {
                    Debug.Log("Wrong vector params " + p);
                }
            }
        }

        return result;
    }



    internal static Vector3[] readPatternVectorArray(string p, char arraySplitChar, char vectorSplitChar)
    {
        List<Vector3> result = new List<Vector3>();
        string[] vectorParts = p.Split(arraySplitChar);
        foreach (string s in vectorParts)
            result.Add(readVector(s, vectorSplitChar));//can't use readVector, that one is for param files

        return result.ToArray();
    }

    internal static string showLanes(SongRow.BoxType[] currentLanes)
    {
        string result = "";
        foreach (SongRow.BoxType t in currentLanes)
        {
            result += "|" + (int)t;
        }
        return result;
    }
    internal static string showLanes(int[] currentLanes)
    {
        string result = "";
        if (currentLanes == null)
        {
            Debug.Log("No lanes to show?");
        } else
        foreach (int i in currentLanes)
        {
            result += "|" + i;
        }
        return result;
    }
}
