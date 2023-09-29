using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Recorder : MonoBehaviour
{
    public static Recorder Instance { get; private set; }

    [Header("Record & Playback")]
    public GameObject rootPlaybackArea;
    //public GameObject playbackUI;
    public GameObject controlUI;
    public Slider playbackSlider;
    public GameObject playButton;
    public Recordable[] objectsToRecord;
    public bool isMainRecordingOn = false;
    public bool isMainPlaybackOn = false;
    public int recordStartFrame;
    public int recordedFramesTotal;
  
    public bool isAutomaticPlayback = false;

    readonly List<float> handGuideTimePoints = new();
    [Header("Timeline UI")]
    [SerializeField]
    RectTransform stateTimelinePanel;
    [SerializeField]
    RectTransform rightHandTimelinePanel;
    [SerializeField]
    RectTransform leftHandTimelinePanel;
    [SerializeField]
    GameObject stateTimelineElementPrefab;
    [SerializeField]
    GameObject handTimelineElementPrefab;
    [SerializeField]
    GameObject assetTimelineElementPrefab;
    [SerializeField]
    GameObject collisionTimelineElementPrefab;
    [SerializeField]
     RectTransform playbackPanelTransform;
     [SerializeField]
    GameObject hideTimelinePanelPrefab;
    [SerializeField]
    GameObject showTimelinePanelPrefab;
     public GameObject assetTimelinePanelPrefab;
     public GameObject collisionTimelinePanel;
    

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        } 
    }

    void Start()
    {
        initialize();
    }

    public int GetSizeOfMainRecordedData()
    {
        return objectsToRecord[0].recordedData.Count;
    }

    void initialize()
    {
        rootPlaybackArea.SetActive(false);
    }

    // Start recording.
    public void StartRecording()
    {
        Manager.Instance.currAppState = Manager.AppState.RECORDING;
        rootPlaybackArea.SetActive(false);
        DebugLogger.Instance.Log("StartRecording");
        AssetPoseRecorder.Instance.DisableGrabForAllAssets();
        foreach (var recordable in objectsToRecord)
        {
            recordable.ResetData();
        }
        isMainRecordingOn = true;
        recordStartFrame = 0;//Time.time;
    }

    //Reset recording
    public void ResetRecording()
    {
        //currAppState = Manager.AppState.NONE;
        rootPlaybackArea.SetActive(false);
        DebugLogger.Instance.Log("ResetRecording");
        foreach (var recordable in objectsToRecord)
        {
            //recordable.playbackObject.GetComponent<QuickTransformDebug>().enabled = false;
            //recordable.GetComponent<QuickTransformDebug>().enabled = true;
            recordable.playbackObject.SetActive(false);
            //Turn off the line renderer
            //recordable.lineObject.GetComponent<LineRenderer>().enabled = false;            
            recordable.ResetData();
        }
        isMainRecordingOn = false;
        isMainPlaybackOn = false;
        isAutomaticPlayback = false;
    }

    // Stop recording.
    public void StopRecording()
    {
        rootPlaybackArea.SetActive(true);
        DebugLogger.Instance.Log("Size of recordedData head: " + objectsToRecord[0].recordedData.Count);
        DebugLogger.Instance.Log("Size of recordedData leftHand: " + objectsToRecord[1].recordedData.Count);
        DebugLogger.Instance.Log("Size of recordedData rightHand: " + objectsToRecord[2].recordedData.Count);

        isMainRecordingOn = false;

        AssetPoseRecorder.Instance.EnableGrabForAllAssets();
        AssetPoseRecorder.Instance.InitializeRecordFramesForAssets();

        LeftHandGestureSequences = GenerateGestureSequences(leftHandTimelinePanel, objectsToRecord[1]);
        RightHandGestureSequences = GenerateGestureSequences(rightHandTimelinePanel, objectsToRecord[2]);

        gestureSequences = LeftHandGestureSequences.Concat(RightHandGestureSequences).ToList();

        //VisualizePath();
        PreparePlayback();
    }

    public void SetAutomaticPlayMode(bool isAutomatic)
    {
        isAutomaticPlayback = isAutomatic;
    }

    void VisualizePath()
    {
        foreach (var recordable in objectsToRecord)
        {
            //recordable.VisualizePath();
        }
    }

    // Start playback.
    public void PreparePlayback()
    {
        try
        {
            if (isMainRecordingOn) return; // Don't allow playback while recording.
            Manager.Instance.currAppState = Manager.AppState.PLAYBACK;    
            DebugLogger.Instance.Log("StartPlayback");
            // Determine the duration of the recording.
            int framesTotal = 0;
            foreach (var recordable in objectsToRecord)
            {
                recordable.playbackObject.SetActive(true);
                //recordable.playbackObject.GetComponent<QuickTransformDebug>().enabled = true;
                //recordable.GetComponent<QuickTransformDebug>().enabled = false;
                //recordable.lineObject.GetComponent<LineRenderer>().enabled = true;
                if (recordable.recordedData.Count > 0)
                {
                    //duration = Mathf.Max(duration, recordable.recordedData.Last().timestamp);
                    //Find last element of recordedData and get its timestamp
                    framesTotal = Mathf.Max(framesTotal, recordable.recordedData[recordable.recordedData.Count - 1].frameNumber);
                }
            }

            playButton.SetActive(true);

            // Set up the slider.
            playbackSlider.minValue = 0;
            playbackSlider.maxValue = framesTotal;
            playbackSlider.value = 0;
            recordedFramesTotal = framesTotal;
            DebugLogger.Instance.Log("Duration of recording: " + recordedFramesTotal);

            isMainPlaybackOn = true;
            isAutomaticPlayback = false;
            recordStartFrame = 0;//Time.time;
            //CalculateHandGuideTimePoints();
        }
        catch (System.Exception e)
        {
            DebugLogger.Instance.LogException(e);
        }
    }

    public void AlignPlaybackSlider()
    {
        //Iterate through the GestureSequences list and set playback slider to the nearest start index within 10 frames
        int currentFrameNum = (int)playbackSlider.value;
        //DebugLogger.Instance.Log("Initial playback slider value: " + currentFrameNum);
        int nearestLeftHandGestureSequenceStartIndex = 0;
        int nearestRightHandGestureSequenceStartIndex = 0;
        DebugLogger.Instance.Log("recordedFramesTotal: " + (int)recordedFramesTotal/50);
        int threshHold = (int)recordedFramesTotal/50;
        foreach (var sequence in RightHandGestureSequences)
        {
            //DebugLogger.Instance.Log("Right hand gesture: " + GestureManager.Instance.GestureToString(sequence.GestureType) + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
            if(Mathf.Abs(currentFrameNum - sequence.StartIndex) < threshHold)
            {
                //DebugLogger.Instance.Log("Found nearest right hand gesture sequence at " + sequence.StartIndex);  
                nearestRightHandGestureSequenceStartIndex = sequence.StartIndex;
                break;
            }
        }
        foreach (var sequence in LeftHandGestureSequences)
        {
            //DebugLogger.Instance.Log("Left hand gesture: " + GestureManager.Instance.GestureToString(sequence.GestureType) + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
            if(Mathf.Abs(currentFrameNum - sequence.StartIndex) < threshHold)
            {
                //DebugLogger.Instance.Log("Found nearest left hand gesture sequence at " + sequence.StartIndex);
                nearestLeftHandGestureSequenceStartIndex = sequence.StartIndex;
                break;
            }
        }

        //Check which of the two is closer to the currentFrameNum

        int nearestStartIndex = 0;
        if(Mathf.Abs(currentFrameNum - nearestRightHandGestureSequenceStartIndex) < Mathf.Abs(currentFrameNum - nearestLeftHandGestureSequenceStartIndex))
        {
            nearestStartIndex = nearestRightHandGestureSequenceStartIndex;
        }
        else
        {
            nearestStartIndex = nearestLeftHandGestureSequenceStartIndex;
        }

        if(nearestStartIndex == 0)
        {
            nearestStartIndex = currentFrameNum;
        }

        playbackSlider.value = nearestStartIndex;
        DebugLogger.Instance.Log("Aligning playback slider to nearest start index: " + nearestStartIndex);
    }

  
    // Stop playback.
    public void StopPlayback()
    {
        DebugLogger.Instance.Log("StopPlayback");
        isMainPlaybackOn = false;
    }

    public void SetTestMode()
    {
        DebugLogger.Instance.Log("Start Testing");
        Manager.Instance.currAppState = Manager.AppState.TEST;
        rootPlaybackArea.SetActive(false);
        //playbackUI.SetActive(false);
    }

    public void DetachFromAllParents(Transform transform)
    {
        transform.SetParent(null);
        DebugLogger.Instance.Log("Detached " + transform.name + " from all parents");
        //controlUI.transform.SetParent(null);
    }

    public void CreateCopyOfObject(GameObject obj)
    {
        GameObject newObj = Instantiate(obj);
        newObj.transform.SetParent(obj.transform.parent);
        newObj.transform.localPosition = obj.transform.localPosition;
        newObj.transform.localRotation = obj.transform.localRotation;
        newObj.transform.localScale = obj.transform.localScale;
        newObj.name = obj.name + "Copy";
        DetachFromAllParents(obj.transform);
    }

    public void DestroyCopyAndSpawnAsset(GameObject obj)
    {
        if(obj.name.StartsWith("Sphere"))
        {
            AssetPoseRecorder.Instance.SpawnSphere(obj.transform);
        }
        else if(obj.name.StartsWith("Cube"))
        {
            AssetPoseRecorder.Instance.SpawnCube(obj.transform);
        }
        else if(obj.name.StartsWith("Text"))
        {
            AssetPoseRecorder.Instance.SpawnText(obj.transform);
        }
        Destroy(obj);
    }

    public int GetSizeOfMainRecordedData(int index)
    {
        return objectsToRecord[index].recordedData.Count;
    }

    public void ExpandRecordedData(int size)
    {
        // For each recordable object, expand the recordedData list to the given size by copying the last element
        foreach (var recordable in objectsToRecord)
        {
            RecordableFrame lastElement = recordable.recordedData[recordable.recordedData.Count - 1];
            for (int i = 0; i < size - recordable.recordedData.Count; i++)
            {
                recordable.recordedData.Add(lastElement);
            }
        }
        AssetPoseRecorder.Instance.ExpandRecordFramesForAssets(size);
        AssetPoseRecorder.Instance.DoRecordSizesMatch();
    }

    public List<GestureSequence> GetContinuousGestureSequences(List<InputManager.Gesture> gestures)
    {
        List<GestureSequence> sequences = new();

        int startIndex = -1;
        InputManager.Gesture? currentGesture = null;

        for (int i = 0; i < gestures.Count; i++)
        {
            if (gestures[i] != InputManager.Gesture.LEFTHANDNONE && gestures[i] != InputManager.Gesture.RIGHTHANDNONE)
            {
                if (currentGesture == null || currentGesture == gestures[i])
                {
                    if (currentGesture == null)
                    {
                        currentGesture = gestures[i];
                        startIndex = i;
                    }
                }
                else
                {
                    sequences.Add(new GestureSequence
                    {
                        StartIndex = startIndex,
                        Length = i - startIndex,
                        GestureType = currentGesture.Value
                    });

                    startIndex = i;
                    currentGesture = gestures[i];
                }
            }
            else if (currentGesture != null)
            {
                sequences.Add(new GestureSequence
                {
                    StartIndex = startIndex,
                    Length = i - startIndex,
                    GestureType = currentGesture.Value
                });

                startIndex = -1;
                currentGesture = null;
            }
        }

        if (currentGesture != null)
        {
            sequences.Add(new GestureSequence
            {
                StartIndex = startIndex,
                Length = gestures.Count - startIndex,
                GestureType = currentGesture.Value
            });
        }

        //Iterate through gestures and assign the lambda 

        return sequences;
    }

    List<GestureSequence> LeftHandGestureSequences = new List<GestureSequence>();
    List<GestureSequence> RightHandGestureSequences = new List<GestureSequence>();

    public List<GestureSequence> GenerateGestureSequences(RectTransform timelinePanel, Recordable recordable)
    {
        DebugLogger.Instance.Log("Generating gesture sequences for " + recordable.name);
        List<InputManager.Gesture> gestures = recordable.recordedData.Select(x => x.gesture).ToList();
        List<GestureSequence> GestureSequences = GetContinuousGestureSequences(gestures);
        foreach (GestureSequence sequence in GestureSequences)
        {
            //DebugLogger.Instance.Log("Sequence name: " + InputManager.Instance.GestureToString(sequence.GestureType) + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
            //DebugLogger.Instance.Log("Start x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex) + ", End x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex + sequence.Length));
            TimelineUIElement.CreateTimelineElement(handTimelineElementPrefab, timelinePanel, sequence.StartIndex, sequence.Length, GetSizeOfMainRecordedData(0), InputManager.Instance.GestureToString(sequence.GestureType));
        }
        return GestureSequences;
    }    

    public List<AssetSequence> GetContinuousChangeSequences(List<string> actions)
    {
        List<AssetSequence> sequences = new List<AssetSequence>();

        int startIndex = -1;
        string currentAction = null;

        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i] != "None")
            {
                if (currentAction == null || currentAction == actions[i])
                {
                    if (currentAction == null)
                    {
                        currentAction = actions[i];
                        startIndex = i;
                    }
                }
                else
                {
                    sequences.Add(new AssetSequence
                    {
                        StartIndex = startIndex,
                        Length = i - startIndex,
                        Action = currentAction
                    });

                    startIndex = i;
                    currentAction = actions[i];
                }
            }
            else if (currentAction != null)
            {
                sequences.Add(new AssetSequence
                {
                    StartIndex = startIndex,
                    Length = i - startIndex,
                    Action = currentAction
                });

                startIndex = -1;
                currentAction = null;
            }
        }

        if (currentAction != null)
        {
            sequences.Add(new AssetSequence
            {
                StartIndex = startIndex,
                Length = actions.Count - startIndex,
                Action = currentAction
            });
        }

        return sequences;
    }

    public List<AssetSequence> GenerateAssetActionSequences(RectTransform timelinePanel, Recordable recordable)
    {
        //DebugLogger.Instance.Log("Generating asset action sequences for " + recordable.name);
    
        List<string> changes = recordable.recordedData.Select(x => x.Action).ToList();
        List<AssetSequence> sequences = GetContinuousChangeSequences(changes);
        foreach (AssetSequence sequence in sequences)
        {
            //DebugLogger.Instance.Log("Sequence name: " + sequence.Action + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
            //DebugLogger.Instance.Log("Start x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex) + ", End x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex + sequence.Length));
            if(recordable.recordedData[sequence.StartIndex].ActionDelegate != null)
            {
                //DebugLogger.Instance.Log("Action delegate: " + recordable.recordedData[sequence.StartIndex].ActionDelegate.Method.Name + ", Parameters: " + string.Join(", ", recordable.recordedData[sequence.StartIndex].ActionDelegate.Method.GetParameters().Select(x => x.Name)));
            }

            if(sequence.Action.StartsWith("Hide"))
            {
                GameObject hideElement = Instantiate(hideTimelinePanelPrefab, timelinePanel);
                hideElement.SetActive(true);
                hideElement.GetComponent<RectTransform>().anchoredPosition = new Vector2(TimelineUIElement.MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex, GetSizeOfMainRecordedData(0)), hideElement.GetComponent<RectTransform>().anchoredPosition.y);
            }
            else if(sequence.Action.StartsWith("Show"))
            {
                GameObject showElement = Instantiate(showTimelinePanelPrefab, timelinePanel);
                showElement.SetActive(true);
                showElement.GetComponent<RectTransform>().anchoredPosition = new Vector2(TimelineUIElement.MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex, GetSizeOfMainRecordedData(0)), showElement.GetComponent<RectTransform>().anchoredPosition.y);
            }
            else //Other types of events - physics, attach etc
            {
                TimelineUIElement.CreateTimelineElement(assetTimelineElementPrefab, timelinePanel, sequence.StartIndex, sequence.Length, GetSizeOfMainRecordedData(0), sequence.Action);
            }
        }
        return sequences;
    }

    public List<AssetSequence> GenerateCollisionSequences(RectTransform collisionTimelinePanelTransform, Recordable recordable) //Strong assumption that all sources of action come from collision
    {        
        DebugLogger.Instance.Log("Generating collision sequences for " + recordable.name);
        try
        {                
            List<string> sourcesOfChanges = recordable.recordedData.Select(x => x.Collision).ToList();
            List<AssetSequence> sequences = GetContinuousChangeSequences(sourcesOfChanges);
            foreach (AssetSequence sequence in sequences)
            {
                //DebugLogger.Instance.Log("Collision sequence name: " + sequence.Action + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
                //DebugLogger.Instance.Log("Start x: " + MapIndexToTimelinePosition(collisionTimelinePanelTransform, sequence.StartIndex) + ", End x: " + MapIndexToTimelinePosition(collisionTimelinePanelTransform, sequence.StartIndex + sequence.Length));
                
                if(recordable.recordedData[sequence.StartIndex].CollisionDelegate != null)
                    DebugLogger.Instance.Log("Collision delegate: " + recordable.recordedData[sequence.StartIndex].CollisionDelegate.Method.Name + ", Parameters: " + string.Join(", ", recordable.recordedData[sequence.StartIndex].CollisionDelegate.Method.GetParameters().Select(x => x.Name)));
                if(recordable.recordedData[sequence.StartIndex].CollidedObject != null)
                {
                    DebugLogger.Instance.Log("Collided object: " + recordable.recordedData[sequence.StartIndex].CollidedObject.name);
                    sequence.CollidingObject1 = recordable.gameObject;
                    sequence.CollidingObject2 = recordable.recordedData[sequence.StartIndex].CollidedObject;
                }

                TimelineUIElement.CreateTimelineElement(collisionTimelineElementPrefab, collisionTimelinePanelTransform, sequence.StartIndex, sequence.Length, GetSizeOfMainRecordedData(0), sequence.Action);
            }
            return sequences;
        }
        catch (System.Exception e)
        {
            DebugLogger.Instance.LogException(e);
        }
        return null;
    }


    //List of asset timelines
    List<GameObject> assetTimelines = new List<GameObject>();

    public void RefreshAssetsTimeline(GameObject timelinePanelPrefab)
    {
        //if(Manager.Instance.currAppState == Manager.AppState.RECORDING || Manager.Instance.currAppState == Manager.AppState.PLAYBACK)
        {
            //Delete all existing asset timelines
            foreach (var timeline in assetTimelines)
            {
                Destroy(timeline);
            }

            //Delete all existing collision timeline elements
            for (int i = 2; i < collisionTimelinePanel.GetComponent<RectTransform>().childCount; i++)
            {
                Destroy(collisionTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
            }


            //GenerateGestureSequences(timelinePanel, recordable);
            int recordableCounter = 0;
            assetSequencesLists.Clear();
            collisionSequencesLists.Clear();
            foreach (var recordable in AssetPoseRecorder.Instance.recordableAssets)
            {
                ++recordableCounter;
                GameObject timelinePanel = Instantiate(timelinePanelPrefab, playbackPanelTransform);
                assetTimelines.Add(timelinePanel);
                timelinePanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(timelinePanel.GetComponent<RectTransform>().anchoredPosition.x, timelinePanel.GetComponent<RectTransform>().anchoredPosition.y - recordableCounter * 100);
                timelinePanel.SetActive(true);
                timelinePanel.GetComponent<RectTransform>().GetChild(0).GetComponent<TMPro.TMP_Text>().text = Manager.Instance.CleanString(recordable.name); //Assign asset name

                if (recordable.recordedData.Count > 0)
                {
                    assetSequencesLists.Add(GenerateAssetActionSequences(timelinePanel.GetComponent<RectTransform>(), recordable));
                    collisionSequencesLists.Add(GenerateCollisionSequences(collisionTimelinePanel.GetComponent<RectTransform>(), recordable));
                }   
            }
        }
    }


    public List<GestureSequence> gestureSequences = new List<GestureSequence>();
    public List<List<AssetSequence>> assetSequencesLists = new List<List<AssetSequence>>();
    public List<List<AssetSequence>> collisionSequencesLists = new List<List<AssetSequence>>();

    public List<InputTimelineSequence> inputSequences = new List<InputTimelineSequence>();


    public void CreateStateMachine1()
    {
        DebugLogger.Instance.Log("Creating state machine");

        //Create a state machine
        var stateMachine = CustomStateMachine.Instance;
        //Create an idle state state0
        var idleState = new State();
        idleState.id = "Idle";
        stateMachine.AddState(idleState.id, idleState);
        stateMachine.SetInitialState(idleState.id);
        State prevProcessedState = idleState;
        InputManager.Gesture prevProcessedGesture = InputManager.Gesture.LEFTHANDNONE;

        //Iterate through all gesture sequences
        foreach (var gesture in gestureSequences)
        {
            DebugLogger.Instance.Log("Processing gesture " + InputManager.Instance.GestureToString(gesture.GestureType) + ", StartIndex : " + gesture.StartIndex + ", Length:" + gesture.Length);
            //Create the first state state1
            var state = new State();
            //state.id = InputManager.Instance.GestureToString(gesture.GestureType);
            state.id = "State" + stateMachine.GetSize();
            stateMachine.AddState(state.id, state);
            //Check collision sequence to see if there are any collision with a startindex within the first 10 frames of the startindex of the gesture. If not, stop creating the state and return.
            bool doesGestureHaveCollision = false;
            bool noChangeBetweenOpenAndClose = false;
            bool doesStateHaveAnyActions = false;
            foreach (var collisionSequence in collisionSequencesLists)
            {
                foreach (var collision in collisionSequence)
                {
                    if (collision.StartIndex >= gesture.StartIndex && collision.StartIndex <= gesture.StartIndex + 10)
                    {
                        prevProcessedGesture = gesture.GestureType;
                        doesGestureHaveCollision = true;

                        if(gesture.GestureType == InputManager.Gesture.RIGHTHANDPINCH)
                        {
                            DebugLogger.Instance.Log("Adding transition from " + prevProcessedState.id + " to " + state.id + " for gesture " + InputManager.Instance.GestureToString(gesture.GestureType) + " and collision " + collision.Action);
                            prevProcessedState.AddTransitionTo(state, (frame) => { return (frame.rightHandGesture == InputManager.Gesture.RIGHTHANDPINCH) && frame.IsColliding(collision.CollidingObject1, collision.CollidingObject2); });
                        }
                        else if(gesture.GestureType == InputManager.Gesture.LEFTHANDPINCH)
                        { 
                            DebugLogger.Instance.Log("Adding transition from " + prevProcessedState.id + " to " + state.id + " for gesture " + InputManager.Instance.GestureToString(gesture.GestureType) + " and collision " + collision.Action);
                            prevProcessedState.AddTransitionTo(state, (frame) => { return (frame.leftHandGesture == InputManager.Gesture.LEFTHANDPINCH) && frame.IsColliding(collision.CollidingObject1, collision.CollidingObject2); });
                        }             
                    }
                }
            }

            if(doesGestureHaveCollision == false)
            {
                DebugLogger.Instance.Log("Gesture " + InputManager.Instance.GestureToString(gesture.GestureType) + " does not have any collision (only change in gesture)");
                DebugLogger.Instance.Log("Adding transition from " + prevProcessedState.id + " to " + state.id + " for gesture " + InputManager.Instance.GestureToString(gesture.GestureType));
                //Check if there was a change from open to close or from close to open
                if(prevProcessedGesture == InputManager.Gesture.LEFTHANDPINCH && gesture.GestureType == InputManager.Gesture.LEFTHANDOPEN)                                  
                    prevProcessedState.AddTransitionTo(state, (frame) => { return frame.leftHandGesture == InputManager.Gesture.LEFTHANDOPEN; });                      
                else if (prevProcessedGesture == InputManager.Gesture.RIGHTHANDPINCH && gesture.GestureType == InputManager.Gesture.RIGHTHANDOPEN)
                    prevProcessedState.AddTransitionTo(state, (frame) => { return frame.leftHandGesture == InputManager.Gesture.RIGHTHANDOPEN; }); 
                else if (prevProcessedGesture == InputManager.Gesture.LEFTHANDOPEN && gesture.GestureType == InputManager.Gesture.LEFTHANDPINCH)
                    prevProcessedState.AddTransitionTo(state, (frame) => { return frame.leftHandGesture == InputManager.Gesture.LEFTHANDPINCH; }); 
                else if (prevProcessedGesture == InputManager.Gesture.RIGHTHANDOPEN && gesture.GestureType == InputManager.Gesture.RIGHTHANDPINCH)
                    prevProcessedState.AddTransitionTo(state, (frame) => { return frame.leftHandGesture == InputManager.Gesture.RIGHTHANDPINCH; }); 
                else
                    noChangeBetweenOpenAndClose = true;    
            }

            if(doesGestureHaveCollision == false && noChangeBetweenOpenAndClose == true)
            {                
                stateMachine.DeleteState(state.id);
                DebugLogger.Instance.Log("Gesture " + InputManager.Instance.GestureToString(gesture.GestureType) + " does not have any collision or change between open and close, so deleting state " + state.id);
            }
            else
            {
                //Find actions in asset sequences that are within the first few frames following the gesture sequence start index and add them to the OnEnterActions of state0
                foreach (var assetSequences in assetSequencesLists)
                {
                    foreach (var assetSequence in assetSequences)
                    {
                        if (assetSequence.StartIndex >= gesture.StartIndex && assetSequence.StartIndex <= gesture.StartIndex + 10)
                        {
                            doesStateHaveAnyActions = true;
                            //Add the action to the OnEnterActions of state0
                            state.OnEnterActions += () => assetSequence.ActionDelegate();
                            DebugLogger.Instance.Log("Added action " + assetSequence.Action + " for state " + state.id + " and gesture " + InputManager.Instance.GestureToString(gesture.GestureType) + " OnEnterActions");
                        }
                    }
                }
                //Find actions in asset sequences that are withing the last few frames before the gesture sequence end index and add them to the OnExit function of state0
                foreach (var assetSequences in assetSequencesLists)
                {
                    foreach (var assetSequence in assetSequences)
                    {
                        if (assetSequence.StartIndex >= gesture.StartIndex + gesture.Length - 10 && assetSequence.StartIndex <= gesture.StartIndex + gesture.Length)
                        {
                            doesStateHaveAnyActions = true;
                            //Add the action to the OnExitActions of state0
                            state.OnExitActions += () => assetSequence.ActionDelegate();
                            DebugLogger.Instance.Log("Added action " + assetSequence.Action + " for state " + state.id + " and gesture " + InputManager.Instance.GestureToString(gesture.GestureType) + " OnExitActions");
                        }
                    }
                }

                //If there are no actions in the asset sequences, then delete the state
                if(doesStateHaveAnyActions == false)
                {
                    stateMachine.DeleteState(state.id);
                    DebugLogger.Instance.Log("Gesture " + InputManager.Instance.GestureToString(gesture.GestureType) + " does not have any actions, so deleting state " + state.id);
                }
                else
                {   
                    //Create timeline element for the state             
                    var stateTimelineElement = TimelineUIElement.CreateTimelineElement(stateTimelineElementPrefab, stateTimelinePanel.GetComponent<RectTransform>(), gesture.StartIndex, gesture.Length, GetSizeOfMainRecordedData(0), state.id);
                    state.timelineElement = stateTimelineElement;
                }
                prevProcessedState = state;
            }


        }
    }

    public void PopulateInputSequence()
    {
        //Create the input sequence by extracting the StartIndex and Length from both the gesture sequence list and the collision sequence list 
        foreach (var gesture in gestureSequences)
        {
            InputTimelineSequence inputSequence = new()
            {
                StartIndex = gesture.StartIndex,
                Length = gesture.Length,
                LinkToGestureSequence = gesture
            };
            inputSequences.Add(inputSequence);
        }

        foreach (var collisionSequence in collisionSequencesLists)
        {
            foreach (var collision in collisionSequence)
            {
                InputTimelineSequence inputSequence = new()
                {
                    StartIndex = collision.StartIndex,
                    Length = collision.Length,
                    LinkToCollisionSequence = collision
                };
                inputSequences.Add(inputSequence);
            }
        }

        //Sort the input sequence by StartIndex
        inputSequences.Sort((x, y) => x.StartIndex.CompareTo(y.StartIndex));

        //Print the input sequence
        foreach (var inputSequence in inputSequences)
        {
            DebugLogger.Instance.Log("Input sequence: StartIndex: " + inputSequence.StartIndex + ", Length: " + inputSequence.Length);
        }
        

    }

    State CreateState(int startIndex, int length)
    {
        var StateMachine = CustomStateMachine.Instance;
        var state = new State
        {
            id = "State" + StateMachine.GetSize()
        };
        StateMachine.AddState(state.id, state);
        state.timelineElement = TimelineUIElement.CreateTimelineElement(stateTimelineElementPrefab, stateTimelinePanel.GetComponent<RectTransform>(), 
                                                        startIndex, length, GetSizeOfMainRecordedData(0), state.id);

        return state;
    }
    

    public List<State> CreateStateMachine(List<GestureSequence> gestures, List<AssetSequence> assets, int recordedFramesTotal)
    {
        // Create a list of events (start or end of a sequence)
        var events = new List<(int Index, string Type, GestureSequence? Gesture, AssetSequence? Asset)>();

        foreach (var gesture in gestures)
        {
            events.Add((gesture.StartIndex, "start", gesture, null));
            events.Add((gesture.StartIndex + gesture.Length, "end", gesture, null));
        }

        foreach (var asset in assets)
        {
            events.Add((asset.StartIndex, "start", null, asset));
            if (asset.Length > 1)
            {
                events.Add((asset.StartIndex + asset.Length, "end", null, asset));
            }
            else
            {
                // For AssetSequence with a length of 1, treat the StartIndex as the end index as well
                events.Add((asset.StartIndex, "end", null, asset));
            }
        }

        // Sort the events by their index
        events = events.OrderBy(e => e.Index).ToList();

        var states = new List<State>();
        int lastIndex = 0;

        // Iterate over events to create states
        for (int i = 0; i < events.Count; i++)
        {
            var currentEvent = events[i];

            if (lastIndex != currentEvent.Index)
            {
                var state = CreateState(lastIndex, currentEvent.Index - lastIndex);
                /*   var state = new State
                    {
                        StartIndex = lastIndex,
                        Length = currentEvent.Index - lastIndex
                    };*/

                if (i > 0)
                {
                    var prevEvent = events[i - 1];
                    state.Gesture = prevEvent.Gesture;
                    state.Asset = prevEvent.Asset;
                }

                //states.Add(state);
            }

            lastIndex = currentEvent.Index;
        }

        // Handle the last state if needed
        if (lastIndex < recordedFramesTotal)
        {
            var state = CreateState(lastIndex, recordedFramesTotal - lastIndex);
            /*var state = new State
            {
                StartIndex = lastIndex,
                Length = recordedFramesTotal - lastIndex
            };*/

            if (events.Count > 0)
            {
                var lastEvent = events.Last();
                state.Gesture = lastEvent.Gesture;
                state.Asset = lastEvent.Asset;
            }

            //states.Add(state);
        }

        return states;
    }


    public void CreateStateMachine()
    {
        CreateStateMachine(gestureSequences, assetSequencesLists.SelectMany(x => x).ToList(), recordedFramesTotal);
        /*
        var StateMachine = CustomStateMachine.Instance;

        PopulateInputSequence();

        //Iterate from 0 to recordedFramesTotal
        int currentIndex = 0;
        while(currentIndex < recordedFramesTotal)
        {
            //Check if there is a gesture sequence or collision sequence starting at currentIndex
            InputTimelineSequence inputSequence = inputSequences.Find(x => x.StartIndex == currentIndex);
            if(inputSequence != null)
            {
               //Find any other input sequence that starts between currentIndex and currentIndex + inputSequence.Length or ends between currentIndex and currentIndex + inputSequence.Length
               List<InputTimelineSequence> inputSequencesWithinCurrentInputSequence = inputSequences.FindAll(x => x.StartIndex >= currentIndex && x.StartIndex <= currentIndex + inputSequence.Length || x.StartIndex + x.Length >= currentIndex && x.StartIndex + x.Length <= currentIndex + inputSequence.Length);
            }
        }

        /*
        if(inputSequences.Count > 0)
        {
            //Create state machine timeline element and set the start index to 0 and length to the start index of the first input sequence (most probably gesture)
            if (inputSequences[0].StartIndex != 0)
                CreateState(0, inputSequences[0].StartIndex);
        }

        //Iterate through all input sequences and create states for each of them
        foreach (var input in inputSequences)
        {
            //Check if there is a gap between the current input sequence and the previous input sequence
            if(input != inputSequences[0])
            {
                int gap = input.StartIndex - (inputSequences[inputSequences.IndexOf(input) - 1].StartIndex + inputSequences[inputSequences.IndexOf(input) - 1].Length);
                if(gap > 0)
                {
                    //Create a state for the gap
                    CreateState(inputSequences[inputSequences.IndexOf(input) - 1].StartIndex + inputSequences[inputSequences.IndexOf(input) - 1].Length, gap);
                }
            }
            //Create a state for the input sequence if the length does not exceed the start index of the next input sequence or the end index of the previous input sequence
            if(inputSequences.IndexOf(input) < inputSequences.Count - 1 && inputSequences.IndexOf(input) > 0)
            {
                if(input.StartIndex + input.Length > inputSequences[inputSequences.IndexOf(input) + 1].StartIndex)
                {
                    CreateState(input.StartIndex, inputSequences[inputSequences.IndexOf(input) + 1].StartIndex - input.StartIndex);                    
                }
                else if (input.Length > 1)
                {
                    CreateState(input.StartIndex, input.Length);
                }

                if(input.StartIndex + input.Length < inputSequences[inputSequences.IndexOf(input) - 1].StartIndex + inputSequences[inputSequences.IndexOf(input) - 1].Length)
                {
                    CreateState(input.StartIndex, inputSequences[inputSequences.IndexOf(input) - 1].StartIndex + inputSequences[inputSequences.IndexOf(input) - 1].Length - input.StartIndex);
                }
            }
            else
            {
                if (input.Length > 1)
                    CreateState(input.StartIndex, input.Length);
            }
            //CreateState(input.StartIndex, input.Length);
        }

        //Create a state machine timeline element and set the start index to the start index of the last input sequence and length to the end of the recording
        if(inputSequences.Count > 0)
        {
            if (inputSequences[inputSequences.Count - 1].StartIndex + inputSequences[inputSequences.Count - 1].Length != recordedFramesTotal)
                CreateState(inputSequences[inputSequences.Count - 1].StartIndex + inputSequences[inputSequences.Count - 1].Length, recordedFramesTotal - (inputSequences[inputSequences.Count - 1].StartIndex + inputSequences[inputSequences.Count - 1].Length));
        }*/
        
    }

 
    int frameCount = 0;

    private void FixedUpdate()
    {
        if (isMainRecordingOn)
        {
            ++frameCount;
            foreach (var recordable in objectsToRecord)
            {
                //recordable.Record(Time.time - recordStartTime);
                recordable.Record(frameCount);
            }
        }
        else if (isMainPlaybackOn)
        {

            if(isAutomaticPlayback)
            {
                playbackSlider.value += 1;//Time.deltaTime;
                if(playbackSlider.value >= recordedFramesTotal)
                {
                    playbackSlider.value = 0;
                }
            }

            int currentFrameNum = (int)playbackSlider.value;

            //Find the first two items in handGuideTimePoints that are greater than currentTime
            //var result = handGuideTimePoints.Where(x => x > currentTime).Take(4).ToList();

            foreach (var recordable in objectsToRecord)
            {
                if (recordable.playbackObject != null)
                {
                    recordable.playbackObject.transform.localPosition = recordable.recordedData[currentFrameNum].rootPosition;
                    recordable.playbackObject.transform.localRotation = recordable.recordedData[currentFrameNum].rootRotation * Quaternion.Euler(recordable.rotationCorrection);//Modify the rotation in the recorded data to add 180 degrees to the  axis
                    
                    // check if the HandPlaybackObjectScript is null and call setPoseForAllFingerJoints only if it is not null
                    if (recordable.playbackObject.GetComponent<HandPlaybackObjectScript>() != null)
                    {    
                        recordable.playbackObject.GetComponent<HandPlaybackObjectScript>().SetPoseForAllFingerJoints(recordable.recordedData[currentFrameNum]);
                        recordable.playbackGestureText.text = InputManager.Instance.GestureToString(recordable.recordedData[currentFrameNum].gesture);  
                    }
                    //Focus Square
                    recordable.playbackFocusSquare.transform.position = recordable.recordedData[currentFrameNum].focusSquarePosition;
                    recordable.playbackFocusSquare.transform.rotation = recordable.recordedData[currentFrameNum].focusSquareRotation;

                }
            }
        }

    }
}





