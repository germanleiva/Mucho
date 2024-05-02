using System;
using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    public GameObject playbackObject;//, playbackObject2, playbackObject3, playbackObject4, playbackObject5;
    //public SkinnedMeshRenderer playbackObject2Renderer, playbackObject3Renderer, playbackObject4Renderer, playbackObject5Renderer;
    //public List<HandFrame> recordedData = new();

    public Vector3 rotationCorrection; 
    public Vector3 positionCorrection;

    public Vector3 initPosBeforePhysicsSimulation;
    public Quaternion initRotBeforePhysicsSimulation;

    public GameObject pinchObj;

    [Header("Recordable Index Finger Joints")]
    public GameObject indexJoint1;
    public GameObject indexJoint2;
    public GameObject indexJoint3;
    [Header("Recordable Middle Finger Joints")]
    public GameObject middleJoint1;
    public GameObject middleJoint2;
    public GameObject middleJoint3;

    [Header("Recordable Ring Finger Joints")]
    public GameObject ringJoint1;
    public GameObject ringJoint2;
    public GameObject ringJoint3;

    [Header("Recordable Pinky Finger Joints")]
    public GameObject pinkyJoint0;
    public GameObject pinkyJoint1;
    public GameObject pinkyJoint2;
    public GameObject pinkyJoint3;

    [Header("Recordable Thumb Finger Joints")]
    public GameObject thumbJoint0;
    public GameObject thumbJoint1;
    public GameObject thumbJoint2;
    public GameObject thumbJoint3;

    [Header("Recordable Ray and Focus squares")]
    public LineRenderer ray;
    public GameObject focusSquare;
    public GameObject playbackFocusSquare; 

    [Header("Misc Properties")]
    public bool showStatus = true;
    public TMPro.TMP_Text playbackGestureText;
    public InputManager.Gesture currentGesture;

    public TMPro.TMP_Text gestureText;

    

    private void Awake()
    {
        //check if playbackObject2, playbackObject3 are null and SetOpacity to 0.5 and 0.25 respectively
        /*if(playbackObject2Renderer != null && playbackObject3Renderer != null) //For hands
        {
            SetOpacity(playbackObject2Renderer, 0.25f);
            SetOpacity(playbackObject3Renderer, 0.15f);
            SetOpacity(playbackObject4Renderer, 0.1f);
            SetOpacity(playbackObject5Renderer, 0.05f);
        }*/
    }

    public void SetOpacity(SkinnedMeshRenderer renderer, float opacity) //For hands
    {
        //SkinnedMeshRenderer renderer = obj.GetComponent<SkinnedMeshRenderer>();
        if (renderer != null)
        {
            Material material = renderer.material;
            if (material != null)
            {
                material.SetFloat("_Opacity", opacity);
                material.SetFloat("_OutlineOpacity", opacity);
            }
        }
    }

    public Vector3 GetCurrentPosition()
    {
        return transform.localPosition;
    }


    private void Update()
    {
        //For hands as focus squares are parts of hands
        if(focusSquare != null)
        {
            if(Manager.Instance.currAppState != Manager.AppState.PLAYBACK && Manager.Instance.currAppState != Manager.AppState.RECORDING_DURING_PLAYBACK) 
            {
                focusSquare.SetActive(true);
                Vector3 firstPoint = ray.transform.TransformPoint(ray.GetPosition(0));
                Vector3 secondPoint = ray.transform.TransformPoint(ray.GetPosition(1));
                
                //Raycast from ray starting point, in the direction of the ray to intersect with layer 6
                //Debug.DrawRay(firstPoint, (secondPoint - firstPoint).normalized * 100, Color.blue);
                if (Physics.Raycast(firstPoint, (secondPoint - firstPoint).normalized, out RaycastHit hit, 10, 1 << 6))        
                {
                    //hit.transform.gameObject.GetComponent<EnvironmentContext>().contextName;

                    focusSquare.transform.position = hit.point;
                    //Raise the focus square by 0.01 units
                    focusSquare.transform.position += new Vector3(0, 0.01f, 0);
                    focusSquare.transform.forward = hit.normal;
                    // Rotate 180 degrees around the Y-axis
                    focusSquare.transform.rotation *= Quaternion.Euler(0, 180, 180);
                }
                else
                {
                    focusSquare.transform.position = Vector3.zero;
                    focusSquare.transform.rotation = Quaternion.identity;

                }
            }
            else
            {
                focusSquare.SetActive(false);
            }
        }
    }

    public void Record(int frameNum, List<HandFrame> handFrames)
    {
        handFrames.Add(new HandFrame(transform.localPosition, transform.localRotation,
            new FingerJoint(indexJoint1.transform.localPosition, indexJoint1.transform.localRotation), new FingerJoint(indexJoint2.transform.localPosition, indexJoint2.transform.localRotation), new FingerJoint(indexJoint3.transform.localPosition, indexJoint3.transform.localRotation),
            new FingerJoint(middleJoint1.transform.localPosition, middleJoint1.transform.localRotation), new FingerJoint(middleJoint2.transform.localPosition, middleJoint2.transform.localRotation), new FingerJoint(middleJoint3.transform.localPosition, middleJoint3.transform.localRotation),
            new FingerJoint(ringJoint1.transform.localPosition, ringJoint1.transform.localRotation), new FingerJoint(ringJoint2.transform.localPosition, ringJoint2.transform.localRotation), new FingerJoint(ringJoint3.transform.localPosition, ringJoint3.transform.localRotation),
            new FingerJoint(pinkyJoint0.transform.localPosition, pinkyJoint0.transform.localRotation), new FingerJoint(pinkyJoint1.transform.localPosition, pinkyJoint1.transform.localRotation), new FingerJoint(pinkyJoint2.transform.localPosition, pinkyJoint2.transform.localRotation), new FingerJoint(pinkyJoint3.transform.localPosition, pinkyJoint3.transform.localRotation),
            new FingerJoint(thumbJoint0.transform.localPosition, thumbJoint0.transform.localRotation), new FingerJoint(thumbJoint1.transform.localPosition, thumbJoint1.transform.localRotation), new FingerJoint(thumbJoint2.transform.localPosition, thumbJoint2.transform.localRotation), new FingerJoint(thumbJoint3.transform.localPosition, thumbJoint3.transform.localRotation),            
            focusSquare.transform.position, focusSquare.transform.rotation, 
            currentGesture, 
            pinchObj.transform.position,
            frameNum));
    
    }

    // Clear the recorded data.
    /*public void ResetData()
    {
        recordedData.Clear();
    }*/


    //For hands
    // public void SetGesture(string gestureStr)
    // {
    //     DebugLogger.Instance.Log("Gesture: " + gestureStr);
    //     currentGesture = (InputManager.Gesture)System.Enum.Parse(typeof(InputManager.Gesture), gestureStr);
    //     gestureText.text = gestureStr;
    // }

    //For hands
    public void SetGestureText(string gestureStr)
    {
        gestureText.text = gestureStr;
    }

}


