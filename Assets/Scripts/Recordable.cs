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

    public AssetRecordingType currentRecordingMode = AssetRecordingType.None;

    public Vector3 initPosBeforePhysicsSimulation;
    public Quaternion initRotBeforePhysicsSimulation;

    private float oldMainPlaybackSliderValue = 0;

    public GameObject followLineObj;

    public GameObject colliderVisualizerObj, colliderBoundaryGizmoObj1, colliderBoundaryGizmoObj2;
    
    //public bool showStatus = true;

    void Start()
    {
        lastAssetPosition = transform.position;
    }

    void Update()
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

        if(currentRecordingMode == Recordable.AssetRecordingType.Physics)
        {
            //recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, "ApplyForce()", true);                
            //Increment the slider value by frame duration
            if( Recorder.Instance.playbackSlider.value < Recorder.Instance.playbackSlider.maxValue)
                Recorder.Instance.playbackSlider.value += 1;
            ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, actionStr: "ApplyForce()", collisionStr: "None", propagateValueToSubsequentFrames: true);
        }
    }

    /*public void RecordPassiveInteractions()
    {
        if(Manager.Instance.currAppState == Manager.AppState.RECORDING)
        {
            if(Recorder.Instance.playbackSlider.value != oldMainPlaybackSliderValue)
            {
                oldMainPlaybackSliderValue = Recorder.Instance.playbackSlider.value;
                //DebugLogger.Instance.Log("Recording passive interactions for " + gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
                RecordAssetFrame();
            }
        }
    }*/

    public void InitializeVisibility()
    {

    }

    public void RecordAssetFrame()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetDataDict[this];
        //DebugLogger.Instance.Log("Recording frame for " + gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
        if(collidedObject != null)
        {
            DebugLogger.Instance.Log("Collision between " + gameObject.name + " and " + collidedObject.name + "during recording");
            if(collidedObject == InputManager.Instance.leftHandPinchObj)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, "None", "Collide(" + Manager.Instance.CleanAssetName(gameObject.name) + ", Left hand)", null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.leftHandPinchObj); }, InputManager.Instance.leftHandPinchObj, currentAssetRecordedData.Count));
            else if(collidedObject == InputManager.Instance.rightHandPinchObj)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, "None", "Collide(" + Manager.Instance.CleanAssetName(gameObject.name) + ", Right hand)", null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.rightHandPinchObj); }, InputManager.Instance.rightHandPinchObj, currentAssetRecordedData.Count));
            else if(collidedObject == InputManager.Instance.headContactObj)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, "None", "Collide(" + Manager.Instance.CleanAssetName(gameObject.name) + ", Head)", null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.headContactObj); }, InputManager.Instance.headContactObj, currentAssetRecordedData.Count));
            
            //else //collision with other assets
            //     currentAssetRecordedData.Add(new(transform.position, transform.rotation, showStatus, "None", "Collide(" + Manager.Instance.CleanAssetName(gameObject.name) + "," + Manager.Instance.CleanAssetName(InputManager.Instance.collidingObjectNotified_2.name), null, null, null, currentAssetRecordedData.Count));    


            /*else if(InputManager.Instance.collidingObjectNotified_2 == InputManager.Instance.leftFocusSquareObj)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, showStatus, "None", "Collide(" + Manager.Instance.CleanAssetName(gameObject.name) + ", Left focus square)", null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.leftFocusSquareObj); }, InputManager.Instance.leftFocusSquareObj, currentAssetRecordedData.Count));
            else if(InputManager.Instance.collidingObjectNotified_2 == InputManager.Instance.rightFocusSquareObj)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, showStatus, "None", "Collide(" + Manager.Instance.CleanAssetName(gameObject.name) + ", Right focus square)", null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.rightFocusSquareObj); }, InputManager.Instance.rightFocusSquareObj, currentAssetRecordedData.Count));
            else if(InputManager.Instance.collidingObjectNotified_2 == InputManager.Instance.headFocusSquareObj)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, showStatus, "None", "Collide(" + Manager.Instance.CleanAssetName(gameObject.name) + ", Head focus square)", null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.headFocusSquareObj); }, InputManager.Instance.headFocusSquareObj, currentAssetRecordedData.Count));
            else
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, showStatus, "None", "None", null, null, null, currentAssetRecordedData.Count));  */  
        }
        else 
        {
            currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, "None", "None", null, null, null, currentAssetRecordedData.Count));
            if(currentAssetRecordedData.Count == 1)
            {
                DebugLogger.Instance.Log("Setting visibility to true for " + gameObject.name + " at index 0");
                ModifyAssetFrame(0, 
                        actionStr: "Show()", 
                        actionDelegate: () => { GetComponent<Recordable>().SetVisibility(true); });
                //ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, actionStr: "None", collisionStr: "None", propagateValueToSubsequentFrames: true);
            }
        }
        //AssetFrame item = new AssetFrame(recordable.gameObject.transform.position, recordable.gameObject.transform.rotation, recordable.showStatus, "None", "None", null, null, null, i);
    }

    public void PlaybackAssetFrame()
    {
        int currentFrameNum = (int)Recorder.Instance.playbackSlider.value;
        
        foreach (var recordable in Recorder.Instance.currentActiveExample.assetDataDict.Keys)
        {
            var data = Recorder.Instance.currentActiveExample.assetDataDict[recordable];
            if (recordable.gameObject != null && data.Count > 0)
            {
                //DebugLogger.Instance.Log("Playing back " + recordable.playbackObject.name + " at " + currentFrameNum);
                recordable.transform.position = data[currentFrameNum].rootPosition;
                recordable.transform.rotation = data[currentFrameNum].rootRotation; 

                recordable.SetVisibility(data[currentFrameNum].showStatusForThisFrame);
                /*if (data[currentFrameNum].showStatusForThisFrame)
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
        }
    }

    //For assets
    public void ModifyAssetFrame(int frameNumber, string actionStr = "None", string collisionStr = "None", Action actionDelegate = null, Action<Frame> collisionDelegate = null, GameObject collidedObject = null, bool propagateValueToSubsequentFrames = false)
    {
        //AssetFrame item = new(transform.position, transform.rotation, showStatus, actionStr, collisionStr, actionDelegate, collisionDelegate, collidedObject, frameNumber);
        DebugLogger.Instance.Log("Inserting asset record frame at index " + frameNumber);

        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetDataDict[this];

        /*if(currentAssetRecordedData[frameNumber].ActionDelegate != null)
        {
            DebugLogger.Instance.Log("InsertAssetRecordFrame() - Action delegate at frame number " + frameNumber + " is not null. Adding to the existing delegate.");
            item.ActionDelegate += currentAssetRecordedData[frameNumber].ActionDelegate; //Copy existing action delegate
            item.ActionStr += "," + currentAssetRecordedData[frameNumber].ActionStr; //Copy existing action
        }*/

        AssetFrame item = currentAssetRecordedData[frameNumber];
        item.rootPosition = transform.position;
        item.rootRotation = transform.rotation;
        item.ActionStr = actionStr;
        item.ActionDelegate = actionDelegate;
        item.CollisionStr = collisionStr;
        item.CollisionDelegate = collisionDelegate;
        item.CollidedObject = collidedObject;
        //item.showStatusForThisFrame = item.showStatusForThisFrame;
        currentAssetRecordedData[frameNumber] = item;
        //currentAssetRecordedData[frameNumber] = item;

        if (propagateValueToSubsequentFrames) // Propagate the value to subsequent frames
        {
            DebugLogger.Instance.Log("InsertAssetRecordFrame() - Propagating value to subsequent frames, starting from index " + frameNumber + " to " + currentAssetRecordedData.Count);
            for (int i = frameNumber + 1; i < currentAssetRecordedData.Count; i++)
            {
                //recordedData[i].showStatusForThisFrame = item.showStatusForThisFrame;
                if (currentAssetRecordedData[i].ActionStr == "ApplyForce()")
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



    public void StartManualRecording()
    {
        Manager.Instance.currAppState = Manager.AppState.ASSETRECORDING;
        currentRecordingMode = AssetRecordingType.ManualAnimation;
        DebugLogger.Instance.Log("StartRecording in " + gameObject.name);
        firstFrameOfManualRecording = (int)Recorder.Instance.playbackSlider.value;
        //currentActiveRecordable = recordable;
        //recordable.currentRecordingMode = Recordable.RecordingType.ManualAnimation;
    }

    public void StopManualRecording()
    {
        Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        currentRecordingMode = Recordable.AssetRecordingType.None;
        DebugLogger.Instance.Log("StopRecording in " + gameObject.name);
        lastFrameOfManualRecording = (int)Recorder.Instance.playbackSlider.value;
        DebugLogger.Instance.Log("First frame: " + firstFrameOfManualRecording + " Last frame: " + lastFrameOfManualRecording);
        //CheckIfAssetIsFollowingAnything(recordable);
        Recorder.Instance.RefreshAssetsTimeline();
    }

    public void CheckIfAssetIsFollowingAnything(Recordable recordable)
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
            var assetFrameData = Recorder.Instance.currentActiveExample.assetDataDict[recordable][i];
            var headFrameData = Recorder.Instance.currentActiveExample.headData[i];//mainRecorder.objectsToRecord[0].recordedData[i];
            var leftHandFrameData = Recorder.Instance.currentActiveExample.leftHandData[i];
            var rightHandFrameData = Recorder.Instance.currentActiveExample.rightHandData[i];

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
        Recorder.Instance.RefreshAssetsTimeline();
    }

    public void CreateFollowLine()
    {
        //GameObject followLineObj = Instantiate(followLinePrefab, transform.position, Quaternion.identity);        
        followLineObj.GetComponent<FollowLine>().InitializeLine(InputManager.Instance.rightHandPinchObj.transform);
    }

    //For assets
    public void Hide()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetDataDict[this];
        //Turn the material in recordable.playbackObject to 0.5 alpha
        gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.translucentMaterial;
        //showStatus = false;
        if(Recorder.Instance.GetSizeOfMainRecordedData() > 0)
        {
            DebugLogger.Instance.Log("Recorded hide for  " + base.gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
            ModifyAssetFrame((int)AssetManager.Instance.mainRecorder.playbackSlider.value, 
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
        //showStatus = true;
        if(Recorder.Instance.GetSizeOfMainRecordedData() > 0)
        {
            DebugLogger.Instance.Log("Recorded show for " + gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
            ModifyAssetFrame((int)AssetManager.Instance.mainRecorder.playbackSlider.value, 
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
                gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.defaultMaterial;
                //If gameobject has TextAsset component then call ChangeTextPanelBackground(Material mat)
                if(gameObject.GetComponentInChildren<TextAsset>() != null)
                {
                    gameObject.GetComponent<TextAsset>().SetTextVisibility(true);
                }
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

    int FindFollowEndIndex(int _frameStart) //TODO: Check if collision indices should be considered
    {
        //Iterate through Recorder.Instance.LeftHandGestureSequences and Recorder.Instance.RightHandGestureSequences and find the StartIndex closest to _frameStart and greater than _frameStart
        int followEndIndexFromLeftGestures = 0;
        if(Recorder.Instance.LeftHandGestureSequences.Count > 0)
        {
            for (int i = 0; i < Recorder.Instance.LeftHandGestureSequences.Count; i++)
            {
                int gestureEndIndex = Recorder.Instance.LeftHandGestureSequences[i].StartIndex + Recorder.Instance.LeftHandGestureSequences[i].Length;
                if(gestureEndIndex > _frameStart)
                {
                    followEndIndexFromLeftGestures = gestureEndIndex;
                    break;
                }
            }
        }

        int followEndIndexFromRightGestures = 0;
        if(Recorder.Instance.RightHandGestureSequences.Count > 0)
        {
            for (int i = 0; i < Recorder.Instance.RightHandGestureSequences.Count; i++)
            {
                int gestureEndIndex = Recorder.Instance.RightHandGestureSequences[i].StartIndex + Recorder.Instance.RightHandGestureSequences[i].Length;
                if(gestureEndIndex > _frameStart)
                {
                    followEndIndexFromRightGestures = gestureEndIndex;
                    break;
                }
            }
        }

        if(followEndIndexFromLeftGestures != 0 && followEndIndexFromRightGestures != 0)
            return Mathf.Min(followEndIndexFromLeftGestures, followEndIndexFromRightGestures);
        else if(followEndIndexFromLeftGestures == 0 && followEndIndexFromRightGestures != 0)
            return followEndIndexFromRightGestures;
        else if(followEndIndexFromLeftGestures != 0 && followEndIndexFromRightGestures == 0)
            return followEndIndexFromLeftGestures;
        else 
            return 0;
    }

    //For assets
    public void AttachToLeftHand()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetDataDict[this];
        DebugLogger.Instance.Log("Attach called for " + gameObject.name + " in example " + Recorder.Instance.currentActiveExample.exampleId);
        currentRecordingMode = Recordable.AssetRecordingType.Follow;

        ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                    actionStr: "Follow(Left hand)", 
                                    actionDelegate: () => { GetComponent<Recordable>().Follow(Recorder.Instance.leftHand.transform);}, 
                                    collisionDelegate: (Frame frame) => { frame.IsColliding(base.gameObject, InputManager.Instance.leftHandPinchObj); }, 
                                    collisionStr: "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Left hand)", 
                                    collidedObject: InputManager.Instance.leftHandPinchObj);   
        //Copy the pose from "other" recordable (hands) at index _frameStart, to this asset and propagate the value to subsequent frames
        int _frameStart = (int)Recorder.Instance.playbackSlider.value;
        DebugLogger.Instance.Log("Copying pose from left hand at frame " + _frameStart + " to frame " + currentAssetRecordedData.Count);

        int followEndIndex = FindFollowEndIndex(_frameStart);

        DebugLogger.Instance.Log("Copying pose from left hand at frame " + _frameStart + " to frame " + followEndIndex);

        for (int i = _frameStart + 1; i < followEndIndex; i++)
        {
            
            currentAssetRecordedData[i].rootPosition = Recorder.Instance.currentActiveExample.leftHandData[i].pinchPosition; //Copy the position of the left hand at frame _frameStart
            currentAssetRecordedData[i].ActionStr = "Follow(Left hand)";
            currentAssetRecordedData[i].CollisionStr = "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Left hand)";
        }

        DebugLogger.Instance.Log("Unfollowing at frame " + followEndIndex);

        ModifyAssetFrame(followEndIndex, 
                        actionStr: "Unfollow()",  
                        actionDelegate: () => { GetComponent<Recordable>().Unfollow(); });

        currentAssetRecordedData[followEndIndex].rootPosition = currentAssetRecordedData[followEndIndex - 1].rootPosition;                          

        for (int i = followEndIndex + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = currentAssetRecordedData[followEndIndex - 1].rootPosition;
            currentAssetRecordedData[i].ActionStr = "None";
            currentAssetRecordedData[i].CollisionStr = "None";
        }

        Recorder.Instance.RefreshAssetsTimeline();
    }

    //For assets
    public void AttachToRightHand()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetDataDict[this];        
        DebugLogger.Instance.Log("Attach called for " + gameObject.name + " in example " + Recorder.Instance.currentActiveExample.exampleId);
        currentRecordingMode = Recordable.AssetRecordingType.Follow;

        ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                    actionStr: "Follow(Right hand)", 
                                    actionDelegate: () => { GetComponent<Recordable>().Follow(Recorder.Instance.rightHand.transform);}, 
                                    collisionDelegate: (Frame frame) => { frame.IsColliding(base.gameObject, InputManager.Instance.rightHandPinchObj); }, 
                                    collisionStr: "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Right hand)", 
                                    collidedObject: InputManager.Instance.rightHandPinchObj);
        //Copy the pose from "other" recordable (hands) at index _frameStart, to this asset and propagate the value to subsequent frames
        int _frameStart = (int)Recorder.Instance.playbackSlider.value;
        DebugLogger.Instance.Log("Copying pose from right hand at frame " + _frameStart + " to frame " + currentAssetRecordedData.Count);

                
        int followEndIndex = FindFollowEndIndex(_frameStart);

        DebugLogger.Instance.Log("Copying pose from right hand at frame " + _frameStart + " to frame " + followEndIndex);

        for (int i = _frameStart + 1; i < followEndIndex; i++)
        {
            
            currentAssetRecordedData[i].rootPosition = Recorder.Instance.currentActiveExample.rightHandData[i].pinchPosition; //Copy the position of the right hand at frame _frameStart
            currentAssetRecordedData[i].ActionStr = "Follow(Right hand)";
            currentAssetRecordedData[i].CollisionStr = "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Right hand)";
        }

        DebugLogger.Instance.Log("Unfollowing at frame " + followEndIndex);

        ModifyAssetFrame(followEndIndex, 
                        actionStr: "Unfollow()",  
                        actionDelegate: () => { GetComponent<Recordable>().Unfollow(); });

        //TODO:Change ModifyAssetFrame to also accept modifications to position!
        currentAssetRecordedData[followEndIndex].rootPosition = currentAssetRecordedData[followEndIndex - 1].rootPosition;                        

        for (int i = followEndIndex + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = currentAssetRecordedData[followEndIndex-1].rootPosition;
            currentAssetRecordedData[i].ActionStr = "None";
            currentAssetRecordedData[i].CollisionStr = "None";
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
        ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                actionStr: "Unfollow()",  
                                actionDelegate: () => { GetComponent<Recordable>().Unfollow(); });
        int _frameStart = (int)Recorder.Instance.playbackSlider.value;
        for (int i = _frameStart + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = currentAssetRecordedData[_frameStart].rootPosition;
            currentAssetRecordedData[i].ActionStr = "None";
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
                currentAssetRecordedData[i].ActionStr = "None";
                currentAssetRecordedData[i].ActionDelegate = null;
                currentAssetRecordedData[i].CollisionStr = "None";
                currentAssetRecordedData[i].CollisionDelegate = null;
            }

            ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
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

    public void ApplyForceInLiveMode(GameObject pinchObj)
    {        
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
        GetComponent<Rigidbody>().useGravity = true;
        //Add force based on the velocity of the rigidbody component of the hand that is pinching this object
        GetComponent<Rigidbody>().AddForce(pinchObj.GetComponent<Rigidbody>().velocity, ForceMode.VelocityChange);

    }

    GameObject collidedObject;

    public void NotifyAsset(GameObject other)
    {
        collidedObject = other;
    }

    //For assets
    void OnCollisionEnter(Collision collision)
    {
        //DebugLogger.Instance.Log("Collision detected between " + base.gameObject.name + " and " + collision.collider.name);
        InputManager.Instance.NotifyCollision(base.gameObject, collision.collider.gameObject);



        //Return if asset is colliding with inputmanager's left or right pinch objects
        if(collision.collider.name == "LeftHandPinchContactSphere" || collision.collider.name == "RightHandPinchContactSphere")
        {
            return;
        }

        if(currentRecordingMode == Recordable.AssetRecordingType.Physics)
        {
            //Delete all force arrows
            foreach (GameObject obj in forceArrows)
            {
                Destroy(obj);
            } 

            currentRecordingMode = Recordable.AssetRecordingType.None;

            DebugLogger.Instance.Log("Collision detected between " + base.gameObject.name + " and " + collision.collider.name);

            

            ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
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
        //Recorder.Instance.RefreshAssetsTimeline();
    }

    //For assets
    void OnCollisionStay(Collision collision)
    {
        if(Manager.Instance.currAppState != Manager.AppState.RECORDING)
        {
            DebugLogger.Instance.Log("Collision detected between " + base.gameObject.name + " and " + collision.collider.name);
            if(collision.collider.name == "LeftHandPinchContactSphere" || collision.collider.name == "RightHandPinchContactSphere" || collision.collider.name == "HeadContactSphere")
            {                 
                InputManager.Instance.NotifyCollision(base.gameObject, collision.collider.gameObject);
            }
                      
        }
        else //Recording
        {
            NotifyAsset(collision.collider.gameObject); //Notify the asset that it has collided with another object during recording
        }
    }

    //For assets
    void OnCollisionExit(Collision collision)
    {
        if(Manager.Instance.currAppState != Manager.AppState.RECORDING)
        {
            //DebugLogger.Instance.Log("Collision ended between " + gameObject.name + " and " + collision.collider.name);
            if(collision.collider.name == "LeftHandPinchContactSphere" || collision.collider.name == "RightHandPinchContactSphere" || collision.collider.name == "HeadContactSphere")
            {      
                InputManager.Instance.NotifyCollision(null,null);
            }          
        }
        else //Recording
        {
            NotifyAsset(null); //Notify the asset that it has stopped colliding with another object during recording
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

    public void ShowColliderVisualizer(bool status)
    {
        colliderVisualizerObj.SetActive(status);
        colliderBoundaryGizmoObj1.SetActive(status);
        if(colliderBoundaryGizmoObj2 != null) //Two gizmos for box collider
        {
            colliderBoundaryGizmoObj2.SetActive(status);
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
public class AssetFrame : ICloneable
{
    public Vector3 rootPosition;
    public Quaternion rootRotation;

    public int frameNumber;


    public Vector3 pinchPosition;


    public bool showStatusForThisFrame = true;
    public InputManager.Gesture gesture;

    public string ActionStr = "None";

    //public Recordable.RecordingType recordingMode;

    public Action ActionDelegate;

    public Action<Frame> CollisionDelegate;

    public GameObject CollidedObject;
    public string CollisionStr = "None";

    //public string voiceCommand = "None";

    //public Vector3 force;

    //Assets 
    public AssetFrame(Vector3 _position, Quaternion _rotation, bool _showStatus, string _action, string _collision, Action _actionDelegate, Action<Frame> _collisionDelegate, GameObject _collidedObject, int _frameNumber)
    {
        rootPosition = _position;
        rootRotation = _rotation;
        showStatusForThisFrame = _showStatus;
        frameNumber = _frameNumber;
        ActionStr = _action;
        CollisionStr = _collision;
        ActionDelegate = _actionDelegate;
        CollisionDelegate = _collisionDelegate;
        CollidedObject = _collidedObject;

    }

    public object Clone()
    {
        // Create a new instance of the class
        AssetFrame clonedFrame = new
        (
            this.rootPosition,
            this.rootRotation,
            this.showStatusForThisFrame,
            this.ActionStr,
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


