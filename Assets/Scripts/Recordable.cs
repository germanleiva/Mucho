using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UnityEngine;

//[RequireComponent(typeof(LineRenderer))]
public class Recordable : MonoBehaviour
{
    //public List<RecordableFrame> recordedData = new();
    public Collider grabCollider;
    public GameObject assetMenu;

    public List<GameObject> forceArrows = new();

    public enum AssetRecordingType
    {
        None,
        ManualAnimation,
        Physics,
        Follow,
        Visibility
    }

    public AssetRecordingType currentRecordingMode = AssetRecordingType.None;

    public Vector3 initPosBeforePhysicsSimulation;
    public Quaternion initRotBeforePhysicsSimulation;

    private float oldMainPlaybackSliderValue = 0;


    public bool showStatus = true;


    //For assets
    public void InsertAssetRecordFrame(int frameNumber, string actionStr = "None", string collisionStr = "None", Action actionDelegate = null, Action<Frame> collisionDelegate = null, GameObject collidedObject = null, bool propagateValueToSubsequentFrames = false)
    {
        RecordableFrame item = new(transform.position, transform.rotation, showStatus, actionStr, collisionStr, actionDelegate, collisionDelegate, collidedObject, frameNumber);
        DebugLogger.Instance.Log("Inserting asset record frame at a specific frame number " + frameNumber);

        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetDataDict[this];

        if(currentAssetRecordedData[frameNumber].ActionDelegate != null)
        {
            DebugLogger.Instance.Log("InsertAssetRecordFrame() - Action delegate at frame number " + frameNumber + " is not null. Adding to the existing delegate.");
            item.ActionDelegate += currentAssetRecordedData[frameNumber].ActionDelegate; //Copy existing action delegate
            item.Action += "," + currentAssetRecordedData[frameNumber].Action; //Copy existing action
        }

        currentAssetRecordedData[frameNumber] = item;

        if (propagateValueToSubsequentFrames) // Propagate the value to subsequent frames
        {
            DebugLogger.Instance.Log("InsertAssetRecordFrame() - Propagating value to subsequent frames, starting from index " + frameNumber + " to " + currentAssetRecordedData.Count);
            for (int i = frameNumber + 1; i < currentAssetRecordedData.Count; i++)
            {
                //recordedData[i].showStatusForThisFrame = item.showStatusForThisFrame;
                if (currentAssetRecordedData[i].Action == "ApplyForce()")
                {
                    DebugLogger.Instance.Log("InsertAssetRecordFrame() - Encountered ApplyForce() at frame number " + i + ". Breaking out of the loop.");
                    break;
                }
                if (currentAssetRecordedData[i].CollisionStr.StartsWith("Collide("))
                {
                    DebugLogger.Instance.Log("InsertAssetRecordFrame() - Encountered Collide() at frame number " + i + ". Breaking out of the loop.");
                    break;
                }

                currentAssetRecordedData[i].rootPosition = item.rootPosition;
                currentAssetRecordedData[i].rootRotation = item.rootRotation;
            }
        }
    }

    //For assets
    public void Hide()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetDataDict[this];
        //Turn the material in recordable.playbackObject to 0.5 alpha
        gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.translucentMaterial;
        showStatus = false;
        if(Recorder.Instance.GetSizeOfMainRecordedData() > 0)
        {
            DebugLogger.Instance.Log("Recorded hide for  " + base.gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
            InsertAssetRecordFrame((int)AssetManager.Instance.mainRecorder.playbackSlider.value, 
                                    actionStr: "Hide()", 
                                    actionDelegate: () => { GetComponent<Recordable>().SetVisibility(false); });
            SetVisibility(false);

            int _frameStart = (int)Recorder.Instance.playbackSlider.value;
            for (int i = _frameStart; i < currentAssetRecordedData.Count; i++)
            {
                currentAssetRecordedData[i].showStatusForThisFrame = false;
            }
        }

        Recorder.Instance.RefreshAssetsTimeline();
    }

    //For assets
    public void Show()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetDataDict[this];
        //Turn the material in recordable.playbackObject to 1 alpha
        gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.defaultMaterial;
        showStatus = true;
        if(Recorder.Instance.GetSizeOfMainRecordedData() > 0)
        {
            DebugLogger.Instance.Log("Recorded show for " + gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
            InsertAssetRecordFrame((int)AssetManager.Instance.mainRecorder.playbackSlider.value, 
                                    actionStr: "Show()", 
                                    actionDelegate: () => { GetComponent<Recordable>().SetVisibility(true); });
            SetVisibility(true);
                    
            int _frameStart = (int)Recorder.Instance.playbackSlider.value;
            for (int i = _frameStart; i < currentAssetRecordedData.Count; i++)
            {
                currentAssetRecordedData[i].showStatusForThisFrame = true;
            }
        }

        Recorder.Instance.RefreshAssetsTimeline();

    }
    
