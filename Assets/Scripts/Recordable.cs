using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//[RequireComponent(typeof(LineRenderer))]
public class Recordable : MonoBehaviour
{
    public GameObject playbackObject;
    public List<RecordFrameData> recordedData = new List<RecordFrameData>();
    public GameObject lineObject;
    private LineRenderer lineRenderer;
    private LineRendererSmoother lineRendererSmoother;

    public Vector3 rotationCorrection; 
    public Vector3 positionCorrection;

    public List<GameObject> childObjectsToRecord;

    [Header("Recordable Index Finger Joints")]
    public GameObject indexJoint0;
    public GameObject indexJoint1;
    public GameObject indexJoint2;
    [Header("Recordable Middle Finger Joints")]
    public GameObject middleJoint0;
    public GameObject middleJoint1;
    public GameObject middleJoint2;

    [Header("Recordable Ring Finger Joints")]
    public GameObject ringJoint0;
    public GameObject ringJoint1;
    public GameObject ringJoint2;

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

    private void Awake()
    {
        lineRenderer = lineObject.GetComponent<LineRenderer>();
        lineRendererSmoother = lineObject.GetComponent<LineRendererSmoother>();
    }

    // Record the current state.
    public void Record(float timestamp)
    {
        //If childObjectsToRecord is not empty, then record the position and rotation of each child object
        bool isNullOrEmpty = childObjectsToRecord?.Any() != true;
        if(isNullOrEmpty == true)
        {
            recordedData.Add(new RecordFrameData(transform.localPosition, transform.localRotation, timestamp));
        }
        else
        {   
            recordedData.Add(new RecordFrameData(transform.localPosition, transform.localRotation, 
                            indexJoint0, indexJoint1, indexJoint2, 
                            middleJoint0, middleJoint1, middleJoint2, 
                            ringJoint0, ringJoint1, ringJoint2, 
                            pinkyJoint0, pinkyJoint1, pinkyJoint2, pinkyJoint3, 
                            thumbJoint0, thumbJoint1, thumbJoint2, thumbJoint3, 
                            timestamp));  
        }
    }

    // Clear the recorded data.
    public void ResetData()
    {
        recordedData.Clear();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }
    }

    // Visualize the path with lines.
    public void VisualizePath()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = recordedData.Count;
            for (int i = 0; i < recordedData.Count; i++)
            {
                lineRenderer.SetPosition(i, recordedData[i].rootPosition);
            }
        }
        if (lineRendererSmoother != null)
        {
                
            lineRendererSmoother.SimplifyAndSmooth();            
            //lineRendererSmoother.GenerateMeshCollider();
        }
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
}

//Store the data for each frame
//[System.Serializable]
public class RecordFrameData
{
    public Vector3 rootPosition;
    public Quaternion rootRotation;
    public float timestamp;

    public FingerJoint indexJoint0, indexJoint1, indexJoint2;
    public FingerJoint middleJoint0, middleJoint1, middleJoint2;
    public FingerJoint ringJoint0, ringJoint1, ringJoint2;
    public FingerJoint pinkyJoint0, pinkyJoint1, pinkyJoint2, pinkyJoint3;
    public FingerJoint thumbJoint0, thumbJoint1, thumbJoint2, thumbJoint3;

    //public string gestureName;
    

    public RecordFrameData(Vector3 _position, Quaternion _rotation, float _timestamp)
    {
        rootPosition = _position;
        rootRotation = _rotation;
        timestamp = _timestamp;
    }
    
    public RecordFrameData(Vector3 _position, Quaternion _rotation, GameObject _indexJoint0, GameObject _indexJoint1, GameObject _indexJoint2, GameObject _middleJoint0, GameObject _middleJoint1, GameObject _middleJoint2, GameObject _ringJoint0, GameObject _ringJoint1, GameObject _ringJoint2, GameObject _pinkyJoint0, GameObject _pinkyJoint1, GameObject _pinkyJoint2, GameObject _pinkyJoint3, GameObject _thumbJoint0, GameObject _thumbJoint1, GameObject _thumbJoint2, GameObject _thumbJoint3, float _timestamp)
    {
        rootPosition = _position;
        rootRotation = _rotation;

        indexJoint0 = new FingerJoint(_indexJoint0);
        indexJoint1 = new FingerJoint(_indexJoint1);
        indexJoint2 = new FingerJoint(_indexJoint2);

        middleJoint0 = new FingerJoint(_middleJoint0);
        middleJoint1 = new FingerJoint(_middleJoint1);
        middleJoint2 = new FingerJoint(_middleJoint2);

        ringJoint0 = new FingerJoint(_ringJoint0);
        ringJoint1 = new FingerJoint(_ringJoint1);
        ringJoint2 = new FingerJoint(_ringJoint2);

        pinkyJoint0 = new FingerJoint(_pinkyJoint0);
        pinkyJoint1 = new FingerJoint(_pinkyJoint1);
        pinkyJoint2 = new FingerJoint(_pinkyJoint2);
        pinkyJoint3 = new FingerJoint(_pinkyJoint3);

        thumbJoint0 = new FingerJoint(_thumbJoint0);
        thumbJoint1 = new FingerJoint(_thumbJoint1);
        thumbJoint2 = new FingerJoint(_thumbJoint2);
        thumbJoint3 = new FingerJoint(_thumbJoint3);

        timestamp = _timestamp;
    }
}

