using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public class FollowLineTrigger : MonoBehaviour
{
    public enum FollowTargetType { LEFTHAND, RIGHTHAND, LEFTFOCUS, RIGHTFOCUS, GAZEFOCUS};

    public FollowTargetType followTargetType;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void OnTriggerEnter(Collider other)
    {
        DebugLogger.Instance.Log("FollowLineTrigger: OnTriggerEnter, collision with " + other.name);
        //Check if the parent of the other collider has the FollowLine component
        FollowLine followLine = other.transform.parent.GetComponent<FollowLine>();
        if(followLine != null)
        {
            followLine.Deactivate();
            if(followTargetType == FollowTargetType.LEFTHAND)
            {
                followLine.asset.GetComponent<Recordable>().AttachToLeftHand();
            }
            else if(followTargetType == FollowTargetType.RIGHTHAND)
            {
                followLine.asset.GetComponent<Recordable>().AttachToRightHand();
            }
            else if(followTargetType == FollowTargetType.LEFTFOCUS)
            {
                followLine.asset.GetComponent<Recordable>().AttachToLeftHandFocusSquare(); 
            }
            else if(followTargetType == FollowTargetType.RIGHTFOCUS)
            {
               followLine.asset.GetComponent<Recordable>().AttachToRightHandFocusSquare();
            }
            else if(followTargetType == FollowTargetType.GAZEFOCUS)
            {
                followLine.asset.GetComponent<Recordable>().AttachToHeadFocusSquare();
            }
            else
            {
                DebugLogger.Instance.Log("FollowLineTrigger: No follow target type specified");
            }
            
        }
    }
}
