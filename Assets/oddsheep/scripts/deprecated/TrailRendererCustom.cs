using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailRendererCustom : MonoBehaviour
{
            //trail.material = AssetManager.instance.trailMaterial;
            //trail.alignment = LineAlignment.TransformZ;
            //trail.startWidth = 0.5f;
            //trail.endWidth = 0.2f;
            //trail.material.SetColor("_BaseColor", color);
            ////trail.startColor = Color.yellow;
            ////trail.endColor = Color.black;
            //trail.time = trailTimestamp;
            //trail.enabled = true;

    public GameObject quad;
    public Material material;
    float time;
    bool enabled;

    Vector3 startPos;
    Vector3 endPos;
    float totalDistance;

    internal void init(Vector3 startPos, Vector3 endPos, float time){
        this.startPos = startPos;
        this.endPos = endPos;
        this.time = time;

        //quad = AssetManager.instance.getTrailQuad();
        //material = quad.GetComponent<Renderer>().material;
    }
    internal void addTime(float t){
        time += t;
    }
    internal void setActive(bool a){
        if (quad != null && !a){
            quad.SetActive(false);
            //AssetManager.instance.freeTrailQuad(quad);
            quad = null;
        }
        if (quad == null && a){
            //quad = AssetManager.instance.getTrailQuad();
            //material = quad.GetComponent<Renderer>().material;
            //quad.SetActive(true);
        }
    }
    internal void setColor(Color c){
        material.SetColor("_BaseColor", c);
    }
    // Start is called before the first frame update
    void Start()
    {
        transform.rotation = Quaternion.FromToRotation(startPos, endPos);
        totalDistance = Vector3.Distance(startPos,endPos);
    }

    // Update is called once per frame
    void Update()
    {
        if (quad != null && quad.activeSelf){
            float dist = Vector3.Distance(startPos, quad.transform.position);
            quad.transform.position = Vector3.Lerp(startPos, endPos, dist / totalDistance);
        }
    }
}
