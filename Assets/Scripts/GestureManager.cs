using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GestureManager : MonoBehaviour
{
    //public enum Gesture { LEFTHANDNONE, LEFTHANDMENUOPEN, LEFTHANDGRAB, LEFTHANDPINCH, LEFTHANDTHROW, RIGHTHANDNONE, RIGHTHANDGRAB, RIGHTHANDTHROW, RIGHTHANDPINCH};
    //public Gesture currentLeftHandGesture = Gesture.LEFTHANDNONE;
    //public Gesture currentRightHandGesture = Gesture.RIGHTHANDNONE;
    public Recordable leftHandRecordable, rightHandRecordable;

    public TMPro.TMP_Text rightHandGestureText, leftHandGestureText;
    // Start is called before the first frame update

    public OVRHand leftHand, rightHand;

    private bool setRightHandNone = false;
    private bool setLeftHandNone = false;

    public static GestureManager Instance { get; private set; }

    private void Awake()
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

    private void Update() 
    {
        if(rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
        {
            //DebugLogger.Instance.LogInVR("Pinch detected on right hand with pinch strength: " + rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index));
            SetRightHandGesture("RIGHTHANDPINCH");
            setRightHandNone = true;
        }
        else
        {
            //Check if the flag setRightHandNone is set to true
            if(setRightHandNone)
            {
                //Set the gesture to NONE
                SetRightHandGesture("RIGHTHANDNONE");
                //Reset the flag
                setRightHandNone = false;
            }
        }

        if(leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
        {
            //DebugLogger.Instance.LogInVR("Pinch detected on left hand with pinch strength: " + rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index));
            SetLeftHandGesture("LEFTHANDPINCH");
            setLeftHandNone = true;
        }
        else
        {
            //Check if the flag setLeftHandNone is set to true
            if(setLeftHandNone)
            {
                //Set the gesture to NONE
                SetLeftHandGesture("LEFTHANDNONE");
                //Reset the flag
                setLeftHandNone = false;
            }
        }
        
    }

    public void SetLeftHandGesture(string gestureStr)
    {
        DebugLogger.Instance.Log("Gesture: " + gestureStr);
        leftHandRecordable.currentGesture = (InputManager.Gesture)System.Enum.Parse(typeof(InputManager.Gesture), gestureStr);
        //SelectTaskForGesture(currentLeftHandGesture);
        leftHandGestureText.text = GestureToString(leftHandRecordable.currentGesture );
    }

    public void SetRightHandGesture(string gestureStr)
    {
        DebugLogger.Instance.Log("Gesture: " + gestureStr);
        rightHandRecordable.currentGesture = (InputManager.Gesture)System.Enum.Parse(typeof(InputManager.Gesture), gestureStr);;
        //SelectTaskForGesture(currentRightHandGesture);
        rightHandGestureText.text = GestureToString(rightHandRecordable.currentGesture);

        //if we are LIVE
        //getTheStateMachine, an make the StateMachine process the current gesture
        //How do we get the collision events?
    }



    public string GestureToString(InputManager.Gesture gesture)
    {
        return gesture switch
        {
            InputManager.Gesture.LEFTHANDNONE => ("None"),
            InputManager.Gesture.LEFTHANDMENUOPEN => ("Menu Open"),
            InputManager.Gesture.LEFTHANDGRAB => ("Closed"),
            InputManager.Gesture.LEFTHANDPINCH => ("Pinch"),
            InputManager.Gesture.LEFTHANDTHROW => ("Open"),
            InputManager.Gesture.RIGHTHANDGRAB => ("Closed"),
            InputManager.Gesture.RIGHTHANDNONE => ("None"),
            InputManager.Gesture.RIGHTHANDTHROW => ("Open"),
            InputManager.Gesture.RIGHTHANDPINCH => ("Pinch"),
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
