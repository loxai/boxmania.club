using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MousePointer : MonoBehaviour
{
    public Camera camera;

    // Update is called once per frame
    void Update()
    {
        //TODO this and some other scripts should be completely disabled for vr mode
#if UNITY_STANDALONE || UNITY_EDITOR
        checkHit();
        if (Input.GetMouseButtonDown(0))
        {
            checkHit(true);
        }
#endif
    }
    void checkHit(bool click = false)
    {
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.tag == "NonVRUIItem")
            {
                //Debug.Log("Hitting " + hit.transform.name + " " + hit.transform.parent.name);
                Button button = hit.transform.parent.GetComponent<Button>();
                if (button != null && button.interactable)
                {
                    //Debug.Log("Hitting " + button.name);
                    if (click)
                        button.onClick.Invoke();
                    button.Select();//.Invokce();
                }
            }
            // Do something with the object that was hit by the raycast.
        }
    }
    void RaycastWorldUI()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);

            pointerData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            Debug.Log("Results at " + Input.mousePosition + " ?" + results.Count);
            if (results.Count > 0)
            {
                Debug.Log("Results " + results.Count);
                //WorldUI is my layer name
                if (results[0].gameObject.layer == LayerMask.NameToLayer("UI"))
                {
                    string dbg = "Root Element: {0} \n GrandChild Element: {1}";
                    Debug.Log(string.Format(dbg, results[results.Count - 1].gameObject.name, results[0].gameObject.name));
                    //Debug.Log("Root Element: "+results[results.Count-1].gameObject.name);
                    //Debug.Log("GrandChild Element: "+results[0].gameObject.name);
                    results.Clear();
                }
            }
        }
    }
}
