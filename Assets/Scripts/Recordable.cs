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
            //recordedData.Add(new TransformData(transform.localPosition, transform.localRotation, childObjectsToRecord[0].transform.localPosition, childObjectsToRecord[1].transform.localPosition, childObjectsToRecord[2].transform.localPosition, childObjectsToRecord[3].transform.localPosition, childObjectsToRecord[4].transform.localPosition, childObjectsToRecord[0].transform.localRotation, childObjectsToRecord[1].transform.localRotation, childObjectsToRecord[2].transform.localRotation, childObjectsToRecord[3].transform.localRotation, childObjectsToRecord[4].transform.localRotation, timestamp));
            //recordedData.Add(new TransformData(transform.localPosition, transform.localRotation, indexJoint1, middleJoint1, ringJoint1, pinkyJoint0, thumbJoint0, timestamp));
            recordedData.Add(new RecordFrameData(transform.localPosition, transform.localRotation, 
                indexJoint1, indexJoint2, indexJoint3, 
                middleJoint1, middleJoint2, middleJoint3, 
                ringJoint1, ringJoint2, ringJoint3, 
                pinkyJoint0, pinkyJoint1, pinkyJoint2, pinkyJoint3, 
                thumbJoint0, thumbJoint1, thumbJoint2, thumbJoint3, 
                timestamp)); 
        }
        //recordedData.Add(new TransformData(transform.localPosition, transform.localRotation, timestamp));
        //recordedData.Add(new TransformData(transform.localPosition, transform.localRotation, timestamp));
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

//[System.Serializable]
public class RecordFrameData
{
    public Vector3 rootPosition;
    public Quaternion rootRotation;
    public float timestamp;

    //public Vector3 indexFinger1Pos, middleFingerPos, ringFingerPos, pinkyFingerPos, thumbFingerPos;
    //public Quaternion indexFingerRot, middleFingerRot, ringFingerRot, pinkyFingerRot, thumbFingerRot;

    public FingerJoint indexJoint1, indexJoint2, indexJoint3;
    public FingerJoint middleJoint1, middleJoint2, middleJoint3;
    public FingerJoint ringJoint1, ringJoint2, ringJoint3;
    public FingerJoint pinkyJoint0, pinkyJoint1, pinkyJoint2, pinkyJoint3;
    public FingerJoint thumbJoint0, thumbJoint1, thumbJoint2, thumbJoint3;


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

        timestamp = _timestamp;
    }
}

public class CustomGesture
{
    public string gestureName;
    public List<RecordFrameData> gestureData;

    public CustomGesture(string _gestureName, List<RecordFrameData> _gestureData)
    {
        gestureName = _gestureName;
        gestureData = _gestureData;
    }
}

public class Trigger
{
    public float startTimestamp, endTimestamp;
    public string triggerName;
    public List<Gesture> gestures;
    public Vector3 velocity;

    public Vector3 acceleration;

    public Trigger(float _startTimestamp, float _endTimestamp, string _triggerName, List<Gesture> _gestures, Vector3 _velocity, Vector3 _acceleration)
    {
        startTimestamp = _startTimestamp;
        endTimestamp = _endTimestamp;
        triggerName = _triggerName;
        gestures = _gestures;
        velocity = _velocity;
        acceleration = _acceleration;
    }
}

