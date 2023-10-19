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

    private void Update()
    {
        //For hands as focus squares are parts of hands
        if(focusSquare != null)
        {
            if(Manager.Instance.currAppState != Manager.AppState.PLAYBACK)
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

    public void Record(int frameNum, string handStr)
    {
        var hand = handStr.Equals("lefthand") ? Recorder.Instance.currentActiveExample.leftHandData : Recorder.Instance.currentActiveExample.rightHandData;

        { 
            hand.Add(new HandFrame(transform.localPosition, transform.localRotation, 
                indexJoint1, indexJoint2, indexJoint3, 
                middleJoint1, middleJoint2, middleJoint3, 
                ringJoint1, ringJoint2, ringJoint3, 
                pinkyJoint0, pinkyJoint1, pinkyJoint2, pinkyJoint3, 
                thumbJoint0, thumbJoint1, thumbJoint2, thumbJoint3, 
                focusSquare.transform.position, focusSquare.transform.rotation, currentGesture, pinchObj.transform.position, frameNum));
        }
    }

    // Clear the recorded data.
    /*public void ResetData()
    {
        recordedData.Clear();
    }*/


    //For hands
    public void SetGesture(string gestureStr)
    {
        DebugLogger.Instance.Log("Gesture: " + gestureStr);
        currentGesture = (InputManager.Gesture)System.Enum.Parse(typeof(InputManager.Gesture), gestureStr);
        gestureText.text = gestureStr;
    }

    //For hands
    public void SetGestureText(string gestureStr)
    {
        gestureText.text = gestureStr;
    }

}


//[System.Serializable]
public class HandFrame
{
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
    public HandFrame(Vector3 _position, Quaternion _rotation, GameObject _indexJoint0, GameObject _indexJoint1, GameObject _indexJoint2, GameObject _middleJoint0, GameObject _middleJoint1, GameObject _middleJoint2, GameObject _ringJoint0, GameObject _ringJoint1, GameObject _ringJoint2, GameObject _pinkyJoint0, GameObject _pinkyJoint1, GameObject _pinkyJoint2, GameObject _pinkyJoint3, GameObject _thumbJoint0, GameObject _thumbJoint1, GameObject _thumbJoint2, GameObject _thumbJoint3, Vector3 _focusSquarePosition, Quaternion _focusSquareRotation, InputManager.Gesture  _gesture, Vector3 _pinchPosition, int _frameNumber)
    {
        rootPosition = _position;
        rootRotation = _rotation;

        indexJoint1 = new FingerJoint(_indexJoint0);
        indexJoint2 = new FingerJoint(_indexJoint1);
        indexJoint3 = new FingerJoint(_indexJoint2);

        middleJoint1 = new FingerJoint(_middleJoint0);
        middleJoint2 = new FingerJoint(_middleJoint1);
        middleJoint3 = new FingerJoint(_middleJoint2);

        ringJoint1 = new FingerJoint(_ringJoint0);
        ringJoint2 = new FingerJoint(_ringJoint1);
        ringJoint3 = new FingerJoint(_ringJoint2);

        pinkyJoint0 = new FingerJoint(_pinkyJoint0);
        pinkyJoint1 = new FingerJoint(_pinkyJoint1);
        pinkyJoint2 = new FingerJoint(_pinkyJoint2);
        pinkyJoint3 = new FingerJoint(_pinkyJoint3);

        thumbJoint0 = new FingerJoint(_thumbJoint0);
        thumbJoint1 = new FingerJoint(_thumbJoint1);
        thumbJoint2 = new FingerJoint(_thumbJoint2);
        thumbJoint3 = new FingerJoint(_thumbJoint3);

        focusSquarePosition = _focusSquarePosition;
        focusSquareRotation = _focusSquareRotation;

        gesture = _gesture;

        pinchPosition = _pinchPosition;

        frameNumber = _frameNumber;

    }

}


