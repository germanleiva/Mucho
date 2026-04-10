using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class Head : MonoBehaviour
{
    public GameObject playbackObject;
    //public List<HeadFrame> recordedData = new();

    public Vector3 rotationCorrection; 
    public Vector3 positionCorrection;

    public Vector3 initPosBeforePhysicsSimulation;
    public Quaternion initRotBeforePhysicsSimulation;
    
    [Header("Recordable Ray and Focus squares")]
    public LineRenderer ray;
    public GameObject focusSquare;
    public GameObject playbackFocusSquare; 
    
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

                // int layerMask = 1 << 6; //This was only considering InteractableSurface
                int layerMask = LayerMask.GetMask("InteractableSurface", "RecordableAsset");
                if (Physics.Raycast(firstPoint, (secondPoint - firstPoint).normalized, out RaycastHit hit, 10, layerMask))        
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
    
    public void PrintAllVoiceCommands()
    {
        //Iterate through all the headFrames in all the examples and print the voice commands
        foreach(var example in Recorder.Instance.examples)
        {
            DebugLogger.Instance.Log("PrintAllVoiceCommands: Example name - " + example.exampleId);
            foreach(var headFrame in example.headFrames)
            {
                if(headFrame.voiceCommand != null)
                {
                    DebugLogger.Instance.Log("PrintAllVoiceCommands: Voice command - " + headFrame.voiceCommand);
                }
                
            }
        }
    }
   

    public void Record(int frameNum)
    {
        Recorder.Instance.currentActiveExample.headFrames.Add(new HeadFrame(transform.position, transform.rotation, focusSquare.transform.position, focusSquare.transform.rotation, null, frameNum));
    }

}


//[System.Serializable]
public class HeadFrame
{
    public Vector3 rootPosition;
    public Quaternion rootRotation;

    public int frameNumber;

    public string voiceCommand;


    //Focus square position and rotation
    public Vector3 focusSquarePosition;
    public Quaternion focusSquareRotation;


    public HeadFrame(Vector3 _position, Quaternion _rotation, Vector3 _focusSquarePosition, Quaternion _focusSquareRotation, string _voiceCommand, int _frameNumber)
    {
        rootPosition = _position;
        rootRotation = _rotation;
        focusSquarePosition = _focusSquarePosition;
        focusSquareRotation = _focusSquareRotation;
        frameNumber = _frameNumber;
        voiceCommand = _voiceCommand;        
    }

    public object Clone()
    {
                // Create a new instance of the class
        HeadFrame clonedFrame = new
        (
            rootPosition,
            rootRotation,
            focusSquarePosition,
            focusSquareRotation,
            voiceCommand,
            frameNumber
        );
        return clonedFrame;
    }

}


