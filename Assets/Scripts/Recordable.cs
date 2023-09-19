using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//[RequireComponent(typeof(LineRenderer))]
public class Recordable : MonoBehaviour
{
    public GameObject playbackObject, playbackObject2, playbackObject3, playbackObject4, playbackObject5;
    public SkinnedMeshRenderer playbackObject2Renderer, playbackObject3Renderer, playbackObject4Renderer, playbackObject5Renderer;
    public List<RecordableFrame> recordedData = new();
    public Collider grabCollider;
    public GameObject assetMenu;

    public enum RecordingMode
    {
        None,
        ManualAnimation,
        Physics,
        Follow
    }

    public RecordingMode recordingMode = RecordingMode.None;

    public Vector3 rotationCorrection; 
    public Vector3 positionCorrection;

    public Vector3 initPosBeforePhysicsSimulation;
    public Quaternion initRotBeforePhysicsSimulation;

    private float oldMainPlaybackSliderValue = 0;

    public bool isSimulationOn = false;

    //public bool isAssetRecordingOn = false;
    //public bool isAssetPlaybackOn = false;

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

    public void InsertAssetRecordFrame(int _frameNumber, string action = "None", string sourceOfAction = "None",  bool propagateValueToSubsequentFrames = false)
    {
        RecordableFrame item = new(transform.position, transform.rotation, showStatus, action, sourceOfAction, _frameNumber);
        DebugLogger.Instance.Log("Inserting asset record frame at a specific frame number " + _frameNumber);
        recordedData[_frameNumber] = item;

        if (propagateValueToSubsequentFrames) // Propagate the value to subsequent frames
        {
            DebugLogger.Instance.Log("InsertAssetRecordFrame() - Propagating value to subsequent frames, starting from index " + _frameNumber + " to " + recordedData.Count);
            for (int i = _frameNumber + 1; i < recordedData.Count; i++)
            {
                //recordedData[i].showStatusForThisFrame = item.showStatusForThisFrame;
                if (recordedData[i].Action == "ApplyForce()")
                {
                    DebugLogger.Instance.Log("InsertAssetRecordFrame() - Encountered ApplyForce() at frame number " + i + ". Breaking out of the loop.");
                    break;
                }
                if (recordedData[i].SourceOfAction.StartsWith("Collide("))
                {
                    DebugLogger.Instance.Log("InsertAssetRecordFrame() - Encountered Collide() at frame number " + i + ". Breaking out of the loop.");
                    break;
                }

                recordedData[i].rootPosition = item.rootPosition;
                recordedData[i].rootRotation = item.rootRotation;
            }
        }
    }

    public void RecordAndPropagateAssetShowStatus(int _frameNumber)
    {
        DebugLogger.Instance.Log("Changing asset show status at a specific frame number " + _frameNumber);
        for (int i = _frameNumber + 1; i < recordedData.Count; i++)
        {
                if (recordedData[i].Action == "ApplyForce()")
                {
                    DebugLogger.Instance.Log("RecordAndPropagateAssetShowStatus() - Encountered ApplyForce() at frame number " + i + ". Breaking out of the loop.");
                    break;
                }
                recordedData[i].showStatusForThisFrame = showStatus;

                //Assign Hide or Show to recordedData[i].SourceOfAssetChange depending on the value of showStatus
                if(showStatus)
                {
                    recordedData[i].Action = "Show()";
                }
                else
                {
                    recordedData[i].Action = "Hide()";
                }
        }
    }

    public void PropagateAssetNoneStatus(int _frameNumber)
    {
        //DebugLogger.Instance.Log("Changing asset show status at a specific frame number " + _frameNumber);
        for (int i = _frameNumber + 1; i < recordedData.Count; i++)
        {
            if (recordedData[i].Action == "ApplyForce()")
            {
                DebugLogger.Instance.Log("PropagateAssetNoneStatus() - Encountered ApplyForce() at frame number " + i + ". Breaking out of the loop.");
                break;
            }
                
            recordedData[i].Action = "None";
        }
    }

