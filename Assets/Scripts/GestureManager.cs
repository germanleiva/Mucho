using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GestureManager : MonoBehaviour
{
    public enum Gesture { LEFTHANDNONE, LEFTHANDMENUOPEN, LEFTHANDGRAB, LEFTHANDPINCH, LEFTHANDTHROW, RIGHTHANDNONE, RIGHTHANDGRAB, RIGHTHANDTHROW, RIGHTHANDPINCH};
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
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update() 
    {
        if((rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index)))
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

        if((leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index)))
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
        DebugLogger.Instance.LogInVR("Gesture: " + gestureStr);
        leftHandRecordable.currentGesture = (Gesture)System.Enum.Parse(typeof(Gesture), gestureStr);;
        //SelectTaskForGesture(currentLeftHandGesture);
        leftHandGestureText.text = GestureToString(leftHandRecordable.currentGesture );
    }

    public void SetRightHandGesture(string gestureStr)
    {
        DebugLogger.Instance.LogInVR("Gesture: " + gestureStr);
        rightHandRecordable.currentGesture = (Gesture)System.Enum.Parse(typeof(Gesture), gestureStr);;
        //SelectTaskForGesture(currentRightHandGesture);
        rightHandGestureText.text = GestureToString(rightHandRecordable.currentGesture);
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


    /*public void SelectTaskForGesture(Gesture gesture)
    {
        //Typecast gestureStr to Gesture
        
        switch (gesture)
        {
            case Gesture.LEFTHANDNONE:
                leftHandGestureText.text = "None";
                break;
            case Gesture.LEFTHANDMENUOPEN:
                leftHandGestureText.text = "Menu Open";
                break;
            case Gesture.LEFTHANDGRAB:
                leftHandGestureText.text = "Closed";
                break;    
            case Gesture.LEFTHANDPINCH:
                leftHandGestureText.text = "Pinch";
                break;
            case Gesture.LEFTHANDTHROW:
                leftHandGestureText.text = "Open";
                break;
            case Gesture.RIGHTHANDGRAB:
                rightHandGestureText.text = "Closed";
                break;
            case Gesture.RIGHTHANDNONE:
                rightHandGestureText.text = "None";
                break;
            case Gesture.RIGHTHANDTHROW:
                rightHandGestureText.text = "Open";
                break;
            case Gesture.RIGHTHANDPINCH:
                rightHandGestureText.text = "Pinch";
                break;
            default:
                break;
        }
    }*/
}
