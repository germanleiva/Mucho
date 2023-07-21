using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GestureManager : MonoBehaviour
{
    public enum Gesture { RIGHTHANDNONE, LEFTHANDMENUOPEN, RIGHTHANDGRAB, RIGHTHANDTHROW, RIGHTHANDPINCH};
    public Gesture currentGesture = Gesture.RIGHTHANDNONE;

    public TMPro.TMP_Text rightHandGestureText, leftHandGestureText;
    // Start is called before the first frame update

    public void SetGesture(string gestureStr)
    {
        currentGesture = (Gesture)System.Enum.Parse(typeof(Gesture), gestureStr);;
        SelectTaskForGesture(currentGesture);
    }

    public void SelectTaskForGesture(Gesture gesture)
    {
        //Typecast gestureStr to Gesture
        
        switch (gesture)
        {
            case Gesture.RIGHTHANDNONE:
                rightHandGestureText.text = "None";
                break;
            case Gesture.LEFTHANDMENUOPEN:
                leftHandGestureText.text = "Menu Open";
                break;
            case Gesture.RIGHTHANDGRAB:
                rightHandGestureText.text = "Grab";
                break;
            case Gesture.RIGHTHANDTHROW:
                rightHandGestureText.text = "Throw";
                break;
            case Gesture.RIGHTHANDPINCH:
                rightHandGestureText.text = "Pinch";
                break;
            default:
                break;
        }
    }
}
