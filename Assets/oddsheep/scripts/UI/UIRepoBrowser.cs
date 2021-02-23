using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIRepoBrowser : MonoBehaviour
{
    //browserRepoSelect
    //browserRepoPagePrev
    //browserRepoPageNext
    //browserRepoPageDetails|0
    //browserRepoPageSelect|2
    //browserRepoCancel
    public class RepoItem
    {
        public string name;
        public string description;
        public string link;
        public string relatedLink;
        public Type type = Type.SONG;

        public string downloadToPath;
        public enum Type
        {
            SONG,
            REPO,
            PTRN,
            //SONG_PATTERN
            //DJ
        }

        public RepoItem(string name, string link, string description, string relatedLink)
        {
            this.type = Type.PTRN;
            this.name = name;
            this.link = link;
            this.relatedLink = relatedLink;
            this.description = description;
        }
        public RepoItem(Type type, string name, string link, string description)
        {
            this.type = type;
            this.name = name;
            this.link = link;
            this.description = description;
        }
        public static RepoItem parse(string line)
        {
            //Debug.Log("Repo parse line " + line);
            RepoItem result = null;
            string[] parts = line.Split(Utils.itemSplitChar);

            if (parts.Length <= 4)
            {
                Type type = (Type)Enum.Parse(Type.SONG.GetType(), parts[0]);

                if (type == Type.PTRN)
                    result = new RepoItem(parts[1], parts[2], parts[3], parts[4]);
                else
                    result = new RepoItem(type, parts[1], parts[2], parts[3]);
            }

            return result;
        }
        public override string ToString()
        {
            return type + " " + link + " " + description;
        }
    }

    int songBrowserPageIndex = 0;
    const int ITEMS_PER_PAGE = 5;

    public List<RepoItem> songList = new List<RepoItem>();

    Text browserTitle;
    List<Text> browserItemNames = new List<Text>();

    Button nextBrowserButton;
    Button prevBrowserButton;

    //TODO will receive data from downloaded repo 

    void Awake()
    {
        nextBrowserButton = transform.Find("pageNavContainer/nextButton").GetComponent<Button>();
        prevBrowserButton = transform.Find("pageNavContainer/prevButton").GetComponent<Button>();

        browserItemNames.Clear();
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);

            if (child.name.StartsWith("itemContainer"))
            {
                browserItemNames.Add(child.Find("Title").GetComponentInChildren<Text>());
            }
        }
    }
    static string[] defaultRepoData = new string[]{
            "REPO|boxmania.club Main Repository|http://boxmania.club/repo/basic.repo|From boxmania.club",
            "SONG|PixelLand|http://boxmania.club/repo/test.mp3|From incompetech.com"};
            //"PTRN|PixelLandPattern|http://boxmania.club/repo/test.ptn|Custom PixelLand pattern|PixelLand",
            //"SONG|testMp3|http://boxmania.club/repo/test.mp3|From incompetech.com",
            //"SONG|testDj|http://boxmania.club/repo/test.dj|From incompetech.com",
            //"SONG|OtherSong6|http://incompetech.com|From incompetech.com",
            //"SONG|OtherSong7|http://incompetech.com|From incompetech.com",
            //"SONG|OtherSong8|http://incompetech.com|From incompetech.com",
            //"SONG|OtherSong9|http://incompetech.com|From incompetech.com"};

    public void init(string[] repoData = null)
    {
        if (repoData == null)
            repoData = defaultRepoData;
        songList.Clear();
        songBrowserPageIndex = 0;

        parseRepoData(repoData);

        populateRepoBrowserPage();
    }
    public void parseRepoData(string[] listData)
    {
        //string[] fileContent = File.ReadAllLines(patternFilePaths[suffix]);
        foreach (string line in listData)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                RepoItem repoSong = RepoItem.parse(line);
                if (repoSong != null)
                    songList.Add(repoSong);
            }
        }
    }
    public RepoItem browserSelectToDownload(int index)
    {
        //TODO download stuff
        return songList[index];
    }
    public RepoItem browserDetailSong(int index)
    {
        RepoItem songData = songList[songBrowserPageIndex * ITEMS_PER_PAGE + index];
        return songData;
    }
    
    void populateRepoBrowserPage()
    {
        //Debug.Log("************* UIMANAGER.populateRepoBrowserPage " + songBrowserPageIndex);

        transform.Find("titleContainer").GetComponentInChildren<Text>().text = "Default Song Repo";

        int pageIndex = songBrowserPageIndex * ITEMS_PER_PAGE;

        //Debug.Log(browserItemNames.Count + " " + songList.Count + " " + pageIndex + " " + songBrowserPageIndex);

        for (int i = 0; i < ITEMS_PER_PAGE; i++)
        {
            if (songList.Count > pageIndex + i)
            {
                RepoItem item = songList[pageIndex + i];
                browserItemNames[i].text = item.type + "|" + item.name;
                browserItemButtonsInteract(browserItemNames[i].transform.parent, true);
            }
            else
            {
                browserItemNames[i].text = "---";
                browserItemButtonsInteract(browserItemNames[i].transform.parent, false);
            }
        }
        prevBrowserButton.interactable = songBrowserPageIndex > 0;
        nextBrowserButton.interactable = pageIndex + ITEMS_PER_PAGE < songList.Count;
    }
    void browserItemButtonsInteract(Transform parent, bool interactable)
    {
        Button[] buttons = parent.GetComponentsInChildren<Button>();
        foreach (Button b in buttons)
            b.interactable = interactable;
    }
    public void prevBrowserPage()
    {
        songBrowserPageIndex--;
        populateRepoBrowserPage();
    }
    public void nextBrowserPage()
    {
        songBrowserPageIndex++;
        populateRepoBrowserPage();
    }
}
