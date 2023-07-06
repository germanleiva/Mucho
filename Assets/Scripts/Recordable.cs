using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//[RequireComponent(typeof(LineRenderer))]
public class Recordable : MonoBehaviour
{
    public GameObject playbackObject;
    public List<TransformData> recordedData = new List<TransformData>();
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
            recordedData.Add(new TransformData(transform.localPosition, transform.localRotation, timestamp));
        }
        else
        {   
            //recordedData.Add(new TransformData(transform.localPosition, transform.localRotation, childObjectsToRecord[0].transform.localPosition, childObjectsToRecord[1].transform.localPosition, childObjectsToRecord[2].transform.localPosition, childObjectsToRecord[3].transform.localPosition, childObjectsToRecord[4].transform.localPosition, childObjectsToRecord[0].transform.localRotation, childObjectsToRecord[1].transform.localRotation, childObjectsToRecord[2].transform.localRotation, childObjectsToRecord[3].transform.localRotation, childObjectsToRecord[4].transform.localRotation, timestamp));
            recordedData.Add(new TransformData(transform.localPosition, transform.localRotation, indexJoint1, middleJoint1, ringJoint1, pinkyJoint0, thumbJoint0, timestamp));
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
                lineRenderer.SetPosition(i, recordedData[i].position);
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
public class TransformData
{
    public Vector3 position;
    public Quaternion rotation;
    public float timestamp;

    //public Vector3 indexFinger1Pos, middleFingerPos, ringFingerPos, pinkyFingerPos, thumbFingerPos;
    //public Quaternion indexFingerRot, middleFingerRot, ringFingerRot, pinkyFingerRot, thumbFingerRot;

    public FingerJoint indexJoint1, middleJoint1, ringJoint1, pinkyJoint0, thumbJoint0;


    public TransformData(Vector3 _position, Quaternion _rotation, float _timestamp)
    {
        position = _position;
        rotation = _rotation;
        timestamp = _timestamp;
    }

    //Method TransformData should also take in Vector3 position and Quaternion rotation parameters for indexFinger, middleFinger, ringFinger, pinkyFinger, thumbFinger
    /*public TransformData(Vector3 _position, Quaternion _rotation, Vector3 _indexFingerPos, Vector3 _middleFingerPos, Vector3 _ringFingerPos, Vector3 _pinkyFingerPos, Vector3 _thumbFingerPos, Quaternion _indexFingerRot, Quaternion _middleFingerRot, Quaternion _ringFingerRot, Quaternion _pinkyFingerRot, Quaternion _thumbFingerRot, float _timestamp)
    {
        position = _position;
        rotation = _rotation;

        indexJoint1 = new FingerJoint(_indexFingerPos, _indexFingerRot);
        middleJoint1 = new FingerJoint(_middleFingerPos, _middleFingerRot);
        ringJoint1 = new FingerJoint(_ringFingerPos, _ringFingerRot);
        pinkyJoint0 = new FingerJoint(_pinkyFingerPos, _pinkyFingerRot);
        thumbJoint0 = new FingerJoint(_thumbFingerPos, _thumbFingerRot);
        
        timestamp = _timestamp;
    }*/

    public TransformData(Vector3 _position, Quaternion _rotation, GameObject _indexFinger, GameObject _middleFinger, GameObject _ringFinger, GameObject _pinkyFinger, GameObject _thumbFinger, float _timestamp)
    {
        position = _position;
        rotation = _rotation;

        indexJoint1 = new FingerJoint(_indexFinger);
        middleJoint1 = new FingerJoint(_middleFinger);
        ringJoint1 = new FingerJoint(_ringFinger);
        pinkyJoint0 = new FingerJoint(_pinkyFinger);
        thumbJoint0 = new FingerJoint(_thumbFinger);
        
        timestamp = _timestamp;
    }
}