    //For assets
    public void SetVisibility(bool _showStatus)
    {
        if(_showStatus)
        {
            if(Manager.Instance.currAppState == Manager.AppState.LIVE)
            {
                //Set mesh renderer for playbackObject 
                gameObject.GetComponent<MeshRenderer>().enabled = true;
            }
            else
            {
                gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.defaultMaterial;
            }
        }
        else
        {
            if(Manager.Instance.currAppState == Manager.AppState.LIVE)
            {
                //Set mesh renderer for playbackObject 
                gameObject.GetComponent<MeshRenderer>().enabled = false;
            }
            else
            {
                gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.translucentMaterial;
            }
            
        }
    }

    //For assets
    public void AttachToLeftHand()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetDataDict[this];
        DebugLogger.Instance.Log("Attach called for " + gameObject.name + " in example " + Recorder.Instance.currentActiveExample.exampleId);
        currentRecordingMode = Recordable.AssetRecordingType.Follow;

        InsertAssetRecordFrame((int)Recorder.Instance.playbackSlider.value, 
                                    actionStr: "Follow(Left hand)", 
                                    actionDelegate: () => { GetComponent<Recordable>().Follow(Recorder.Instance.leftHand.transform);}, 
                                    collisionDelegate: (Frame frame) => { frame.IsColliding(base.gameObject, InputManager.Instance.leftHandPinchObj); }, 
                                    collisionStr: "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Left hand)", 
                                    collidedObject: InputManager.Instance.leftHandPinchObj);   
        //Copy the pose from "other" recordable (hands) at index _frameStart, to this asset and propagate the value to subsequent frames
        int _frameStart = (int)Recorder.Instance.playbackSlider.value;
        for (int i = _frameStart + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = Recorder.Instance.currentActiveExample.leftHandData[i].pinchPosition; //Copy the position of the left hand at frame _frameStart
            currentAssetRecordedData[i].Action = "Follow(Left hand)";
            currentAssetRecordedData[i].CollisionStr = "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Left hand)";
        }
        Recorder.Instance.RefreshAssetsTimeline();

    }

    //For assets
    public void AttachToRightHand()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetDataDict[this];        
        DebugLogger.Instance.Log("Attach called for " + gameObject.name + " in example " + Recorder.Instance.currentActiveExample.exampleId);
        currentRecordingMode = Recordable.AssetRecordingType.Follow;

        InsertAssetRecordFrame((int)Recorder.Instance.playbackSlider.value, 
                                    actionStr: "Follow(Right hand)", 
                                    actionDelegate: () => { GetComponent<Recordable>().Follow(Recorder.Instance.rightHand.transform);}, 
                                    collisionDelegate: (Frame frame) => { frame.IsColliding(base.gameObject, InputManager.Instance.rightHandPinchObj); }, 
                                    collisionStr: "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Right hand)", 
                                    collidedObject: InputManager.Instance.rightHandPinchObj);
        //Copy the pose from "other" recordable (hands) at index _frameStart, to this asset and propagate the value to subsequent frames
        int _frameStart = (int)Recorder.Instance.playbackSlider.value;
        for (int i = _frameStart + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = Recorder.Instance.currentActiveExample.rightHandData[i].pinchPosition; //Copy the position of the right hand at frame _frameStart
            currentAssetRecordedData[i].Action = "Follow(Right hand)";
            currentAssetRecordedData[i].CollisionStr = "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Right hand)";
        }
        
        Recorder.Instance.RefreshAssetsTimeline();
        //recordable.playbackObject.transform.SetParent(mainRecorder.objectsToRecord[2].playbackObject.transform);
    }

    //For assets
    public void AttachToLeftHandFocusSquare()
    {
        DebugLogger.Instance.Log("Attach called for " + gameObject.name);
        //currentRecordingMode = Recordable.RecordingType.Follow;
        //CopyPoseFromFocusSquare(Recorder.Instance.objectsToRecord[1], (int)Recorder.Instance.playbackSlider.value, copyFirstRecord:false, copyRotation:false);
        Recorder.Instance.RefreshAssetsTimeline();
        //recordable.playbackObject.transform.SetParent(mainRecorder.objectsToRecord[1].playbackObject.transform);
    }

    //For assets
    public void AttachToRightHandFocusSquare()
    {
        DebugLogger.Instance.Log("Attach called for " + gameObject.name);
        //currentRecordingMode = Recordable.RecordingType.Follow;
        //CopyPoseFromFocusSquare(Recorder.Instance.objectsToRecord[2], (int)Recorder.Instance.playbackSlider.value, copyFirstRecord:false, copyRotation:false);
        Recorder.Instance.RefreshAssetsTimeline();
        //recordable.playbackObject.transform.SetParent(mainRecorder.objectsToRecord[2].playbackObject.transform);
    }

    //For assets
    public void AttachToHeadFocusSquare()
    {
        DebugLogger.Instance.Log("Attach called for " + gameObject.name);
        currentRecordingMode = Recordable.AssetRecordingType.Follow;
        //CopyPoseFromFocusSquare(Recorder.Instance.objectsToRecord[0], (int)Recorder.Instance.playbackSlider.value, copyFirstRecord:false, copyRotation:false);
        Recorder.Instance.RefreshAssetsTimeline();
        //recordable.playbackObject.transform.SetParent(mainRecorder.objectsToRecord[0].playbackObject.transform);
    }
    
    //For assets
    public void Detach()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetDataDict[this];
        DebugLogger.Instance.Log("Detach called for " + gameObject.name);
        currentRecordingMode = Recordable.AssetRecordingType.None;
        //Unfollow();
        InsertAssetRecordFrame((int)Recorder.Instance.playbackSlider.value, 
                                actionStr: "Unfollow()",  
                                actionDelegate: () => { GetComponent<Recordable>().Unfollow(); });
        int _frameStart = (int)Recorder.Instance.playbackSlider.value;
        for (int i = _frameStart + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = currentAssetRecordedData[_frameStart].rootPosition;
            currentAssetRecordedData[i].Action = "None";
            currentAssetRecordedData[i].CollisionStr = "None";
        }
        //CopyPoseFromRecordable(this, (int)Recorder.Instance.playbackSlider.value, copyFirstRecord:true, copyRotation:false);
        Recorder.Instance.RefreshAssetsTimeline();
        //recordable.playbackObject.transform.SetParent(null);
    }


    //For assets
    public void PrepareForceSimulation(Vector3 initialVelocity)
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetDataDict[this];

        if(Manager.Instance.currAppState == Manager.AppState.ASSETRECORDING)
        {
            oldMainPlaybackSliderValue = Recorder.Instance.playbackSlider.value; //This is so awkward, but it works
            currentRecordingMode = Recordable.AssetRecordingType.Physics;
            initPosBeforePhysicsSimulation = transform.position;
            initRotBeforePhysicsSimulation = transform.rotation;

            int _frameStart = (int)Recorder.Instance.playbackSlider.value;
            for (int i = _frameStart; i < currentAssetRecordedData.Count; i++)
            {
                currentAssetRecordedData[i].rootPosition = currentAssetRecordedData[_frameStart].rootPosition;
                currentAssetRecordedData[i].Action = "None";
                currentAssetRecordedData[i].ActionDelegate = null;
                currentAssetRecordedData[i].CollisionStr = "None";
                currentAssetRecordedData[i].CollisionDelegate = null;
            }

            InsertAssetRecordFrame((int)Recorder.Instance.playbackSlider.value, 
                                    actionStr: "ApplyForce()", 
                                    actionDelegate: () => { GetComponent<Recordable>().ApplyForce(initialVelocity); });



            ApplyForce(initialVelocity);
        }
    }

    //For assets
    public void ApplyForce(Vector3 initialVelocity)
    {        
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        GetComponent<Rigidbody>().useGravity = true;
        GetComponent<Rigidbody>().AddForce(initialVelocity, ForceMode.VelocityChange);
    }

    //For assets
    void OnCollisionEnter(Collision collision)
    {
        DebugLogger.Instance.Log("Collision detected between " + base.gameObject.name + " and " + collision.collider.name);
        InputManager.Instance.NotifyCollision(base.gameObject, collision.collider.gameObject);

        //Return if object is colliding with inputmanager's left or right pinch objects
        if(collision.collider.name == "LeftHandPinchContactSphere" || collision.collider.name == "RightHandPinchContactSphere")
        {
            //return;
        }

        if(Manager.Instance.currAppState == Manager.AppState.ASSETRECORDING)
        {
            //Delete all force arrows
            foreach (GameObject obj in forceArrows)
            {
                Destroy(obj);
            } 

            currentRecordingMode = Recordable.AssetRecordingType.None;

            DebugLogger.Instance.Log("Collision detected between " + base.gameObject.name + " and " + collision.collider.name);

            InsertAssetRecordFrame((int)Recorder.Instance.playbackSlider.value, 
                                actionStr: "ResetPhysics()", 
                                collisionStr: "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + "," + Manager.Instance.CleanAssetName(collision.collider.name) + ")", 
                                actionDelegate: () => { GetComponent<Recordable>().ResetPhysicsPropertiesInLiveMode(); }, 
                                collidedObject: collision.collider.gameObject);

            ResetPhysicsProperties();          
            
        }

    }

    //For assets
    public void ResetPhysicsProperties()
    {
        Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        //Recorder.Instance.isMainPlaybackOn = false;
        DebugLogger.Instance.Log("Resetting physics properties");        
        Recorder.Instance.playbackSlider.value = oldMainPlaybackSliderValue;
        transform.position = initPosBeforePhysicsSimulation;
        transform.rotation = initRotBeforePhysicsSimulation;
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        GetComponent<Rigidbody>().useGravity = false;
        Recorder.Instance.RefreshAssetsTimeline();
    }

    public void ResetPhysicsPropertiesInLiveMode()
    {
        //Recorder.Instance.isMainPlaybackOn = false;
        DebugLogger.Instance.Log("Resetting physics properties in live mode");        

        GetComponent<Rigidbody>().mass = 0f;
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        GetComponent<Rigidbody>().useGravity = false;
        Recorder.Instance.RefreshAssetsTimeline();
    }

    //For assets
    void OnCollisionStay(Collision collision)
    {
        if(Manager.Instance.currAppState != Manager.AppState.RECORDING)
        {
            DebugLogger.Instance.Log("Collision detected between " + base.gameObject.name + " and " + collision.collider.name);
            if(collision.collider.name == "LeftHandPinchContactSphere")
            {                
                InputManager.Instance.NotifyCollision(base.gameObject, collision.collider.gameObject);
            }
            else if(collision.collider.name == "RightHandPinchContactSphere")
            {                
                InputManager.Instance.NotifyCollision(base.gameObject, collision.collider.gameObject);
            }            
        }
    }

    //For assets
    void OnCollisionExit(Collision collision)
    {
        if(Manager.Instance.currAppState != Manager.AppState.RECORDING)
        {
            //DebugLogger.Instance.Log("Collision ended between " + gameObject.name + " and " + collision.collider.name);
            if(collision.collider.name == "LeftHandPinchContactSphere")
            {
                
                InputManager.Instance.NotifyCollision(null,null);
            }
            else if(collision.collider.name == "RightHandPinchContactSphere")
            {
                
                InputManager.Instance.NotifyCollision(null,null);
            }            
        }
    }

    //For assets
    public void Follow(Transform other)
    {
        transform.SetParent(other);
    }

    //For assets
    public void Unfollow()
    {
        transform.SetParent(null);
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
public class RecordableFrame : ICloneable
{
    public Vector3 rootPosition;
    public Quaternion rootRotation;

    public int frameNumber;


    public Vector3 pinchPosition;


    public bool showStatusForThisFrame = true;
    public InputManager.Gesture gesture;

    public string Action = "None";

    //public Recordable.RecordingType recordingMode;

    public Action ActionDelegate;

    public Action<Frame> CollisionDelegate;

    public GameObject CollidedObject;
    public string CollisionStr = "None";

    //public Vector3 force;

    //Assets 
    public RecordableFrame(Vector3 _position, Quaternion _rotation, bool _showStatus, string _action, string _collision, Action _actionDelegate, Action<Frame> _collisionDelegate, GameObject _collidedObject, int _frameNumber)
    {
        rootPosition = _position;
        rootRotation = _rotation;
        showStatusForThisFrame = _showStatus;
        frameNumber = _frameNumber;
        Action = _action;
        CollisionStr = _collision;
        ActionDelegate = _actionDelegate;
        CollisionDelegate = _collisionDelegate;
        CollidedObject = _collidedObject;

    }

    public object Clone()
    {
        // Create a new instance of the class
        RecordableFrame clonedFrame = new
        (
            this.rootPosition,
            this.rootRotation,
            this.showStatusForThisFrame,
            this.Action,
            this.CollisionStr,
            this.ActionDelegate,
            this.CollisionDelegate,
            this.CollidedObject,
            this.frameNumber
        );

        clonedFrame.pinchPosition = this.pinchPosition;
        clonedFrame.gesture = this.gesture;        

        // Return the cloned object
        return clonedFrame;
    }


}


