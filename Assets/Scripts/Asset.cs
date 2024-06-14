using System;
using System.Collections;
using System.Collections.Generic;
using Assets.OVR.Scripts;
using UnityEngine;
using UnityEngine.Serialization;

//[RequireComponent(typeof(LineRenderer))]
public class Asset : MonoBehaviour
{
    //public List<RecordableFrame> recordedData = new();
    public Collider grabCollider;
    public GameObject assetMenu;

    Vector3 lastAssetPosition = Vector3.zero;

    public List<GameObject> forceArrows = new();

    int firstFrameOfManualRecording, lastFrameOfManualRecording;
    
    public enum AssetRecordingType
    {
        None,
        ManualAnimation,
        Physics,
        Follow,
        Visibility
    }

    public Vector3 initPosBeforePhysicsSimulation;
    public Quaternion initRotBeforePhysicsSimulation;
    
    public GameObject followLineObj;

    public GameObject colliderVisualizerObj, colliderBoundaryGizmoObj1, colliderBoundaryGizmoObj2;

    //TODO the color is the color of the material of the asset, we do not need this extra variable
    public Color CurrentColor
    {
        get { return gameObject.GetComponent<MeshRenderer>().material.color; }
        set { 
            gameObject.GetComponent<MeshRenderer>().material.color = value;
            //DebugLogger.Instance.Log("Setting color to " + _color + " for " + gameObject.name);
        }
    }
    
    public Material defaultMaterial;
    
    //public bool showStatus = true;