    public void CopyPoseFromRecordable(Recordable other, int _frameStart, int _frameEnd = 0, bool copyFirstRecord = false, bool copyRotation = false)
    {
        if (copyFirstRecord) //Copy the pose from "other" recordable (hands) at index _frameStart, to this asset and propagate the value to subsequent frames
        {
            DebugLogger.Instance.Log("Copying first pose from " + other.gameObject.name + " to " + gameObject.name + " from frame number " + _frameStart + " to " + recordedData.Count);
            //recordedData[_frameStart].SourceOfAssetChange = "Unfollow()";
            recordedData[_frameStart].Action = "None";
            for (int i = _frameStart + 1; i < recordedData.Count; i++)
            {
                if(recordedData[i].Action == "ApplyForce()")
                {
                    DebugLogger.Instance.Log("CopyPoseFromRecordable() - Encountered ApplyForce() at frame number " + i + ". Breaking out of the loop.");
                    break;
                }
                recordedData[i].rootPosition = other.recordedData[_frameStart].rootPosition;
                recordedData[i].Action = "None";
                recordedData[i].SourceOfAction = "None";
                //recordedData[i].SourceOfAssetChange = "Unfollow(" + other.gameObject.name + ")";
                if(copyRotation) recordedData[i].rootRotation = other.recordedData[_frameStart].rootRotation;
            }
        }
        else //Copy all the poses from "other" recordable (hands, head focus) to this asset and propagate the value to subsequent frames
        {
            Vector3 offset = recordedData[_frameStart].rootPosition - other.recordedData[_frameStart].rootPosition;
            DebugLogger.Instance.Log("Copying all pose data from " + other.gameObject.name + " to " + gameObject.name + " from frame number " + _frameStart + " to " + recordedData.Count);
            if(_frameEnd == 0)
            {
                _frameEnd = recordedData.Count;
            }
            for (int i = _frameStart + 1; i < _frameEnd; i++)
            {
                if(recordedData[i].Action == "ApplyForce()")
                {
                    DebugLogger.Instance.Log("CopyPoseFromRecordable() - Encountered ApplyForce() at frame number " + i + ". Breaking out of the loop.");
                    break;
                }
                recordedData[i].rootPosition = other.recordedData[i].rootPosition + offset;
                recordedData[i].Action = "Follow(" + Manager.Instance.CleanString(other.gameObject.name) + ")"; 
                recordedData[i].SourceOfAction = "Collide(" + Manager.Instance.CleanString(gameObject.name) + ", " + Manager.Instance.CleanString(other.gameObject.name) + ")";                         
                if(copyRotation) recordedData[i].rootRotation = other.recordedData[i].rootRotation;
            }
        }
    }


    public void CopyPoseFromFocusSquare(Recordable other, int _frameStart, int _frameEnd = 0, bool copyFirstRecord = false, bool copyRotation = false)
    {
        if (copyFirstRecord)
        {
            DebugLogger.Instance.Log("Copying first pose from " + other.gameObject.name + " to " + gameObject.name + " from frame number " + _frameStart + " to " + recordedData.Count);
            //recordedData[_frameStart].SourceOfAssetChange = "Unfollow()";
            recordedData[_frameStart].Action = "None";
            for (int i = _frameStart + 1; i < recordedData.Count; i++)
            {
                if(recordedData[i].Action == "ApplyForce()")
                {
                    DebugLogger.Instance.Log("CopyPoseFromFocusSquare() - Encountered ApplyForce() at frame number " + i + ". Breaking out of the loop.");
                    break;
                }
                recordedData[i].rootPosition = other.recordedData[_frameStart].focusSquarePosition;
                recordedData[i].Action = "None";
                //recordedData[i].SourceOfAssetChange = "Unfollow(" + other.gameObject.name + ")";
                if(copyRotation) recordedData[i].rootRotation = other.recordedData[_frameStart].focusSquareRotation;
            }
        }
        else
        {
            Vector3 offset = recordedData[_frameStart].rootPosition - other.recordedData[_frameStart].focusSquarePosition;
            DebugLogger.Instance.Log("Copying all pose data from " + other.gameObject.name + " to " + gameObject.name + " from frame number " + _frameStart + " to " + recordedData.Count);
            if(_frameEnd == 0)
            {
                _frameEnd = recordedData.Count;
            }
            for (int i = _frameStart + 1; i < _frameEnd; i++)
            {
                if(recordedData[i].Action == "ApplyForce()")
                {
                    DebugLogger.Instance.Log("CopyPoseFromFocusSquare() - Encountered ApplyForce() at frame number " + i + ". Breaking out of the loop.");
                    break;
                }
                recordedData[i].rootPosition = other.recordedData[i].focusSquarePosition + offset;
                recordedData[i].Action = "Follow(" + Manager.Instance.CleanString(other.gameObject.name) + ", FocusSquare)"; //Remove the last five characters from the string other.gameObject.name  
                         
                if(copyRotation) recordedData[i].rootRotation = other.recordedData[i].focusSquareRotation;
            }
        }
    }

