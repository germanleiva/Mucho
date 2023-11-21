using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandPlaybackObjectScript : MonoBehaviour
{
    public GameObject indexFinger;
    public GameObject middleFinger;
    public GameObject ringFinger;
    public GameObject pinkyFinger;
    public GameObject thumbFinger;
    
    [Header("Playback Index Finger Joints")]
    public GameObject indexJoint1;
    public GameObject indexJoint2;
    public GameObject indexJoint3;
    [Header("Playback Middle Finger Joints")]
    public GameObject middleJoint1;
    public GameObject middleJoint2;
    public GameObject middleJoint3;

    [Header("Playback Ring Finger Joints")]
    public GameObject ringJoint1;
    public GameObject ringJoint2;
    public GameObject ringJoint3;

    [Header("Playback Pinky Finger Joints")]
    public GameObject pinkyJoint0;
    public GameObject pinkyJoint1;
    public GameObject pinkyJoint2;
    public GameObject pinkyJoint3;

    [Header("Playback Thumb Finger Joints")]
    public GameObject thumbJoint0;
    public GameObject thumbJoint1;
    public GameObject thumbJoint2;
    public GameObject thumbJoint3;

    public GameObject pinchObj;


    public void SetPoseForAllFingerJoints(HandFrame frame)
    {
        //indexJoint1.transform.localPosition = frame.indexJoint1.position;
        indexJoint1.transform.localRotation = frame.indexJoint1.rotation;
        //indexJoint2.transform.localPosition = frame.indexJoint2.position;
        indexJoint2.transform.localRotation = frame.indexJoint2.rotation;
        //indexJoint3.transform.localPosition = frame.indexJoint3.position;
        indexJoint3.transform.localRotation = frame.indexJoint3.rotation;

        //middleJoint1.transform.localPosition = frame.middleJoint1.position;
        middleJoint1.transform.localRotation = frame.middleJoint1.rotation;
        //middleJoint2.transform.localPosition = frame.middleJoint2.position;
        middleJoint2.transform.localRotation = frame.middleJoint2.rotation;
        //middleJoint3.transform.localPosition = frame.middleJoint3.position;
        middleJoint3.transform.localRotation = frame.middleJoint3.rotation;

        //ringJoint1.transform.localPosition = frame.ringJoint1.position;
        ringJoint1.transform.localRotation = frame.ringJoint1.rotation;
        //ringJoint2.transform.localPosition = frame.ringJoint2.position;
        ringJoint2.transform.localRotation = frame.ringJoint2.rotation;
        //ringJoint3.transform.localPosition = frame.ringJoint3.position;
        ringJoint3.transform.localRotation = frame.ringJoint3.rotation;

        //pinkyJoint0.transform.localPosition = frame.pinkyJoint0.position;
        pinkyJoint0.transform.localRotation = frame.pinkyJoint0.rotation;
        //pinkyJoint1.transform.localPosition = frame.pinkyJoint1.position;
        pinkyJoint1.transform.localRotation = frame.pinkyJoint1.rotation;
        //pinkyJoint2.transform.localPosition = frame.pinkyJoint2.position;
        pinkyJoint2.transform.localRotation = frame.pinkyJoint2.rotation;
        //pinkyJoint3.transform.localPosition = frame.pinkyJoint3.position;
        pinkyJoint3.transform.localRotation = frame.pinkyJoint3.rotation;

        //thumbJoint0.transform.localPosition = frame.thumbJoint0.position;
        thumbJoint0.transform.localRotation = frame.thumbJoint0.rotation;
        //thumbJoint1.transform.localPosition = frame.thumbJoint1.position;
        thumbJoint1.transform.localRotation = frame.thumbJoint1.rotation;
        //thumbJoint2.transform.localPosition = frame.thumbJoint2.position;
        thumbJoint2.transform.localRotation = frame.thumbJoint2.rotation;
        //thumbJoint3.transform.localPosition = frame.thumbJoint3.position;
        thumbJoint3.transform.localRotation = frame.thumbJoint3.rotation;

        pinchObj.transform.position = frame.pinchPosition;
    }


}
