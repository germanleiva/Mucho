using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public enum Gesture { LEFTHANDNONE, LEFTHANDMENUOPEN, LEFTHANDGRAB, LEFTHANDPINCH, LEFTHANDTHROW, RIGHTHANDNONE, RIGHTHANDGRAB, RIGHTHANDTHROW, RIGHTHANDPINCH};

    public Recordable leftHand, rightHand;

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
            DebugLogger.Instance.Log("Processing gestures");
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

            DebugLogger.Instance.Log("Processing collisions between assets");


       }
       else if (Manager.Instance.currAppState == Manager.AppState.RECORDING)
       {} 
    }

    public void SetAssetInContactWithLeftHand(Recordable assetName, bool isContact)
    {
        if (isContact)
        {
            //DebugLogger.Instance.Log("Left Hand in contact with " + assetName.name);
            assetInContactWithLeftHand = assetName;
        }
        else
        {
            //DebugLogger.Instance.Log("Left Hand no longer in contact with " + assetName.name);
            assetInContactWithLeftHand = null;
        }
    }

    public void SetAssetInContactWithRightHand(Recordable assetName, bool isContact)
    {
        if (isContact)
        {
            //DebugLogger.Instance.Log("Right Hand in contact with " + assetName.name);
            assetInContactWithRightHand = assetName;
        }
        else
        {
            //DebugLogger.Instance.Log("Right Hand no longer in contact with " + assetName.name);
            assetInContactWithRightHand = null;
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
