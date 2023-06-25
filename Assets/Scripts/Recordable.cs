using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[RequireComponent(typeof(LineRenderer))]
public class Recordable : MonoBehaviour
{
    public GameObject playbackObject;
    public List<TransformData> recordedData = new List<TransformData>();
    private LineRenderer lineRenderer;
    private LineRendererSmoother lineRendererSmoother;

    private void Awake()
    {
        lineRenderer = playbackObject.GetComponent<LineRenderer>();
        lineRendererSmoother = playbackObject.GetComponent<LineRendererSmoother>();
    }

    // Record the current state.
    public void Record(float timestamp)
    {
        recordedData.Add(new TransformData(transform.position, transform.rotation, timestamp));
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

    public TransformData(Vector3 _position, Quaternion _rotation, float _timestamp)
    {
        position = _position;
        rotation = _rotation;
        timestamp = _timestamp;
    }
}

