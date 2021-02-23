using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationRotation : MonoBehaviour
{
    public Vector3 axis;
    public float speed;

    // Update is called once per frame
    void Update()
    {
        transform.eulerAngles += axis * speed * Time.deltaTime;
    }
}