//[System.Serializable]
public class HandFrame
{
    public Boolean isActive = true;
    public Vector3 rootPosition;
    public Quaternion rootRotation;

    public int frameNumber;

    public FingerJoint indexJoint1, indexJoint2, indexJoint3;
    public FingerJoint middleJoint1, middleJoint2, middleJoint3;
    public FingerJoint ringJoint1, ringJoint2, ringJoint3;
    public FingerJoint pinkyJoint0, pinkyJoint1, pinkyJoint2, pinkyJoint3;
    public FingerJoint thumbJoint0, thumbJoint1, thumbJoint2, thumbJoint3;

    public Vector3 pinchPosition;

    //Focus square position and rotation
    public Vector3 focusSquarePosition;
    public Quaternion focusSquareRotation;

    public bool showStatusForThisFrame = true;
    public InputManager.Gesture gesture;

    public string Action = "None";

    //public Recordable.RecordingType recordingMode;

    public Action ActionDelegate;

    public Action<Frame> CollisionDelegate;

    public GameObject CollidedObject;
    public string Collision = "None";


    //Hands
    public HandFrame(Vector3 _position, Quaternion _rotation, 
                    FingerJoint _indexJoint0, FingerJoint _indexJoint1, FingerJoint _indexJoint2, 
                    FingerJoint _middleJoint0, FingerJoint _middleJoint1, FingerJoint _middleJoint2, 
                    FingerJoint _ringJoint0, FingerJoint _ringJoint1, FingerJoint _ringJoint2, 
                    FingerJoint _pinkyJoint0, FingerJoint _pinkyJoint1, FingerJoint _pinkyJoint2, FingerJoint _pinkyJoint3, 
                    FingerJoint _thumbJoint0, FingerJoint _thumbJoint1, FingerJoint _thumbJoint2, FingerJoint _thumbJoint3, 
                    Vector3 _focusSquarePosition, Quaternion _focusSquareRotation, 
                    InputManager.Gesture _gesture, 
                    Vector3 _pinchPosition,
                    int _frameNumber)
    {
        rootPosition = _position;
        rootRotation = _rotation;

        indexJoint1 = _indexJoint0;
        indexJoint2 = _indexJoint1;
        indexJoint3 = _indexJoint2;

        middleJoint1 = _middleJoint0;
        middleJoint2 = _middleJoint1;
        middleJoint3 = _middleJoint2;

        ringJoint1 = _ringJoint0;
        ringJoint2 = _ringJoint1;
        ringJoint3 = _ringJoint2;
        
        pinkyJoint0 = _pinkyJoint0;
        pinkyJoint1 = _pinkyJoint1;
        pinkyJoint2 = _pinkyJoint2;
        pinkyJoint3 = _pinkyJoint3;

        thumbJoint0 = _thumbJoint0;
        thumbJoint1 = _thumbJoint1;
        thumbJoint2 = _thumbJoint2;
        thumbJoint3 = _thumbJoint3;

        focusSquarePosition = _focusSquarePosition;
        focusSquareRotation = _focusSquareRotation;

        gesture = _gesture;

        pinchPosition = _pinchPosition;

        frameNumber = _frameNumber;

    }

