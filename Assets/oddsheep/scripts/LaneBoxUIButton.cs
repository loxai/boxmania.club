using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LaneBoxUIButton : Button
{
    HitChecker hc;
    //Material material;

    void Awake()
    {
        //material = transform.parent.GetComponentInChildren<Image>().color;
    }
    internal void setHitChecker(HitChecker hc)
    {
        this.hc = hc;
    }
    //TODO find way to determine which controller triggered onSelect
    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        bool mockRightController = true;
        hc.audioModsSelect(mockRightController);
        transform.GetComponent<Image>().color = mockRightController ? Color.green : Color.blue;
    }
    public override void OnDeselect(BaseEventData eventData)
    {
        base.OnDeselect(eventData);
        hc.audioModsDeselect();
        transform.GetComponent<Image>().color = Color.gray;
    }
}
