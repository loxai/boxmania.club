using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIHelp : MonoBehaviour
{
    Button nextButton;
    Button prevButton;
    Image image;
    Text title;
    Text description;

    public Sprite[] mainOptionImages;
    public string[] mainOptionTitles = new string[] { "Choose song to play", "You can choose different patterns (when available)",
        "Set the number of lanes (4, 6 or 8)", "Customise your exercice",
        "Customise the difficulty of your challenge",
        "Different beaters for different gameplay and effort",
        "Make it easier or harder to hit lanes",
        "Themes to change the looks","Create your custom style",
        "VFX to make it look nicer"
    };
    public string[] mainOptionTexts = new string[] { "Click on the name of the current song to open the song browser.\r\nThe song browser includes music in boxmaniaResources/songs folder.", "- Auto: a song pattern/map is created automatically. Default.\r\n- Custom: a song pattern found in boxmaniaResources/patterns\r\n- Rec: Your created pattern (through the Create options). Share your best!",
        "More lanes = better coordination required", "You can move the lane (translucid) boxes by pressing " + InputUtils.getDragKeyName(),
        "- Trails: enables trailing boxes (you need to hold the lane active)\r\n- Directional: you can only hit the boxes in the indicated direction\r\n- +1 Beat: adds more boxes to the pattern\r\n- Touch To Hit: easier mode, having the beater in lane is enough to hit the box",
        "- Beater A: simple 'boxing glove', more intense exercise\r\n- Beater B: a hammer... er... hammer time?\r\n- Beater C: a sword? we'll see...",
        "Just changes the Hit Size...",
        "You can choose boxmania.club default themes, or...", "Bring your own assets to make the game look the way you like.\r\nYou can copy your content to the /boxmaniaResources folder through PC.\r\nVisit boxmania.club for more info.",
        "Some visual effects to polish the looks. Eventually. First working mechanics, then the make up."
    };
    public Sprite[] songBrowserImages;
    internal string[] songBrowserTitles = new string[] { "Select a song", "Download more music" };
    internal string[] songBrowserTexts = new string[] { "Browse boxmaniaResources/songs folder for songs to play", "Click More Songs to download more music" };

    public Sprite[] repoBrowserImages;
    internal string[] repoBrowserTitles = new string[] { "Download online content", "Are you a musician?" };
    internal string[] repoBrowserTexts = new string[] { "You can browse the boxmania.club repository for more content.\r\nYou can get new songs and song patterns through the repository.", "Would you like to include a song or repository? Make a request through the forum at boxmania.club" };

    public Sprite[] createPatternImages;
    internal string[] createPatternTitles = new string[] { "Create your own song pattern", "Make a choice", "Different actions", "Save and share" };
    internal string[] createPatternTexts = new string[] { "By default, songs are analysed and an auto pattern is generated.\r\nBut you can also use patterns created by other users or by yourself, using the Create Song Pattern option",
        "Choose the song and number of lanes.\r\nMove the lanes (if desired, you can also do this while recording), dragging them with " + InputUtils.getDragKeyName() + ", then press Record to start recording the session.\r\nYou can test play the pattern once you finish, and save it.",
        "Hit a lane to create a regular beat box.\r\nBefore hitting the lane, press any of these to create a different beat box:\r\n- " + InputUtils.getHoldKeyName() + ": creates a box with trail.\r\n- " + InputUtils.getDirectionalKeyName() + ": creates a directional box that can only be hit one way.",
        "Once a pattern is saved, it'll be stored in the boxmaniaResources/patterns folder. It's a text file that you can edit, tweak and share. Post it online and see if others can handle!"};
    /*
    public Sprite[] songSelectImages;
    internal string[] songSelectTitles = new string[] { "Choose song to play", "You can choose different patterns (when available)" };
    internal string[] songSelectTexts = new string[] { "Click on the name of the current song to open the song browser.\r\nThe song browser includes music in boxmaniaResources/songs folder.", "- Auto: a song pattern/map is created automatically. Default.\r\n- Custom: a song pattern found in boxmaniaResources/patterns\r\n- REC: Your created pattern (through the Create options). Share your best!" };

    public Sprite[] lanesNumImages;
    internal string[] lanesNumTitles = new string[] { "Set the number of lanes (4, 6 or 8)", "Customise your exercice" };
    internal string[] lanesNumTexts = new string[] { "More lanes = better coordination required", "You can move the lane (translucid) boxes by pressing " + InputUtils.getDragKeyName() };

    public Sprite[] difficultyImages;
    internal string[] difficultyTitles = new string[] { "Customise your challenge" };
    internal string[] difficultyTexts = new string[] { "- Trails: enables trailing boxes (you need to hold the lane active)\r\n- Directional: you can only hit the boxes in the indicated direction\r\n- +1 Beat: adds more boxes to the pattern\r\n- Touch To Hit: easier mode, having the beater in lane is enough to hit the box" };

    public Sprite[] beaterImages;
    internal string[] beaterTitles = new string[] { "Different beaters for different gameplay" };
    internal string[] beaterTexts = new string[] { "- Beater A: simple 'boxing glove', more intense exercise\r\n- Beater B: a hammer... er... hammer time?\r\n- Beater C: a sword? we'll see..." };

    public Sprite[] hitSizeImages;
    internal string[] hitSizeTitles = new string[] { "Easier or harder to hit lanes" };
    internal string[] hitSizeTexts = new string[] { "Just changes the sizes..." };

    public Sprite[] themeImages;
    internal string[] themeTitles = new string[] { "Change the looks","Create your custom style" };
    internal string[] themeTexts = new string[] { "You can choose boxmania.club default themes, or...", "Bring your own assets to make the game look the way you like.\r\nYou can copy your content to the /boxmaniaResources folder through PC.\r\nVisit boxmania.club for more info." };

    public Sprite[] vfxImages;
    internal string[] vfxTitles = new string[] { "Making it look nicer" };
    internal string[] vfxTexts = new string[] { "Some visual effects to polish the looks. Eventually. First working mechanics, then the make up." };
    */
    public Sprite[] currentImages;
    public string[] currentTitles;
    public string[] currentTexts;

    int pageIndex = 0;

    void Awake()
    {
        nextButton = transform.Find("imageContainer/nextButton").GetComponent<Button>();
        prevButton = transform.Find("imageContainer/prevButton").GetComponent<Button>();
        image = transform.Find("imageContainer/Image").GetComponent<Image>();
        title = transform.Find("Text").GetComponent<Text>();
        description = transform.Find("SubText").GetComponent<Text>();
    }
    internal void help(string name)
    {
        /*
        if (name == "helpSongSelect")
        {
            gameObject.SetActive(true);
            pageIndex = 0;
            currentImages = songSelectImages;
            currentTitles = songSelectTitles;
            currentTexts = songSelectTexts;
            populatePage();
        }
        if (name == "helpLanesNum")
        {
            gameObject.SetActive(true);
            pageIndex = 0;
            currentImages = lanesNumImages;
            currentTitles = lanesNumTitles;
            currentTexts = lanesNumTexts;
            populatePage();
        }
        if (name == "helpDifficulty")
        {
            gameObject.SetActive(true);
            pageIndex = 0;
            currentImages = difficultyImages;
            currentTitles = difficultyTitles;
            currentTexts = difficultyTexts;
            populatePage();
        }
        if (name == "helpBeater")
        {
            gameObject.SetActive(true);
            pageIndex = 0;
            currentImages = beaterImages;
            currentTitles = beaterTitles;
            currentTexts = beaterTexts;
            populatePage();
        }
        if (name == "helpHitSize")
        {
            gameObject.SetActive(true);
            pageIndex = 0;
            currentImages = hitSizeImages;
            currentTitles = hitSizeTitles;
            currentTexts = hitSizeTexts;
            populatePage();
        }
        if (name == "helpTheme")
        {
            gameObject.SetActive(true);
            pageIndex = 0;
            currentImages = themeImages;
            currentTitles = themeTitles;
            currentTexts = themeTexts;
            populatePage();
        }
        if (name == "helpVfx")
        {
            gameObject.SetActive(true);
            pageIndex = 0;
            currentImages = vfxImages;
            currentTitles = vfxTitles;
            currentTexts = vfxTexts;
            populatePage();
        }
         * */
        if (name == "helpMainOptions")
        {
            gameObject.SetActive(true);
            pageIndex = 0;
            currentImages = mainOptionImages;
            currentTitles = mainOptionTitles;
            currentTexts = mainOptionTexts;
            populatePage();
        }
        if (name == "helpSongBrowser")
        {
            gameObject.SetActive(true);
            pageIndex = 0;
            currentImages = songBrowserImages;
            currentTitles = songBrowserTitles;
            currentTexts = songBrowserTexts;
            populatePage();
        }
        if (name == "helpRepoBrowser")
        {
            gameObject.SetActive(true);
            pageIndex = 0;
            currentImages = repoBrowserImages;
            currentTitles = repoBrowserTitles;
            currentTexts = repoBrowserTexts;
            populatePage();
        }
        if (name == "helpCreatePattern")
        {
            gameObject.SetActive(true);
            pageIndex = 0;
            currentImages = createPatternImages;
            currentTitles = createPatternTitles;
            currentTexts = createPatternTexts;
            populatePage();
        }
        if (name == "helpPrev")
        {
            pageIndex--;
            populatePage();
        }
        if (name == "helpNext")
        {
            pageIndex++;
            populatePage();
        }
        if (name == "helpOk")
        {
            gameObject.SetActive(false);
        }
    }
    void populatePage()
    {
        if (currentImages.Length > pageIndex)
            image.sprite = currentImages[pageIndex];
        if (currentTitles.Length > pageIndex)
            title.text = currentTitles[pageIndex];
        if (currentTexts.Length > pageIndex)
            description.text = currentTexts[pageIndex];

        if (pageIndex > 0)
            prevButton.interactable = true;
        else
            prevButton.interactable = false;

        if (pageIndex + 1 < currentImages.Length || pageIndex + 1 < currentTitles.Length || pageIndex + 1 < currentTexts.Length)
            nextButton.interactable = true;
        else
            nextButton.interactable = false;
    }
}
