using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    enum State
    {
        BASIC_MENU,
        MAIN_MENU,
        SONG_OPEN_BROWSER,
        SONG_SELECT_BROWSER,
        SONG_PATTERN_OPEN_BROWSER,
        YOUTUBE_OPEN_BROWSER,
        PLAY,
        PLAY_PATTERN_TEST,
        RECORD_PATTERN,
        RECORD_SONG,
        CREATE_MENU,
        CREATE_PATTERN_MENU,
        CREATE_SONG_MENU,
        CREATE_MODEL_MENU,
        CREATE_PLAY,
        SONG_REPO_OPEN_BROWSER
    }
    const string FADE_OUT_CMD = "*FO";
    const string FADE_IN_CMD = "*FI";

    List<SongData> songList = new List<SongData>();
    int songBrowserPageIndex = 0;
    const int ITEMS_PER_PAGE = 5;

    State state = State.BASIC_MENU;

    public GameObject basicNavPage;
    public GameObject mainPage;
    public GameObject scorePage;
    public GameObject fileBrowserPage;
    public GameObject createNavPage;
    public GameObject createPatternPage;
    public GameObject notificationPage;
    public GameObject createSongPage;
    public GameObject createModelPage;
    public GameObject translucidSpawnPage;
    List<GameObject> pages = new List<GameObject>();
    public UIRepoBrowser uiRepoBrowserPage;
    public UIHelp uiHelpPage;

    //public GameObject vrPointer;
    //public GameObject handPointer;

    public Button songSelect;
    public Button songSelectPattern;
    

    public Button playButton;
    public Button stopButton;
    public Button optionsButton;
    public Button userIdButton;
    public Button createNavButton;
    public Button recordButton;
    public Button backButton;
    public Button playModeButton;
    //public Button quitButton;
    List<Button> navButtons = new List<Button>();

    public Button nextBrowserButton;
    public Button prevBrowserButton;

    public Spawner spawner;
    public SongLoader songLoader;
    //public YTManager ytManager;

    public Sprite introImage;
    public Sprite soundCloudBrowseImage;

    //public GameObject hideView;
    //bool useScreenFade = false;
    //public OVRScreenFadeCustom screenFade;

    public GameObject transitionSpace;

    Text browserTitle;
    List<Text> browserItemNames = new List<Text>();

    PersistentData persistentData = new PersistentData();
    // Start is called before the first frame update

    //string buttonPressOnNextUpdate = null;
    //float buttonPressOnNextUpdateTimestamp = 0;
    class ActionDetails{
        internal enum Type
        {
            BUTTON_PRESS,
            DOWNLOAD_ITEM,
            LOAD_SONG
        }
        public Type type;
        public UIRepoBrowser.RepoItem repoItem;
        public string buttonPressName;

        public ActionDetails(string buttonPressName)
        {
            type = Type.BUTTON_PRESS;
            this.buttonPressName = buttonPressName;
        }
        public ActionDetails(UIRepoBrowser.RepoItem item)
        {
            type = Type.DOWNLOAD_ITEM;
            this.repoItem = item;
        }
        public override string ToString()
        {
            return type + " " + repoItem + " " + buttonPressName;
        }
    }
    delegate void FadeAction(ActionDetails details);
    FadeAction actionOnNextUpdate;
    ActionDetails actionDetailsOnNextUpdate;
    float actionTimestamp;

    void Start()
    {
        string mac = Utils.getMacAddress();

        Debug.Log(">>>>>>>>>>>>>>>>>>>>>>>>>>>>mac " + mac + " short " + Utils.macToNicknameShort());
#if !UNITY_STANDALONE && !UNITY_EDITOR
        if (!Utils.hasPermission())
            Utils.requestPermission();
#endif
        persistentData.Load();

        Debug.Log("Persistent song record count " + persistentData.persistedUserData.songRecords.Count);

        //Debug.Log("Persistent song record " + persistentData.persistedUserData.songRecords[0].name + " " + persistentData.persistedUserData.songRecords[0].score);

        pages.Add(basicNavPage);
        pages.Add(mainPage);
        pages.Add(scorePage);
        pages.Add(fileBrowserPage);
        pages.Add(createNavPage);
        pages.Add(createPatternPage);
        pages.Add(notificationPage);
        pages.Add(createSongPage);
        pages.Add(translucidSpawnPage);
        pages.Add(createModelPage);
        pages.Add(uiRepoBrowserPage.gameObject);
        pages.Add(uiHelpPage.gameObject);
        userIdButton.GetComponentInChildren<Text>().text = Utils.macToNicknameShort();

        navButtons.Add(playButton);
        navButtons.Add(stopButton);
        navButtons.Add(recordButton);
        navButtons.Add(optionsButton);
        navButtons.Add(createNavButton);
        navButtons.Add(backButton);
        navButtons.Add(playModeButton);

        setState(State.BASIC_MENU);

        SongData selectedSong = Settings.instance.getSelectedSong();
        if (selectedSong != null)
            setUISelectedSongAux(selectedSong);
        //songSelectPattern.GetComponentInChildren<Text>().text = selectedSong.name;

        Settings.instance.setBeater(1);

        //TODO this setup should happen somewhere else, possibly loading settings file first
        Settings.instance.setHitBoxSize(1);
        Settings.instance.setNumLanesIndex(0);
        //Settings.instance.difficultyPlusOneBox = false;
        //Settings.instance.setLanesPosition()

        if (!Utils.hasPermission())
        {
            Debug.Log("No permission? ");
        }

        Debug.Log("Show welcome notification");
        showNotification(10, "boxmania.club, the custom rhythm game", "Welcome! this is a work in progress." + System.Environment.NewLine + 
                            "the aim is to create a fully customisable VR rhythm game (no need for mods!)" + System.Environment.NewLine + 
                            "it is in very early stage, and I dedicate whenever I have some spare time." + System.Environment.NewLine + 
                            "I think it can be quite fun to create song charts in game, then share with others (working to make that possible)." + System.Environment.NewLine + 
                            "current uptdate focuses on youtube support." + System.Environment.NewLine + 
                            "check the /boxmaniaResources folder. that's where all the modding possibilities happen!" + System.Environment.NewLine +
                            "text config and pattern files, custom songs/3d models)" + System.Environment.NewLine +
                            "also some new songs and bugfixes (maybe?)" + System.Environment.NewLine +
                            "Feedback is welcomed, will try to keep improving :)" + System.Environment.NewLine + 

                            "btw, your username is " + Utils.macToNicknameShort()+ System.Environment.NewLine
                            , introImage);

        //spawner.previewSelectedSong();
    }
    
    void showNavButtons(Button[] selectedButtons)
    {
        foreach (Button b in navButtons)
        {
            b.gameObject.SetActive(false);
            foreach (Button bShow in selectedButtons)
                if (bShow == b)
                {
                    b.gameObject.SetActive(true);
                    break;
                }
        }
    }
    
    //TODO show in front of user gaze
    void showNotificationEvent(EventParam eventParam)
    {
        showNotification(1f, "Notification Id " + eventParam.int1, eventParam.string1, null, "eventNotificationOk");
    }
    public void showNotification(float duration, string text = null, string subText = null, Sprite sprite = null, string buttonPressString = "notificationOk")
    {
        Debug.Log("UIManager.showNotification " + text + " " + notificationPage + " " + subText);
        if (notificationPage == null)
            return;
        Image image = notificationPage.transform.Find("Image").GetComponent<Image>();
        if (sprite != null)
        {
            image.sprite = sprite;
            image.preserveAspect = true;
            image.enabled = true;
        }
        else
            image.enabled = false;

        Text textUI = notificationPage.transform.Find("Text").GetComponent<Text>();
        if (text == null)
        {
            textUI.gameObject.SetActive(false);
        }
        else
        {
            textUI.gameObject.SetActive(true);
            textUI.text = text;
        }

        textUI = notificationPage.transform.Find("SubText").GetComponent<Text>();
        if (subText == null)
        {
            textUI.gameObject.SetActive(false);
        }
        else
        {
            textUI.gameObject.SetActive(true);
            textUI.text = subText;
        }

        //Button button = notificationPage.transform.GetComponentInChildren<Button>();
        //button.onClick += null;
        notificationPage.SetActive(true);
    }
    void showPage(GameObject selectedPage)
    {
        showPages(new GameObject[]{ selectedPage });
    }
    void showPages(GameObject[] selectedPages)
    {
        foreach (GameObject go in pages)
        {
            Debug.Log(">>>>>>>>>>>>>>>>>>>>>>>> GO disable " + go);
            if (go != null)
            {
                go.SetActive(false);
                foreach (GameObject goShow in selectedPages)
                    if (goShow == go)
                    {
                        go.SetActive(true);
                        break;
                    }
            }
        }
    }
    int prevNumLanes = 0;
    void setState(State newState)
    {
        Debug.Log("************* STATE CHANGE " + newState);
        switch (newState)
        {
            case State.BASIC_MENU:
                showPage(basicNavPage);
                showNavButtons(new Button[] { optionsButton, playButton, playModeButton, createNavButton, userIdButton });
                break;
            case State.MAIN_MENU:
                setUISelectedLayout(mainPage);
                setUISelectedDifficulty();
                setUISelectedBeater();
                setUISelectedSize();
                setUISelectedTheme();

                setUISelectedSongAux(Settings.instance.getSelectedSong());

                //setSelected(mainPage.transform.Find("layoutContainer/dButton").GetComponent<Button>(), Settings.instance.getLayout() == 3);

                showPages(new GameObject[] { basicNavPage, mainPage});
                showNavButtons(new Button[] { optionsButton, playButton, playModeButton, createNavButton });
                break;
            case State.SONG_PATTERN_OPEN_BROWSER:
                initSongBrowser();
                showPage(fileBrowserPage);
                showNavButtons(new Button[] { optionsButton, backButton });
                break;
            case State.YOUTUBE_OPEN_BROWSER:
                showPage(fileBrowserPage);
                initYouTubeBrowser();
                showNavButtons(new Button[] { optionsButton, backButton });
                break;
            case State.SONG_OPEN_BROWSER:
                showPage(fileBrowserPage);
                initSongBrowser();
                showNavButtons(new Button[] { optionsButton, backButton });
                break;
            case State.SONG_REPO_OPEN_BROWSER:
                showPage(uiRepoBrowserPage.gameObject);
                //uiRepoBrowserPage.init();
                showNavButtons(new Button[] { optionsButton, backButton });
                break;
            case State.CREATE_MENU:
                showPages(new GameObject[] { basicNavPage, createNavPage });
                showNavButtons(new Button[] { optionsButton, createNavButton });
                break;
            case State.CREATE_PATTERN_MENU:
                prevNumLanes = Settings.instance.getNumLanes();
                Button savePatternButton = createPatternPage.transform.Find("manageContainer/saveButton").GetComponent<Button>();
                Button testPatternButton = createPatternPage.transform.Find("manageContainer/testButton").GetComponent<Button>();
                savePatternButton.interactable = Spawner.instance.hasRecordedPattern();
                testPatternButton.interactable = Spawner.instance.hasRecordedPattern();

                setUISelectedSongAux(Settings.instance.getSelectedSong());
                setUISelectedLayout();

                showPages(new GameObject[] { basicNavPage, createPatternPage });
                recordButton.interactable = true;
                showNavButtons(new Button[] { optionsButton, recordButton, backButton });
                break;
            case State.RECORD_PATTERN:
                showPages(new GameObject[] { basicNavPage });
                showNavButtons(new Button[] { stopButton });
                break;
            case State.RECORD_SONG:
                showPages(new GameObject[] { basicNavPage, createSongPage });
                showNavButtons(new Button[] { stopButton });
                break;
            case State.PLAY_PATTERN_TEST:
                showPages(new GameObject[] { basicNavPage, scorePage, translucidSpawnPage});
                showNavButtons(new Button[] { stopButton });
                break;
            case State.CREATE_SONG_MENU:
                prevNumLanes = Settings.instance.getNumLanes();
                Settings.instance.setNumLanes(8);
                Spawner.instance.initLanes();

                spawner.setLaneAudio(true);
                showPages(new GameObject[] { basicNavPage, createSongPage });
                recordButton.interactable = false;//TODO proper audio recording (and slot saving)
                showNavButtons(new Button[] { optionsButton, recordButton, backButton });
                break;
            case State.CREATE_MODEL_MENU:
                prevNumLanes = Settings.instance.getNumLanes();
                Settings.instance.setNumLanes(8);
                Spawner.instance.initLanes();

                spawner.setModelMode(true);
                showPages(new GameObject[] { basicNavPage, createModelPage });
                showNavButtons(new Button[] { optionsButton, backButton });
                break;
            case State.PLAY:
                //screenFade.FadeOut(loadSong);
                //fadeOut(loadSong, tring);
                showPages(new GameObject[] { basicNavPage, scorePage, translucidSpawnPage });
                showNavButtons(new Button[] { stopButton });
                //optionsButton.interactable = false;
                //loadSong();
                break;
        }
        state = newState;
    }

    UnityWebRequest webRequest = null;
    float lastWebRequestProgress;
    IEnumerator downloadCoroutine(UIRepoBrowser.RepoItem repoItem, downloadCompleteDelegate downloadComplete)
    {

        Debug.Log("Downloading " + repoItem.link);
        using (UnityWebRequest www = UnityWebRequest.Get(repoItem.link))
        {
            webRequest = www;
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
                downloadComplete.Invoke(www.downloadHandler, repoItem, www.error);
            }
            else
            {
                downloadComplete.Invoke(www.downloadHandler, repoItem);
            }
            Debug.Log("Done? " + webRequest.downloadProgress);
            webRequest = null;
        }
        //fadeIn();
    }
    delegate void downloadCompleteDelegate(DownloadHandler downloadHandler, UIRepoBrowser.RepoItem repoItem, string error = null);

    void downloadComplete(DownloadHandler downloadHandler, UIRepoBrowser.RepoItem repoItem, string error)
    {
        Debug.Log("Download complete " + repoItem);
        if (error != null)
        {
            showNotification(0, "Download Error", "Could not download " + repoItem.link + System.Environment.NewLine + error);
        }
        else
        {
            switch (repoItem.type)
            {
                case UIRepoBrowser.RepoItem.Type.REPO:
                    //Debug.Log("Received " + downloadHandler.text);
                    string[] parts = downloadHandler.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);//file coming from internet so full newline options considered

                    setState(State.SONG_REPO_OPEN_BROWSER);
                    uiRepoBrowserPage.init(parts);
                    //File.WriteAllText(localPath, downloadHandler.text);
                    break;
                case UIRepoBrowser.RepoItem.Type.SONG:
                    if (Path.GetExtension(repoItem.link).Contains("mp3")){
                        File.WriteAllBytes(repoItem.downloadToPath, downloadHandler.data);
                    } else
                    if (Path.GetExtension(repoItem.link).Contains("dj")){
                        File.WriteAllText(repoItem.downloadToPath, downloadHandler.text);
                    }
                    break;
            }
        }
        fadeIn();
    }
    void startDownload(ActionDetails actionDetails)
    {
        Debug.Log("Download File " + actionDetails);

        if (actionDetails.type == ActionDetails.Type.DOWNLOAD_ITEM){
            UIRepoBrowser.RepoItem item = actionDetails.repoItem;

            switch(item.type){
                case UIRepoBrowser.RepoItem.Type.REPO:
                    break;
                case UIRepoBrowser.RepoItem.Type.PTRN:
                    item.downloadToPath = Path.Combine(Path.Combine(AssetManager.instance.getBaseFolder(), "patterns"), item.link);//TODO download all lane num pattern files
                    break;
                case UIRepoBrowser.RepoItem.Type.SONG:
                    item.downloadToPath = Path.Combine(Path.Combine(AssetManager.instance.getBaseFolder(), "songs"), Path.GetFileName(item.link));
                    break;

            }
        }

        StartCoroutine(downloadCoroutine(actionDetails.repoItem, downloadComplete));
    }
    void loadSong(ActionDetails actionDetails)//string detail = null)
    {
        SongData songData = Settings.instance.getSelectedSong();
        Debug.Log("***************** Load song " + songData);

        int patternLanes = Settings.instance.getNumLanes();
        SongData.PatternType patternType = Settings.instance.selectedPattern;

        Settings.PlayMode playMode = Settings.PlayMode.PLAY;
        if (state == State.RECORD_PATTERN)
            playMode = Settings.PlayMode.RECORD_PATTERN;
        if (state == State.PLAY_PATTERN_TEST)
            playMode = Settings.PlayMode.TEST_PATTERN_PLAY;

        if (state == State.RECORD_PATTERN)
        {
            songLoader.loadSong(songData, songReadyToRecord, patternLanes, patternType, playMode);
            //if (songData.isDefaultSong())
            //    songLoader.loadSong(AssetManager.instance.getDefaultSong(songData.defaultIndex), songReadyToRecord, patternLanes, patternType, playMode);
            //else
            //    if (songData.isYoutube)
            //        songLoader.loadSong(songData, songReadyToRecord, patternLanes, SongData.PatternType.CUSTOM, playMode);
            //    else
            //        songLoader.loadSong(songData, songReadyToRecord, patternLanes, patternType, playMode);
        }
        else
        {
            songLoader.loadSong(songData, songReadyToPlay, patternLanes, patternType, playMode);
            //if (songData.isDefaultSong())
            //    songLoader.loadSong(AssetManager.instance.getDefaultSong(songData.defaultIndex), songReadyToPlay, patternLanes, patternType, playMode);
            //else
            //    if (songData.isYoutube)
            //        songLoader.loadSong(songData, songReadyToPlayYT, patternLanes, SongData.PatternType.CUSTOM, playMode);
            //    else
            //        songLoader.loadSong(songData, songReadyToPlay, patternLanes, patternType, playMode);
        }

    }
    float count = 0;
    // Update is called once per frame
    float getCurrentBpm()
    {
        return createSongPage.transform.Find("bpmContainer").GetComponentInChildren<Slider>().value;
    }
    void Update()
    {
        if (InputUtils.isCustomLayoutSwitchDown())// && spawner.getPlayMode() != Settings.PlayMode.PLAY)
        {
            //buttonPress("layoutCustom");
            switchCustomLayout();
            Debug.Log("Is custom layout " + Settings.instance.isUsingCustomLayouts());
        }

        if (webRequest != null && (webRequest.downloadProgress - lastWebRequestProgress) > 0.1)
        {
            try
            {
                Debug.Log("Download progress " + webRequest.downloadProgress);//TODO make it visible
                lastWebRequestProgress = webRequest.downloadProgress;
            }catch{}
        }

        //if (transitionSpace.activeSelf && buttonPressOnNextUpdate != null)
        if (actionOnNextUpdate != null && actionTimestamp > 0 && actionTimestamp < Time.time)
        {
            Debug.Log(">>>>>>>>>>>>>>>>>>>>>>>UPDATE PRESSING " + actionOnNextUpdate + " " + actionDetailsOnNextUpdate);
            FadeAction tmp = actionOnNextUpdate;
            actionOnNextUpdate = null;
            actionTimestamp = 0;
            tmp(actionDetailsOnNextUpdate);
        }
        //if (state != State.PLAY_PATTERN_TEST && state != State.PLAY)
        //{
        //    vrPointer.transform.position = handPointer.transform.position + new Vector3(0,-1,0f);
        //}
#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.M))
        {
            Slider bpmSlider = createSongPage.transform.Find("bpmContainer").GetComponentInChildren<Slider>();
            bpmSlider.value = Mathf.Min(bpmSlider.value + 1, 280);
            EventManager.TriggerEvent(EventManager.EVENT_BPM_CHANGE, new BeatData(-1, -1, bpmSlider.value));
        }
        if (Input.GetKey(KeyCode.N))
        {
            Slider bpmSlider = createSongPage.transform.Find("bpmContainer").GetComponentInChildren<Slider>();
            bpmSlider.value = Mathf.Max(bpmSlider.value - 1, 30);
            EventManager.TriggerEvent(EventManager.EVENT_BPM_CHANGE, new BeatData(-1, -1, bpmSlider.value));
        }
        if (Input.GetKey(KeyCode.K))
        {
            Slider crossFadeSlider = createSongPage.transform.Find("crossFadeContainer").GetComponentInChildren<Slider>();
            crossFadeSlider.value = Mathf.Min(crossFadeSlider.value + 1, 100);
            EventManager.TriggerEvent(EventManager.EVENT_CROSS_FADE_CHANGE, new EventParam(crossFadeSlider.value));
        }
        if (Input.GetKey(KeyCode.J))
        {
            Slider crossFadeSlider = createSongPage.transform.Find("crossFadeContainer").GetComponentInChildren<Slider>();
            crossFadeSlider.value = Mathf.Max(crossFadeSlider.value - 1, -100);
            EventManager.TriggerEvent(EventManager.EVENT_CROSS_FADE_CHANGE, new EventParam(crossFadeSlider.value));
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (AudioRecorder.instance.IsRecording)
                AudioRecorder.instance.StopRecording();
            else
                AudioRecorder.instance.StartRecording(Path.Combine(AssetManager.instance.getBaseFolder(),"testRecord " + getCurrentBpm()+ ".wav"));
        }
#endif
        //screenFade.oscillate();
        /*
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.A))
            prevBrowserPage();
        if (Input.GetKeyDown(KeyCode.S))
            nextBrowserPage();

        if (Input.GetKeyDown(KeyCode.Z))
        {
            browserSelectSong(1);
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            buttonPress("stop");
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            //buttonPress("play*FO");
            //songLoader.loadSong(AssetManager.instance.getDefaultSong(3), songReady, spawner.totalLanes);
            //songLoader.loadSong(Path.Combine(AssetManager.instance.getBaseFolder(),"songs/Pixelland.mp3"), songReady, SongData.AUTO4);
            //songLoader.loadSong(Path.Combine(AssetManager.instance.getBaseFolder(), "songs/airport-lounge-by-kevin-macleod.mp3"), songReady, spawner.totalLanes);
            songLoader.loadSong(Path.Combine(AssetManager.instance.getBaseFolder(), "songs/TheFatRat+-+No+No+No.mp3"), songReadyToPlay, Settings.instance.getNumLanes(), SongData.PatternType.AUTO, Settings.PlayMode.PLAY);
            
        }
        //EventManager.TriggerEvent(EventManager.EVENT_SONG_PLAY, null);//new SongParam(Path.Combine(AssetManager.instance.getBaseFolder(), "Pixelland.mp3")));

#endif
         * */
    }

    public void testytselect()
    {
        //astronomia https://www.youtube.com/watch?v=--cxZbnmmoc
        //astronomia pianox https://www.youtube.com/watch?v=7tqtp3kJnVI
        //memes pianox https://www.youtube.com/watch?v=Wj1vo0b8z-E
        //moonlight sonata pianox https://www.youtube.com/watch?v=wrzSXDehrEM
        //360 Icarus 666 https://www.youtube.com/watch?v=A5yVXgoh22Y
        //360 Life Support | 360° VR Music Video Composed By AI  https://www.youtube.com/watch?v=0RhLXep_730
        //360 K-391, Alan Walker, Ahrix - End Of Time https://www.youtube.com/watch?v=SNnpPcgzPec
        //360 r-rated psychedelic experience https://www.youtube.com/watch?v=OZA4WIgQW8M
        //360 Marshmello proud - https://www.youtube.com/watch?v=3IBsuqXRtiU
        //360 imagine dragons -believer https://www.youtube.com/watch?v=khM0jVV28Ks
        //360 avicii - waiting for love https://www.youtube.com/watch?v=edcJ_JNeyhg
        //"360"https://www.youtube.com/watch?v=9lUieikqT5g
        //if ends with 360, it loads through ytdl360 player, otherwiser regualar youtbe 
        //youtubeSelectSong("https://www.youtube.com/watch?v=IBpvsWqYmUY", "highHopesBeatSaber360");
        //youtubeSelectSong("https://www.youtube.com/watch?v=9lUieikqT5g", "YenneferSongWitcher");
        //youtubeSelectSong("https://www.youtube.com/watch?v=M_5e8IBfOQQ", "NeonRedInstrumentalRemix_MiracleOfSound_YTSynthwave");
        //youtubeSelectSong("https://www.youtube.com/watch?v=iJ2nA2MH8vU", "ariadnaGandreBeatSaber360");
        //youtubeSelectSong("https://www.youtube.com/watch?v=SsFa5JhSDH4", "YTTEST-ShowIt2Me360");//Show it 2 Me - 360 Music Video
        //youtubeSelectSong("www.youtube.com/watch?v=Os2Ui3-OvbQ", "YTTEST-doommetal");//doommetal
        youtubeSelectSong("https://www.youtube.com/watch?v=QmXotvtA_po","QmXotvtA_po");
    }
    public void youtubeSelectSong(string url, string name)
    {
        SongData selectedSong = new SongData(url, name, true);
        Settings.instance.setSelectedSong(selectedSong);
        setUISelectedSongAux(selectedSong);

        if (state == State.SONG_PATTERN_OPEN_BROWSER)
        {
            setState(State.CREATE_PATTERN_MENU);
        }
        else
        {
            setState(State.MAIN_MENU);
        }
    }
    public void browserSelectSong(int index)
    {
        Debug.Log("Song selected, current state " + state);
        if (state == State.SONG_PATTERN_OPEN_BROWSER)
        {
            setState(State.CREATE_PATTERN_MENU);
            //SongData selectedSong = songList[songBrowserPageIndex * ITEMS_PER_PAGE + index];
            //Settings.instance.setSelectedSong(selectedSong);
            //setSelectedSongAuxUI(selectedSong);
        }
        else
        {
            setState(State.MAIN_MENU);
        }
        SongData selectedSong = songList[songBrowserPageIndex * ITEMS_PER_PAGE + index];
        Settings.instance.setSelectedSong(selectedSong);
        setUISelectedSongAux(selectedSong);

        //if the selected song has custom patterns, preselect that
        if (isInteractive("songContainer/patternContainer/customButton"))
        {
            Settings.instance.selectedPattern = SongData.PatternType.CUSTOM;
            setUISelectedPattern();
        }
        spawner.previewSelectedSong();
    }
    void setUISelectedSongAux(SongData song)
    {
        string songName = prepareSongName(Settings.instance.getSelectedSong().name, true);
        Debug.Log("setUISelectedSongAux" + song + " " + songName);
        songSelect.GetComponentInChildren<Text>().text = songName;
        songSelectPattern.GetComponentInChildren<Text>().text = songName;

        Dictionary<string, string> patternPaths = new Dictionary<string, string>();
        SongData.findPatterns(patternPaths, song.name);

        bool customButtonInteractive = patternPaths.ContainsKey(SongData.getPatternSuffix(4, SongData.PatternType.CUSTOM)) ||
            patternPaths.ContainsKey(SongData.getPatternSuffix(6, SongData.PatternType.CUSTOM)) || patternPaths.ContainsKey(SongData.getPatternSuffix(8, SongData.PatternType.CUSTOM));
        setInteractive("songContainer/patternContainer/customButton", customButtonInteractive);

        bool recButtonInteractive = patternPaths.ContainsKey(SongData.getPatternSuffix(4, SongData.PatternType.REC)) ||
            patternPaths.ContainsKey(SongData.getPatternSuffix(6, SongData.PatternType.REC)) || patternPaths.ContainsKey(SongData.getPatternSuffix(8, SongData.PatternType.REC));
        setInteractive("songContainer/patternContainer/recButton", recButtonInteractive);

        bool layout4PatternExists = Settings.instance.selectedPattern == SongData.PatternType.AUTO ||
            (Settings.instance.selectedPattern == SongData.PatternType.CUSTOM && patternPaths.ContainsKey(SongData.getPatternSuffix(4, SongData.PatternType.CUSTOM))) ||
            (Settings.instance.selectedPattern == SongData.PatternType.REC && patternPaths.ContainsKey(SongData.getPatternSuffix(4, SongData.PatternType.REC)));

        bool layout6PatternExists = Settings.instance.selectedPattern == SongData.PatternType.AUTO ||
            (Settings.instance.selectedPattern == SongData.PatternType.CUSTOM && patternPaths.ContainsKey(SongData.getPatternSuffix(6, SongData.PatternType.CUSTOM))) ||
            (Settings.instance.selectedPattern == SongData.PatternType.REC && patternPaths.ContainsKey(SongData.getPatternSuffix(6, SongData.PatternType.REC)));

        bool layout8PatternExists = Settings.instance.selectedPattern == SongData.PatternType.AUTO ||
            (Settings.instance.selectedPattern == SongData.PatternType.CUSTOM && patternPaths.ContainsKey(SongData.getPatternSuffix(8, SongData.PatternType.CUSTOM))) ||
            (Settings.instance.selectedPattern == SongData.PatternType.REC && patternPaths.ContainsKey(SongData.getPatternSuffix(8, SongData.PatternType.REC)));

        //Debug.Log("Selected pattern AUX UI " +song.name + " " + Settings.instance.selectedPattern + ". Saved patterns:");// + " " + SongData.getPatternSuffix(8, SongData.PatternType.REC) + " " + layout8PatternExists);
        //foreach (string s in patternPaths.Keys)
        //    Debug.Log(s + ":::" + patternPaths[s]);

        setInteractive("layoutContainer/aButton", layout4PatternExists);
        setInteractive("layoutContainer/bButton", layout6PatternExists);
        setInteractive("layoutContainer/cButton", layout8PatternExists);

        int currentLayout = Settings.instance.getNumLanesIndex();
        if (currentLayout == 0 && !layout4PatternExists)
            currentLayout++;
        if (currentLayout == 1 && !layout6PatternExists)
            currentLayout++;
        if (currentLayout == 2 && !layout8PatternExists)
            if (layout4PatternExists)
                currentLayout = 0;
            else
                currentLayout = 1;

        //TODO only do this on song select
        //if (customButtonInteractive)
        //    Settings.instance.selectedPattern = SongData.PatternType.CUSTOM;
        //if (recButtonInteractive)
        //    Settings.instance.selectedPattern = SongData.PatternType.REC;

        Settings.instance.setNumLanesIndex(currentLayout);
        setUISelectedLayout();
        setUISelectedPattern();
        //TODO is there some edge case that could cause trouble? worst case scenario we set pattern to AUTO
    }
    void setSelected(Button button, bool selected)
    {
        ColorBlock colors = button.colors;//.colorMultiplier = 2;
        colors.colorMultiplier = selected ? 3 : 1;
        button.colors = colors;
    }
    void setUISelectedDifficulty()
    {
        setSelected(mainPage.transform.Find("difficultyContainer/aButton").GetComponent<Button>(), Settings.instance.difficultyPlusOneBox);
        setSelected(mainPage.transform.Find("difficultyContainer/bButton").GetComponent<Button>(), Settings.instance.difficultyTrailBoxes);
        //setSelected(mainPage.transform.Find("difficultyContainer/cButton").GetComponent<Button>(), Settings.instance.difficultyObstacles);
        setSelected(mainPage.transform.Find("difficultyContainer/dButton").GetComponent<Button>(), Settings.instance.difficultyTouchToHit);
        setSelected(mainPage.transform.Find("difficultyContainer/eButton").GetComponent<Button>(), Settings.instance.difficultyDirectional);
    }
    void setUISelectedPattern()
    {
        setSelected(mainPage.transform.Find("songContainer/patternContainer/autoButton").GetComponent<Button>(), Settings.instance.selectedPattern == SongData.PatternType.AUTO);
        setSelected(mainPage.transform.Find("songContainer/patternContainer/customButton").GetComponent<Button>(), Settings.instance.selectedPattern == SongData.PatternType.CUSTOM);
        setSelected(mainPage.transform.Find("songContainer/patternContainer/recButton").GetComponent<Button>(), Settings.instance.selectedPattern == SongData.PatternType.REC);
    }
    void setUISelectedBeater()
    {
        setSelected(mainPage.transform.Find("beaterContainer/aButton").GetComponent<Button>(), Settings.instance.getBeater() == 0);
        setSelected(mainPage.transform.Find("beaterContainer/bButton").GetComponent<Button>(), Settings.instance.getBeater() == 1);
        setSelected(mainPage.transform.Find("beaterContainer/cButton").GetComponent<Button>(), Settings.instance.getBeater() == 2);
    }
    void setUISelectedLayout()
    {
        if (mainPage.activeSelf)
            setUISelectedLayout(mainPage);
        if (createPatternPage.activeSelf)
            setUISelectedLayout(createPatternPage);
    }
    void setUISelectedLayout(GameObject menu)
    {
        setSelected(menu.transform.Find("layoutContainer/aButton").GetComponent<Button>(), Settings.instance.getNumLanesIndex() == 0);
        setSelected(menu.transform.Find("layoutContainer/bButton").GetComponent<Button>(), Settings.instance.getNumLanesIndex() == 1);
        setSelected(menu.transform.Find("layoutContainer/cButton").GetComponent<Button>(), Settings.instance.getNumLanesIndex() == 2);
        setSelected(menu.transform.Find("layoutContainer/customButton").GetComponent<Button>(), Settings.instance.isUsingCustomLayouts());
    }
    void setUISelectedSize()
    {
        setSelected(mainPage.transform.Find("sizeContainer/aButton").GetComponent<Button>(), Settings.instance.getHitBoxSize() == 0);
        setSelected(mainPage.transform.Find("sizeContainer/bButton").GetComponent<Button>(), Settings.instance.getHitBoxSize() == 1);
        setSelected(mainPage.transform.Find("sizeContainer/cButton").GetComponent<Button>(), Settings.instance.getHitBoxSize() == 2);
    }
    void setUISelectedTheme()
    {
        setSelected(mainPage.transform.Find("themeContainer/aButton").GetComponent<Button>(), Settings.instance.getTheme() == 0);
        setSelected(mainPage.transform.Find("themeContainer/bButton").GetComponent<Button>(), Settings.instance.getTheme() == 1);
        setSelected(mainPage.transform.Find("themeContainer/cButton").GetComponent<Button>(), Settings.instance.getTheme() == 2);
        setSelected(mainPage.transform.Find("themeContainer/customButton").GetComponent<Button>(), Settings.instance.getTheme() == 3);
    }

    void setInteractive(string uiElement, bool interactive)
    {
        Transform found = mainPage.transform.Find(uiElement);
        if (found == null)
            Debug.Log("UI ELEMENT NOT FOUND " + uiElement);
        else
        {
            found.GetComponent<Button>().interactable = interactive;
        }
    }
    bool isInteractive(string uiElement)
    {
        bool result = false;
        Transform found = mainPage.transform.Find(uiElement);
        if (found == null)
            Debug.Log("UI ELEMENT NOT FOUND " + uiElement);
        else
        {
            result = found.GetComponent<Button>().interactable;
        }
        return result;
    }
    public void browserDetailSong(int index)
    {
        SongData songData = songList[songBrowserPageIndex * ITEMS_PER_PAGE + index];
        int score = 0;
        if (persistentData.persistedUserData.songRecords.ContainsKey(songData.name))
        {
            score = persistentData.persistedUserData.songRecords[songData.name].score;
        }
        showNotification(0, prepareSongName(songData.name, false), "Top score: " + score);
        spawner.previewSelectedSong(songData);
    }
    //todo
    //TODO after recording and saving, song selected label (pattern types available) should be updated
    //TODO when stopping a test pattern, should keep open the create menu like it does for stop while recording
    public void songReadyToRecord(SongData songData)
    {
        Debug.Log("************* SONG READY RECORD, ROWS " + songData.rows.Count);
        if (songData.rows.Count == 3)
            Debug.Log(songData.rows[SongRow.Type.BPM].Count + " bpm, " + songData.rows[SongRow.Type.BEAT].Count + " beats, " + songData.rows[SongRow.Type.BOX].Count + " boxes");
        //if (screenFade.isFaded())
        //screenFade.FadeIn();
        fadeIn();

        bool willPlay = spawner.begin(songData, Settings.PlayMode.RECORD_PATTERN);// spawner.play(clip, songRows);

        Debug.Log("Will play? " + willPlay);
        //TODO show error reason it won't play
        if (!willPlay)
        {
            setState(State.BASIC_MENU);
            showNotification(10, "Ooops!", "Could not load " + songData + "(" + Settings.PlayMode.RECORD_PATTERN + ")");
        }
    }

    public void songReadyToPlayYT(SongData songData)
    {
        spawner.begin(songData, Settings.PlayMode.PLAY);
    }
    public void songReadyToRecordYT(SongData songData)
    {
        Debug.Log("TODO");
    }
    public void songReadyToPlay(SongData songData)
    {
        Debug.Log("************* SONG READY PLAY, ROWS " + songData.rows.Count);
        if (songData.rows.Count == 3)
            Debug.Log(songData.rows[SongRow.Type.BPM].Count + " bpm, " + songData.rows[SongRow.Type.BEAT].Count + " beats, " + songData.rows[SongRow.Type.BOX].Count + " boxes");
        //if (screenFade.isFaded())
        //screenFade.FadeIn();
        fadeIn();

        bool willPlay = false;
        if (state == State.PLAY_PATTERN_TEST && spawner.hasRecordedPattern())
            willPlay = spawner.begin(songData, Settings.PlayMode.TEST_PATTERN_PLAY);
        else
            willPlay = spawner.begin(songData, Settings.PlayMode.PLAY);

        Debug.Log("Will play? " + willPlay);
        //TODO show error reason it won't play
        if (!willPlay)
        {
            setState(State.BASIC_MENU);
            showNotification(10, "Ooops!", "Could not load " + songData + "(" + Settings.PlayMode.RECORD_PATTERN + ")");
        }

    }

    void initYouTubeBrowser()
    {
        Debug.Log("************* INIT YOUTUBE BROWSER");
        songList.Clear();
        songBrowserPageIndex = 0;

        //songList.Add(new SongData())
        for (int i = 0; i < AssetManager.instance.defaultSongCount(); i++)
        {
            //if (AssetManager.instance.getDefaultSong(i) == null)
            //    Debug.Log("INIT_BROWSER: defaultSong " + i + " NULL");
            //if (AssetManager.instance.getDefaultSong(i) != null)
            songList.Add(new SongData(AssetManager.instance.getDefaultSong(i).name, i));
        }
        string[] songPaths = null;

        try
        {
            songPaths = Directory.GetFiles(Path.Combine(AssetManager.instance.getBaseFolder(), "songs"), "*.mp3");
        }
        catch (Exception e)
        {
            Debug.Log("Could not access storage... " + e);
        }
        
        if (songPaths != null)
        for (int i = 0; i < songPaths.Length;i++ )
        {
            songList.Add(new SongData(Path.GetFileName(songPaths[i]), songPaths[i]));
        }

        browserItemNames.Clear();
        int childCount = fileBrowserPage.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = fileBrowserPage.transform.GetChild(i);
            if (child.name.StartsWith("itemContainer"))
            {
                browserItemNames.Add(child.Find("Title").GetComponentInChildren<Text>());
            }
        }

        populateSongBrowserPage();
    }
    void initSongBrowser()
    {
        Debug.Log("************* INIT SONG BROWSER");
        songList.Clear();
        songBrowserPageIndex = 0;

        for (int i = 0; i < AssetManager.instance.defaultSongCount(); i++)
        {
            //if (AssetManager.instance.getDefaultSong(i) == null)
            //    Debug.Log("INIT_BROWSER: defaultSong " + i + " NULL");
            //if (AssetManager.instance.getDefaultSong(i) != null)
            songList.Add(new SongData(AssetManager.instance.getDefaultSong(i).name, i));
        }
        string[] songPaths = null;

        try
        {
            songPaths = Directory.GetFiles(Path.Combine(AssetManager.instance.getBaseFolder(), "songs"), "*.mp3");
        }
        catch (Exception e)
        {
            Debug.Log("Could not access storage... " + e);
        }
        
        if (songPaths != null)
        for (int i = 0; i < songPaths.Length;i++ )
        {
            songList.Add(new SongData(Path.GetFileName(songPaths[i]), songPaths[i]));
        }

        browserItemNames.Clear();
        int childCount = fileBrowserPage.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = fileBrowserPage.transform.GetChild(i);
            if (child.name.StartsWith("itemContainer"))
            {
                browserItemNames.Add(child.Find("Title").GetComponentInChildren<Text>());
            }
        }

        populateSongBrowserPage();
        //prevBrowserButton.interactable = false;
        //nextBrowserButton.interactable = songList.Count > ITEMS_PER_PAGE;

        //for (int i = 0; i < Math.Min(songList.Count, ITEMS_PER_PAGE);i++){
        //    browserItemNames[i].text = songList[i].name;
        //}
    }
    string prepareSongName(string songName, bool justName)
    {
        string result = songName;
        string[] parts = songName.Split(Utils.songNameSplitChar);
        if (parts.Length > 1){
            if (justName)
                result = parts[0];
            else
            {
                result = parts[0] + System.Environment.NewLine + parts[1];
                if (parts.Length == 3 && parts[2] != "na")
                {
                    result += " - " + parts[2];
                }
            }
        }
        return result;
    }
    void populateSongBrowserPage()
    {
        Debug.Log("************* UIMANAGER.populateSongBrowserPage " + songBrowserPageIndex);
        //if (songBrowserPageIndex > 0)
        //    fileBrowserPage.transform.Find("titleContainer").GetComponentInChildren<Text>().text = "Songs in /boxmaniaResources/songs";
        //else
        fileBrowserPage.transform.Find("titleContainer").GetComponentInChildren<Text>().text = "Songs";

        int pageIndex = songBrowserPageIndex * ITEMS_PER_PAGE;
        for (int i = 0; i < ITEMS_PER_PAGE; i++)
        {
            if (songList.Count > pageIndex + i)
            {
                browserItemNames[i].text = prepareSongName(songList[pageIndex + i].name, false);
                browserItemButtonsInteract(browserItemNames[i].transform.parent, true);
            }
            else
            {
                browserItemNames[i].text = "---";
                browserItemButtonsInteract(browserItemNames[i].transform.parent, false);
            }
        }
        prevBrowserButton.interactable = songBrowserPageIndex > 0;

        //Debug.Log(pageIndex + " " + ITEMS_PER_PAGE + " " + songList.Count);
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
        //if (state == State.SONG_OPEN_BROWSER)
        //{
            songBrowserPageIndex--;
            populateSongBrowserPage();
        //}
    }
    public void nextBrowserPage()
    {
        //if (state == State.SONG_OPEN_BROWSER)
        //{
            songBrowserPageIndex++;
            populateSongBrowserPage();
        //}
    }
    void setCustomLayout(bool setCustom)
    {
        Settings.instance.setUsingCustomLayouts(setCustom);
        Vector3[] selectedLayout = Settings.instance.setLanesPosition(Settings.instance.isUsingCustomLayouts());
        //Settings.instance.setNumLanesIndex(2)
        spawner.setLanesPosition(selectedLayout, false);
    }
    void switchCustomLayout()
    {
        setCustomLayout(!Settings.instance.isUsingCustomLayouts());
    }
    void userCustomisesLayout(EventParam eventParam)
    {//user dragged a box so we set current mode to custom and only change the positions that changed
        Settings.instance.setUsingCustomLayouts(true);
        //Settings.instance.setCustomLayout(((LayoutParam)eventParam).lanePos);
        Settings.instance.setCustomLayout(spawner.getLanesPosition());
        setUISelectedLayout();
    }
    public void sliderChange(string name)
    {
        //Debug.Log("Slider change for " + name);
        if (name == "bpmSlider")
        {
            Slider bpmSlider = createSongPage.transform.Find("bpmContainer").GetComponentInChildren<Slider>();
            //if (((int)bpmSlider.value) % 2 == 0)
            //{
                bpmSlider.transform.parent.GetComponentInChildren<Text>().text = bpmSlider.value + " BPM";
                EventManager.TriggerEvent(EventManager.EVENT_BPM_CHANGE, new BeatData(-1, -1, bpmSlider.value));
            //}
        }
        if (name == "crossFadeSlider")
        {
            Slider crossFadeSlider = createSongPage.transform.Find("crossFadeContainer").GetComponentInChildren<Slider>();
            string text = "C";// Math.Abs(crossFadeSlider.value) + " Cross Fade Change";
            if (crossFadeSlider.value < 0)
                text = "L";
            if (crossFadeSlider.value > 0)
                text = "R";

            text += Math.Abs(crossFadeSlider.value) + " Cross Fade Change";

            crossFadeSlider.transform.parent.GetComponentInChildren<Text>().text = text;
            EventManager.TriggerEvent(EventManager.EVENT_CROSS_FADE_CHANGE, new EventParam(crossFadeSlider.value));
        }
    }
    //browserRepoCancel

    public void buttonPressBrowser(string name)
    {
        //borwserRepo...
        if (name == "browserRepoNav"){
            setState(State.SONG_REPO_OPEN_BROWSER);
            uiRepoBrowserPage.init();
        }
        if (name == "browserRepoCancel")//TODO
        {
            //uiRepoBrowserPage.init();
            setState(State.BASIC_MENU);
        }
        if (name == "browserRepoPageNext")
        {
            uiRepoBrowserPage.nextBrowserPage();
        }
        if (name == "browserRepoPagePrev"){
            uiRepoBrowserPage.prevBrowserPage();
        }
        if (name.StartsWith("browserRepoDetails"))
        {//browserRepoDetails|4
            int ind = name.IndexOf(Utils.itemSplitChar) + 1;
            string indStr = name.Substring(ind, name.Length - ind);
            UIRepoBrowser.RepoItem repoSong = uiRepoBrowserPage.browserDetailSong(int.Parse(indStr));
            showNotification(0, repoSong.name, repoSong.link + System.Environment.NewLine + repoSong.description);
        }
        if (name.StartsWith("browserRepoSelect"))
        {//browserRepoSelect|4
            int ind = name.IndexOf(Utils.itemSplitChar) + 1;
            string indStr = name.Substring(ind, name.Length - ind);
            UIRepoBrowser.RepoItem repoSong = uiRepoBrowserPage.browserSelectToDownload(int.Parse(indStr));
            //TODO download song
            //Debug.Log("//TODO download " + repoSong.name + " " + repoSong.link);
            fadeOut(startDownload, new ActionDetails(repoSong));
            //then change state
        }
    }
    void delayedButtonPress(ActionDetails actionDetails)
    {
        //Debug.Log("Delayed button press " + actionDetails);
        buttonPress(actionDetails.buttonPressName);
    }
    public void buttonPress(string name)
    {
        //fadeOut("");
        Debug.Log("Button press received: " + name);
        if (name.EndsWith(FADE_OUT_CMD))//cmnds ending with * mean we do a fade out before starting
        {
            //TODO hide hand models as they get stuck when loading custom theme (objs and all, doesn't happen loading song)
            //fadeOut(name.Substring(0, name.Length - FADE_OUT_CMD.Length) + FADE_IN_CMD);
            //we fade out, then we execute the actual cmd (which should internally decide to fade in)
            fadeOut(delayedButtonPress, new ActionDetails(name.Substring(0, name.Length - FADE_OUT_CMD.Length)));// + FADE_IN_CMD));

            return;
        }

        if (name.EndsWith(FADE_IN_CMD))
        {


            fadeIn(delayedButtonPress, new ActionDetails(name.Substring(0, name.Length - FADE_IN_CMD.Length)));
            return;
        }
        if (name == "notificationOk")
        {
            notificationPage.SetActive(false);
        }
        if (name.StartsWith("help"))
        {
            uiHelpPage.help(name);
        }
        if (name == "backNav")
        {
            setState(State.BASIC_MENU);
            spawner.setLaneAudio(false);
            spawner.setModelMode(false);
            Settings.instance.setNumLanes(prevNumLanes);
        }
        if (name == "createNav")
        {
            if (createNavPage.activeSelf)
                setState(State.BASIC_MENU);
            else
                setState(State.CREATE_MENU);
        }
        if (name == "createPatternNav")
        {
            setState(State.CREATE_PATTERN_MENU);
        }
        if (name == "optionsNav")
        {
            if (state == State.CREATE_SONG_MENU)
                spawner.setLaneAudio(false);
            if (mainPage.activeSelf)
                setState(State.BASIC_MENU);
            else
                setState(State.MAIN_MENU);
        }
        if (name == "createModelNav")
        {
            setState(State.CREATE_MODEL_MENU);
        }
        if (name == "createSongNav")
        {
            setState(State.CREATE_SONG_MENU);
        }
        if (name == "record")
        {
            if (state == State.CREATE_PATTERN_MENU){
                setState(State.RECORD_PATTERN);
                fadeOut(loadSong, null);
            }
            if (state == State.CREATE_SONG_MENU)
            {
                setState(State.RECORD_SONG);
                AudioRecorder.instance.StartRecording(Path.Combine(AssetManager.instance.getBaseFolder(), "testRecord " + getCurrentBpm() + ".wav"));
            }
        }
        if (name == "play")
        {
            EventManager.TriggerEvent(EventManager.EVENT_SONG_PLAY, null);
            setState(State.PLAY);
            fadeOut(loadSong, null);
        }
        if (name == "patternTest")
        {
            //Settings.instance.selectedPattern = SongData.PatternType.REC;
            setState(State.PLAY_PATTERN_TEST);
            fadeOut(loadSong, null);
        }
        if (name == "stop")
        {
            spawner.stop();
            if (state == State.RECORD_PATTERN || state == State.PLAY_PATTERN_TEST)
                setState(State.CREATE_PATTERN_MENU);
            else
                if (state == State.RECORD_SONG)
                {
                    setState(State.CREATE_SONG_MENU);
                    AudioRecorder.instance.StopRecording();
                }
                else
                {
                    setState(State.BASIC_MENU);
                    //showSongResult(null);
                }
        }
        if (name == "songPatternBrowserNav")
        {        
            setState(State.SONG_PATTERN_OPEN_BROWSER);
        }
        if (name.StartsWith("browserRepo"))
            buttonPressBrowser(name);

        if (name == "songBrowserNav")
        {
            setState(State.SONG_OPEN_BROWSER);
        }
        if (name == "cancelBrowser")
        {
            if (state == State.SONG_OPEN_BROWSER)
                setState(State.MAIN_MENU);
            else
                if (state == State.SONG_PATTERN_OPEN_BROWSER)
                    setState(State.CREATE_PATTERN_MENU);
                else
                    setState(State.BASIC_MENU);
        }
        //if (name == "mockSoundCloud")
        //{
        //    showNotification(19, "SoundCloud Connect", null, soundCloudBrowseImage);
        //}
        if (name == "patternAuto")
        {
            Settings.instance.selectedPattern = SongData.PatternType.AUTO;
            //setInteractive("layoutContainer/aButton", true);
            //setInteractive("layoutContainer/bButton", true);
            //setInteractive("layoutContainer/cButton", true);
            setUISelectedSongAux(Settings.instance.getSelectedSong());
        }
        if (name == "patternCustom")
        {
            Settings.instance.selectedPattern = SongData.PatternType.CUSTOM;

            setUISelectedSongAux(Settings.instance.getSelectedSong());
        }
        if (name == "patternRecording")
        {
            Settings.instance.selectedPattern = SongData.PatternType.REC;
            setUISelectedSongAux(Settings.instance.getSelectedSong());
        }
        if (name == "patternSave")
        {
            Spawner.instance.saveRecording();
        }
        if (name == "difficultyPlusOneBox")
        {
            Settings.instance.difficultyPlusOneBox = !Settings.instance.difficultyPlusOneBox;
            setUISelectedDifficulty();
        }
        if (name == "difficultyTouchToHit")
        {
            Settings.instance.difficultyTouchToHit = !Settings.instance.difficultyTouchToHit;
            setUISelectedDifficulty();
        }
        if (name == "difficultyTrailBoxes")
        {
            Settings.instance.difficultyTrailBoxes = !Settings.instance.difficultyTrailBoxes;
            setUISelectedDifficulty();
        }
        if (name == "difficultyDirectional")
        {
            Settings.instance.difficultyDirectional = !Settings.instance.difficultyDirectional;
            setUISelectedDifficulty();
        }



        if (name == "layoutA")
        {
            Settings.instance.setNumLanesIndex(0);
            spawner.setLanesPosition(Settings.instance.getLayout(), false);
            setUISelectedLayout();
        }
        if (name == "layoutB")
        {
            Settings.instance.setNumLanesIndex(1);
            spawner.setLanesPosition(Settings.instance.getLayout(), false);
            setUISelectedLayout();
        }
        if (name == "layoutC")
        {
            Settings.instance.setNumLanesIndex(2);
            spawner.setLanesPosition(Settings.instance.getLayout(), false);
            setUISelectedLayout();
        }
        if (name == "layoutCustom")
        {
            switchCustomLayout();
            setUISelectedLayout();
        }

        if (name == "beaterA")
        {
            Settings.instance.setBeater(0);
            setUISelectedBeater();
        }
        if (name == "beaterB")
        {
            Settings.instance.setBeater(1);
            setUISelectedBeater();
        }
        if (name == "beaterC")
        {
            Settings.instance.setBeater(2);
            setUISelectedBeater();
        }

        if (name == "hitBoxSizeA")
        {
            Settings.instance.setHitBoxSize(0);
            setUISelectedSize();
        }
        if (name == "hitBoxSizeB")
        {
            Settings.instance.setHitBoxSize(1);
            setUISelectedSize();
        }
        if (name == "hitBoxSizeC")
        {
            Settings.instance.setHitBoxSize(2);
            setUISelectedSize();
        }

        if (name == "themeA")
        {
            Settings.instance.setDefaultThemeA();
            setUISelectedTheme();
            fadeIn();
        }
        if (name == "themeB")
        {
            Settings.instance.setDefaultThemeB();
            setUISelectedTheme();
            fadeIn();
        }
        if (name == "themeC")
        {
            Settings.instance.setDefaultThemeC();
            setUISelectedTheme();
            fadeIn();
        }
        if (name == "themeCustom")
        {
            bool isSet = Settings.instance.setCustomTheme();
            if (isSet)
                setUISelectedTheme();
            else
                showNotification(10, "Ooops!", "Could not load themes/customTheme/customTheme.thm in /boxmaniaResources folder");
            fadeIn();
        }
#if !UNITY_EDITOR
        if (name == "quit")
            Application.Quit();
#endif
    }

    private void fadeIn()
    {
        transitionSpace.SetActive(false);
    }
    private void fadeIn(FadeAction fadeAction, ActionDetails actionDetails)
    {
        //if (actionOnNextUpdate != null && actionTimestamp > 0 && actionTimestamp < Time.time)

        //Debug.Log(">>>>>>>>>>>>>>>> FADEIN " + actionDetails);
        transitionSpace.SetActive(false);
        actionTimestamp = Time.time + 0.6f;

        actionOnNextUpdate = fadeAction;
        actionDetailsOnNextUpdate = actionDetails;

        Debug.Log("((((( " + actionOnNextUpdate + " " + actionDetailsOnNextUpdate + " " + actionTimestamp + " " + Time.time);
    }
    private void fadeOut(FadeAction fadeAction, ActionDetails actionDetails)
    {
        //Debug.Log(">>>>>>>>>>>>>>>> FADEOUT " + actionDetails);
        transitionSpace.SetActive(true);

        actionTimestamp = Time.time + 0.6f;

        actionOnNextUpdate = fadeAction;
        actionDetailsOnNextUpdate = actionDetails;
    }

    void showSongResult(EventParam eventParam)
    {
        buttonPress("stop");
        //TODO separate score save for AUTO vs NONAUTO, difficulty, lanes number
        int score = GetComponent<ScoreManager>().getScore();
        //Debug.Log("Show song result " + score);
        SongData songData = Settings.instance.getSelectedSong();
        bool newRecord = false;
        if (persistentData.persistedUserData.songRecords.ContainsKey(songData.name))
        {
            PersistedSongData psd = persistentData.persistedUserData.songRecords[songData.name];
            if (psd.score < score)//new record
            {
                newRecord = true;
                psd.score = score;
            }
        }
        else
        {
            newRecord = true;
            persistentData.persistedUserData.songRecords.Add(songData.name, new PersistedSongData(songData.name, score));
        }
        persistentData.Save();

        string subText = "Score: ";
        if (newRecord)
            subText = "New Top Score: ";
        showNotification(0, prepareSongName(songData.name, false), subText + score);
    }
    //void stoppedSong(EventParam eventParam)
    //{
    //    buttonPress("stop");
    //}
    void OnEnable()
    {
        EventManager.StartListening(EventManager.EVENT_SHOW_NOTIFICATION, showNotificationEvent);//triggered by spawner when song ends
        EventManager.StartListening(EventManager.EVENT_END_OF_SONG, showSongResult);//triggered by spawner when song ends
        EventManager.StartListening(EventManager.EVENT_USER_LANE_LAYOUT_CHANGE, userCustomisesLayout);
        //EventManager.StartListening(EventManager.EVENT_SONG_STOP, stoppedSong);//triggerend by user stop button
    }

    void OnDisable()
    {
        EventManager.StopListening(EventManager.EVENT_SHOW_NOTIFICATION, showNotificationEvent);
        EventManager.StopListening(EventManager.EVENT_END_OF_SONG, showSongResult);
        EventManager.StopListening(EventManager.EVENT_USER_LANE_LAYOUT_CHANGE, userCustomisesLayout);
        //EventManager.StopListening(EventManager.EVENT_SONG_STOP, stoppedSong);
    }

}
