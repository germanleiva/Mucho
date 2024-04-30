using System;
using System.Collections.Generic;
using UnityEngine;

//[RequireComponent(typeof(LineRenderer))]
public class Asset : MonoBehaviour
{
    //public List<RecordableFrame> recordedData = new();
    public Collider grabCollider;
    public GameObject assetMenu;

    Vector3 lastAssetPosition = Vector3.zero;

    public List<GameObject> forceArrows = new();

    int firstFrameOfManualRecording, lastFrameOfManualRecording;

    public bool isThisObjThrown;

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

    private float oldMainPlaybackSliderValue = 0;

    public GameObject followLineObj;

    public GameObject colliderVisualizerObj, colliderBoundaryGizmoObj1, colliderBoundaryGizmoObj2;

    public Color currentObjColor;

    public Material defaultMaterial;
    
    //public bool showStatus = true;

    void Start()
    {
        isThisObjThrown = false;
        lastAssetPosition = transform.position;
        defaultMaterial = gameObject.GetComponent<MeshRenderer>().material;
        currentObjColor = gameObject.GetComponent<MeshRenderer>().material.color;
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

        if(Manager.Instance.currAppState == Manager.AppState.ASSETRECORDING && isThisObjThrown)
        {
            //recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, "ApplyForce()", true);                
            //Increment the slider value by frame duration
            if( Recorder.Instance.playbackSlider.value < Recorder.Instance.playbackSlider.maxValue)
                Recorder.Instance.playbackSlider.value += 1;
            ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, actionStr: ACTION_ENUM.APPLY_FORCE, collisionStr: COLLISION_ENUM.NONE, propagateValueToSubsequentFrames: true);
        }


    }

    public void RecordAssetFrame()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        //DebugLogger.Instance.Log("Recording frame for " + gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
        if(collidedObjectDuringRecording != null)
        {            
            //DebugLogger.Instance.Log("RecordAssetFrame: Collision between " + gameObject.name + " and " + collidedObject.name + "during recording");
            if(collidedObjectDuringRecording == InputManager.Instance.leftHandPinchObj)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, ACTION_ENUM.NONE, COLLISION_ENUM.COLLIDE, null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.leftHandPinchObj); }, InputManager.Instance.leftHandPinchObj, currentObjColor, currentAssetRecordedData.Count));
            else if(collidedObjectDuringRecording == InputManager.Instance.rightHandPinchObj)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, ACTION_ENUM.NONE, COLLISION_ENUM.COLLIDE, null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.rightHandPinchObj); }, InputManager.Instance.rightHandPinchObj, currentObjColor, currentAssetRecordedData.Count));
            else if(collidedObjectDuringRecording == InputManager.Instance.headContactObj)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, ACTION_ENUM.NONE, COLLISION_ENUM.COLLIDE, null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.headContactObj); }, InputManager.Instance.headContactObj, currentObjColor, currentAssetRecordedData.Count));
            else if(collidedObjectDuringRecording == InputManager.Instance.leftFocus)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, ACTION_ENUM.NONE, COLLISION_ENUM.COLLIDE, null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.leftFocus); }, InputManager.Instance.leftFocus, currentObjColor, currentAssetRecordedData.Count));
            else if(collidedObjectDuringRecording == InputManager.Instance.rightFocus)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, ACTION_ENUM.NONE, COLLISION_ENUM.COLLIDE, null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.rightFocus); }, InputManager.Instance.rightFocus, currentObjColor, currentAssetRecordedData.Count));
            else if(collidedObjectDuringRecording == InputManager.Instance.gazeFocus)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, ACTION_ENUM.NONE, COLLISION_ENUM.COLLIDE, null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.gazeFocus); }, InputManager.Instance.gazeFocus, currentObjColor, currentAssetRecordedData.Count));
            else if(collidedObjectDuringRecording == InputManager.Instance.playbackLeftHandPinchObj)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, ACTION_ENUM.NONE, COLLISION_ENUM.COLLIDE, null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.leftHandPinchObj); }, InputManager.Instance.leftHandPinchObj, currentObjColor, currentAssetRecordedData.Count));
            else if(collidedObjectDuringRecording == InputManager.Instance.playbackRightHandPinchObj)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, ACTION_ENUM.NONE, COLLISION_ENUM.COLLIDE, null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.rightHandPinchObj); }, InputManager.Instance.rightHandPinchObj, currentObjColor, currentAssetRecordedData.Count));
            else if(collidedObjectDuringRecording == InputManager.Instance.playbackHeadContactObj)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, ACTION_ENUM.NONE, COLLISION_ENUM.COLLIDE, null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.headContactObj); }, InputManager.Instance.headContactObj, currentObjColor, currentAssetRecordedData.Count));                
            else if(collidedObjectDuringRecording == InputManager.Instance.playbackLeftFocus)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, ACTION_ENUM.NONE, COLLISION_ENUM.COLLIDE, null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.leftFocus); }, InputManager.Instance.leftFocus, currentObjColor, currentAssetRecordedData.Count));
            else if(collidedObjectDuringRecording == InputManager.Instance.playbackRightFocus)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, ACTION_ENUM.NONE, COLLISION_ENUM.COLLIDE, null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.rightFocus); }, InputManager.Instance.rightFocus, currentObjColor, currentAssetRecordedData.Count));
            else if(collidedObjectDuringRecording == InputManager.Instance.playbackGazeFocus)
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, ACTION_ENUM.NONE, COLLISION_ENUM.COLLIDE, null, (Frame frame) => { frame.IsColliding(gameObject, InputManager.Instance.gazeFocus); }, InputManager.Instance.gazeFocus, currentObjColor, currentAssetRecordedData.Count));   
            else
            {
                DebugLogger.Instance.Log("RecordAssetFrame: unaccounted collision between " + gameObject.name + " and " + collidedObjectDuringRecording.name + "during recording");
                currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, ACTION_ENUM.NONE, COLLISION_ENUM.NONE, null, null, null, currentObjColor, currentAssetRecordedData.Count));    
            }
        }
        else 
        {
            currentAssetRecordedData.Add(new(transform.position, transform.rotation, true, ACTION_ENUM.NONE, COLLISION_ENUM.NONE, null, null, null, currentObjColor, currentAssetRecordedData.Count));
            if(currentAssetRecordedData.Count == 1)
            {
                DebugLogger.Instance.Log("Setting visibility to true for " + gameObject.name + " at index 0");
                ModifyAssetFrame(0, 
                        actionStr: ACTION_ENUM.SHOW, 
                        actionDelegate: () => { GetComponent<Asset>().SetVisibility(true); });
                //ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, actionStr: "None", collisionStr: "None", propagateValueToSubsequentFrames: true);
            }
        }
        //AssetFrame item = new AssetFrame(recordable.gameObject.transform.position, recordable.gameObject.transform.rotation, recordable.showStatus, "None", "None", null, null, null, i);
    }

    public void PlaybackAssetFrame()
    {
        int currentFrameNum = (int)Recorder.Instance.playbackSlider.value;
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        
        //foreach (var recordable in Recorder.Instance.currentActiveExample.assetDataDict.Keys)
        //{
            //var data = Recorder.Instance.currentActiveExample.assetDataDict[recordable];
            if (Recorder.Instance.GetSizeOfMainRecordedData() > 0 && currentFrameNum < currentAssetRecordedData.Count)
            {
                //DebugLogger.Instance.Log("Playing back " + recordable.playbackObject.name + " at " + currentFrameNum);
                transform.position = currentAssetRecordedData[currentFrameNum].rootPosition;
                transform.rotation = currentAssetRecordedData[currentFrameNum].rootRotation; 
                SetColor(currentAssetRecordedData[currentFrameNum].color);
                

                SetVisibility(currentAssetRecordedData[currentFrameNum].showStatusForThisFrame);
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
        //}
    }

    //For assets
    public AssetFrame ModifyAssetFrame(int frameNumber, ACTION_ENUM actionStr = ACTION_ENUM.NONE, COLLISION_ENUM collisionStr = COLLISION_ENUM.NONE, Action actionDelegate = null, Action<Frame> collisionDelegate = null, GameObject collidedObject = null, bool propagateValueToSubsequentFrames = false)
    {
        //AssetFrame item = new(transform.position, transform.rotation, showStatus, actionStr, collisionStr, actionDelegate, collisionDelegate, collidedObject, frameNumber);
        //DebugLogger.Instance.Log("Inserting asset record frame at index " + frameNumber);

        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];

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
        //item.color = currentObjColor;
        //item.showStatusForThisFrame = item.showStatusForThisFrame;

        /*if(currentAssetRecordedData[frameNumber].CollisionStr != "None")
        {
            DebugLogger.Instance.Log("InsertAssetRecordFrame() - Collision delegate at frame number " + frameNumber + " is not null. Adding to the existing delegate.");
            item.CollisionDelegate = currentAssetRecordedData[frameNumber].CollisionDelegate; //Copy existing collision delegate
            item.CollisionStr = "," + currentAssetRecordedData[frameNumber].CollisionStr; //Copy existing collision
        }*/


        currentAssetRecordedData[frameNumber] = item;
        //currentAssetRecordedData[frameNumber] = item;

        if (propagateValueToSubsequentFrames) // Propagate the value to subsequent frames
        {
            //DebugLogger.Instance.Log("InsertAssetRecordFrame() - Propagating value to subsequent frames, starting from index " + frameNumber + " to " + currentAssetRecordedData.Count);
            for (int i = frameNumber + 1; i < currentAssetRecordedData.Count; i++)
            {
                //recordedData[i].showStatusForThisFrame = item.showStatusForThisFrame;
                if (currentAssetRecordedData[i].ActionStr == ACTION_ENUM.APPLY_FORCE)
                {
                    DebugLogger.Instance.Log("InsertAssetRecordFrame() - Encountered ApplyForce() at frame number " + i + ". Breaking out of the loop.");
                    break;
                }
                if (currentAssetRecordedData[i].CollisionStr == COLLISION_ENUM.COLLIDE)
                {
                    DebugLogger.Instance.Log("InsertAssetRecordFrame() - Encountered Collide() at frame number " + i + ". Breaking out of the loop.");
                    break;
                }

                currentAssetRecordedData[i].rootPosition = item.rootPosition;
                currentAssetRecordedData[i].rootRotation = item.rootRotation;
            }
        }

        return item;
    }



    public void StartManualRecording()
    {
        Manager.Instance.currAppState = Manager.AppState.ASSETRECORDING;

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
            var assetFrameData = Recorder.Instance.currentActiveExample.assetFramesDict[recordable][i];
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

    //For assets
    public void RecordHide()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        //Turn the material in recordable.playbackObject to 0.5 alpha
        gameObject.GetComponent<MeshRenderer>().material = AssetManager.Instance.translucentMaterial;
        AssetFrame assetFrame = null;
        //showStatus = false;
        if(Recorder.Instance.GetSizeOfMainRecordedData() > 0)
        {
            DebugLogger.Instance.Log("Recorded hide for  " + base.gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
            assetFrame = ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                    actionStr: ACTION_ENUM.HIDE, 
                                    actionDelegate: () => { GetComponent<Asset>().SetVisibility(false); });
            

            int _frameStart = (int)Recorder.Instance.playbackSlider.value;
            for (int i = _frameStart; i < currentAssetRecordedData.Count; i++)
            {
                currentAssetRecordedData[i].showStatusForThisFrame = false;
            }
        }

        SetVisibility(false);

        Recorder.Instance.CreateTimelineActionsForAsset(this);
        
    }

    //For assets
    public void RecordShow()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        //Turn the material in recordable.playbackObject to 1 alpha
        gameObject.GetComponent<MeshRenderer>().material = defaultMaterial;
        //showStatus = true;
        AssetFrame assetFrame = null;
        if(Recorder.Instance.GetSizeOfMainRecordedData() > 0)
        {
            DebugLogger.Instance.Log("Recorded show for " + gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
            assetFrame = ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                    actionStr: ACTION_ENUM.SHOW, 
                                    actionDelegate: () => { GetComponent<Asset>().SetVisibility(true); });
            
                    
            int _frameStart = (int)Recorder.Instance.playbackSlider.value;
            for (int i = _frameStart; i < currentAssetRecordedData.Count; i++)
            {
                currentAssetRecordedData[i].showStatusForThisFrame = true;
            }
        }

        SetVisibility(true);

        Recorder.Instance.CreateTimelineActionsForAsset(this);

    }

    public void RecordColorChange(UnityEngine.UI.Image buttonImage)
    {
        Color color = buttonImage.color;
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        AssetFrame assetFrame = null;
        if(Recorder.Instance.GetSizeOfMainRecordedData() > 0)
        {
            DebugLogger.Instance.Log("Recorded color change for " + gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
            assetFrame = ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                        actionStr: ACTION_ENUM.CHANGE_COLOR, 
                                        actionDelegate: () => { GetComponent<Asset>().SetColor(color); });
            

            int _frameStart = (int)Recorder.Instance.playbackSlider.value;
            for (int i = _frameStart; i < currentAssetRecordedData.Count; i++)
            {
                currentAssetRecordedData[i].color = color;
            }
        }

        SetColor(color);

        Recorder.Instance.CreateTimelineActionsForAsset(this);
    }

    public void ReplaceMesh(GameObject newMeshObj)
    {
        //Replace the mesh of the current gameObject with the mesh of newMeshObj
        gameObject.GetComponent<MeshFilter>().mesh = newMeshObj.GetComponent<MeshFilter>().mesh;
    }

    public void SetColor(Color _color)
    {
        currentObjColor = _color;
        gameObject.GetComponent<MeshRenderer>().material.color = _color;
        //DebugLogger.Instance.Log("Setting color to " + _color + " for " + gameObject.name);
    }

    public void RecordPinToLeftHand()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        DebugLogger.Instance.Log("Recorded pin for " + gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
        AssetFrame assetFrame;
        assetFrame = ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                actionStr: ACTION_ENUM.PIN, 
                                actionDelegate: () => { GetComponent<Asset>().Pin(InputManager.Instance.leftHandPinchObj.transform.position); }); 
        
        int _frameStart = (int)Recorder.Instance.playbackSlider.value;
        for (int i = _frameStart + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = Recorder.Instance.currentActiveExample.leftHandFrames[_frameStart].pinchPosition;
            //currentAssetRecordedData[i].ActionStr = "Pin()";
        }


        //Pin(Recorder.Instance.leftHand.GetCurrentPosition());

        Recorder.Instance.CreateTimelineActionsForAsset(this);
    }

    public void RecordPinToRightHand()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        DebugLogger.Instance.Log("Recorded pin for " + gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
        AssetFrame assetFrame;
        assetFrame = ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                actionStr: ACTION_ENUM.PIN, 
                                actionDelegate: () => { GetComponent<Asset>().Pin(InputManager.Instance.rightHandPinchObj.transform.position); });
        
        int _frameStart = (int)Recorder.Instance.playbackSlider.value;
        for (int i = _frameStart + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = Recorder.Instance.currentActiveExample.rightHandFrames[_frameStart].pinchPosition;
            //currentAssetRecordedData[i].ActionStr = "Pin()";
        }

        //Pin(Recorder.Instance.rightHand.GetCurrentPosition());

        Recorder.Instance.CreateTimelineActionsForAsset(this);
    }

    public void RecordPinToLeftFocus()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        DebugLogger.Instance.Log("Recorded pin for " + gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
        AssetFrame assetFrame;
        assetFrame = ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                actionStr: ACTION_ENUM.PIN, 
                                actionDelegate: () => { GetComponent<Asset>().Pin(InputManager.Instance.leftFocus.transform.position); });
        
        int _frameStart = (int)Recorder.Instance.playbackSlider.value;
        for (int i = _frameStart + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = Recorder.Instance.currentActiveExample.leftHandFrames[_frameStart].focusSquarePosition;
            //currentAssetRecordedData[i].ActionStr = "Pin()";
        }

        //Pin(Recorder.Instance.leftFocus.transform.position);

        Recorder.Instance.CreateTimelineActionsForAsset(this);
    }

    public void RecordPinToRightFocus()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        DebugLogger.Instance.Log("Recorded pin for " + gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
        AssetFrame assetFrame;
        assetFrame = ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                actionStr: ACTION_ENUM.PIN, 
                                actionDelegate: () => { GetComponent<Asset>().Pin(InputManager.Instance.rightFocus.transform.position); });
        
        int _frameStart = (int)Recorder.Instance.playbackSlider.value;
        for (int i = _frameStart + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = Recorder.Instance.currentActiveExample.rightHandFrames[_frameStart].focusSquarePosition;
            //currentAssetRecordedData[i].ActionStr = "Pin()";
        }

        //Pin(Recorder.Instance.rightFocus.transform.position);
        
        Recorder.Instance.CreateTimelineActionsForAsset(this);
    }

    public void RecordPinToGazeFocus()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        DebugLogger.Instance.Log("Recorded pin for " + gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
        AssetFrame assetFrame;
        assetFrame = ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                actionStr: ACTION_ENUM.PIN, 
                                actionDelegate: () => { GetComponent<Asset>().Pin(InputManager.Instance.gazeFocus.transform.position); });
        
        int _frameStart = (int)Recorder.Instance.playbackSlider.value;
        for (int i = _frameStart + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = Recorder.Instance.currentActiveExample.headFrames[_frameStart].focusSquarePosition;
            //currentAssetRecordedData[i].ActionStr = "Pin()";
        }

        //Pin(Recorder.Instance.gazeFocus.transform.position);

        Recorder.Instance.CreateTimelineActionsForAsset(this);

    }

    /*
    public void RecordPinToWall()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetDataDict[this];
        DebugLogger.Instance.Log("Recorded pin for " + gameObject.name + " at " + Recorder.Instance.playbackSlider.value);
        ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                actionStr: "Pin()", 
                                actionDelegate: () => { GetComponent<Recordable>().Pin(InputManager.Instance.wall.transform.position); });
        
        int _frameStart = (int)Recorder.Instance.playbackSlider.value;
        for (int i = _frameStart + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = Recorder.Instance.currentActiveExample.wallData[_frameStart].focusSquarePosition;
            //currentAssetRecordedData[i].ActionStr = "Pin()";
        }

        //Pin(Recorder.Instance.wall.transform.position);

        Recorder.Instance.RefreshTimelineAndStates();
        //Recorder.Instance.RefreshTimelineAssets();
    } */

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

    int FindFollowEndIndex(int _frameStart) 
    {
        
        int followEndIndexFromStates = 0;
        var statePlaceholders = Recorder.Instance.currentActiveExample.StatePlaceholders;
        foreach (var statePlaceholder in statePlaceholders)
        {
            int stateEndIndex = statePlaceholder.StartIndex + statePlaceholder.Length;
            if(stateEndIndex > _frameStart)
            {
                followEndIndexFromStates = stateEndIndex;
                break;
            }
        }      
        return followEndIndexFromStates;  
    }

    //For assets
    public void FollowLeftHand()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        DebugLogger.Instance.Log("Attach called for " + gameObject.name + " in example " + Recorder.Instance.currentActiveExample.exampleId);

        var assetFrame = ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                    actionStr: ACTION_ENUM.FOLLOW_LEFT_HAND, 
                                    actionDelegate: () => { GetComponent<Asset>().Follow(Recorder.Instance.leftHand.transform);}, 
                                    collisionDelegate: (Frame frame) => { frame.IsColliding(base.gameObject, InputManager.Instance.leftHandPinchObj); }, 
                                    // collisionStr: "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Left hand)", 
                                    collisionStr: COLLISION_ENUM.COLLIDE,
                                    collidedObject: InputManager.Instance.leftHandPinchObj);   
        //Copy the pose from "other" recordable (hands) at index _frameStart, to this asset and propagate the value to subsequent frames
        int _frameStart = (int)Recorder.Instance.playbackSlider.value;
        DebugLogger.Instance.Log("Copying pose from left hand at frame " + _frameStart + " to frame " + currentAssetRecordedData.Count);

        int followEndIndex = FindFollowEndIndex(_frameStart) - 1;

        DebugLogger.Instance.Log("Copying pose from left hand at frame " + _frameStart + " to frame " + followEndIndex);

        for (int i = _frameStart + 1; i < followEndIndex; i++)
        {
            
            currentAssetRecordedData[i].rootPosition = Recorder.Instance.currentActiveExample.leftHandFrames[i].pinchPosition; //Copy the position of the left hand at frame _frameStart
            currentAssetRecordedData[i].ActionStr = ACTION_ENUM.FOLLOW_LEFT_HAND;
            // currentAssetRecordedData[i].CollisionStr = "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Left hand)";
            currentAssetRecordedData[i].CollisionStr = COLLISION_ENUM.COLLIDE;

        }

        DebugLogger.Instance.Log("Unfollowing at frame " + followEndIndex);

        ModifyAssetFrame(followEndIndex, 
                        actionStr: ACTION_ENUM.UNFOLLOW,  
                        actionDelegate: () => { GetComponent<Asset>().Unfollow(); });

        currentAssetRecordedData[followEndIndex].rootPosition = currentAssetRecordedData[followEndIndex - 1].rootPosition;                          

        for (int i = followEndIndex + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = currentAssetRecordedData[followEndIndex - 1].rootPosition;
            currentAssetRecordedData[i].ActionStr = ACTION_ENUM.NONE;
            currentAssetRecordedData[i].CollisionStr = COLLISION_ENUM.NONE;
        }
        
        Recorder.Instance.RefreshTimelineAssets(assetFrame);
    }

    //For assets
    public void FollowRightHand()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];        
        DebugLogger.Instance.Log("Attach called for " + gameObject.name + " in example " + Recorder.Instance.currentActiveExample.exampleId);


        var assetFrame = ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                    actionStr: ACTION_ENUM.FOLLOW_RIGHT_HAND, 
                                    actionDelegate: () => { GetComponent<Asset>().Follow(Recorder.Instance.rightHand.transform);}, 
                                    collisionDelegate: (Frame frame) => { frame.IsColliding(base.gameObject, InputManager.Instance.rightHandPinchObj); }, 
                                    // collisionStr: "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Right hand)", 
                                    collisionStr: COLLISION_ENUM.COLLIDE, 
                                    collidedObject: InputManager.Instance.rightHandPinchObj);
        //Copy the pose from "other" recordable (hands) at index _frameStart, to this asset and propagate the value to subsequent frames
        int _frameStart = (int)Recorder.Instance.playbackSlider.value;
        DebugLogger.Instance.Log("Copying pose from right hand at frame " + _frameStart + " to frame " + currentAssetRecordedData.Count);

                
        int followEndIndex = FindFollowEndIndex(_frameStart) - 1;

        DebugLogger.Instance.Log("Copying pose from right hand at frame " + _frameStart + " to frame " + followEndIndex);

        for (int i = _frameStart + 1; i < followEndIndex; i++)
        {
            
            currentAssetRecordedData[i].rootPosition = Recorder.Instance.currentActiveExample.rightHandFrames[i].pinchPosition; //Copy the position of the right hand at frame _frameStart
            currentAssetRecordedData[i].ActionStr = ACTION_ENUM.FOLLOW_RIGHT_HAND;
            // currentAssetRecordedData[i].CollisionStr = "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Right hand)";
            currentAssetRecordedData[i].CollisionStr = COLLISION_ENUM.COLLIDE;
        }

        DebugLogger.Instance.Log("Unfollowing at frame " + followEndIndex);

        ModifyAssetFrame(followEndIndex, 
                        actionStr: ACTION_ENUM.UNFOLLOW,  
                        actionDelegate: () => { GetComponent<Asset>().Unfollow(); });

        //TODO:Change ModifyAssetFrame to also accept modifications to position!
        currentAssetRecordedData[followEndIndex].rootPosition = currentAssetRecordedData[followEndIndex - 1].rootPosition;                        

        for (int i = followEndIndex + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = currentAssetRecordedData[followEndIndex-1].rootPosition;
            currentAssetRecordedData[i].ActionStr = ACTION_ENUM.NONE;
            currentAssetRecordedData[i].CollisionStr = COLLISION_ENUM.NONE;
        }                        
        
        Recorder.Instance.RefreshTimelineAssets(assetFrame);

        //recordable.playbackObject.transform.SetParent(mainRecorder.objectsToRecord[2].playbackObject.transform);
    }



    //For assets
    public void AttachToLeftHandFocusSquare()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];  
        DebugLogger.Instance.Log("Follow left focus called for " + gameObject.name + " in example " + Recorder.Instance.currentActiveExample.exampleId);


        ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                    actionStr: ACTION_ENUM.FOLLOW_L_FOCUS, 
                                    actionDelegate: () => { GetComponent<Asset>().Follow(InputManager.Instance.leftFocus.transform);}, 
                                    collisionDelegate: (Frame frame) => { frame.IsColliding(base.gameObject, InputManager.Instance.leftFocus); }, 
                                    // collisionStr: "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Left focus)", 
                                    collisionStr: COLLISION_ENUM.COLLIDE,
                                    collidedObject: InputManager.Instance.leftFocus);

        int _frameStart = (int)Recorder.Instance.playbackSlider.value;

        int followEndIndex = FindFollowEndIndex(_frameStart) - 1;
        //int followEndIndex = Recorder.Instance.GetSizeOfMainRecordedData();

        DebugLogger.Instance.Log("Copying pose from left focus square at frame " + _frameStart + " to frame " + followEndIndex);

        Vector3 startPosDiff = Recorder.Instance.currentActiveExample.leftHandFrames[_frameStart].focusSquarePosition - currentAssetRecordedData[_frameStart].rootPosition;

        for (int i = _frameStart + 1; i < followEndIndex; i++)
        {   
            currentAssetRecordedData[i].rootPosition = Recorder.Instance.currentActiveExample.leftHandFrames[i].focusSquarePosition - startPosDiff;
            currentAssetRecordedData[i].ActionStr = ACTION_ENUM.FOLLOW_L_FOCUS;
            // currentAssetRecordedData[i].CollisionStr = "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Left focus)";
            currentAssetRecordedData[i].CollisionStr = COLLISION_ENUM.COLLIDE;
        }
       
        DebugLogger.Instance.Log("Unfollowing at frame " + followEndIndex);

        ModifyAssetFrame(followEndIndex, 
                        actionStr: ACTION_ENUM.UNFOLLOW,  
                        actionDelegate: () => { GetComponent<Asset>().Unfollow(); });

        currentAssetRecordedData[followEndIndex].rootPosition =  Recorder.Instance.currentActiveExample.leftHandFrames[followEndIndex - 1].focusSquarePosition - startPosDiff;

        for (int i = followEndIndex + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = currentAssetRecordedData[followEndIndex - 1].rootPosition;
            currentAssetRecordedData[i].ActionStr = ACTION_ENUM.NONE;
            currentAssetRecordedData[i].CollisionStr = COLLISION_ENUM.NONE;
        }

        Recorder.Instance.RefreshTimelineCollisions();
    }

    //For assets
    public void AttachToRightHandFocusSquare()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];  
        DebugLogger.Instance.Log("AttachToRightHandFocusSquare: Follow right focus called for " + gameObject.name + " in example " + Recorder.Instance.currentActiveExample.exampleId);


        ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                    actionStr: ACTION_ENUM.FOLLOW_R_FOCUS, 
                                    actionDelegate: () => { GetComponent<Asset>().Follow(InputManager.Instance.rightFocus.transform);}, 
                                    collisionDelegate: (Frame frame) => { frame.IsColliding(base.gameObject, InputManager.Instance.rightFocus); }, 
                                    // collisionStr: "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Right focus)", 
                                    collisionStr: COLLISION_ENUM.COLLIDE, 

                                    collidedObject: InputManager.Instance.rightFocus);

        int _frameStart = (int)Recorder.Instance.playbackSlider.value;

        int followEndIndex = FindFollowEndIndex(_frameStart) - 1;
        //int followEndIndex = Recorder.Instance.GetSizeOfMainRecordedData();

        DebugLogger.Instance.Log("Copying pose from right focus square at frame " + _frameStart + " to frame " + followEndIndex);

        Vector3 startPosDiff = Recorder.Instance.currentActiveExample.rightHandFrames[_frameStart].focusSquarePosition - currentAssetRecordedData[_frameStart].rootPosition;

        for (int i = _frameStart + 1; i < followEndIndex; i++)
        {   
            currentAssetRecordedData[i].rootPosition = Recorder.Instance.currentActiveExample.rightHandFrames[i].focusSquarePosition - startPosDiff;
            currentAssetRecordedData[i].ActionStr = ACTION_ENUM.FOLLOW_R_FOCUS;
            // currentAssetRecordedData[i].CollisionStr = "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Right focus)";
            currentAssetRecordedData[i].CollisionStr = COLLISION_ENUM.COLLIDE;
        }

        DebugLogger.Instance.Log("Unfollowing at frame " + followEndIndex);

        ModifyAssetFrame(followEndIndex, 
                        actionStr: ACTION_ENUM.UNFOLLOW,  
                        actionDelegate: () => { GetComponent<Asset>().Unfollow(); });
        
        currentAssetRecordedData[followEndIndex].rootPosition =  Recorder.Instance.currentActiveExample.rightHandFrames[followEndIndex - 1].focusSquarePosition - startPosDiff;

        for (int i = followEndIndex + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = currentAssetRecordedData[followEndIndex - 1].rootPosition;
            currentAssetRecordedData[i].ActionStr = ACTION_ENUM.NONE;
            currentAssetRecordedData[i].CollisionStr = COLLISION_ENUM.NONE;
        }

        Recorder.Instance.RefreshTimelineCollisions();
    }

    //For assets
    public void AttachToGazeFocusSquare()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];  
        DebugLogger.Instance.Log("AttachToGazeFocusSquare: Follow gaze focus called for " + gameObject.name + " in example " + Recorder.Instance.currentActiveExample.exampleId);


        ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                    actionStr: ACTION_ENUM.FOLLOW_G_FOCUS, 
                                    actionDelegate: () => { GetComponent<Asset>().Follow(InputManager.Instance.gazeFocus.transform);}, 
                                    collisionDelegate: (Frame frame) => { frame.IsColliding(base.gameObject, InputManager.Instance.gazeFocus); }, 
                                    // collisionStr: "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Gaze focus)", 
                                    collisionStr: COLLISION_ENUM.COLLIDE, 
                                    collidedObject: InputManager.Instance.gazeFocus);

        int _frameStart = (int)Recorder.Instance.playbackSlider.value;

        int followEndIndex = FindFollowEndIndex(_frameStart);

        DebugLogger.Instance.Log("Copying pose from head focus square at frame " + _frameStart + " to frame " + followEndIndex);

        Vector3 startPosDiff = Recorder.Instance.currentActiveExample.headFrames[_frameStart].focusSquarePosition - currentAssetRecordedData[_frameStart].rootPosition;

        for (int i = _frameStart + 1; i < followEndIndex; i++)
        {   
            currentAssetRecordedData[i].rootPosition = Recorder.Instance.currentActiveExample.headFrames[i].focusSquarePosition - startPosDiff;
            currentAssetRecordedData[i].ActionStr = ACTION_ENUM.FOLLOW_G_FOCUS;
            // currentAssetRecordedData[i].CollisionStr = "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + ", Gaze focus)";
            currentAssetRecordedData[i].CollisionStr = COLLISION_ENUM.COLLIDE;
        }

        DebugLogger.Instance.Log("Unfollowing at frame " + followEndIndex);

        ModifyAssetFrame(followEndIndex, 
                        actionStr: ACTION_ENUM.UNFOLLOW,  
                        actionDelegate: () => { GetComponent<Asset>().Unfollow(); });

        currentAssetRecordedData[followEndIndex].rootPosition =  Recorder.Instance.currentActiveExample.headFrames[followEndIndex - 1].focusSquarePosition - startPosDiff;

        for (int i = followEndIndex + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = currentAssetRecordedData[followEndIndex - 1].rootPosition;
            currentAssetRecordedData[i].ActionStr = ACTION_ENUM.NONE;
            currentAssetRecordedData[i].CollisionStr = COLLISION_ENUM.NONE;
        }

        Recorder.Instance.RefreshTimelineCollisions();
    }
    
    //For assets
    public void Detach()
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];
        DebugLogger.Instance.Log("Unfollow called for " + gameObject.name);

        //Unfollow();
        AssetFrame assetFrame = ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                actionStr: ACTION_ENUM.UNFOLLOW,  
                                actionDelegate: () => { GetComponent<Asset>().Unfollow(); });
        int _frameStart = (int)Recorder.Instance.playbackSlider.value;
        for (int i = _frameStart + 1; i < currentAssetRecordedData.Count; i++)
        {
            currentAssetRecordedData[i].rootPosition = currentAssetRecordedData[_frameStart].rootPosition;
            currentAssetRecordedData[i].ActionStr = ACTION_ENUM.NONE;
            currentAssetRecordedData[i].CollisionStr = COLLISION_ENUM.NONE;
        }
        //CopyPoseFromRecordable(this, (int)Recorder.Instance.playbackSlider.value, copyFirstRecord:true, copyRotation:false);
        Recorder.Instance.RefreshTimelineAssets(assetFrame);
        //recordable.playbackObject.transform.SetParent(null);
    }


    //For assets
    public void PrepareForceSimulation(Vector3 initialVelocity)
    {
        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetFramesDict[this];

        if(Manager.Instance.currAppState == Manager.AppState.ASSETRECORDING && isThisObjThrown)
        {
            AssetManager.Instance.HideMiscObjs();

            InputManager.Instance.leftHandPinchObj.SetActive(false);
            InputManager.Instance.rightHandPinchObj.SetActive(false);
            
            oldMainPlaybackSliderValue = Recorder.Instance.playbackSlider.value; //This is so awkward, but it works

            initPosBeforePhysicsSimulation = transform.position;
            initRotBeforePhysicsSimulation = transform.rotation;

            int _frameStart = (int)Recorder.Instance.playbackSlider.value;
            for (int i = _frameStart; i < currentAssetRecordedData.Count; i++)
            {
                currentAssetRecordedData[i].rootPosition = currentAssetRecordedData[_frameStart].rootPosition;
                currentAssetRecordedData[i].ActionStr = ACTION_ENUM.NONE;
                currentAssetRecordedData[i].ActionDelegate = null;
                currentAssetRecordedData[i].CollisionStr = COLLISION_ENUM.NONE;
                currentAssetRecordedData[i].CollisionDelegate = null;
            }

            ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                    actionStr: ACTION_ENUM.APPLY_FORCE, 
                                    actionDelegate: () => { GetComponent<Asset>().ApplyForceInLiveMode(initialVelocity); });
                                    //actionDelegate: () => { GetComponent<Recordable>().ApplyForce(initialVelocity); });


            ApplyForce(initialVelocity);
            DebugLogger.Instance.Log("Intial velocity magnitude is " + initialVelocity.magnitude);
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

    public void NotifyAsset(GameObject other)
    {
        collidedObjectDuringRecording = other;
    }

    //For assets
    void OnCollisionEnter(Collision collision)
    {
        DebugLogger.Instance.Log("Recordable.OnCollisionEnter: Notifying collision detected between " + base.gameObject.name + " and " + collision.collider.name);
        InputManager.Instance.NotifyCollision(base.gameObject, collision.collider.gameObject);

        //Return if asset is colliding with inputmanager's left or right pinch objects
        if(collision.collider.name == "LeftHandPinchContactSphere" || collision.collider.name == "RightHandPinchContactSphere" || 
            collision.collider.name == "HeadContactSphere" || collision.collider.name == "OVRRightHandVisual_Playback" || 
            collision.collider.name == "OVRLeftHandVisual_Playback")
        {
            return;
        }

        if(Manager.Instance.currAppState == Manager.AppState.ASSETRECORDING && isThisObjThrown)
        {            
            Manager.Instance.currAppState = Manager.AppState.PLAYBACK;

            DebugLogger.Instance.Log("Recordable.OnCollisionEnter: Collision detected between " + base.gameObject.name + " and " + collision.collider.name);            

            ModifyAssetFrame((int)Recorder.Instance.playbackSlider.value, 
                                actionStr: ACTION_ENUM.RESET_PHYSICS, 
                                // collisionStr: "Collide(" + Manager.Instance.CleanAssetName(base.gameObject.name) + "," + Manager.Instance.CleanAssetName(collision.collider.name) + ")", 
                                collisionStr: COLLISION_ENUM.COLLIDE, 
                                actionDelegate: () => { GetComponent<Asset>().ResetPhysicsPropertiesInLiveMode(); }, 
                                collidedObject: collision.collider.gameObject);

            ResetPhysicsProperties();           
        }
    }

    //For assets
    public void ResetPhysicsProperties()
    {
        DebugLogger.Instance.Log("ResetPhysicsProperties called for " + gameObject.name);
        //Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        isThisObjThrown = false;
        //Recorder.Instance.isMainPlaybackOn = false;
        InputManager.Instance.leftHandPinchObj.SetActive(true);
        InputManager.Instance.rightHandPinchObj.SetActive(true);

        InputManager.Instance.NotifyCollision(null,null);       
        Recorder.Instance.playbackSlider.value = oldMainPlaybackSliderValue;
        transform.position = initPosBeforePhysicsSimulation;
        transform.rotation = initRotBeforePhysicsSimulation;
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
        GetComponent<Rigidbody>().useGravity = false;
        
        
        Recorder.Instance.RefreshTimelineAssets(null);
       
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
                InputManager.Instance.NotifyCollision(base.gameObject, collision.collider.gameObject);
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
        if(Manager.Instance.currAppState != Manager.AppState.RECORDING && Manager.Instance.currAppState != Manager.AppState.RECORDING_DURING_PLAYBACK)
        {
            //DebugLogger.Instance.Log("Notifying collision ended between " + gameObject.name + " and " + collision.collider.name);
            if(collision.collider.name == "LeftHandPinchContactSphere" || collision.collider.name == "RightHandPinchContactSphere" || collision.collider.name == "HeadContactSphere")
            {      
                InputManager.Instance.NotifyCollision(null,null);
            }          
        }
        else //Recording
        {
            //NotifyAsset(null); //Notify the asset that it has stopped colliding with another object during recording
            collidedObjectDuringRecording = null;
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

    Manager.AppState oldAppState;
    public void ShowColliderVisualizer(bool status)
    {
        colliderVisualizerObj.SetActive(status);
        colliderBoundaryGizmoObj1.SetActive(status);
        if(status)
        {
            oldAppState = Manager.Instance.currAppState;
            Manager.Instance.currAppState = Manager.AppState.EDITCOLLIDERS;
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

}


[System.Serializable]
public class AssetFrame : ICloneable
{
    public Vector3 rootPosition;
    public Quaternion rootRotation;

    public int frameNumber;


    public Vector3 pinchPosition;


    public bool showStatusForThisFrame = true;
    public InputManager.Gesture gesture;

    public ACTION_ENUM ActionStr = ACTION_ENUM.NONE;

    //public Recordable.RecordingType recordingMode;

    public Action ActionDelegate;

    public Action<Frame> CollisionDelegate;

    public GameObject CollidedObject;
    public COLLISION_ENUM CollisionStr = COLLISION_ENUM.NONE;
    public Color color;

    //public string voiceCommand = "None";

    //public Vector3 force;

    //Assets 
    public AssetFrame(Vector3 _position, Quaternion _rotation, bool _showStatus, ACTION_ENUM _action, COLLISION_ENUM _collision, Action _actionDelegate, Action<Frame> _collisionDelegate, GameObject _collidedObject, Color _color, int _frameNumber)
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
        color = _color;
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
            this.color,
            this.frameNumber
        );

        clonedFrame.pinchPosition = this.pinchPosition;
        clonedFrame.gesture = this.gesture;        

        // Return the cloned object
        return clonedFrame;
    }


}