    public object Clone()
    {
        // Create a new instance of the class
        HandFrame clonedFrame = new
        (
            rootPosition,
            rootRotation,
            new FingerJoint(indexJoint1.position, indexJoint1.rotation),
            new FingerJoint(indexJoint2.position, indexJoint2.rotation),
            new FingerJoint(indexJoint3.position, indexJoint3.rotation),
            new FingerJoint(middleJoint1.position, middleJoint1.rotation),
            new FingerJoint(middleJoint2.position, middleJoint2.rotation),
            new FingerJoint(middleJoint3.position, middleJoint3.rotation),
            new FingerJoint(ringJoint1.position, ringJoint1.rotation),
            new FingerJoint(ringJoint2.position, ringJoint2.rotation),
            new FingerJoint(ringJoint3.position, ringJoint3.rotation),
            new FingerJoint(pinkyJoint0.position, pinkyJoint0.rotation),
            new FingerJoint(pinkyJoint1.position, pinkyJoint1.rotation),
            new FingerJoint(pinkyJoint2.position, pinkyJoint2.rotation),
            new FingerJoint(pinkyJoint3.position, pinkyJoint3.rotation),
            new FingerJoint(thumbJoint0.position, thumbJoint0.rotation),
            new FingerJoint(thumbJoint1.position, thumbJoint1.rotation),
            new FingerJoint(thumbJoint2.position, thumbJoint2.rotation),
            new FingerJoint(thumbJoint3.position, thumbJoint3.rotation),
            focusSquarePosition,
            focusSquareRotation,
            gesture,
            pinchPosition,
            frameNumber
        );

        return clonedFrame;

    }

}

public class FingerJoint
{
    public Vector3 position;
    public Quaternion rotation;

    public FingerJoint(Vector3 _position, Quaternion _rotation)
    {
        position = _position;
        rotation = _rotation;
    }

    public FingerJoint(GameObject _joint)
    {
        position = _joint.transform.localPosition;
        rotation = _joint.transform.localRotation;
    }

    public object Clone()
    {
        // Create a new instance of the class
        FingerJoint clonedJoint = new(this.position, this.rotation);

        // Return the cloned object
        return clonedJoint;
    }
}


