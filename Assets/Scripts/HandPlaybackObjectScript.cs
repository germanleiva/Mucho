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
    public GameObject indexJoint0;
    public GameObject indexJoint1;
    public GameObject indexJoint2;
    [Header("Playback Middle Finger Joints")]
    public GameObject middleJoint0;
    public GameObject middleJoint1;
    public GameObject middleJoint2;

    [Header("Playback Ring Finger Joints")]
    public GameObject ringJoint0;
    public GameObject ringJoint1;
    public GameObject ringJoint2;

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

    public void interpolatePoseForAllFingerJoints(RecordFrameData previousFrame, RecordFrameData nextFrame, float t)
    {
        LerpBetweenFrames(indexJoint0.transform, previousFrame.indexJoint0, nextFrame.indexJoint0, t);
        LerpBetweenFrames(indexJoint1.transform, previousFrame.indexJoint1, nextFrame.indexJoint1, t);
        LerpBetweenFrames(indexJoint2.transform, previousFrame.indexJoint2, nextFrame.indexJoint2, t);
        LerpBetweenFrames(middleJoint0.transform, previousFrame.middleJoint0, nextFrame.middleJoint0, t);
        LerpBetweenFrames(middleJoint1.transform, previousFrame.middleJoint1, nextFrame.middleJoint1, t);
        LerpBetweenFrames(middleJoint2.transform, previousFrame.middleJoint2, nextFrame.middleJoint2, t);
        LerpBetweenFrames(ringJoint0.transform, previousFrame.ringJoint0, nextFrame.ringJoint0, t);
        LerpBetweenFrames(ringJoint1.transform, previousFrame.ringJoint1, nextFrame.ringJoint1, t);
        LerpBetweenFrames(ringJoint2.transform, previousFrame.ringJoint2, nextFrame.ringJoint2, t);
        LerpBetweenFrames(pinkyJoint0.transform, previousFrame.pinkyJoint0, nextFrame.pinkyJoint0, t);
        LerpBetweenFrames(pinkyJoint1.transform, previousFrame.pinkyJoint1, nextFrame.pinkyJoint1, t);
        LerpBetweenFrames(pinkyJoint2.transform, previousFrame.pinkyJoint2, nextFrame.pinkyJoint2, t);
        LerpBetweenFrames(pinkyJoint3.transform, previousFrame.pinkyJoint3, nextFrame.pinkyJoint3, t);
        LerpBetweenFrames(thumbJoint0.transform, previousFrame.thumbJoint0, nextFrame.thumbJoint0, t);
        LerpBetweenFrames(thumbJoint1.transform, previousFrame.thumbJoint1, nextFrame.thumbJoint1, t);
        LerpBetweenFrames(thumbJoint2.transform, previousFrame.thumbJoint2, nextFrame.thumbJoint2, t);
        LerpBetweenFrames(thumbJoint3.transform, previousFrame.thumbJoint3, nextFrame.thumbJoint3, t);
        
    }
}
