using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public class FollowLineTrigger : MonoBehaviour
{
    public enum FollowTargetType { LEFTHAND, RIGHTHAND, LEFTFOCUS, RIGHTFOCUS, GAZEFOCUS};

    public FollowTargetType followTargetType;

    new public Renderer renderer;
    public Material highlightMaterial;
    Material defaultMaterial;

    // Start is called before the first frame update
    void Start()
    {
        defaultMaterial = renderer.material;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void OnCollisionEnter(Collision collision)
    {
        //DebugLogger.Instance.Log("FollowLineTrigger: OnTriggerEnter, collision with " + collision.gameObject.name);
        //Check if the parent of the other collider has the FollowLine component
        
        if(collision.gameObject.name != "FollowGuideSphere")
        {
            //DebugLogger.Instance.Log("FollowGuideSphere not found");
            return;
        }
        
        FollowLine followLine = collision.gameObject.transform.parent.parent.GetComponentInChildren<FollowLine>();

        if(followLine != null)
        {
            //DebugLogger.Instance.Log("FollowLineTrigger: FollowLine found in " + collision.gameObject.transform.parent.parent.name);
            //followLine.DeactivateFollowLine();
            renderer.material = highlightMaterial;
            var asset = followLine.asset.GetComponent<Asset>();
            
            followLine.FollowLineAction = () =>
            {
                asset.RecordFollow(followTargetType); 
                renderer.material = defaultMaterial;
            };
        }
    }

    void OnCollisionExit(Collision collision)
    {
        //DebugLogger.Instance.Log("FollowLineTrigger: OnTriggerExit, collision with " + collision.gameObject.name);
        //Check if the parent of the other collider has the FollowLine component

        if(collision.gameObject.name != "FollowGuideSphere")
        {
            //DebugLogger.Instance.Log("FollowGuideSphere not found");
            return;
        }
        
        FollowLine followLine = collision.gameObject.transform.parent.parent.GetComponentInChildren<FollowLine>();

        if(followLine != null)
        {
            //followLine.ResetFollowLine();
            renderer.material = defaultMaterial;
            followLine.FollowLineAction = null;
        }
    }
}