    public void Record(int frameNum)
    {
        //If childObjectsToRecord is not empty, then record the position and rotation of each child object
        bool isNullOrEmpty = childObjectsToRecord?.Any() != true;
        if(isNullOrEmpty == true)//Head or Assets
        { 
            recordedData.Add(new RecordableFrame(transform.position, transform.rotation, focusSquare.transform.position, focusSquare.transform.rotation, frameNum));       
        }
        else//Hands
        {   
            recordedData.Add(new RecordableFrame(transform.localPosition, transform.localRotation, 
                indexJoint1, indexJoint2, indexJoint3, 
                middleJoint1, middleJoint2, middleJoint3, 
                ringJoint1, ringJoint2, ringJoint3, 
                pinkyJoint0, pinkyJoint1, pinkyJoint2, pinkyJoint3, 
                thumbJoint0, thumbJoint1, thumbJoint2, thumbJoint3, 
                focusSquare.transform.position, focusSquare.transform.rotation, currentGesture, frameNum));
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
        if(recordingMode == RecordingMode.Physics)
        {
            recordingMode = Recordable.RecordingMode.None;
            DebugLogger.Instance.Log("Collision detected between " + gameObject.name + " and " + collision.collider.name);
            InsertAssetRecordFrame((int)AssetPoseRecorder.Instance.mainRecorder.playbackSlider.value, action: "None", sourceOfAction: "Collide(" + Manager.Instance.CleanString(gameObject.name) + "," + Manager.Instance.CleanString(collision.collider.name) + ")", propagateValueToSubsequentFrames: false);
            //DebugLogger.Instance.Log("Collide(" + Manager.Instance.CleanString(gameObject.name) + "," + Manager.Instance.CleanString(collision.collider.name) + ")");
            PropagateAssetNoneStatus((int)AssetPoseRecorder.Instance.mainRecorder.playbackSlider.value);
            ResetPhysicsProperties();          
            Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        }

        if(Manager.Instance.currAppState != Manager.AppState.RECORDING)
        {
            //DebugLogger.Instance.Log("Collision detected between " + gameObject.name + " and " + collision.collider.name);
        }
    }


    void OnCollisionStay(Collision collision)
    {
        if(Manager.Instance.currAppState != Manager.AppState.RECORDING)
        {
            //DebugLogger.Instance.Log("Collision detected between " + gameObject.name + " and " + collision.collider.name);
            if(collision.collider.name == "LeftHandCollider")
            {
                InputManager.Instance.SetAssetInContactWithLeftHand(this, true);
            }
            else if(collision.collider.name == "RightHandCollider")
            {
                InputManager.Instance.SetAssetInContactWithRightHand(this, true);
            }            
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if(Manager.Instance.currAppState != Manager.AppState.RECORDING)
        {
            DebugLogger.Instance.Log("Collision ended between " + gameObject.name + " and " + collision.collider.name);
            if(collision.collider.name == "LeftHandCollider")
            {
                InputManager.Instance.SetAssetInContactWithLeftHand(this, false);
            }
            else if(collision.collider.name == "RightHandCollider")
            {
                InputManager.Instance.SetAssetInContactWithRightHand(this, false);
            }            
        }
    }


    public void ApplyForce(Vector3 initialVelocity)
    {
        oldMainPlaybackSliderValue = AssetPoseRecorder.Instance.mainRecorder.playbackSlider.value; //This is so awkward, but it works
        recordingMode = Recordable.RecordingMode.Physics;
        //isAssetRecordingOn = true;
        initPosBeforePhysicsSimulation = transform.position;
        initRotBeforePhysicsSimulation = transform.rotation;
        //Manager.Instance.currAppState = Manager.AppState.ASSETRECORDING;
        //isSimulationOn = true;
        GetComponent<Rigidbody>().mass = 0f;
        GetComponent<Collider>().isTrigger = false;
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        GetComponent<Rigidbody>().useGravity = true;
        GetComponent<Rigidbody>().AddForce(initialVelocity, ForceMode.VelocityChange);
    }

    public void ResetPhysicsProperties()
    {
        DebugLogger.Instance.Log("Resetting physics properties");        
        AssetPoseRecorder.Instance.mainRecorder.playbackSlider.value = oldMainPlaybackSliderValue;
        transform.position = initPosBeforePhysicsSimulation;
        transform.rotation = initRotBeforePhysicsSimulation;
        GetComponent<Rigidbody>().mass = 1f;
        GetComponent<Collider>().isTrigger = true;
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        GetComponent<Rigidbody>().useGravity = false;
        Recorder.Instance.RefreshAssetsTimeline(Recorder.Instance.assetTimelinePanelPrefab);
    }

    public void SetGesture(string gestureStr)
    {
        DebugLogger.Instance.Log("Gesture: " + gestureStr);
        currentGesture = (InputManager.Gesture)System.Enum.Parse(typeof(InputManager.Gesture), gestureStr);
        gestureText.text = gestureStr;
    }

    public void SetGestureText(string gestureStr)
    {
        gestureText.text = gestureStr;
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
public class RecordableFrame
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
    public string SourceOfAction = "None";

    //public Vector3 force;

    //Assets 
    public RecordableFrame(Vector3 _position, Quaternion _rotation, bool _showStatus, string _action, string _sourceOfAction, int _frameNumber)
    {
        rootPosition = _position;
        rootRotation = _rotation;
        showStatusForThisFrame = _showStatus;
        frameNumber = _frameNumber;
        Action = _action;
        SourceOfAction = _sourceOfAction;
    }

    //Head
    public RecordableFrame(Vector3 _position, Quaternion _rotation, Vector3 _focusSquarePosition, Quaternion _focusSquareRotation, int _frameNumber)
    {
        rootPosition = _position;
        rootRotation = _rotation;
        focusSquarePosition = _focusSquarePosition;
        focusSquareRotation = _focusSquareRotation;
        frameNumber = _frameNumber;
        
    }

    //Hands
    public RecordableFrame(Vector3 _position, Quaternion _rotation, GameObject _indexJoint0, GameObject _indexJoint1, GameObject _indexJoint2, GameObject _middleJoint0, GameObject _middleJoint1, GameObject _middleJoint2, GameObject _ringJoint0, GameObject _ringJoint1, GameObject _ringJoint2, GameObject _pinkyJoint0, GameObject _pinkyJoint1, GameObject _pinkyJoint2, GameObject _pinkyJoint3, GameObject _thumbJoint0, GameObject _thumbJoint1, GameObject _thumbJoint2, GameObject _thumbJoint3, Vector3 _focusSquarePosition, Quaternion _focusSquareRotation, InputManager.Gesture  _gesture, int _frameNumber)
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

        frameNumber = _frameNumber;

    }

}


