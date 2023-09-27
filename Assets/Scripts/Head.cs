using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;

//[RequireComponent(typeof(LineRenderer))]
public class Head : MonoBehaviour
{
    public GameObject playbackObject, playbackObject2, playbackObject3, playbackObject4, playbackObject5;
    public SkinnedMeshRenderer playbackObject2Renderer, playbackObject3Renderer, playbackObject4Renderer, playbackObject5Renderer;
    public List<HeadFrame> recordedData = new();
    public Collider grabCollider;
    public GameObject assetMenu;

    public enum RecordingType
    {
        None,
        ManualAnimation,
        Physics,
        Follow,
        Visibility
    }

    public RecordingType currentRecordingMode = RecordingType.None;

    public Vector3 rotationCorrection; 
    public Vector3 positionCorrection;

    public Vector3 initPosBeforePhysicsSimulation;
    public Quaternion initRotBeforePhysicsSimulation;

    private float oldMainPlaybackSliderValue = 0;

    public bool isSimulationOn = false;

    public List<GameObject> childObjectsToRecord;

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
    //public Vector3 appliedForce;
    //public ForceArrow forceArrow;
    public TMPro.TMP_Text playbackGestureText;
    public InputManager.Gesture currentGesture;

    public TMPro.TMP_Text gestureText;
    

    //public GestureManager.Gesture currentRightHandGesture = GestureManager.Gesture.RIGHTHANDNONE;
    

    private void Awake()
    {

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

   

    public void Record(int frameNum)
    {
        //If childObjectsToRecord is not empty, then record the position and rotation of each child object
        bool isNullOrEmpty = childObjectsToRecord?.Any() != true;
        if(isNullOrEmpty == true)//Head or Assets
        { 
            recordedData.Add(new HeadFrame(transform.position, transform.rotation, focusSquare.transform.position, focusSquare.transform.rotation, frameNum));       
        }
    }

    // Clear the recorded data.
    public void ResetData()
    {
        recordedData.Clear();
        /*if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }*/
    }

}


//[System.Serializable]
public class HeadFrame
{
    public Vector3 rootPosition;
    public Quaternion rootRotation;

    public int frameNumber;

    public FingerJoint indexJoint1, indexJoint2, indexJoint3;
    public FingerJoint middleJoint1, middleJoint2, middleJoint3;
    public FingerJoint ringJoint1, ringJoint2, ringJoint3;
    public FingerJoint pinkyJoint0, pinkyJoint1, pinkyJoint2, pinkyJoint3;
    public FingerJoint thumbJoint0, thumbJoint1, thumbJoint2, thumbJoint3;

    //Focus square position and rotation
    public Vector3 focusSquarePosition;
    public Quaternion focusSquareRotation;

    public bool showStatusForThisFrame = true;
    public InputManager.Gesture gesture;

    public string Action = "None";

    public Recordable.RecordingType recordingMode;

    public Action ActionDelegate;

    public Action<Frame> CollisionDelegate;

    public GameObject CollidedObject;
    public string Collision = "None";

    //public Vector3 force;

    //Assets 

    //Head
    public HeadFrame(Vector3 _position, Quaternion _rotation, Vector3 _focusSquarePosition, Quaternion _focusSquareRotation, int _frameNumber)
    {
        rootPosition = _position;
        rootRotation = _rotation;
        focusSquarePosition = _focusSquarePosition;
        focusSquareRotation = _focusSquareRotation;
        frameNumber = _frameNumber;
        
    }



}


