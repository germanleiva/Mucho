using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class AssetMenu : MonoBehaviour
{
    public Transform target; 
    public Transform lineEnd;
    public Transform lineStart;
    public LineRenderer lineRenderer;
    bool isGrabbed = false;

    public GameObject rootObject;

    void Awake()
    {
        //set position 60cm in front of target
        //transform.position = target.position + target.forward * 0.6f;
    }

    // Update is called once per frame
    void Update()
    {
        if(!isGrabbed) 
        {
            transform.LookAt(target);
            //Change the position of the transform to be 20cm above the lineEnd gameobject's highest point
            transform.position = lineEnd.position + new Vector3(0, 0.2f, 0);
        }

        lineRenderer.SetPosition(0, lineStart.position);
        lineRenderer.SetPosition(1, lineEnd.position);
    }

    public void MenuGrabbed(bool _isGrabbed)
    {   
        isGrabbed = _isGrabbed;
    }

    public void toggleAssetMenu()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    public void hideAssetMenu()
    {
        gameObject.SetActive(false);
    }

    public void DeleteRootObject()
    {
        Destroy(rootObject);
    }
}
