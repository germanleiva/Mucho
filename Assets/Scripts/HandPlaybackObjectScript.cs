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

    //public GameObject indexJoint1, middleJoint1, ringJoint1, pinkyJoint0, thumbJoint0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LerpBetweenFrames(Transform joint, FingerJoint previousFrameJoint, FingerJoint nextFrameJoint, float t)
    {
        joint.transform.localPosition = Vector3.Lerp(previousFrameJoint.position, nextFrameJoint.position, t);
        joint.transform.localRotation = Quaternion.Lerp(previousFrameJoint.rotation, nextFrameJoint.rotation, t);
    }

    public void interpolatePoseForAllFingerJoints(TransformData previousFrame, TransformData nextFrame, float t)
    {
        LerpBetweenFrames(indexJoint1.transform, previousFrame.indexJoint1, nextFrame.indexJoint1, t);
        LerpBetweenFrames(middleJoint1.transform, previousFrame.middleJoint1, nextFrame.middleJoint1, t);
        LerpBetweenFrames(ringJoint1.transform, previousFrame.ringJoint1, nextFrame.ringJoint1, t);
        LerpBetweenFrames(pinkyJoint0.transform, previousFrame.pinkyJoint0, nextFrame.pinkyJoint0, t);
        LerpBetweenFrames(thumbJoint0.transform, previousFrame.thumbJoint0, nextFrame.thumbJoint0, t);
    }


    /*public void interpolatePoseForAllFingerJoints(TransformData previousFrame, TransformData nextFrame, float t)
    {
        indexJoint1.transform.localPosition = Vector3.Lerp(previousFrame.indexJoint1.position, nextFrame.indexJoint1.position, t);
        indexJoint1.transform.localRotation = Quaternion.Lerp(previousFrame.indexJoint1.rotation, nextFrame.indexJoint1.rotation, t);
        middleJoint1.transform.localPosition = Vector3.Lerp(previousFrame.middleJoint1.position, nextFrame.middleJoint1.position, t);
        middleJoint1.transform.localRotation = Quaternion.Lerp(previousFrame.middleJoint1.rotation, nextFrame.middleJoint1.rotation, t);
        ringJoint1.transform.localPosition = Vector3.Lerp(previousFrame.ringJoint1.position, nextFrame.ringJoint1.position, t);
        ringJoint1.transform.localRotation = Quaternion.Lerp(previousFrame.ringJoint1.rotation, nextFrame.ringJoint1.rotation, t);
        pinkyJoint0.transform.localPosition = Vector3.Lerp(previousFrame.pinkyJoint0.position, nextFrame.pinkyJoint0.position, t);
        pinkyJoint0.transform.localRotation = Quaternion.Lerp(previousFrame.pinkyJoint0.rotation, nextFrame.pinkyJoint0.rotation, t);
        thumbJoint0.transform.localPosition = Vector3.Lerp(previousFrame.thumbJoint0.position, nextFrame.thumbJoint0.position, t);
        thumbJoint0.transform.localRotation = Quaternion.Lerp(previousFrame.thumbJoint0.rotation, nextFrame.thumbJoint0.rotation, t);
    }*/

}
