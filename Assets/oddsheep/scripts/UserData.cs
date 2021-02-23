using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[System.Serializable]
public class PersistedUserData
{
    public Dictionary<string, PersistedSongData> songRecords;
    public PersistedUserData(){
        //songRecords = new List<PersistedSongData>();
        songRecords = new Dictionary<string, PersistedSongData>();
    }
}
[System.Serializable]
public class PersistedSongData
{
    public string name;
    public int score;

    public PersistedSongData(string name, int score)
    {
        this.name = name;
        this.score = score;
    }
    //public int playTimes;
}
public class PersistentData
{
    public PersistedUserData persistedUserData = new PersistedUserData();
    public void Save()
    {
        if (persistedUserData != null)
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(Application.persistentDataPath + "/userData.sv");
            bf.Serialize(file, persistedUserData);
            file.Close();
        }
    }
    public PersistedUserData Load()
    {
        if (File.Exists(Application.persistentDataPath + "/userData.sv"))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/userData.sv", FileMode.Open);
            persistedUserData = (PersistedUserData)bf.Deserialize(file);
            file.Close();
        }
        return persistedUserData;
    }
}