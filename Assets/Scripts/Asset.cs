using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.OVR.Scripts;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Serialization;

//[RequireComponent(typeof(LineRenderer))]
public class Asset : MonoBehaviour
{
    private Grabbable _grabbable;
    
    public Quaternion InitialRotation { get; set; }
    public Vector3 InitialPosition { get; set; }

    [NonSerialized]
    public List<ForceArrow> forceArrows = new();
    
    [SerializeField]
    private Collider grabCollider;
    
    [SerializeField]
    public GameObject assetMenu;
    
    [SerializeField]
    private GameObject followLineObj;

    [SerializeField]
    private GameObject colliderVisualizerObj, colliderBoundaryGizmoObj1;
    
    [NonSerialized]
    public Material defaultMaterial;
    
    int firstFrameOfManualRecording, lastFrameOfManualRecording;
    
    public enum AssetRecordingType
    {
        None,
        ManualAnimation,
        Physics,
        Follow,
        Visibility
    }
    

    //TODO the color is the color of the material of the asset, we do not need this extra variable
    public Color CurrentColor
    {
        get => gameObject.GetComponent<MeshRenderer>().material.color;
        set => gameObject.GetComponent<MeshRenderer>().material.color = value;
        //DebugLogger.Instance.Log("Setting color to " + _color + " for " + gameObject.name);
    }

    public bool isVisible
    {
        get => gameObject.GetComponent<MeshRenderer>().material == defaultMaterial;
        set
        {
            //DebugLogger.Instance.Log("SetVisibility: true for " + gameObject.name + " at index " + (int)Recorder.Instance.playbackSlider.value);
            if(value)
            {
                if(Manager.Instance.currAppState == Manager.AppState.LIVE)
                {
                    DebugLogger.Instance.Log("LIVE mode: Setting visibility to true for " + gameObject.name);
                    //Set mesh renderer for playbackObject 
                    gameObject.GetComponent<MeshRenderer>().enabled = true;
                    gameObject.GetComponent<MeshRenderer>().material = defaultMaterial;
                    //If gameobject has TextAsset component then call ChangeTextPanelBackground(Material mat)
                    if(gameObject.GetComponentInChildren<TextAsset>() != null)
                    {
                        gameObject.GetComponent<TextAsset>().SetTextVisibility(true);
                    }
                }
                else
                {
                    gameObject.GetComponent<MeshRenderer>().material = defaultMaterial;
                }
            }
            else
            {
                if(Manager.Instance.currAppState == Manager.AppState.LIVE)
                {
                    DebugLogger.Instance.Log("LIVE mode: Setting visibility to false for " + gameObject.name);
                    //Set mesh renderer for playbackObject 
                    gameObject.GetComponent<MeshRenderer>().enabled = false;
                    gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.translucentMaterial;
                    if(gameObject.GetComponentInChildren<TextAsset>() != null)
                    {
                        gameObject.GetComponent<TextAsset>().SetTextVisibility(false);
                    }
                }
                else
                {
                    gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.translucentMaterial;
                }
            }
        }
    }
    
