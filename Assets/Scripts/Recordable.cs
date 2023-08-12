using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//[RequireComponent(typeof(LineRenderer))]
public class Recordable : MonoBehaviour
{
    public GameObject playbackObject, playbackObject2, playbackObject3, playbackObject4, playbackObject5;
    public SkinnedMeshRenderer playbackObject2Renderer, playbackObject3Renderer, playbackObject4Renderer, playbackObject5Renderer;
    public List<RecordFrameData> recordedData = new();
    public Collider grabCollider;
    public GameObject assetMenu;
    //public ForceArrow lastAttachedForceArrow;
    //public GameObject lineObject;
    //private LineRenderer lineRenderer;
    //private LineRendererSmoother lineRendererSmoother;

    public enum RecordingMode
    {
        None,
        ManualAnimation,
        Physics,
        Attach
    }

    public RecordingMode recordingMode = RecordingMode.None;

    public Vector3 rotationCorrection; 
    public Vector3 positionCorrection;

    public Vector3 initPosBeforePhysicsSimulation;
    public Quaternion initRotBeforePhysicsSimulation;

    private float oldMainPlaybackSliderValue = 0;

    public bool isSimulationOn = false;

    public bool isAssetRecordingOn = false;
    public bool isAssetPlaybackOn = false;

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
    public ForceArrow forceArrow;
    public TMPro.TMP_Text playbackGestureText;
    public GestureManager.Gesture currentGesture;
    //public GestureManager.Gesture currentRightHandGesture = GestureManager.Gesture.RIGHTHANDNONE;
    

