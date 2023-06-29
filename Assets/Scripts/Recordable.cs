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
            recordedData.Add(new TransformData(transform.localPosition, transform.localRotation, childObjectsToRecord[0].transform.localPosition, childObjectsToRecord[1].transform.localPosition, childObjectsToRecord[2].transform.localPosition, childObjectsToRecord[3].transform.localPosition, childObjectsToRecord[4].transform.localPosition, childObjectsToRecord[0].transform.localRotation, childObjectsToRecord[1].transform.localRotation, childObjectsToRecord[2].transform.localRotation, childObjectsToRecord[3].transform.localRotation, childObjectsToRecord[4].transform.localRotation, timestamp));
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



//[System.Serializable]
public class TransformData
{
    public Vector3 position;
    public Quaternion rotation;
    public float timestamp;

    public Vector3 indexFingerPos, middleFingerPos, ringFingerPos, pinkyFingerPos, thumbFingerPos;
    public Quaternion indexFingerRot, middleFingerRot, ringFingerRot, pinkyFingerRot, thumbFingerRot;

    public TransformData(Vector3 _position, Quaternion _rotation, float _timestamp)
    {
        position = _position;
        rotation = _rotation;
        timestamp = _timestamp;
    }

    //Method TransformData should also take in Vector3 position and Quaternion rotation parameters for indexFinger, middleFinger, ringFinger, pinkyFinger, thumbFinger
    public TransformData(Vector3 _position, Quaternion _rotation, Vector3 _indexFingerPos, Vector3 _middleFingerPos, Vector3 _ringFingerPos, Vector3 _pinkyFingerPos, Vector3 _thumbFingerPos, Quaternion _indexFingerRot, Quaternion _middleFingerRot, Quaternion _ringFingerRot, Quaternion _pinkyFingerRot, Quaternion _thumbFingerRot, float _timestamp)
    {
        position = _position;
        rotation = _rotation;

        indexFingerPos = _indexFingerPos;
        middleFingerPos = _middleFingerPos;
        ringFingerPos = _ringFingerPos;
        pinkyFingerPos = _pinkyFingerPos;
        thumbFingerPos = _thumbFingerPos;

        indexFingerRot = _indexFingerRot;
        middleFingerRot = _middleFingerRot;
        ringFingerRot = _ringFingerRot;
        pinkyFingerRot = _pinkyFingerRot;
        thumbFingerRot = _thumbFingerRot;
        
        timestamp = _timestamp;
    }
}

