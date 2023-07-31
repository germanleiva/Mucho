using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GestureManager : MonoBehaviour
{
    public enum Gesture { LEFTHANDNONE, LEFTHANDMENUOPEN, LEFTHANDGRAB, LEFTHANDPINCH, LEFTHANDTHROW, RIGHTHANDNONE, RIGHTHANDGRAB, RIGHTHANDTHROW, RIGHTHANDPINCH};
    public Gesture currentGesture = Gesture.RIGHTHANDNONE;

    public TMPro.TMP_Text rightHandGestureText, leftHandGestureText;
    // Start is called before the first frame update

    public OVRHand leftHand, rightHand;

    private bool setRightHandNone = false;
    private bool setLeftHandNone = false;

    private void Update() 
    {
        if((rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index)))
        {
            //DebugLogger.Instance.LogInVR("Pinch detected on right hand with pinch strength: " + rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index));
            SetGesture("RIGHTHANDPINCH");
            setRightHandNone = true;
        }
        else
        {
            //Check if the flag setRightHandNone is set to true
            if(setRightHandNone)
            {
                //Set the gesture to NONE
                SetGesture("RIGHTHANDNONE");
                //Reset the flag
                setRightHandNone = false;
            }
        }

        if((leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index)))
        {
            //DebugLogger.Instance.LogInVR("Pinch detected on left hand with pinch strength: " + rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index));
            SetGesture("LEFTHANDPINCH");
            setLeftHandNone = true;
        }
        else
        {
            //Check if the flag setLeftHandNone is set to true
            if(setLeftHandNone)
            {
                //Set the gesture to NONE
                SetGesture("LEFTHANDNONE");
                //Reset the flag
                setLeftHandNone = false;
            }
        }
        
    }

    public void SetGesture(string gestureStr)
    {
        DebugLogger.Instance.LogInVR("Gesture: " + gestureStr);
        currentGesture = (Gesture)System.Enum.Parse(typeof(Gesture), gestureStr);;
        SelectTaskForGesture(currentGesture);
    }

    public void SelectTaskForGesture(Gesture gesture)
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
    }
}