    private void Awake()
    {
        _grabbable = GetComponent<Grabbable>();
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised += HandlePointerEventRaised;
        }
    }

    private void OnDestroy()
    {
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised -= HandlePointerEventRaised;
        }
    }

    private void HandlePointerEventRaised(PointerEvent pointerEvent)
    {
        //Check if the event is a release
        if (pointerEvent.Type == PointerEventType.Unselect)
        {
            SetInitialVisualMainValues();
            
            Recorder.Instance.UpdateAllAssetFramesAndCollisions((int)Recorder.Instance.playbackSlider.value, Recorder.Instance.currentActiveExample, Recorder.Instance.RecreateTimelineUI_Collisions);
        }
    }

    public void SetInitialVisualMainValues()
    {
        InitialPosition = transform.position;
        InitialRotation = transform.rotation;
    }

    void Start()
    {
        defaultMaterial = gameObject.GetComponent<MeshRenderer>().material;
    }
    
    void FixedUpdate()
    {
        /*
        if(recordable.currentRecordingMode == Recordable.AssetRecordingType.ManualAnimation)
        {
            if (Vector3.Distance(recordable.gameObject.transform.position, lastAssetPosition) > movementRecordThreshold)
            {
                DebugLogger.Instance.Log("Added new frame data for " + recordable.gameObject.name + " at " + mainRecorder.playbackSlider.value);

                //recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, "Collide(" + Manager.Instance.CleanString(recordable.gameObject.name) + ", hand)", true);
                recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, actionStr: "None", collisionStr: "None", propagateValueToSubsequentFrames: true);
                //Increment the slider value by a small value proportional to the total recording time
                mainRecorder.playbackSlider.value += playbackSpeed;
            }
            lastAssetPosition = recordable.gameObject.transform.position;
            
        }

        if(Manager.Instance.currAppState == Manager.AppState.SIMULATING)
        {
            //recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, "ApplyForce()", true);                
            //Increment the slider value by frame duration
            if( Recorder.Instance.playbackSlider.value + 1 <= Recorder.Instance.playbackSlider.maxValue)
                Recorder.Instance.playbackSlider.value += 1;
            //ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, propagateValueToSubsequentFrames: true);
        }
        */


    }

    public void PlaybackAssetFrame()
    {
        int currentFrameNum = (int)Recorder.Instance.playbackSlider.value;
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetsDict[this].assetFrames;
        
        //foreach (var recordable in Recorder.Instance.currentActiveExample.assetDataDict.Keys)
        //{
            //var data = Recorder.Instance.currentActiveExample.assetDataDict[recordable];
            if (Recorder.Instance.currentActiveExample.RecordedDataCount > 0 && currentFrameNum < currentAssetRecordedData.Count)
            {
                //DebugLogger.Instance.Log("Playing back " + recordable.playbackObject.name + " at " + currentFrameNum);
                transform.position = currentAssetRecordedData[currentFrameNum].rootPosition;
                transform.rotation = currentAssetRecordedData[currentFrameNum].rootRotation; 
                CurrentColor = currentAssetRecordedData[currentFrameNum].color;
                isVisible = currentAssetRecordedData[currentFrameNum].isVisible;
                /*if (data[currentFrameNum].isVisible)
                {
                    //DebugLogger.Instance.Log("Showing " + recordable.playbackObject.name + " at " + currentFrameNum);
                    recordable.gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.defaultMaterial;
                }
                else
                {
                    //DebugLogger.Instance.Log("Hiding " + recordable.playbackObject.name + " at " + currentFrameNum);
                    if(Recorder.Instance.isAutomaticPlayback) 
                        recordable.gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.transparentMaterial;
                    else 
                        recordable.gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.translucentMaterial;
                }*/
            }
        //}
    }

    public void RecordAction(ACTION_ENUM actionType, Action actionDelegate,int frameStart = -1)
    {
        if (frameStart == -1)
        {
            frameStart = (int)Recorder.Instance.playbackSlider.value;
        }

        var newAction = new AssetActionSequence
        {
            StartIndex = frameStart,
            ActionType = actionType,
            ActionDelegate = actionDelegate,
            TargetAsset = this
        };
        Recorder.Instance.currentActiveExample.assetsDict[this].assetActions.Add(newAction);

        if (newAction.IsFollow())
        {
            var frameEndForFollow = FindFollowEndIndex((int)Recorder.Instance.playbackSlider.value);
            newAction.Length = frameEndForFollow - frameStart;

            var newEndAction = new AssetActionSequence
            {
                StartIndex = frameEndForFollow,
                ActionType = ACTION_ENUM.FOLLOW_END,
                ActionDelegate = () => { this.ApplyUnfollow(); },
                TargetAsset = this
            };

            newAction.associatedEndAction = newEndAction;
                
            Recorder.Instance.currentActiveExample.assetsDict[this].assetActions.Add(newEndAction);
        }

        Recorder.Instance.UpdateAllAssetFramesAndCollisions(frameStart, Recorder.Instance.currentActiveExample, () =>
        {
            Recorder.Instance.RecreateTimelineUI_Collisions();
            Recorder.Instance.RecreateTimelineUI_ActionsForAsset(this);
        });
    }

    public void SaveMainVisualValuesIn(AssetFrame assetFrame)
    {
        assetFrame.rootPosition = transform.position;
        assetFrame.rootRotation = transform.rotation;
        assetFrame.color = CurrentColor;
        // assetFrame.isVisible = gameObject.GetComponent<MeshRenderer>().enabled;
        assetFrame.isVisible = isVisible;
    }

    public void ResetMainVisualValues()
    {
        transform.position = InitialPosition;
        transform.rotation = InitialRotation;
        CurrentColor = Color.gray;
        isVisible = true;
    }

    //For assets
    public void RecordHide()
    {
        RecordAction(ACTION_ENUM.HIDE, () =>
        {
            GetComponent<Asset>().isVisible = false;
        });

        /*
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        //Turn the material in recordable.playbackObject to 0.5 alpha
        gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.translucentMaterial;
        AssetFrame currentAssetFrame = currentAssetRecordedData[(int)Recorder.Instance.playbackSlider.value];
        //showStatus = false;
        if(Recorder.Instance.GetSizeOfMainRecordedData() > 0)
        {
            DebugLogger.Instance.Log("Recorded hide for  " + base.gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
            ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                    actionType: ACTION_ENUM.HIDE, 
                                    collisionType: currentAssetFrame.CollisionType,
                                    collidedObject: currentAssetFrame.CollidedObject,
                                    actionDelegate: () => { GetComponent<Asset>().SetVisibility(false); });

            int _frameStart = (int)Recorder.Instance.playbackSlider.value;
            for (int i = _frameStart; i < currentAssetRecordedData.Count; i++)
            {
                currentAssetRecordedData[i].isVisible = false;
            }
        }

        SetVisibility(false);
        
        Recorder.Instance.CreateTimelineActionsForAsset(this);*/
        
    }

    //For assets
    public void RecordShow()
    {
        RecordAction(ACTION_ENUM.SHOW, () =>
        {
            GetComponent<Asset>().isVisible = true;
        });
        
        /*var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        //Turn the material in recordable.playbackObject to 1 alpha
        gameObject.GetComponent<MeshRenderer>().material = defaultMaterial;
        //showStatus = true;
        AssetFrame currentAssetFrame = currentAssetRecordedData[(int)Recorder.Instance.playbackSlider.value];;
        if(Recorder.Instance.GetSizeOfMainRecordedData() > 0)
        {
            DebugLogger.Instance.Log("Recorded show for " + gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
            ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                    actionType: ACTION_ENUM.SHOW, 
                                    collisionType: currentAssetFrame.CollisionType,
                                    collidedObject: currentAssetFrame.CollidedObject,
                                    actionDelegate: () => { GetComponent<Asset>().SetVisibility(true); });
            
                    
            int _frameStart = (int)Recorder.Instance.playbackSlider.value;
            for (int i = _frameStart; i < currentAssetRecordedData.Count; i++)
            {
                currentAssetRecordedData[i].isVisible = true;
            }
        }

        SetVisibility(true);*/
    }

    public void RecordColorChange(UnityEngine.UI.Image buttonImage)
    {
        Color color = buttonImage.color;
        RecordAction(ACTION_ENUM.CHANGE_COLOR, () =>
        {
            //TODO this is not considering live vs other modes
            GetComponent<Asset>().CurrentColor = color;
        });

        return; //TODO check that we disabled unnecesary code
        /*Color color = buttonImage.color;
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        AssetFrame currentAssetFrame = currentAssetRecordedData[(int)Recorder.Instance.playbackSlider.value];;
        if(Recorder.Instance.GetSizeOfMainRecordedData() > 0)
        {
            DebugLogger.Instance.Log("Recorded color change for " + gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
            ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                        actionType: ACTION_ENUM.CHANGE_COLOR, 
                                        collisionType: currentAssetFrame.CollisionType,
                                        collidedObject: currentAssetFrame.CollidedObject,
                                        actionDelegate: () => { GetComponent<Asset>().SetColor(color); });
            

            int _frameStart = (int)Recorder.Instance.playbackSlider.value;
            for (int i = _frameStart; i < currentAssetRecordedData.Count; i++)
            {
                currentAssetRecordedData[i].color = color;
            }
        }

        SetColor(color);

        Recorder.Instance.CreateTimelineActionsForAsset(this);*/
    }

    public void ReplaceMesh(GameObject newMeshObj)
    {
        //Replace the mesh of the current gameObject with the mesh of newMeshObj
        gameObject.GetComponent<MeshFilter>().mesh = newMeshObj.GetComponent<MeshFilter>().mesh;
    }

    public void RecordPin(Vector3 position)
    {
        RecordAction(ACTION_ENUM.PIN, () =>
        {
            //For now, this behaviour is the same for all the modes of the app
            GetComponent<Asset>().Pin(position);
        });
    }

    public void Pin(Vector3 location)
    {
        transform.position = location;
    }

    int FindFollowEndIndex(int frameStart) 
    {
        var statePlaceholders = Recorder.Instance.currentActiveExample.StatePlaceholders;
        foreach (var statePlaceholder in statePlaceholders)
        {
            int stateEndIndex = statePlaceholder.StartIndex + statePlaceholder.Length;
            if(stateEndIndex > frameStart)
            {
                return stateEndIndex;
            }
        }      
        return 0;
    }

    //For assets
    public void RecordFollow(FollowLineTrigger.FollowTargetType followTargetType)
    {
        GameObject followTargetInLiveMode;
        GameObject followTargetInPlayback;
        ACTION_ENUM actionType;
        
        switch (followTargetType)
        {
            case FollowLineTrigger.FollowTargetType.LEFTHAND:
                actionType = ACTION_ENUM.FOLLOW_LEFT_HAND;
                followTargetInLiveMode = InputManager.Instance.leftHandPinchObj;
                followTargetInPlayback = InputManager.Instance.playbackLeftHandPinchObj;
                break;
            case FollowLineTrigger.FollowTargetType.RIGHTHAND:
                actionType = ACTION_ENUM.FOLLOW_RIGHT_HAND;
                followTargetInLiveMode = InputManager.Instance.rightHandPinchObj;
                followTargetInPlayback = InputManager.Instance.playbackRightHandPinchObj;
                break;
            case FollowLineTrigger.FollowTargetType.RIGHTFOCUS:
                actionType = ACTION_ENUM.FOLLOW_R_FOCUS;
                followTargetInLiveMode = InputManager.Instance.rightFocus;
                followTargetInPlayback = InputManager.Instance.playbackRightFocus;
                break;
            case FollowLineTrigger.FollowTargetType.LEFTFOCUS:
                actionType = ACTION_ENUM.FOLLOW_L_FOCUS;
                followTargetInLiveMode = InputManager.Instance.leftFocus;
                followTargetInPlayback = InputManager.Instance.playbackLeftFocus;
                break;
            case FollowLineTrigger.FollowTargetType.GAZEFOCUS:
                actionType = ACTION_ENUM.FOLLOW_G_FOCUS;
                followTargetInLiveMode = InputManager.Instance.gazeFocus;
                followTargetInPlayback = InputManager.Instance.playbackGazeFocus;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(followTargetType), followTargetType, null);
        }
        
        RecordAction(actionType, () =>
        {
            if (Manager.Instance.currAppState == Manager.AppState.LIVE)
            {
                GetComponent<Asset>().ApplyFollow(followTargetInLiveMode.transform);
            }
            else
            {
                GetComponent<Asset>().ApplyFollow(followTargetInPlayback.transform);
            }
            
        });
    }
    
    //For assets
    public void Detach()
    {
        RecordUnfollow((int)Recorder.Instance.playbackSlider.value);
    }

    public void RecordUnfollow(int startFrame)
    {
        //TODO delete
        throw new Exception("DEPRECATED");
    }

    //For assets
    public void ApplyForce(Vector3 initialVelocity)
    {
        var rigidBody = GetComponent<Rigidbody>();
        rigidBody.constraints = RigidbodyConstraints.None;
        rigidBody.useGravity = true;
        rigidBody.AddForce(initialVelocity, ForceMode.VelocityChange);
    }


    public void ApplyForceInLiveMode(Vector3 initialVelocity)
    {        
        DebugLogger.Instance.Log("ApplyForceInLiveMode : Velocity during recording is " + initialVelocity);
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        GetComponent<Rigidbody>().useGravity = true;
        //Find whether InputManager.Instance.leftPinchObj or InputManager.Instance.rightPinchObj is closest to the asset
        float distanceToLeftPinchObj = Vector3.Distance(transform.position, InputManager.Instance.leftHandPinchObj.transform.position);
        float distanceToRightPinchObj = Vector3.Distance(transform.position, InputManager.Instance.rightHandPinchObj.transform.position);
        if(distanceToLeftPinchObj < distanceToRightPinchObj)
        {
            DebugLogger.Instance.Log("ApplyForceInLiveMode : Applying force to left pinch object with velocity magnitude " + InputManager.Instance.leftHandVelocity.magnitude);
            GetComponent<Rigidbody>().AddForce(InputManager.Instance.leftHandVelocity , ForceMode.VelocityChange);
            //GetComponent<Rigidbody>().AddForce(initialVelocity , ForceMode.VelocityChange);
        }
        else
        {
            DebugLogger.Instance.Log("ApplyForceInLiveMode : Applying force to right pinch object with velocity " + InputManager.Instance.rightHandVelocity.magnitude);
            GetComponent<Rigidbody>().AddForce(InputManager.Instance.rightHandVelocity, ForceMode.VelocityChange);
        }

        //GetComponent<Rigidbody>().AddForce(pinchObj.GetComponent<Rigidbody>().velocity, ForceMode.VelocityChange);

    }

    GameObject collidedObjectDuringRecording;

    private void OnTriggerEnter(Collider other)
    {
        var currentFrameIndex = (int)Recorder.Instance.playbackSlider.value;

        //Added to save the PotentialAssetToChange while dragging a PremadeAsset such as the Basketball
        if (Manager.Instance.currAppState == Manager.AppState.PLAYBACK && other.gameObject.name.StartsWith("Premade"))
        {
            other.gameObject.GetComponent<MeshCopy>().PotentialAssetToChange = gameObject;
        }
        
        if (Manager.Instance.currAppState == Manager.AppState.SIMULATING)
        {
            //If we are simulating we need to save the collision
            string[] userHandsAndHeadTags = {"Head", "LeftHand", "RightHand"};
            
            if(!other.gameObject.CompareTag("Untagged"))
            {
                if (!userHandsAndHeadTags.Contains(other.gameObject.tag))
                {
                    List<AssetActionSequence> addedResetPhysicsActions = new();

                    foreach (var action in Recorder.Instance.currentActiveExample.assetsDict[this].assetActions)
                    {
                        if (action.ActionType == ACTION_ENUM.APPLY_FORCE_START && action.Length == 0 &&
                            action.StartIndex <= currentFrameIndex)
                        {
                            //This asset has a pending unclosed APPLY_FORCE_START action, this collision is the end of that action
                            action.Length = currentFrameIndex - action.StartIndex;

                            var newResetPhysicsAction = new AssetActionSequence
                            {
                                StartIndex = currentFrameIndex,
                                ActionType = ACTION_ENUM.APPLY_FORCE_END,
                                ActionDelegate = ResetPhysicsPropertiesInLiveMode,
                                TargetAsset = this
                            };

                            action.associatedEndAction = newResetPhysicsAction;

                            addedResetPhysicsActions.Add(newResetPhysicsAction);

                            ResetPhysicsPropertiesInLiveMode();
                        }
                    }

                    foreach (var resetPhysicsActionToAdd in addedResetPhysicsActions)
                    {
                        Recorder.Instance.currentActiveExample.assetsDict[this].assetActions
                            .Add(resetPhysicsActionToAdd);
                    }
                }

                DebugLogger.Instance.Log("Asset.OnCollisionEnter: AddNewCollision >> collision between " 
                                         + base.gameObject.name + " and " + GetComponent<Collider>().name);
                if (!base.gameObject.CompareTag("Floor") &&
                    !other.gameObject.CompareTag("Floor"))
                {
                    Recorder.Instance.currentActiveExample.AddNewCollision((int)Recorder.Instance.playbackSlider.value,
                        base.gameObject, other.gameObject);
                }
            }
            
        }
        
        InputManager.Instance.SaveCurrentCollision(base.gameObject, other.gameObject);

        //TODO Check if this is needed
        
        //Return if asset is colliding with inputmanager's left or right pinch objects
        //Another way of saying this is: if you are colliding with the real-time hands or head ignore the collision
        // if(collision.collider.name == "LeftHandPinchContactSphere" || collision.collider.name == "RightHandPinchContactSphere" || 
        //     collision.collider.name == "HeadContactSphere" || collision.collider.name == "OVRRightHandVisual_Playback" || 
        //     collision.collider.name == "OVRLeftHandVisual_Playback")
        // {
        //     return;
        // }

        // if(Manager.Instance.currAppState == Manager.AppState.SIMULATING)
        // {            
        //     Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        //
        //     DebugLogger.Instance.Log("Recordable.OnCollisionEnter: Collision detected between " + base.gameObject.name + " and " + collision.collider.name);            
        //
        //     ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
        //                         // actionType: ACTION_ENUM.APPLY_FORCE_END, 
        //                         // collisionStr: "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + "," + Manager.Instance.CleanAssetName(collision.collider.name) + ")", 
        //                         collisionType: COLLISION_ENUM.COLLIDE, 
        //                         // actionDelegate: () => { GetComponent<Asset>().ResetPhysicsPropertiesInLiveMode(); }, 
        //                         collidedObject: collision.collider.gameObject);
        //
        //     ResetPhysicsProperties();           
        // }
    }

    //For assets
    void OnCollisionEnter(Collision collision)
    {
        OnTriggerEnter(collision.collider);
    }

    //For assets
    public void ResetPhysicsProperties()
    {
        DebugLogger.Instance.Log("ResetPhysicsProperties called for " + gameObject.name);
        //Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        //Recorder.Instance.isMainPlaybackOn = false;
        InputManager.Instance.leftHandPinchObj.SetActive(true);
        InputManager.Instance.rightHandPinchObj.SetActive(true);

        InputManager.Instance.SaveCurrentCollision(null,null);       
        
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        GetComponent<Rigidbody>().useGravity = false;
    }

    public void ResetPhysicsPropertiesInLiveMode()
    {
        //Recorder.Instance.isMainPlaybackOn = false;
        DebugLogger.Instance.Log("Resetting physics properties in live mode");    

        var rigidBody = GetComponent<Rigidbody>();
        rigidBody.mass = 0f; //TODO Check we don't do this when ApplyForce
        rigidBody.constraints = RigidbodyConstraints.FreezeAll;
        rigidBody.useGravity = false;
    }

    //For assets
    void OnCollisionStay(Collision collision)
    {
        
        if(Manager.Instance.currAppState != Manager.AppState.RECORDING)
        {
            //DebugLogger.Instance.Log("Notifying collision detected between " + base.gameObject.name + " and " + collision.collider.name);
            if(collision.collider.name == "LeftHandPinchContactSphere" || collision.collider.name == "RightHandPinchContactSphere" || collision.collider.name == "HeadContactSphere")
            {                 
                InputManager.Instance.SaveCurrentCollision(base.gameObject, collision.collider.gameObject);
            }
                      
        }
        else //Recording
        {
            //NotifyAsset(collision.collider.gameObject); //Notify the asset that it has collided with another object during recording
            collidedObjectDuringRecording = collision.collider.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //Added to remove the PotentialAssetToChange while dragging a PremadeAsset such as the Basketball
        if (Manager.Instance.currAppState == Manager.AppState.PLAYBACK && other.gameObject.name.StartsWith("Basketball"))
        {
            other.gameObject.GetComponent<MeshCopy>().PotentialAssetToChange = null;
        }
        
        if (Manager.Instance.currAppState == Manager.AppState.SIMULATING)
        {
            if(!other.gameObject.CompareTag("Untagged"))
            {
                DebugLogger.Instance.Log("EndPreviousCollision >> collision ended between " + gameObject.name + " and " + other.name);

                Recorder.Instance.currentActiveExample.EndPreviousCollision((int)Recorder.Instance.playbackSlider.value,base.gameObject, other.gameObject);
            }
        }
        
        if(Manager.Instance.currAppState != Manager.AppState.RECORDING)
        {
            if(other.name == "LeftHandPinchContactSphere" || other.name == "RightHandPinchContactSphere" || other.name == "HeadContactSphere")
            {      
                InputManager.Instance.SaveCurrentCollision(null,null);
            }          
        }
        else //Recording
        {
            //NotifyAsset(null); //Notify the asset that it has stopped colliding with another object during recording
            collidedObjectDuringRecording = null;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        OnTriggerExit(collision.collider);
    }

    //For assets
    public void ApplyFollow(Transform other)
    {
        transform.SetParent(other);
    }

    //For assets
    public void ApplyUnfollow()
    {
        transform.SetParent(null);
    }

    Manager.AppState oldAppState;
    public void ShowColliderVisualizer(bool status)
    {
        colliderVisualizerObj.SetActive(status);
        colliderBoundaryGizmoObj1.SetActive(status);
        if(status)
        {
            oldAppState = Manager.Instance.currAppState;
            Manager.Instance.currAppState = Manager.AppState.EDIT_BOUNDING_SPHERE;
        }
        else
        {
            Manager.Instance.currAppState = oldAppState;
        }
    }
    
    
}

[System.Serializable]
public class AssetFrame : ICloneable
{
    public Vector3 rootPosition;
    public Quaternion rootRotation;
    
    [FormerlySerializedAs("isShown")] [FormerlySerializedAs("showStatusForThisFrame")] public bool isVisible = true;

    public Color color;

    //public string voiceCommand = "None";

    //public Vector3 force;

    //Assets 
    public AssetFrame(Vector3 _position, 
    Quaternion _rotation, 
    bool _showStatus, 
    Color _color)
    {
        rootPosition = _position;
        rootRotation = _rotation;
        isVisible = _showStatus;
        color = _color;
    }

    public object Clone()
    {
        // Create a new instance of the class
        AssetFrame clonedFrame = new
        (
            this.rootPosition,
            this.rootRotation,
            this.isVisible,
            this.color
        );

        // Return the cloned object
        return clonedFrame;
    }


}