    private void Awake()
    {
        //lineRenderer = lineObject.GetComponent<LineRenderer>();
        //lineRendererSmoother = lineObject.GetComponent<LineRendererSmoother>();
        //check if playbackObject2, playbackObject3 are null and SetOpacity to 0.5 and 0.25 respectively
        if(playbackObject2Renderer != null && playbackObject3Renderer != null)
        {
            SetOpacity(playbackObject2Renderer, 0.25f);
            SetOpacity(playbackObject3Renderer, 0.15f);
            SetOpacity(playbackObject4Renderer, 0.1f);
            SetOpacity(playbackObject5Renderer, 0.05f);
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

    public void InsertAssetRecordFrame(float _timestamp, bool propagateValueToSubsequentFrames = false)
    {
        //Print timestamps of first and last recorded frames
        /*if (recordedData.Count > 0)
        {
            DebugLogger.Instance.Log("First recorded frame timestamp: " + recordedData[0].timestamp);
            DebugLogger.Instance.Log("Last recorded frame timestamp: " + recordedData[recordedData.Count - 1].timestamp);
        }
        else
        {
            DebugLogger.Instance.Log("No recorded frames");
        }*/
        RecordFrameData item = new(transform.localPosition, transform.localRotation, showStatus, _timestamp);
        int index = recordedData.BinarySearch(item, Comparer<RecordFrameData>.Create((x, y) => x.timestamp.CompareTo(y.timestamp)));
        if (index < 0)
        {
            //DebugLogger.Instance.Log("Item not found at _timestamp " + _timestamp + ", inserting at index " + ~index);
            index = ~index; // if item is not found, BinarySearch returns the bitwise complement of the insert point
            //recordedData.Insert(index, item);
        }
        else //copy the contents of item into the right position
        {
            //DebugLogger.Instance.Log("Item found at _timestamp " + _timestamp + ", inserting at index " + index);
            
        }
        if(index >= recordedData.Count)
        {
            //DebugLogger.Instance.Log("Index exceeds count, adding asset record frame at the end");
            recordedData.Add(item);
        }
        else
        {
            //DebugLogger.Instance.Log("Index does not exceed count, inserting asset record frame at index " + index);
            recordedData[index] = item;
        }
        //recordedData[index] = item;
        //DebugLogger.Instance.Log("Inserting asset record frame at index " + index);

        if (propagateValueToSubsequentFrames) // Propagate the value to subsequent frames
        {
            DebugLogger.Instance.Log("Propagating value to subsequent frames, starting from index " + index + " to " + recordedData.Count);
            for (int i = index + 1; i < recordedData.Count; i++)
            {
                recordedData[i].showStatusForThisFrame = item.showStatusForThisFrame;
                recordedData[i].rootPosition = item.rootPosition;
                recordedData[i].rootRotation = item.rootRotation;
            }
        }
    }

    public void PropagateShowStatusToSubsequentFrames(float _timestamp, bool _status)
    {
        int index = 0;
        //Traverse the list of recordedData and find the index of the frame with the given timestamp
        for (int i = 0; i < recordedData.Count; i++)
        {
            if (_timestamp < recordedData[i].timestamp)
            {
                index = i;
                //PropagateShowStatusToSubsequentFrames(i);
                DebugLogger.Instance.Log("Found index " + index + " with timestamp " + recordedData[i].timestamp + " greater than " + _timestamp);
                break;
            }
        }
        if(index == 0)
        {
            DebugLogger.Instance.Log("No index found with timestamp " + _timestamp + " with last recorded frame timestamp " + recordedData[^1].timestamp);
            return;
        }
        else
        {
            DebugLogger.Instance.Log("Propagating " + _status + " to subsequent frames, starting from index " + index + " to " + recordedData.Count);
            for (int i = index + 1; i < recordedData.Count; i++)
            {
                recordedData[i].showStatusForThisFrame = _status;
                //recordedData[i].rootPosition = recordedData[index].rootPosition;
                //recordedData[i].rootRotation = recordedData[index].rootRotation;
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
                //recordedData.Add(new RecordFrameData(transform.localPosition, transform.localRotation, showStatus, timestamp)); 
                //Insert recordframedata object into recordeddata list for a specific timestamp
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
                focusSquare.transform.position, focusSquare.transform.rotation, currentGesture, timestamp));
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

    //OnCollisionEnter
    void OnCollisionEnter(Collision collision)
    {
        if(isAssetRecordingOn)
        {
            DebugLogger.Instance.Log("Collision detected between " + gameObject.name + " and " + collision.collider.name);
            isAssetRecordingOn = false;
            //isSimulationOn = false;
            ResetPhysicsProperties();
            recordingMode = Recordable.RecordingMode.None;
            Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        }
    }



    public void ApplyForce(Vector3 initialVelocity)
    {
        oldMainPlaybackSliderValue = AssetPoseRecorder.Instance.mainRecorder.playbackSlider.value; //This is so awkward, but it works
        recordingMode = Recordable.RecordingMode.Physics;
        isAssetRecordingOn = true;
        initPosBeforePhysicsSimulation = transform.position;
        initRotBeforePhysicsSimulation = transform.rotation;
        Manager.Instance.currAppState = Manager.AppState.ASSETRECORDING;
        //isSimulationOn = true;
        GetComponent<Rigidbody>().mass = 0f;
        GetComponent<Collider>().isTrigger = false;
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        GetComponent<Rigidbody>().useGravity = true;
        GetComponent<Rigidbody>().AddForce(initialVelocity, ForceMode.VelocityChange);
    }

    public void ResetPhysicsProperties()
    {
        AssetPoseRecorder.Instance.mainRecorder.playbackSlider.value = oldMainPlaybackSliderValue;
        transform.position = initPosBeforePhysicsSimulation;
        transform.rotation = initRotBeforePhysicsSimulation;
        GetComponent<Rigidbody>().mass = 1f;
        GetComponent<Collider>().isTrigger = true;
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        GetComponent<Rigidbody>().useGravity = false;
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

    public FingerJoint indexJoint1, indexJoint2, indexJoint3;
    public FingerJoint middleJoint1, middleJoint2, middleJoint3;
    public FingerJoint ringJoint1, ringJoint2, ringJoint3;
    public FingerJoint pinkyJoint0, pinkyJoint1, pinkyJoint2, pinkyJoint3;
    public FingerJoint thumbJoint0, thumbJoint1, thumbJoint2, thumbJoint3;

    //Focus square position and rotation
    public Vector3 focusSquarePosition;
    public Quaternion focusSquareRotation;

    public bool showStatusForThisFrame = true;
    public GestureManager.Gesture gesture;

    //public Vector3 force;

    //Assets 
    public RecordFrameData(Vector3 _position, Quaternion _rotation, bool _showStatus, float _timestamp) 
    {
        rootPosition = _position;
        rootRotation = _rotation;
        showStatusForThisFrame = _showStatus;
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
    public RecordFrameData(Vector3 _position, Quaternion _rotation, GameObject _indexJoint0, GameObject _indexJoint1, GameObject _indexJoint2, GameObject _middleJoint0, GameObject _middleJoint1, GameObject _middleJoint2, GameObject _ringJoint0, GameObject _ringJoint1, GameObject _ringJoint2, GameObject _pinkyJoint0, GameObject _pinkyJoint1, GameObject _pinkyJoint2, GameObject _pinkyJoint3, GameObject _thumbJoint0, GameObject _thumbJoint1, GameObject _thumbJoint2, GameObject _thumbJoint3, Vector3 _focusSquarePosition, Quaternion _focusSquareRotation, GestureManager.Gesture  _gesture, float _timestamp)
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

