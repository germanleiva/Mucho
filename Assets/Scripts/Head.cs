using System.Collections.Generic;
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
        Recorder.Instance.currentActiveExample.headData.Add(new HeadFrame(transform.position, transform.rotation, focusSquare.transform.position, focusSquare.transform.rotation, frameNum));       
    }

    // Clear the recorded data.
    /*public void ResetData()
    {
        recordedData.Clear();
    }*/

}


//[System.Serializable]
public class HeadFrame
{
    public Vector3 rootPosition;
    public Quaternion rootRotation;

    public int frameNumber;


    //Focus square position and rotation
    public Vector3 focusSquarePosition;
    public Quaternion focusSquareRotation;


    public HeadFrame(Vector3 _position, Quaternion _rotation, Vector3 _focusSquarePosition, Quaternion _focusSquareRotation, int _frameNumber)
    {
        rootPosition = _position;
        rootRotation = _rotation;
        focusSquarePosition = _focusSquarePosition;
        focusSquareRotation = _focusSquareRotation;
        frameNumber = _frameNumber;
        
    }

}


