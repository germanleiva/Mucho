using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//[RequireComponent(typeof(LineRenderer))]
public class Recordable : MonoBehaviour
{
    public GameObject playbackObject, playbackObject2, playbackObject3;
    public SkinnedMeshRenderer playbackObject2Renderer, playbackObject3Renderer;
    public List<RecordFrameData> recordedData = new List<RecordFrameData>();
    //public GameObject lineObject;
    //private LineRenderer lineRenderer;
    //private LineRendererSmoother lineRendererSmoother;

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

    [Header("Recordable Ray and Focus squares")]
    public LineRenderer ray;
    public GameObject focusSquare;
    public GameObject playbackFocusSquare; 

    [Header("Misc Properties")]
    public bool showStatus = true;
    public Vector3 appliedForce;
    

    private void Awake()
    {
        //lineRenderer = lineObject.GetComponent<LineRenderer>();
        //lineRendererSmoother = lineObject.GetComponent<LineRendererSmoother>();
        //check if playbackObject2, playbackObject3 are null and SetOpacity to 0.5 and 0.25 respectively
        if(playbackObject2Renderer != null && playbackObject3Renderer != null)
        {
            SetOpacity(playbackObject2Renderer, 0.25f);
            SetOpacity(playbackObject3Renderer, 0.15f);
        }
    }

    public void SetOpacity(SkinnedMeshRenderer renderer, float opacity)
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
        if(focusSquare != null)
        {
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
    }

    // Record the current state.
    public void Record(float timestamp)
    {
        //If childObjectsToRecord is not empty, then record the position and rotation of each child object
        bool isNullOrEmpty = childObjectsToRecord?.Any() != true;
        if(isNullOrEmpty == true)//Head or Assets
        { 
            if(focusSquare != null)//Head
            {
                recordedData.Add(new RecordFrameData(transform.localPosition, transform.localRotation, focusSquare.transform.position, focusSquare.transform.rotation, timestamp));       
            }
            else //Assets
            {
                recordedData.Add(new RecordFrameData(transform.localPosition, transform.localRotation, showStatus, timestamp)); 
            }           
        }
        else//Hands
        {   
            recordedData.Add(new RecordFrameData(transform.localPosition, transform.localRotation, 
                indexJoint1, indexJoint2, indexJoint3, 
                middleJoint1, middleJoint2, middleJoint3, 
                ringJoint1, ringJoint2, ringJoint3, 
                pinkyJoint0, pinkyJoint1, pinkyJoint2, pinkyJoint3, 
                thumbJoint0, thumbJoint1, thumbJoint2, thumbJoint3, 
                focusSquare.transform.position, focusSquare.transform.rotation, timestamp));
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

    // Visualize the path with lines.
    /*public void VisualizePath()
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
    }*/
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

    //Focus square position and rotation
    public Vector3 focusSquarePosition;
    public Quaternion focusSquareRotation;

    public bool currentShowStatus = true;

    //public Vector3 force;

    //Assets 
    public RecordFrameData(Vector3 _position, Quaternion _rotation, bool _showStatus, float _timestamp) 
    {
        rootPosition = _position;
        rootRotation = _rotation;
        currentShowStatus = _showStatus;
        timestamp = _timestamp;
    }

    //Head
    public RecordFrameData(Vector3 _position, Quaternion _rotation, Vector3 _focusSquarePosition, Quaternion _focusSquareRotation, float _timestamp)
    {
        rootPosition = _position;
        rootRotation = _rotation;
        focusSquarePosition = _focusSquarePosition;
        focusSquareRotation = _focusSquareRotation;
        timestamp = _timestamp;
    }

    //Hands
    public RecordFrameData(Vector3 _position, Quaternion _rotation, GameObject _indexJoint0, GameObject _indexJoint1, GameObject _indexJoint2, GameObject _middleJoint0, GameObject _middleJoint1, GameObject _middleJoint2, GameObject _ringJoint0, GameObject _ringJoint1, GameObject _ringJoint2, GameObject _pinkyJoint0, GameObject _pinkyJoint1, GameObject _pinkyJoint2, GameObject _pinkyJoint3, GameObject _thumbJoint0, GameObject _thumbJoint1, GameObject _thumbJoint2, GameObject _thumbJoint3, Vector3 _focusSquarePosition, Quaternion _focusSquareRotation, float _timestamp)
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

        timestamp = _timestamp;
    }
}

/*public class CustomGesture
{
    public string gestureName;
    public List<RecordFrameData> gestureData;

    public CustomGesture(string _gestureName, List<RecordFrameData> _gestureData)
    {
        gestureName = _gestureName;
        gestureData = _gestureData;
    }
}

*/

