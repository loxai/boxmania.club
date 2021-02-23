using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboVisual : MonoBehaviour
{
    public Material mat = null;
    public Color comboHighlight;
    public GameObject effect;
    public GameObject effect2;
    Color originalColor;

    void Awake()
    {
        if (mat == null)
        {
            mat = gameObject.GetComponentInChildren<Renderer>().materials[0];
        }

        if (mat != null){
            originalColor = mat.GetColor("_BaseColor");
        }
        if (effect != null)
                effect.SetActive(false);
        if (effect2 != null)
                effect2.SetActive(false);
    }
    // Start is called before the first frame update
    //void Start()
    //{
        
    //}

    // Update is called once per frame
    void Update()
    {
        
    }
    void combo(EventParam eventParam){
        if (mat != null)
            mat.SetColor("_BaseColor", new Color(originalColor.r + comboHighlight.r * eventParam.float1, originalColor.g + comboHighlight.g * eventParam.float1, originalColor.b + comboHighlight.b * eventParam.float1, originalColor.a + comboHighlight.a * eventParam.float1));
        if (effect != null)
            if (eventParam.float1 > 0.9f)
                effect.SetActive(true);
            else
                effect.SetActive(false);
        if (effect2 != null)
            if (eventParam.float1 > 0.9f)
                effect2.SetActive(true);
            else
                effect2.SetActive(false);
    }
    void OnEnable()
    {
        EventManager.StartListening(EventManager.EVENT_COMBO_CHAIN, combo);
    }

    void OnDisable()
    {
        EventManager.StopListening(EventManager.EVENT_COMBO_CHAIN, combo);
    }
}
