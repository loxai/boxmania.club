using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    //public GameObject notificationLocation;
    int score;
    int combo;
    int maxCombo = 10;
    GameObject currentNotification;
    public GameObject scoreUI;
    Text scoreUIText;

    Vector3 originalScale;
    Vector3 minimalScale = new Vector3(0.1f, 0.1f, 0.1f);
    bool growing = true;
    // Start is called before the first frame update
    void Awake()
    {
        scoreUIText = scoreUI.transform.GetComponentInChildren<Text>();
    }

    public int getScore()
    {
        return score;
    }
    // Update is called once per frame
    void Update()
    {
        if (currentNotification == null)
            return;

        if (growing)
            currentNotification.transform.localScale = Vector3.Lerp(currentNotification.transform.localScale, originalScale, Time.deltaTime * 3);
        else
            currentNotification.transform.localScale = Vector3.Lerp(currentNotification.transform.localScale, minimalScale, Time.deltaTime);
        if (currentNotification.transform.localScale == originalScale)
            growing = false;
        if (currentNotification.transform.localScale == minimalScale)
        {
            currentNotification.transform.localScale = originalScale;
            AssetManager.instance.freeScoreNotification(currentNotification);
            currentNotification = null;
        }
    }
    void hit(EventParam eventParam)
    {
        if (!scoreUIText.isActiveAndEnabled)//ignore score notifications if the scoreUI (showing points) is not enabled
            return;
        float precision = eventParam.float1;
        int notificationLevel = 1;
        if (precision < 0.75)
            notificationLevel = 2;
        if (precision < 0.4)
            notificationLevel = 3;
        if (precision < 0.1)
            notificationLevel = 4;

        initNotification(notificationLevel);

        if (combo < maxCombo)
            combo += 1;
        EventManager.TriggerEvent(EventManager.EVENT_COMBO_CHAIN, new EventParam((float)combo / maxCombo));
        //score += 100 + 10 * combo + (int)Mathf.Pow(10, notificationLevel);
        score += 100 + 10 * combo * notificationLevel;

        scoreUIText.text = score + System.Environment.NewLine;
        if (combo > 2)
            scoreUIText.text += " x " + combo;

        //Debug.Log(notificationLevel + " " + score + " " + combo);
    }
    void miss(EventParam eventParam)
    {
        if (!scoreUIText.isActiveAndEnabled)//ignore score notifications if the scoreUI (showing points) is not enabled
            return;
        initNotification(0);
        combo = 0;
        EventManager.TriggerEvent(EventManager.EVENT_COMBO_CHAIN, new EventParam((float)combo / maxCombo));
    }
    void initNotification(int level)
    {
        if (currentNotification != null)
        {
            currentNotification.transform.localScale = originalScale;
            AssetManager.instance.freeScoreNotification(currentNotification);
        }

        currentNotification = AssetManager.instance.getScoreNotification(level);
        originalScale = currentNotification.transform.localScale;

        minimalScale = originalScale / 2;

        currentNotification.transform.localScale = minimalScale;
        growing = true;

        //currentNotification.transform.position = notificationLocation.transform.position;
    }
    //public void setEnabled(bool enabled)
    //{
    //    this.enabled = enabled;
    //    reset(null);
    //}
    void reset(EventParam eventParam)
    {
        score = 0;
        scoreUIText.text = "0";
        combo = 0;

        if (currentNotification != null)
        {
            currentNotification.transform.localScale = originalScale;
            AssetManager.instance.freeScoreNotification(currentNotification);
        }
    }
    void finished(EventParam eventParam)
    {
        //TODO post result screen, save score, blah blah
    }
    void OnEnable()
    {
        EventManager.StartListening(EventManager.EVENT_BEATBOX_HIT, hit);
        EventManager.StartListening(EventManager.EVENT_BEATBOX_MISS, miss);
        EventManager.StartListening(EventManager.EVENT_SONG_PLAY, reset);
        EventManager.StartListening(EventManager.EVENT_SONG_STOP, finished);
    }

    void OnDisable()
    {
        EventManager.StopListening(EventManager.EVENT_BEATBOX_HIT, hit);
        EventManager.StopListening(EventManager.EVENT_BEATBOX_MISS, miss);
        EventManager.StopListening(EventManager.EVENT_SONG_PLAY, reset);
        EventManager.StopListening(EventManager.EVENT_SONG_STOP, finished);
    }
}
