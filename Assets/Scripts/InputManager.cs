using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public enum Gesture { LEFTHANDNONE, LEFTHANDMENUOPEN, LEFTHANDGRAB, LEFTHANDPINCH, LEFTHANDTHROW, RIGHTHANDNONE, RIGHTHANDGRAB, RIGHTHANDTHROW, RIGHTHANDPINCH};

    public Recordable leftHand, rightHand;

    //public 

    public Recordable assetInContactWithLeftHand, assetInContactWithRightHand;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this; 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ProcessEvents();        
    }

    public void SetLeftHandGesture(string gestureStr)
    {
        //DebugLogger.Instance.Log("Gesture: " + gestureStr);
        leftHand.currentGesture = (Gesture)System.Enum.Parse(typeof(Gesture), gestureStr);
        leftHand.SetGestureText(GestureToString(leftHand.currentGesture));
        //SelectTaskForGesture(currentLeftHandGesture);
        //leftHandGestureText.text = GestureToString(leftHand.currentGesture );
    }

    public void SetRightHandGesture(string gestureStr)
    {
        //DebugLogger.Instance.Log("Gesture: " + gestureStr);
        rightHand.currentGesture = (Gesture)System.Enum.Parse(typeof(Gesture), gestureStr);
        rightHand.SetGestureText(GestureToString(rightHand.currentGesture));
        //SelectTaskForGesture(currentRightHandGesture);
        

        //if we are LIVE
        //getTheStateMachine, an make the StateMachine process the current gesture
        //How do we get the collision events?
    }

    void ProcessEvents()
    {
       if (Manager.Instance.currAppState == Manager.AppState.LIVE)
       {
            /*DebugLogger.Instance.Log("Processing gestures in live mode");
            DebugLogger.Instance.Log("Left Hand: " + leftHand.currentGesture.ToString() + " Right Hand: " + rightHand.currentGesture.ToString());

            DebugLogger.Instance.Log("Processing collisions between hands and assets");
            if (assetInContactWithLeftHand != null)
            {
                DebugLogger.Instance.Log("Left Hand in contact with " + assetInContactWithLeftHand.name);
                //assetInContactWithLeftHand.ProcessCollision(leftHand);
            }
            if (assetInContactWithRightHand != null)
            {
                DebugLogger.Instance.Log("Right Hand in contact with " + assetInContactWithRightHand.name);
                //assetInContactWithRightHand.ProcessCollision(rightHand);
            }

            DebugLogger.Instance.Log("Processing collisions between assets");*/


       }
       else if (Manager.Instance.currAppState == Manager.AppState.INIT || Manager.Instance.currAppState == Manager.AppState.PLAYBACK || Manager.Instance.currAppState == Manager.AppState.ASSETRECORDING)
       {
            DebugLogger.Instance.Log("Processing gestures in live mode");
            DebugLogger.Instance.Log("Left Hand: " + leftHand.currentGesture.ToString() + " Right Hand: " + rightHand.currentGesture.ToString());

            DebugLogger.Instance.Log("Processing collisions between hands and assets");

            if (assetInContactWithLeftHand != null)
            {
                DebugLogger.Instance.Log("Left Hand in contact with " + assetInContactWithLeftHand.name);
                if (leftHand.currentGesture == Gesture.LEFTHANDPINCH)
                {
                    DebugLogger.Instance.Log("Left Hand is pinching " + assetInContactWithLeftHand.name);
                    //AttachAssetToHand(assetInContactWithLeftHand, leftHand);
                    assetInContactWithLeftHand.Follow(leftHand.transform);
                }
                else if (leftHand.currentGesture == Gesture.LEFTHANDNONE)
                {
                    DebugLogger.Instance.Log("Left Hand is no longer pinching " + assetInContactWithLeftHand.name);
                    assetInContactWithLeftHand.Unfollow();
                }               
            }
            else
            {
                //assetInContactWithLeftHand.Unfollow();
            }


            if (assetInContactWithRightHand != null)
            {
                DebugLogger.Instance.Log("Right Hand in contact with " + assetInContactWithRightHand.name);
                if (rightHand.currentGesture == Gesture.RIGHTHANDPINCH)
                {
                    DebugLogger.Instance.Log("Right Hand is pinching " + assetInContactWithRightHand.name);
                    assetInContactWithRightHand.Follow(rightHand.transform);
                }    
                else if (rightHand.currentGesture == Gesture.RIGHTHANDNONE)
                {
                    DebugLogger.Instance.Log("Right Hand is no longer pinching " + assetInContactWithRightHand.name);
                    assetInContactWithRightHand.Unfollow();
                }
            }
            else
            {
                //assetInContactWithRightHand.Unfollow();
            }

            //DebugLogger.Instance.Log("Processing collisions between assets");

       } 
    }

    public void SetAssetInContactWithLeftHand(Recordable asset)
    {
        if (assetInContactWithLeftHand != asset) //assign only if it is a different asset
        {
            assetInContactWithLeftHand = asset;
        }
    }

    public void SetAssetInContactWithRightHand(Recordable asset)
    {
        if (assetInContactWithRightHand != asset) //assign only if it is a different asset
        {
            assetInContactWithRightHand = asset;
        }
    }



    public string GestureToString(Gesture gesture)
    {
        return gesture switch
        {
            Gesture.LEFTHANDNONE => ("None"),
            Gesture.LEFTHANDMENUOPEN => ("Menu Open"),
            Gesture.LEFTHANDGRAB => ("Closed"),
            Gesture.LEFTHANDPINCH => ("Pinch"),
            Gesture.LEFTHANDTHROW => ("Open"),
            Gesture.RIGHTHANDGRAB => ("Closed"),
            Gesture.RIGHTHANDNONE => ("None"),
            Gesture.RIGHTHANDTHROW => ("Open"),
            Gesture.RIGHTHANDPINCH => ("Pinch"),
            _ => ("ERROR"),
        };
    }

}

public class GestureSequence
{
    public int StartIndex { get; set; }
    public int Length { get; set; }
    public InputManager.Gesture GestureType { get; set; }
}
