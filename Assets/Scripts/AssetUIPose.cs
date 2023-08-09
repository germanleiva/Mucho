using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetUIPose : MonoBehaviour
{
    public Transform target; 
    public Transform lineEnd;
    public Transform lineStart;
    public LineRenderer lineRenderer;
    bool isGrabbed = false;

    // Update is called once per frame
    void Update()
    {
        if(!isGrabbed) 
        {
            transform.LookAt(target);
        }

        lineRenderer.SetPosition(0, lineStart.position);
        lineRenderer.SetPosition(1, lineEnd.position);
    }

    public void UIGrabbed(bool _isGrabbed)
    {   
        isGrabbed = _isGrabbed;
        if (isGrabbed)
        {

        }
    }
}
