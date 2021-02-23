//timestamp,rowType(0=beatboxLanes),L0-L7(0=inactive, 1=beatbox, 2=...)
//ts,1=bpmChange,floatBpm
using System;
using UnityEngine;
public class SongRow
{

    public enum Type
    {
        BPM,
        BOX,
        BEAT,
        LANE_POS
    }
    //TODO set fixed size for timestamp, so that rows are aligned and more readable
    //TODO there are a few places where BoxType should be used, instead of magic numbers...
    //TODO DIRECTIONAL boxes (DIR_N, etc.)
    public enum BoxType
    {
        NONE = 0,
        REGULAR = 1,
        HOLD = 2,
        BOMB = 3,
        BLOCK_V = 4,
        BLOCK_H = 5,
        LEFT = 6,
        RIGHT = 7,
        UP = 8,
        DOWN = 9,
        MAYBE_HOLD = 10,
        DIR_N = 11,
        DIR_NE = 12,
        DIR_E = 13,
        DIR_SE = 14,
        DIR_S = 15,
        DIR_SW = 16,
        DIR_W = 17,
        DIR_NW = 18,

    }
    public float timestamp = -1;
    public Type type;
    public int[] activeLanes;//TODO this should be BoxType
    public float bpm;
    public Vector3[] lanePositions;
    //public SongRow() { }
    public SongRow(Vector3[] lanePositions, float timestamp)
    {
        this.lanePositions = lanePositions;
        this.timestamp = timestamp;
        type = Type.LANE_POS;
    }
    public SongRow(float timestamp)
    {
        this.timestamp = timestamp;
        type = Type.BEAT;
    }
    public SongRow(float timestamp, HitChecker.RegisteredBeat[] activeLanes)
    {
        this.timestamp = timestamp;
        //TODO fix deprecated int[] activeLanes to BoxType[] activeLanes
        this.activeLanes = new int[activeLanes.Length];
        for (int i = 0; i < activeLanes.Length; i++)
            if (activeLanes[i] != null)
                this.activeLanes[i] = (int)activeLanes[i].type;

        type = Type.BOX;
    }
    public SongRow(float timestamp, BoxType[] activeLanes)
    {
        this.timestamp = timestamp;
        //TODO fix deprecated int[] activeLanes to BoxType[] activeLanes
        this.activeLanes = new int[activeLanes.Length];
        for (int i = 0; i < activeLanes.Length; i++)
            this.activeLanes[i] = (int)activeLanes[i];

        type = Type.BOX;
    }
    public SongRow(float timestamp, int[] activeLanes)
    {
        this.timestamp = timestamp;
        this.activeLanes = activeLanes;
        type = Type.BOX;
    }
    public SongRow(float timestamp, float bpm)
    {
        this.timestamp = timestamp;
        this.bpm = bpm;
        type = Type.BPM;
    }
    //public int getLaneValue(int lane, int maxLanes)
    //{
    //    //int mod = lane % (maxLanes - 1);
    //    //if (lane >= maxLanes)
    //    return activeLanes[lane];
    //}
    static bool validRow(string rowStr)
    {
        return rowStr.StartsWith(Type.BOX.ToString()) || rowStr.StartsWith(Type.BPM.ToString()) || rowStr.StartsWith(Type.BEAT.ToString()) || rowStr.StartsWith(Type.LANE_POS.ToString());
    }
    public static SongRow parse(string rowStr)
    {
        SongRow result = null;
        if (validRow(rowStr))
        {
            string[] parts = rowStr.Split(Utils.itemSplitChar);
            try
            {
                Type type = (Type)Enum.Parse(Type.BOX.GetType(), parts[0]);

                //Debug.Log("SongRow.parse " + type + " >>> " + rowStr);

                float timestamp = -1;
                switch (type)
                {
                    case Type.BOX:
                        timestamp = float.Parse(parts[1]);
                        string[] lanesStr = parts[2].Split(Utils.subItemSplitChar);

                        int[] activeLanes = new int[AssetManager.MAX_LANES];
                        for (int i = 0; i < Math.Min(activeLanes.Length, lanesStr.Length); i++)
                            activeLanes[i] = int.Parse(lanesStr[i]);
                        //for (int i = 0; i < activeLanes.Length; i++)
                        //    if (parts.Length > i + 2)
                        //        activeLanes[i] = int.Parse(parts[i + 2]);
                        result = new SongRow(timestamp, activeLanes);
                        break;
                    case Type.BPM:
                        timestamp = float.Parse(parts[1]);
                        float bpm = float.Parse(parts[2]);
                        result = new SongRow(timestamp, bpm);
                        break;
                    case Type.BEAT:
                        timestamp = float.Parse(parts[1]);
                        result = new SongRow(timestamp);
                        break;
                    case Type.LANE_POS:
                        timestamp = float.Parse(parts[1]);
                        //Debug.Log("LP SIZE " + Utils.readPatternVectorArray(parts[2], subItemSplitChar, vectorSplitChar).Length);
                        result = new SongRow(Utils.readPatternVectorArray(parts[2], Utils.subItemSplitChar, Utils.vectorSplitChar), timestamp);
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.Log("Pattern Parsing error " + rowStr + " " + e);
                //result = new Row(-1, -1);
            }
        }

        return result;
    }
    public override string ToString()
    {
        string result = type.ToString();
        switch (type)
        {
            case Type.BOX:
                result += Utils.itemSplitChar.ToString() + timestamp.ToString("000.00000");
                if (activeLanes != null)
                {
                    result += Utils.itemSplitChar.ToString();
                    for (int i = 0; i < activeLanes.Length; i++)
                    {
                        result += activeLanes[i].ToString();
                        if (i < activeLanes.Length - 1)
                            result += Utils.subItemSplitChar.ToString();
                    }
                }
                break;
            case Type.BPM:
                result += Utils.itemSplitChar.ToString() + timestamp.ToString("000.00000");
                result += Utils.itemSplitChar.ToString() + bpm;
                break;
            case Type.BEAT:
                result += Utils.itemSplitChar.ToString() + timestamp.ToString("000.00000");
                break;
            case Type.LANE_POS:
                result += Utils.itemSplitChar.ToString() + timestamp.ToString("000.00000");
                result += Utils.itemSplitChar.ToString();
                for (int i = 0; i < lanePositions.Length; i++)
                {
                    result += lanePositions[i].x + Utils.vectorSplitChar.ToString() + lanePositions[i].y + Utils.vectorSplitChar.ToString() + lanePositions[i].z;
                    if (i < lanePositions.Length - 1)
                        result += Utils.subItemSplitChar.ToString();
                }
                break;
        }
        return result;
    }

    internal static int getDirection(int boxTypeInt)
    {
        //Type type = (BoxType)Enum.Parse(BoxType.DIR_N.GetType(), boxTypeInt);

        return getDirection((BoxType)boxTypeInt);
    }
    internal static int getDirection(BoxType boxType)
    {
        int result = -1;
        switch (boxType)
        {
            case SongRow.BoxType.DIR_N:
                result = 0;
                break;
            case SongRow.BoxType.DIR_NW:
                result = 1;
                break;
            case SongRow.BoxType.DIR_W:
                result = 2;
                break;
            case SongRow.BoxType.DIR_SW:
                result = 3;
                break;
            case SongRow.BoxType.DIR_S:
                result = 4;
                break;
            case SongRow.BoxType.DIR_SE:
                result = 5;
                break;
            case SongRow.BoxType.DIR_E:
                result = 6;
                break;
            case SongRow.BoxType.DIR_NE:
                result = 7;
                break;
        }
        return result;
    }
}