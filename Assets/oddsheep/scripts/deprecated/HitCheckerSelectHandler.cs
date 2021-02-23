using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HitCheckerSelectHandler : MaskableGraphic, ISelectHandler, IDeselectHandler
{

    public void OnSelect(BaseEventData eventData)
    {
        Debug.Log("Selected! " + eventData.selectedObject.name);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        Debug.Log("DeSelected! " + eventData.selectedObject.name);
    }
}
