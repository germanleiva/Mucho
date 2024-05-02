using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinchGestureDetector : MonoBehaviour
{
    public OVRHand leftHand, rightHand;

    private bool setRightHandNone = false;
    private bool setLeftHandNone = false;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
        {
            //DebugLogger.Instance.LogInVR("Pinch detected on right hand with pinch strength: " + rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index));
            InputManager.Instance.SetRightHandGesture(InputManager.Gesture.RIGHTHANDPINCH);
            setRightHandNone = true;
        }
        else
        {
            //Check if the flag setRightHandNone is set to true
            if(setRightHandNone)
            {
                //Set the gesture to NONE
                InputManager.Instance.SetRightHandGesture(InputManager.Gesture.RIGHTHANDNONE);
                //Reset the flag
                setRightHandNone = false;
            }
        }

        if(leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
        {
            //DebugLogger.Instance.LogInVR("Pinch detected on left hand with pinch strength: " + rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index));
            InputManager.Instance.SetLeftHandGesture(InputManager.Gesture.LEFTHANDPINCH);
            setLeftHandNone = true;
        }
        else
        {
            //Check if the flag setLeftHandNone is set to true
            if(setLeftHandNone)
            {
                //Set the gesture to NONE
                InputManager.Instance.SetLeftHandGesture(InputManager.Gesture.LEFTHANDNONE);
                //Reset the flag
                setLeftHandNone = false;
            }
        }
    }
}
