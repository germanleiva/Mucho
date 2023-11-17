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
    
    void OnTriggerEnter(Collider other)
    {
        //DebugLogger.Instance.Log("FollowLineTrigger: OnTriggerEnter, collision with " + other.gameObject.name);
        //Check if the parent of the other collider has the FollowLine component
        if(other.gameObject.name != "FollowGuideSphere")
        {
            //DebugLogger.Instance.Log("FollowGuideSphere not found");
            return;
        }

        if(other.gameObject.transform.parent?.GetComponentInChildren<FollowLine>() == null)
        {
            //DebugLogger.Instance.Log("FollowLineTrigger: No FollowLine component found for collider " + other.gameObject.name);
            return;
        }
        
        FollowLine followLine = other.gameObject.transform.parent.GetComponentInChildren<FollowLine>();

        if(followLine != null)
        {
            //followLine.DeactivateFollowLine();
            renderer.material = highlightMaterial;  
            if(followTargetType == FollowTargetType.LEFTHAND)
            {                
                followLine.FollowLineAction = () => { followLine.asset.GetComponent<Recordable>().AttachToLeftHand(); renderer.material = defaultMaterial;};             
                //followLine.asset.GetComponent<Recordable>().AttachToLeftHand();
            }
            else if(followTargetType == FollowTargetType.RIGHTHAND)
            {                
                followLine.FollowLineAction = () => { followLine.asset.GetComponent<Recordable>().AttachToRightHand(); renderer.material = defaultMaterial;};
                //followLine.asset.GetComponent<Recordable>().AttachToRightHand();
            }
            else if(followTargetType == FollowTargetType.LEFTFOCUS)
            {
                followLine.FollowLineAction = () => { followLine.asset.GetComponent<Recordable>().AttachToLeftHandFocusSquare(); renderer.material = defaultMaterial;};
            }
            else if(followTargetType == FollowTargetType.RIGHTFOCUS)
            {
               followLine.FollowLineAction = () => { followLine.asset.GetComponent<Recordable>().AttachToRightHandFocusSquare(); renderer.material = defaultMaterial;};
            }
            else if(followTargetType == FollowTargetType.GAZEFOCUS)
            {
                followLine.FollowLineAction = () => { followLine.asset.GetComponent<Recordable>().AttachToGazeFocusSquare(); renderer.material = defaultMaterial;};
            }
            else
            {
                DebugLogger.Instance.Log("FollowLineTrigger: No follow target type specified");
            }
            
        }
    }

    void OnTriggerExit(Collider other)
    {
        //DebugLogger.Instance.Log("FollowLineTrigger: OnTriggerExit, collision with " + other.gameObject.name);
        //Check if the parent of the other collider has the FollowLine component

        if(other.gameObject.name != "FollowGuideSphere")
        {
            //DebugLogger.Instance.Log("FollowGuideSphere not found");
            return;
        }

        if(other.gameObject.transform.parent?.GetComponentInChildren<FollowLine>() == null)
        {
            //DebugLogger.Instance.Log("FollowLineTrigger: No FollowLine component found for collider " + other.gameObject.name);
            return;
        }
        
        FollowLine followLine = other.gameObject.transform.parent.GetComponentInChildren<FollowLine>();

        if(followLine != null)
        {
            //followLine.ResetFollowLine();
            renderer.material = defaultMaterial;
            followLine.FollowLineAction = null;
        }
    }
}
