using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FurnitureSpawner : MonoBehaviour
{
    GameObject defaultPrefab;

    public class Furniture
    {
        public enum Type
        {
            REGULAR,
            MOVE,
            BEAT,//TODO
            HEADER,
        }
        public Type type = Type.REGULAR;
        public Vector3 pos;
        public Vector3 rot;
        public Vector3 scale;

        public Vector3 endOffset;

        public string name;
        public string objPath;
        public string mtlPath;
        public string setPath;

        public float factor;
        public float duration;

        public bool isSet()
        {
            return setPath != null;
        }
        public Furniture(string name, string setPath)
        {
            type = Type.HEADER;
            this.name = name;
            this.setPath = setPath;
        }
        public Furniture(string name, string objPath, string mtlPath)
        {
            type = Type.HEADER;
            this.name = name;
            this.objPath = objPath;
            this.mtlPath = mtlPath;
        }
        public Furniture(string name, Vector3 pos, Vector3 rot, Vector3 scale, float factor, float duration)
        {
            type = Type.BEAT;
            this.name = name;
            this.pos = pos;
            this.rot = rot;
            this.scale = scale;
            this.factor = factor;
            this.duration = duration;
        }
        public Furniture(string name, Vector3 pos, Vector3 rot, Vector3 scale, float factor, float duration, Vector3 endOffset)
        {
            type = Type.MOVE;
            this.name = name;
            this.pos = pos;
            this.rot = rot;
            this.scale = scale;
            this.factor = factor;
            this.duration = duration;
            this.endOffset = endOffset;
        }
        public Furniture(string name, Vector3 pos, Vector3 rot, Vector3 scale)
        {
            type = Type.REGULAR;
            this.name = name;
            this.pos = pos;
            this.rot = rot;
            this.scale = scale;
        }
        public override string ToString()
        {
            return type + " pos" + pos + " rot" + rot + " scale" + scale + " factor" + factor + " duration" + duration + " endOffset" + endOffset;
        }
        internal static Furniture parse(string line){

            Furniture result = null;
            if (line.StartsWith(Furniture.Type.HEADER.ToString()))
                result = parseHeader(line);

            if (line.StartsWith(Furniture.Type.REGULAR.ToString()))
                result = parseRegular(line);

            if (line.StartsWith(Furniture.Type.BEAT.ToString()))
                result = parseBeat(line);
            if (line.StartsWith(Furniture.Type.MOVE.ToString()))
                result = parseMove(line);

            return result;
        }
        private static Furniture parseHeader(string line)
        {
            Furniture result = null;
            string[] parts = line.Split(Utils.itemSplitChar);

            if (parts.Length == 4)
                result = new Furniture(parts[1], parts[2], parts[3]);//header is obj/mtl type
            if (parts.Length == 3)
                result = new Furniture(parts[1], parts[2]);//header is set type

            return result;
        }
        private static Furniture parseRegular(string line)
        {
            //REGULAR|name|x,y,z|rx,ry,rz|sx,sy,sz
            Furniture result = null;
            string[] parts = line.Split(Utils.itemSplitChar);

            if (parts.Length == 5)
            {

                Vector3 pos = Utils.readVector(parts[2]);
                Vector3 rot = Utils.readVector(parts[3]);
                Vector3 scale = Utils.readVector(parts[4]);
                result = new Furniture(parts[1], pos, rot, scale);
            }
            return result;
        }
        private static Furniture parseBeat(string line)
        {
            //BEAT|name|x,y,z|rx,ry,rz|sx,sy,sz|factor|duration
            Furniture result = null;
            string[] parts = line.Split(Utils.itemSplitChar);

            if (parts.Length == 7)
            {

                Vector3 pos = Utils.readVector(parts[2]);
                Vector3 rot = Utils.readVector(parts[3]);
                Vector3 scale = Utils.readVector(parts[4]);
                result = new Furniture(parts[1], pos, rot, scale, float.Parse(parts[5]), float.Parse(parts[6]));
            }
            return result;
        }
        private static Furniture parseMove(string line)
        {
            //MOVE|name|x,y,z|rx,ry,rz|sx,sy,sz|factor|duration|ex,ey,ez
            Furniture result = null;
            string[] parts = line.Split(Utils.itemSplitChar);

            if (parts.Length == 8)
            {

                Vector3 pos = Utils.readVector(parts[2]);
                Vector3 rot = Utils.readVector(parts[3]);
                Vector3 scale = Utils.readVector(parts[4]);
                result = new Furniture(parts[1], pos, rot, scale, float.Parse(parts[5]), float.Parse(parts[6]), Utils.readVector(parts[7]));
                Debug.Log("PARSE MOVE " + result);
            }
            return result;
        }
    }
    internal void spawn(Furniture furniture)
    {
        GameObject instance = Instantiate(defaultPrefab);
        instance.transform.position = furniture.pos;
        instance.transform.eulerAngles = furniture.rot;
        instance.transform.localScale = furniture.scale;

        instance.transform.parent = transform.parent;//attached to base (platform) location, will destroy all children when needed (TODO)
    }
    static internal GameObject spawn(Furniture furniture, GameObject source)
    {
        GameObject instance = Instantiate(source);
        instance.transform.position = furniture.pos;
        instance.transform.eulerAngles = furniture.rot;
        instance.transform.localScale = furniture.scale;

        return instance;
    }
    
    static internal List<Furniture> readFurnitureFile(string filePath)
    {
        List<Furniture> items = new List<Furniture>();

        StreamReader reader = new StreamReader(filePath);
        string line = null;

        while ((line = reader.ReadLine()) != null)
        {
            Furniture furniture = Furniture.parse(line);

            if (furniture != null)
                items.Add(furniture);
        }

        reader.Close();

        return items;
    }

}
