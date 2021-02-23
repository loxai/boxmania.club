using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ObjectLoader
{
    public static string ReadFile(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        StreamReader reader = new StreamReader(path);
        string contents = reader.ReadToEnd();
        reader.Close();

        return contents;
    }

    //instantiates all items in the file, using headers paths, and assigns it to parentTransform (eg. platformLocation.transform)
    public static GameObject LoadObjSet(string setPath, Transform parentTransform)
    {
        //GameObject instance = LoadObj(objPath, mtlPath);//this is the model that has a tweak file

        GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObject.transform.parent = parentTransform;
        baseObject.GetComponent<Collider>().enabled = false;
        baseObject.GetComponent<Renderer>().enabled = false;
        baseObject.name = Path.GetFileNameWithoutExtension(setPath);

        List<FurnitureSpawner.Furniture> items = FurnitureSpawner.readFurnitureFile(setPath);//list of moved clones for the original instance

        Dictionary<string, GameObject> headers = new Dictionary<string, GameObject>();
        foreach (FurnitureSpawner.Furniture f in items)
        {
            if (f.type == FurnitureSpawner.Furniture.Type.HEADER)
            {
                GameObject spawned = null;
                if (f.isSet())
                {
                    Debug.Log("Subset at " + f.setPath + " ** " + Path.GetDirectoryName(setPath));
                    spawned = LoadObjSet(Path.Combine(Path.GetDirectoryName(setPath), f.setPath), parentTransform);
                    //spawned.transform.parent = baseObject.transform;
                } else
                    spawned = LoadObj(Path.Combine(Path.GetDirectoryName(setPath), f.objPath), Path.Combine(Path.GetDirectoryName(setPath), f.mtlPath));

                //spawned.transform.parent = baseObject.transform;
                spawned.SetActive(false);
                headers.Add(f.name, spawned);
            }
        }
        foreach (FurnitureSpawner.Furniture f in items)
        {
            GameObject prefab = null;
            if (headers.ContainsKey(f.name))
                prefab = headers[f.name];
            else
            {
                prefab = AssetManager.instance.getBuildBlock();
                Debug.Log(f.name + " not defined, using default build block");
            }
            GameObject spawned = null;
            switch (f.type)
            {
                case FurnitureSpawner.Furniture.Type.REGULAR:
                    spawned = FurnitureSpawner.spawn(f, prefab);//instance from the prefab
                    spawned.SetActive(true);
                    //spawned.transform.localScale = baseObject.transform.localScale;
                    spawned.transform.parent = baseObject.transform;
                    break;
                case FurnitureSpawner.Furniture.Type.BEAT:
                    spawned = FurnitureSpawner.spawn(f, prefab);//instance from the prefab
                    spawned.SetActive(true);
                    //spawned.transform.localScale = baseObject.transform.localScale;
                    spawned.transform.parent = baseObject.transform;
                    BlockBeat blockBeat = spawned.AddComponent<BlockBeat>();
                    blockBeat.init(f.duration, f.factor, f.scale);
                    break;
                case FurnitureSpawner.Furniture.Type.MOVE:
                    spawned = FurnitureSpawner.spawn(f, prefab);//instance from the prefab
                    spawned.SetActive(true);
                    //spawned.transform.localScale = baseObject.transform.localScale;
                    spawned.transform.parent = baseObject.transform;
                    Hallway hallway = spawned.AddComponent<Hallway>();
                    hallway.init(f.duration, f.factor, f.endOffset);
                    break;
            }
            //Debug.Log("Spawning furniture " + f);
        }

        //spawned.transform.parent = transform.parent;//attached to base (platform) location, will destroy all children when needed (TODO)
        //return instance;

        return baseObject;
    }
    public static GameObject LoadObj(string objPath, string mtlPath)
    {
        //ObjImporter.shader = Shader.Find(AssetManager.instance.defaultColorShaderName);//AssetManager.instance.defaultColorShader;
        //if (ObjImporter.shader == null)
        //    ObjImporter.shader = AssetManager.instance.fallbackShader;
        ObjImporter.material = AssetManager.instance.objMaterial;
        //Debug.Log("*****ERR: can't find shader " + AssetManager.instance.defaultColorShaderName);
        GameObject result = null;

        Hashtable textures = new Hashtable();

        Debug.Log("Loading obj " + objPath + " mtl " + mtlPath);
        if (mtlPath == null || !File.Exists(mtlPath))
        {
            Debug.Log("Material path " + mtlPath + " doesn't exist, switching to " + objPath);
            mtlPath = objPath.Substring(0, objPath.Length - ".obj".Length) + ".mtl";
            Debug.Log("New path " + mtlPath);
        }

        string objContents = ReadFile(objPath);
        string matContents = ReadFile(mtlPath);//resourcePath + ".mtl");

        //bool hasTexture = false;
        Hashtable[] mtls = null;
        if (matContents != null)
        {
            mtls = ObjImporter.ImportMaterialSpecs(matContents);
            
            //Debug.Log("Material count " + mtls.Length);

            for (int i = 0; i < mtls.Length; i++)
            {
                if (mtls[i].ContainsKey("mainTexName"))
                {
                    var resDir = Path.GetDirectoryName(mtlPath);
                    string texName = ((string)mtls[i]["mainTexName"]).Replace(' ','_');
                    var texPath = Path.Combine(resDir, texName);

                    //Debug.Log("****************************************TEX PATH " + texPath);
                    var texture = Utils.LoadTexture(texPath);
                    if (texture != null)
                    {
                        //hasTexture = true;
                        //ObjImporter.shader = Shader.Find(AssetManager.instance.defaultTextureShaderName);//AssetManager.instance.defaultTextureShader;
                        //if (ObjImporter.shader == null)
                        //    ObjImporter.shader = AssetManager.instance.fallbackShader;
                            //Debug.Log("*****ERR: can't find shader " + AssetManager.instance.defaultColorShaderName);
                        textures[mtls[i]["mainTexName"]] = texture;
                    }
                }
            }
        }

        result = ObjImporter.Import(objContents, matContents, textures);
        if (result == null)
        {
            Debug.LogError("Oops. GameObject not loaded from path: " + objPath + "||" + mtlPath);
            //throw new Exception("GameObject could not be loaded from " + resourcePath);
        }

        //Shader shader = Shader.Find("LightweightRenderPipeline/Unlit");
        
        //Renderer[] renderers = result.GetComponentsInChildren<Renderer>();
        //foreach (Renderer r in renderers)
        //{
        //    Material[] mats = r.materials;
        //    for (int i = 0; i < mats.Length; i++)
        //    {
        //        if (hasTexture)
        //            mats[i].shader = AssetManager.instance.defaultTextureShader;
        //        else
        //            mats[i].shader = AssetManager.instance.defaultColorShader;
        //        /*
        //        //mats[i] = AssetManager.Instance.testMaterial;
        //        //break;
        //        if (unlit)
        //        {
        //            mats[i].shader = AssetManager.Instance.unlitShader;
        //        }
        //        else if (translucid)
        //        {
        //            mats[i].shader = AssetManager.Instance.translucidShader;
        //        }
        //        else
        //        {
        //            mats[i].shader = AssetManager.Instance.defaultShader;
        //        }

        //        if (colorTweak != Color.white)
        //        {
        //            //Debug.Log("SETTING TINT!!!!!!!!!--------------------------------------------------------------------- " + mats[i].name + " " + template.name);
        //            //mats[i].color = new Color((mats[i].color.r + colorTweak.r) / 2, (mats[i].color.g + colorTweak.g) / 2, (mats[i].color.b + colorTweak.b) / 2, (mats[i].color.a + colorTweak.a) / 2); 

        //            Color originalColor = mats[i].color;
        //            float a = colorTweak.a;
        //            float oneMinusA = 1 - colorTweak.a;
        //            mats[i].color = new Color(mats[i].color.r * oneMinusA + colorTweak.r * a, mats[i].color.g * oneMinusA + colorTweak.g * a, mats[i].color.b * oneMinusA + colorTweak.b * a);

        //            //mats[i].color = new Color((mats[i].color.r + colorTweak.r * colorTweak.a), (mats[i].color.g + colorTweak.g * colorTweak.a), (mats[i].color.b + colorTweak.b * colorTweak.a));//, (mats[i].color.a + colorTweak.a)); 
        //        }
        //         * */
        //    }

        //    r.materials = mats;
        //}
        return result;
    }
}
