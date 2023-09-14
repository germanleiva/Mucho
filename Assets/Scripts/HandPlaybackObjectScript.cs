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

    public void InterpolatePoseForAllFingerJoints(RecordableFrame previousFrame, RecordableFrame nextFrame, float t)
    {
        LerpBetweenFrames(indexJoint1.transform, previousFrame.indexJoint1, nextFrame.indexJoint1, t);
        LerpBetweenFrames(indexJoint2.transform, previousFrame.indexJoint2, nextFrame.indexJoint2, t);
        LerpBetweenFrames(indexJoint3.transform, previousFrame.indexJoint3, nextFrame.indexJoint3, t);

        LerpBetweenFrames(middleJoint1.transform, previousFrame.middleJoint1, nextFrame.middleJoint1, t);
        LerpBetweenFrames(middleJoint2.transform, previousFrame.middleJoint2, nextFrame.middleJoint2, t);
        LerpBetweenFrames(middleJoint3.transform, previousFrame.middleJoint3, nextFrame.middleJoint3, t);

        LerpBetweenFrames(ringJoint1.transform, previousFrame.ringJoint1, nextFrame.ringJoint1, t);
        LerpBetweenFrames(ringJoint2.transform, previousFrame.ringJoint2, nextFrame.ringJoint2, t);
        LerpBetweenFrames(ringJoint3.transform, previousFrame.ringJoint3, nextFrame.ringJoint3, t);

        LerpBetweenFrames(pinkyJoint0.transform, previousFrame.pinkyJoint0, nextFrame.pinkyJoint0, t);
        LerpBetweenFrames(pinkyJoint1.transform, previousFrame.pinkyJoint1, nextFrame.pinkyJoint1, t);
        LerpBetweenFrames(pinkyJoint2.transform, previousFrame.pinkyJoint2, nextFrame.pinkyJoint2, t);
        LerpBetweenFrames(pinkyJoint3.transform, previousFrame.pinkyJoint3, nextFrame.pinkyJoint3, t);

        LerpBetweenFrames(thumbJoint0.transform, previousFrame.thumbJoint0, nextFrame.thumbJoint0, t);
        LerpBetweenFrames(thumbJoint1.transform, previousFrame.thumbJoint1, nextFrame.thumbJoint1, t);
        LerpBetweenFrames(thumbJoint2.transform, previousFrame.thumbJoint2, nextFrame.thumbJoint2, t);
        LerpBetweenFrames(thumbJoint3.transform, previousFrame.thumbJoint3, nextFrame.thumbJoint3, t);
    }

    public void SetPoseForAllFingerJoints(RecordableFrame frame)
    {
        indexJoint1.transform.localPosition = frame.indexJoint1.position;
        indexJoint1.transform.localRotation = frame.indexJoint1.rotation;
        indexJoint2.transform.localPosition = frame.indexJoint2.position;
        indexJoint2.transform.localRotation = frame.indexJoint2.rotation;
        indexJoint3.transform.localPosition = frame.indexJoint3.position;
        indexJoint3.transform.localRotation = frame.indexJoint3.rotation;

        middleJoint1.transform.localPosition = frame.middleJoint1.position;
        middleJoint1.transform.localRotation = frame.middleJoint1.rotation;
        middleJoint2.transform.localPosition = frame.middleJoint2.position;
        middleJoint2.transform.localRotation = frame.middleJoint2.rotation;
        middleJoint3.transform.localPosition = frame.middleJoint3.position;
        middleJoint3.transform.localRotation = frame.middleJoint3.rotation;

        ringJoint1.transform.localPosition = frame.ringJoint1.position;
        ringJoint1.transform.localRotation = frame.ringJoint1.rotation;
        ringJoint2.transform.localPosition = frame.ringJoint2.position;
        ringJoint2.transform.localRotation = frame.ringJoint2.rotation;
        ringJoint3.transform.localPosition = frame.ringJoint3.position;
        ringJoint3.transform.localRotation = frame.ringJoint3.rotation;

        pinkyJoint0.transform.localPosition = frame.pinkyJoint0.position;
        pinkyJoint0.transform.localRotation = frame.pinkyJoint0.rotation;
        pinkyJoint1.transform.localPosition = frame.pinkyJoint1.position;
        pinkyJoint1.transform.localRotation = frame.pinkyJoint1.rotation;
        pinkyJoint2.transform.localPosition = frame.pinkyJoint2.position;
        pinkyJoint2.transform.localRotation = frame.pinkyJoint2.rotation;
        pinkyJoint3.transform.localPosition = frame.pinkyJoint3.position;
        pinkyJoint3.transform.localRotation = frame.pinkyJoint3.rotation;

        thumbJoint0.transform.localPosition = frame.thumbJoint0.position;
        thumbJoint0.transform.localRotation = frame.thumbJoint0.rotation;
        thumbJoint1.transform.localPosition = frame.thumbJoint1.position;
        thumbJoint1.transform.localRotation = frame.thumbJoint1.rotation;
        thumbJoint2.transform.localPosition = frame.thumbJoint2.position;
        thumbJoint2.transform.localRotation = frame.thumbJoint2.rotation;
        thumbJoint3.transform.localPosition = frame.thumbJoint3.position;
        thumbJoint3.transform.localRotation = frame.thumbJoint3.rotation;
    }


    /*public void I(TransformData previousFrame, TransformData nextFrame, float t)
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
