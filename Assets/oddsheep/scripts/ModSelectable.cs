using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModSelectable : MonoBehaviour
{
    HitChecker hc;
    Material material;

    static ModSelectable leftSelected;
    static ModSelectable rightSelected;

    internal void setHitChecker(HitChecker hc)
    {
        this.hc = hc;
        material = gameObject.GetComponent<Renderer>().material;
    }
    private void OnTriggerEnter(Collider other)
    {
        bool rightController = other.tag == "Right";

        if (rightController && rightSelected != null)
            rightSelected.deselect();
        if (!rightController && leftSelected != null)
            leftSelected.deselect();

        hc.audioModsSelect(rightController);
        material.SetColor("_BaseColor", rightController? Color.green: Color.blue);

        if (rightController)
            rightSelected = this;
        else
            leftSelected = this;
    }
    private void OnTriggerExit(Collider other)
    {
        //hc.audioModsDeselect();
        //material.SetColor("_BaseColor", Color.gray);
    }
    void deselect()
    {
        hc.audioModsDeselect();
        material.SetColor("_BaseColor", Color.gray);
    }
}