    void Start()
    {
        lastAssetPosition = transform.position;
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
        */

        if(Manager.Instance.currAppState == Manager.AppState.SIMULATING)
        {
            //recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, "ApplyForce()", true);                
            //Increment the slider value by frame duration
            if( Recorder.Instance.playbackSlider.value < Recorder.Instance.playbackSlider.maxValue)
                Recorder.Instance.playbackSlider.value += 1;
            ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                propagateValueToSubsequentFrames: true);
        }


    }

    public void RecordAssetFrame()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetsDict[this].assetFrames;
        currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, CurrentColor));
    }

    public void PlaybackAssetFrame()
    {
        int currentFrameNum = (int)Recorder.Instance.playbackSlider.value;
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetsDict[this].assetFrames;
        
        //foreach (var recordable in Recorder.Instance.currentActiveExample.assetDataDict.Keys)
        //{
            //var data = Recorder.Instance.currentActiveExample.assetDataDict[recordable];
            if (Recorder.Instance.GetSizeOfMainRecordedData() > 0 && currentFrameNum < currentAssetRecordedData.Count)
            {
                //DebugLogger.Instance.Log("Playing back " + recordable.playbackObject.name + " at " + currentFrameNum);
                transform.position = currentAssetRecordedData[currentFrameNum].rootPosition;
                transform.rotation = currentAssetRecordedData[currentFrameNum].rootRotation; 
                CurrentColor = currentAssetRecordedData[currentFrameNum].color;
                SetVisibility(currentAssetRecordedData[currentFrameNum].isVisible);
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

    //For assets
    public void ModifyAssetFrame(int frameNumber, bool propagateValueToSubsequentFrames = false)
    {
        var recordedAssetFrames = Recorder.Instance.currentActiveExample.assetsDict[this].assetFrames;
        AssetFrame currentAssetFrame = recordedAssetFrames[frameNumber];
        currentAssetFrame.rootPosition = transform.position;
        currentAssetFrame.rootRotation = transform.rotation;
        recordedAssetFrames[frameNumber] = currentAssetFrame;

        if (propagateValueToSubsequentFrames) // Propagate the value to subsequent frames
        {
            //DebugLogger.Instance.Log("InsertAssetRecordFrame() - Propagating value to subsequent frames, starting from index " + frameNumber + " to " + currentAssetRecordedData.Count);
            for (int i = frameNumber + 1; i < recordedAssetFrames.Count; i++)
            {
                recordedAssetFrames[i].rootPosition = currentAssetFrame.rootPosition;
                recordedAssetFrames[i].rootRotation = currentAssetFrame.rootRotation;
            }
        }
    }



    public void StartManualRecording()
    {
        Manager.Instance.currAppState = Manager.AppState.SIMULATING;

        DebugLogger.Instance.Log("StartRecording in " + gameObject.name);
        firstFrameOfManualRecording = (int)Recorder.Instance.playbackSlider.value;
        //currentActiveRecordable = recordable;
        //recordable.currentRecordingMode = Recordable.RecordingType.ManualAnimation;
    }

    public void StopManualRecording()
    {
        Manager.Instance.currAppState = Manager.AppState.PLAYBACK;

        DebugLogger.Instance.Log("StopRecording in " + gameObject.name);
        lastFrameOfManualRecording = (int)Recorder.Instance.playbackSlider.value;
        DebugLogger.Instance.Log("First frame: " + firstFrameOfManualRecording + " Last frame: " + lastFrameOfManualRecording);
        //CheckIfAssetIsFollowingAnything(recordable);
        //Recorder.Instance.RefreshTimelineAndStates();
    }

    public void CheckIfAssetIsFollowingAnything(Asset recordable)
    {
        // Initialize variables to keep track of the count of the closest objects
        int leftHandCount = 0;
        int rightHandCount = 0;
        int leftFocusSquareCount = 0;
        int rightFocusSquareCount = 0;
        int headFocusSquareCount = 0;

        // Iterate over the frames from firstFrameOfManualRecording to lastFrameOfManualRecording
        for (int i = firstFrameOfManualRecording; i <= lastFrameOfManualRecording; i++)
        {
            // Get the RecordFrameData for the current frame
            var assetFrameData = Recorder.Instance.currentActiveExample.assetsDict[recordable].assetFrames[i];
            var headFrameData = Recorder.Instance.currentActiveExample.headFrames[i];//mainRecorder.objectsToRecord[0].recordedData[i];
            var leftHandFrameData = Recorder.Instance.currentActiveExample.leftHandFrames[i];
            var rightHandFrameData = Recorder.Instance.currentActiveExample.rightHandFrames[i];

            // Calculate the distances to the left hand, right hand, left focus square, and right focus square
            // Note: We need to replace the placeholders below with the actual way to access the positions of these objects
            float distanceToLeftHand = Vector3.Distance(assetFrameData.rootPosition, leftHandFrameData.rootPosition);
            float distanceToRightHand = Vector3.Distance(assetFrameData.rootPosition, rightHandFrameData.rootPosition);
            float distanceToLeftFocusSquare = Vector3.Distance(assetFrameData.rootPosition, leftHandFrameData.focusSquarePosition);
            float distanceToRightFocusSquare = Vector3.Distance(assetFrameData.rootPosition, rightHandFrameData.focusSquarePosition);
            float distanceToHeadFocusSquare = Vector3.Distance(assetFrameData.rootPosition, headFrameData.focusSquarePosition);

            // Find the minimum distance and increment the count for the corresponding object
            float minDistance = Mathf.Min(distanceToLeftHand, distanceToRightHand, distanceToLeftFocusSquare, distanceToRightFocusSquare, distanceToHeadFocusSquare);
            if (minDistance == distanceToLeftHand) leftHandCount++;
            else if (minDistance == distanceToRightHand) rightHandCount++;
            else if (minDistance == distanceToLeftFocusSquare) leftFocusSquareCount++;
            else if (minDistance == distanceToRightFocusSquare) rightFocusSquareCount++;
            else headFocusSquareCount++;
        }

        // Determine which object was closest most frequently and return that information
        int maxCount = Mathf.Max(leftHandCount, rightHandCount, leftFocusSquareCount, rightFocusSquareCount, headFocusSquareCount);
        if (maxCount == leftHandCount)
        {
            DebugLogger.Instance.Log("Asset is following left hand");            
            //recordable.CopyPoseFromRecordable(mainRecorder.objectsToRecord[1], firstFrameOfManualRecording, lastFrameOfManualRecording, copyFirstRecord:false, copyRotation:false);
            //recordable.CopyPoseFromRecordable(recordable, lastFrameOfManualRecording, copyFirstRecord:true, copyRotation:false);
        }
        else if (maxCount == rightHandCount)
        {
            DebugLogger.Instance.Log("Asset is following right hand");
            //recordable.CopyPoseFromRecordable(mainRecorder.objectsToRecord[2], firstFrameOfManualRecording, lastFrameOfManualRecording, copyFirstRecord:false, copyRotation:false);
            //recordable.CopyPoseFromRecordable(recordable, lastFrameOfManualRecording, copyFirstRecord:true, copyRotation:false);
        }
        else if (maxCount == leftFocusSquareCount)
        {
            DebugLogger.Instance.Log("Asset is following left focus square");
            //recordable.CopyPoseFromFocusSquare(mainRecorder.objectsToRecord[1], firstFrameOfManualRecording, lastFrameOfManualRecording, copyFirstRecord:false, copyRotation:false);
            //recordable.CopyPoseFromFocusSquare(recordable, lastFrameOfManualRecording, copyFirstRecord:true, copyRotation:false);
        }
        else if (maxCount == rightFocusSquareCount)
        {
            DebugLogger.Instance.Log("Asset is following right focus square");
            //recordable.CopyPoseFromFocusSquare(mainRecorder.objectsToRecord[2], firstFrameOfManualRecording, lastFrameOfManualRecording, copyFirstRecord:false, copyRotation:false);
            //recordable.CopyPoseFromFocusSquare(recordable, lastFrameOfManualRecording, copyFirstRecord:true, copyRotation:false);
        }
        else
        {
            DebugLogger.Instance.Log("Asset is following head focus square");
            //recordable.CopyPoseFromFocusSquare(mainRecorder.objectsToRecord[0], firstFrameOfManualRecording, lastFrameOfManualRecording, copyFirstRecord:false, copyRotation:false);
            //recordable.CopyPoseFromFocusSquare(recordable, lastFrameOfManualRecording, copyFirstRecord:true, copyRotation:false);
        }
  
        //Finally, refresh the timeline
        //Recorder.Instance.RefreshTimelineAndStates();
    }

    /*public void CreateFollowLine()
    {
        //GameObject followLineObj = Instantiate(followLinePrefab, transform.position, Quaternion.identity);        
        followLineObj.GetComponent<FollowLine>().ResetFollowLine();
    }*/


    public void RecordAction(ACTION_ENUM actionType, Action actionDelegate,int frameStart = -1)
    {
        if (frameStart == -1)
        {
            frameStart = (int)Recorder.Instance.playbackSlider.value;
        }

        var newAction = CreateAction(frameStart, actionType, actionDelegate);

        if (newAction.IsFollow())
        {
            var frameEndForFollow = FindFollowEndIndex((int)Recorder.Instance.playbackSlider.value);
            newAction.Length = frameEndForFollow - frameStart;
            RecordUnfollow(frameEndForFollow);
        }
        
        Recorder.Instance.currentActiveExample.assetsDict[this].assetActions.Add(newAction);

        UpdateAllAssetFramesAndCollisions(frameStart, Recorder.Instance.currentActiveExample);
        
        Recorder.Instance.CreateTimelineActionsForAsset(this);
    }
    
    public AssetActionSequence CreateAction(int frameStart, ACTION_ENUM actionType, Action actionDelegate)
    {
        var newAssetActionSequence = new AssetActionSequence();
        newAssetActionSequence.StartIndex = frameStart;
        newAssetActionSequence.ActionType = actionType;
        newAssetActionSequence.ActionDelegate = actionDelegate;

        return newAssetActionSequence;
    }

    public void SaveMainVisualValuesIn(AssetFrame assetFrame)
    {
        assetFrame.rootPosition = transform.position;
        assetFrame.rootRotation = transform.rotation;
        assetFrame.color = gameObject.GetComponent<MeshRenderer>().material.color;
        // assetFrame.isVisible = gameObject.GetComponent<MeshRenderer>().enabled;
        assetFrame.isVisible = gameObject.GetComponent<MeshRenderer>().material == defaultMaterial;
    }

    public void ResetMainVisualValues()
    {
        transform.position = InitialPosition;
        transform.rotation = InitialRotation;
        CurrentColor = Color.gray;
        SetVisibility(true);
    }

    private IEnumerator SimulateAssetFramesAndCollisions(Example example, Dictionary<int, List<AssetActionSequence>> actionsToPerformGroupedByFrames)
    {
        var oldState = Manager.Instance.currAppState;
        var oldActiveExample = Recorder.Instance.currentActiveExample;
        
        Recorder.Instance.currentActiveExample = example;
        
        Manager.Instance.currAppState = Manager.AppState.SIMULATING;
        
        var allAssets = Recorder.Instance.allAssets;
        for (int frameIndex = 0; frameIndex < Recorder.Instance.GetSizeOfMainRecordedData(); frameIndex++)
        {
            Recorder.Instance.playbackSlider.value = frameIndex;
            //DebugLogger.Instance.Log("AddAssetFrameToAssetFramesDict: Adding frame at index " + Recorder.Instance.currentActiveExample.assetDataDict[recordable].Count);
            foreach (var anAction in actionsToPerformGroupedByFrames[frameIndex])
            {
                // if (frameIndex == anAction.FrameStart) This check should be unnecesary
                anAction.ActionDelegate(); //This will execute the action and any related collision
            }
            
            yield return new WaitForSeconds(0.01f); 
            //We pause the execution of this routine to let Unity send the collision events: OnCollisionEnter, OnCollisionStay, OnCollisionExit
            //Collisions are saved in the corresponding model Example
            
            allAssets.ForEach(asset =>
            {
                asset.SaveMainVisualValuesIn(example.assetsDict[asset].assetFrames[frameIndex]);
            });
        }

        //Set the frameEnd of all the unclosed collisions to the final frame of the recorded data
        foreach (var collisionModelWithoutFrameEnd in Recorder.Instance.currentActiveExample.CollisionModelsWithoutFrameEnd())
        {
            collisionModelWithoutFrameEnd.Length = (example.RecordedDataCount - 1) - collisionModelWithoutFrameEnd.StartIndex;
        }
        
        Manager.Instance.currAppState = oldState;
        Recorder.Instance.currentActiveExample = oldActiveExample;

        // Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        // InputManager.Instance.SetPlaybackObjectsActive(false);
        // //DebugLogger.Instance.Log("AddAssetFrameToAssetFramesDict: DoRecordSizesMatch() - " + DoRecordSizesMatch());
        //     
        // Recorder.Instance.RecreateTimelineAssetRows();
        //     
        // Recorder.Instance.RecreateCollisionsInTimeline();
    }
    
    public void UpdateAllAssetFramesAndCollisions(int updateFrameStart, Example example)
    {
        //TODO for now this update should focus on the receiver asset values and not other assets, but physic simulations might make this action to affect other assets
        //TODO use the updateFrameStart so we update only the FrameStart > updateFrameStart
        
        //Create a dictionary where the key is an indexFrame and the value is the corresponding assetAction
        var allActionsGroupedByFrames = new Dictionary<int, List<AssetActionSequence>>();
        Recorder.Instance.allAssets.ForEach(asset => {
            //We bring back the asset to its initial state
            asset.ResetMainVisualValues();
            example.assetsDict[asset].assetActions.ForEach(action => {
                if (!allActionsGroupedByFrames.ContainsKey(action.StartIndex))
                {
                    allActionsGroupedByFrames[action.StartIndex] = new List<AssetActionSequence>();
                }
                allActionsGroupedByFrames[action.StartIndex].Add(action);
            });
        });

        //We clear the collision models before simulating the frames
        example.CollisionModels.Clear();
        
        StartCoroutine(SimulateAssetFramesAndCollisions(example, allActionsGroupedByFrames));
    }
    
    //For assets
    public void RecordHide()
    {
        RecordAction(ACTION_ENUM.HIDE,() => { GetComponent<Asset>().SetVisibility(false); });

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
        RecordAction(ACTION_ENUM.SHOW,() => { GetComponent<Asset>().SetVisibility(true); });
        
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
    
    //For assets
    public void SetVisibility(bool _showStatus)
    {
        //DebugLogger.Instance.Log("SetVisibility: true for " + gameObject.name + " at index " + (int)Recorder.Instance.playbackSlider.value);
        if(_showStatus)
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
        RecordAction(ACTION_ENUM.UNFOLLOW, () =>
        {
            // if (Manager.Instance.currAppState == Manager.AppState.LIVE)
            // {
            // For now, UNFOLLOW is the same for all the modes of the app
                GetComponent<Asset>().ApplyUnfollow();
            // }
        }, startFrame);
    }


    //For assets
    public void PrepareForceSimulation(Vector3 initialVelocity)
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetsDict[this].assetFrames;

        if(Manager.Instance.currAppState == Manager.AppState.SIMULATING)
        {
            AssetManager.Instance.HideMiscObjs();

            InputManager.Instance.leftHandPinchObj.SetActive(false);
            InputManager.Instance.rightHandPinchObj.SetActive(false);

            Recorder.Instance.SavePlaybackPosition();

            initPosBeforePhysicsSimulation = transform.position;
            initRotBeforePhysicsSimulation = transform.rotation;

            int _frameStart = (int)Recorder.Instance.playbackSlider.value;
            for (int i = _frameStart; i < currentAssetRecordedData.Count; i++)
            {
                currentAssetRecordedData[i].rootPosition = currentAssetRecordedData[_frameStart].rootPosition;
            }

            ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value);
            
            ApplyForce(initialVelocity);
            DebugLogger.Instance.Log("Initial velocity magnitude is " + initialVelocity.magnitude);
        }
    }

    //For assets
    public void ApplyForce(Vector3 initialVelocity)
    {        
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        GetComponent<Rigidbody>().useGravity = true;
        GetComponent<Rigidbody>().AddForce(initialVelocity, ForceMode.VelocityChange);
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

    //For assets
    void OnCollisionEnter(Collision collision)
    {
        if (Manager.Instance.currAppState == Manager.AppState.SIMULATING)
        {
            //If we are simulating we need to save the collision
            Recorder.Instance.currentActiveExample.AddNewCollision((int)Recorder.Instance.playbackSlider.value,base.gameObject, collision.collider.gameObject);
        }

        DebugLogger.Instance.Log("Recordable.OnCollisionEnter: Notifying collision detected between " + base.gameObject.name + " and " + collision.collider.name);
        InputManager.Instance.SaveCurrentCollision(base.gameObject, collision.collider.gameObject);

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
        //                         // actionType: ACTION_ENUM.RESET_PHYSICS, 
        //                         // collisionStr: "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + "," + Manager.Instance.CleanAssetName(collision.collider.name) + ")", 
        //                         collisionType: COLLISION_ENUM.COLLIDE, 
        //                         // actionDelegate: () => { GetComponent<Asset>().ResetPhysicsPropertiesInLiveMode(); }, 
        //                         collidedObject: collision.collider.gameObject);
        //
        //     ResetPhysicsProperties();           
        // }
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

        Recorder.Instance.RetrievePlaybackPosition();
        
        transform.position = initPosBeforePhysicsSimulation;
        transform.rotation = initRotBeforePhysicsSimulation;
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        GetComponent<Rigidbody>().useGravity = false;
        
        
        Recorder.Instance.RecreateTimelineAssetRows();
       
    }

    public void ResetPhysicsPropertiesInLiveMode()
    {
        //Recorder.Instance.isMainPlaybackOn = false;
        DebugLogger.Instance.Log("Resetting physics properties in live mode");        

        GetComponent<Rigidbody>().mass = 0f;
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        GetComponent<Rigidbody>().useGravity = false;

    }

    //For assets
    void OnCollisionStay(Collision collision)
    {
        
        if(Manager.Instance.currAppState != Manager.AppState.RECORDING && Manager.Instance.currAppState != Manager.AppState.RECORDING_DURING_PLAYBACK)
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

    //For assets
    void OnCollisionExit(Collision collision)
    {
        if (Manager.Instance.currAppState == Manager.AppState.SIMULATING)
        {
            Recorder.Instance.currentActiveExample.EndPreviousCollision((int)Recorder.Instance.playbackSlider.value,base.gameObject, collision.collider.gameObject);
        }
        
        if(Manager.Instance.currAppState != Manager.AppState.RECORDING && Manager.Instance.currAppState != Manager.AppState.RECORDING_DURING_PLAYBACK)
        {
            //DebugLogger.Instance.Log("Notifying collision ended between " + gameObject.name + " and " + collision.collider.name);
            if(collision.collider.name == "LeftHandPinchContactSphere" || collision.collider.name == "RightHandPinchContactSphere" || collision.collider.name == "HeadContactSphere")
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
        if(colliderBoundaryGizmoObj2 != null) //Two gizmos for box collider
        {
            colliderBoundaryGizmoObj2.SetActive(status);
        }
    }
    
    public Quaternion InitialRotation { get; set; }
    public Vector3 InitialPosition { get; set; }
    
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


