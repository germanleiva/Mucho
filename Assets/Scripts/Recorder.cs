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
    public Head head;
    public Hand leftHand;
    public Hand rightHand;

    
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

    public GameObject statesTimelinePanel;

    public GameObject mergesStatePanel;


    List<GestureSequence> LeftHandGestureSequences = new();
    List<GestureSequence> RightHandGestureSequences = new();

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

        leftHand.ResetData();
        rightHand.ResetData();
        head.ResetData();
        AssetPoseRecorder.Instance.ResetAssetRecordings();
        //RefreshAssetsTimeline(); 

        isMainRecordingOn = true;
        isMainPlaybackOn = false;
        recordStartFrame = 0;//Time.time;
        frameCount = 0;
        RefreshAssetsTimeline(); 
    }



    // Stop recording.
    public void StopRecording()
    {
        rootPlaybackArea.SetActive(true);

        DebugLogger.Instance.Log("Size of recordedData head: " + head.recordedData.Count);
        DebugLogger.Instance.Log("Size of recordedData leftHand: " + leftHand.recordedData.Count);
        DebugLogger.Instance.Log("Size of recordedData rightHand: " + rightHand.recordedData.Count);

        isMainRecordingOn = false;

        //AssetPoseRecorder.Instance.EnableGrabForAllAssets();
        AssetPoseRecorder.Instance.InitializeRecordFramesForAssets();
        RefreshAssetsTimeline();   

        PreparePlayback();
    }

    public void SetAutomaticPlayMode(bool isAutomatic)
    {
        isAutomaticPlayback = isAutomatic;
        Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
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

            head.playbackObject.SetActive(true);
            leftHand.playbackObject.SetActive(true);
            rightHand.playbackObject.SetActive(true);

            if (head.recordedData.Count > 0)
            {
                framesTotal = Mathf.Max(framesTotal, head.recordedData[head.recordedData.Count - 1].frameNumber);
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
            
        }
        catch (System.Exception e)
        {
            DebugLogger.Instance.LogException(e);
        }
    }

    public void AlignPlaybackSlider()
    {
        int currentFrameNum = (int)playbackSlider.value;
        int threshHold = (int)recordedFramesTotal / 50;

        int nearestRightHandGestureSequenceStartIndex = int.MaxValue;
        int nearestRightHandGestureSequenceEndIndex = int.MaxValue;

        foreach (var sequence in RightHandGestureSequences)
        {
            if (Mathf.Abs(currentFrameNum - sequence.StartIndex) < threshHold)
            {
                nearestRightHandGestureSequenceStartIndex = sequence.StartIndex;
            }
            if (Mathf.Abs(currentFrameNum - (sequence.StartIndex + sequence.Length)) < threshHold)
            {
                nearestRightHandGestureSequenceEndIndex = sequence.StartIndex + sequence.Length;
            }
        }

        int nearestLeftHandGestureSequenceStartIndex = int.MaxValue;
        int nearestLeftHandGestureSequenceEndIndex = int.MaxValue;

        foreach (var sequence in LeftHandGestureSequences)
        {
            if (Mathf.Abs(currentFrameNum - sequence.StartIndex) < threshHold)
            {
                nearestLeftHandGestureSequenceStartIndex = sequence.StartIndex;
            }
            if (Mathf.Abs(currentFrameNum - (sequence.StartIndex + sequence.Length)) < threshHold)
            {
                nearestLeftHandGestureSequenceEndIndex = sequence.StartIndex + sequence.Length;
            }
        }

        // Determine the closest start and end indices among right and left hand sequences
        int nearestStartIndex = Mathf.Abs(currentFrameNum - nearestRightHandGestureSequenceStartIndex) < Mathf.Abs(currentFrameNum - nearestLeftHandGestureSequenceStartIndex)
                                ? nearestRightHandGestureSequenceStartIndex
                                : nearestLeftHandGestureSequenceStartIndex;

        int nearestEndIndex = Mathf.Abs(currentFrameNum - nearestRightHandGestureSequenceEndIndex) < Mathf.Abs(currentFrameNum - nearestLeftHandGestureSequenceEndIndex)
                            ? nearestRightHandGestureSequenceEndIndex
                            : nearestLeftHandGestureSequenceEndIndex;

        // Determine if the start or end index is closer to the current frame number
        int nearestIndex = Mathf.Abs(currentFrameNum - nearestStartIndex) < Mathf.Abs(currentFrameNum - nearestEndIndex)
                        ? nearestStartIndex
                        : nearestEndIndex;

        if (nearestIndex == int.MaxValue)
        {
            nearestIndex = currentFrameNum;
        }

        playbackSlider.value = nearestIndex;
        //DebugLogger.Instance.Log("Aligning playback slider to nearest index: " + nearestIndex);
    }



    // Stop playback.
    public void StopPlayback()
    {
        //DebugLogger.Instance.Log("StopPlayback");
        isMainPlaybackOn = false;
    }

    public void SetTestMode()
    {
        //DebugLogger.Instance.Log("Start Testing");
        Manager.Instance.currAppState = Manager.AppState.TEST;
        rootPlaybackArea.SetActive(false);
        //playbackUI.SetActive(false);
    }
    
    public int GetSizeOfMainRecordedData()
    {
        //return objectsToRecord[0].recordedData.Count;
        return head.recordedData.Count;
    }
    public void ExpandRecordedData(int size)
    {
        // For each recordable object, expand the recordedData list to the given size by copying the last element
        /*foreach (var recordable in objectsToRecord)
        {
            RecordableFrame lastElement = recordable.recordedData[recordable.recordedData.Count - 1];
            for (int i = 0; i < size - recordable.recordedData.Count; i++)
            {
                recordable.recordedData.Add(lastElement);
            }
        }
        AssetPoseRecorder.Instance.ExpandRecordFramesForAssets(size);
        AssetPoseRecorder.Instance.DoRecordSizesMatch();*/
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



    public List<GestureSequence> GenerateGestureSequences(RectTransform timelinePanel, Hand hand)
    {
        DebugLogger.Instance.Log("Generating gesture sequences for " + hand.name);
        List<InputManager.Gesture> gestures = hand.recordedData.Select(x => x.gesture).ToList();
        List<GestureSequence> GestureSequences = GetContinuousGestureSequences(gestures);
        foreach (GestureSequence sequence in GestureSequences)
        {
            //DebugLogger.Instance.Log("Sequence name: " + InputManager.Instance.GestureToString(sequence.GestureType) + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
            //DebugLogger.Instance.Log("Start x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex) + ", End x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex + sequence.Length));
            TimelineUIElement.CreateTimelineElement(handTimelineElementPrefab, timelinePanel, sequence.StartIndex, sequence.Length, GetSizeOfMainRecordedData(), InputManager.Instance.GestureToString(sequence.GestureType));
        }
        return GestureSequences;
    }
    

    public List<AssetAction> GetContinuousChangeSequences(List<string> actions)
    {
        List<AssetAction> sequences = new List<AssetAction>();

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
                    sequences.Add(new AssetAction
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
                sequences.Add(new AssetAction
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
            sequences.Add(new AssetAction
            {
                StartIndex = startIndex,
                Length = actions.Count - startIndex,
                Action = currentAction,
            });
        }

        return sequences;
    }

    public List<AssetAction> GenerateAssetActionSequences(RectTransform timelinePanel, Recordable recordable)
    {
        DebugLogger.Instance.Log("Generating asset action sequences for " + recordable.name);

        List<string> changes = recordable.recordedData.Select(x => x.Action).ToList();
        List<AssetAction> sequences = GetContinuousChangeSequences(changes);
        foreach (AssetAction sequence in sequences)
        {
            //DebugLogger.Instance.Log("Sequence name: " + sequence.Action + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
            //DebugLogger.Instance.Log("Start x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex) + ", End x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex + sequence.Length));
            if (recordable.recordedData[sequence.StartIndex].ActionDelegate != null)
            {
                sequence.ActionDelegate = recordable.recordedData[sequence.StartIndex].ActionDelegate;
                DebugLogger.Instance.Log("Action delegate found: " + recordable.recordedData[sequence.StartIndex].ActionDelegate.Method.Name + ", Parameters: " + string.Join(", ", recordable.recordedData[sequence.StartIndex].ActionDelegate.Method.GetParameters().Select(x => x.Name)));
            }

            if (sequence.Action.StartsWith("Hide"))
            {
                GameObject hideElement = Instantiate(hideTimelinePanelPrefab, timelinePanel);
                hideElement.SetActive(true);
                hideElement.GetComponent<RectTransform>().anchoredPosition = new Vector2(TimelineUIElement.MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex, GetSizeOfMainRecordedData()), hideElement.GetComponent<RectTransform>().anchoredPosition.y);
            }
            else if (sequence.Action.StartsWith("Show"))
            {
                GameObject showElement = Instantiate(showTimelinePanelPrefab, timelinePanel);
                showElement.SetActive(true);
                showElement.GetComponent<RectTransform>().anchoredPosition = new Vector2(TimelineUIElement.MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex, GetSizeOfMainRecordedData()), showElement.GetComponent<RectTransform>().anchoredPosition.y);
            }
            else //Other types of events - physics, attach etc
            {
                if(recordable.recordedData[sequence.StartIndex].Action != "Unfollow()" && recordable.recordedData[sequence.StartIndex].Action != "ResetPhysics()")
                    TimelineUIElement.CreateTimelineElement(assetTimelineElementPrefab, timelinePanel, sequence.StartIndex, sequence.Length, GetSizeOfMainRecordedData(), sequence.Action);
            }
        }
        return sequences;
    }

    public List<AssetAction> GenerateCollisionSequences(RectTransform collisionTimelinePanelTransform, Recordable recordable) //Strong assumption that all sources of action come from collision
    {
        DebugLogger.Instance.Log("Generating collision sequences for " + recordable.name);
        try
        {
            List<string> sourcesOfChanges = recordable.recordedData.Select(x => x.Collision).ToList();
            List<AssetAction> sequences = GetContinuousChangeSequences(sourcesOfChanges);
            foreach (AssetAction sequence in sequences)
            {
                //DebugLogger.Instance.Log("Collision sequence name: " + sequence.Action + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
                //DebugLogger.Instance.Log("Start x: " + MapIndexToTimelinePosition(collisionTimelinePanelTransform, sequence.StartIndex) + ", End x: " + MapIndexToTimelinePosition(collisionTimelinePanelTransform, sequence.StartIndex + sequence.Length));

                if (recordable.recordedData[sequence.StartIndex].CollisionDelegate != null)
                    DebugLogger.Instance.Log("Collision delegate found: " + recordable.recordedData[sequence.StartIndex].CollisionDelegate.Method.Name + ", Parameters: " + string.Join(", ", recordable.recordedData[sequence.StartIndex].CollisionDelegate.Method.GetParameters().Select(x => x.Name)));
                if (recordable.recordedData[sequence.StartIndex].CollidedObject != null)
                {
                    DebugLogger.Instance.Log("Collided object: " + recordable.recordedData[sequence.StartIndex].CollidedObject.name);
                    sequence.CollidingObject1 = recordable.gameObject;
                    sequence.CollidingObject2 = recordable.recordedData[sequence.StartIndex].CollidedObject;
                }

                TimelineUIElement.CreateTimelineElement(collisionTimelineElementPrefab, collisionTimelinePanelTransform, sequence.StartIndex, sequence.Length, GetSizeOfMainRecordedData(), sequence.Action);
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

    public void RefreshAssetsTimeline()
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

        //Delete all existing left and right hand timeline elements
        for (int i = 2; i < leftHandTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(leftHandTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }
        for (int i = 2; i < rightHandTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(rightHandTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }

        //Generate gesture sequences for left and right hand
        LeftHandGestureSequences = GenerateGestureSequences(leftHandTimelinePanel, leftHand);
        RightHandGestureSequences = GenerateGestureSequences(rightHandTimelinePanel, rightHand);
        gestureSequences = LeftHandGestureSequences.Concat(RightHandGestureSequences).ToList();


        //GenerateGestureSequences(timelinePanel, recordable);
        int recordableCounter = 0;
        assetSequencesLists.Clear();
        collisionSequencesLists.Clear();
        foreach (var recordable in AssetPoseRecorder.Instance.recordableAssets)
        {
            ++recordableCounter;
            GameObject timelinePanel = Instantiate(assetTimelinePanelPrefab, playbackPanelTransform);
            assetTimelines.Add(timelinePanel);
            timelinePanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(timelinePanel.GetComponent<RectTransform>().anchoredPosition.x, timelinePanel.GetComponent<RectTransform>().anchoredPosition.y - recordableCounter * 100);
            timelinePanel.SetActive(true);
            timelinePanel.GetComponent<RectTransform>().GetChild(0).GetComponent<TMPro.TMP_Text>().text = Manager.Instance.CleanAssetName(recordable.name); //Assign asset name

            if (recordable.recordedData.Count > 0)
            {
                assetSequencesLists.Add(GenerateAssetActionSequences(timelinePanel.GetComponent<RectTransform>(), recordable));
                collisionSequencesLists.Add(GenerateCollisionSequences(collisionTimelinePanel.GetComponent<RectTransform>(), recordable));
            }
        }
        //CreateStateMachine();
       
    }


    public List<GestureSequence> gestureSequences = new();
    public List<List<AssetAction>> assetSequencesLists = new();
    public List<List<AssetAction>> collisionSequencesLists = new();

    Dictionary<State, StateTimelineUIElement> StatesDict = new();
    State CreateState(int startIndex, int length)
    {
        var StateMachine = CustomStateMachine.Instance;
        var state = new State
        {
            id = "State" + StateMachine.GetSize()
        };
        StateMachine.AddState(state.id, state);

        var stateUI = StateTimelineUIElement.CreateStateTimelineElement(stateTimelineElementPrefab, stateTimelinePanel.GetComponent<RectTransform>(),
                                                        startIndex, length, GetSizeOfMainRecordedData(), state);
        
        state.timelineElement = stateUI;

        StatesDict.Add(state, stateUI.GetComponent<StateTimelineUIElement>());

        return state;
    }

    State MergeStates(State state1, State state2)
    {
        var state1UIElement = StatesDict[state1];
        var state2UIElement = StatesDict[state2];


        var StateMachine = CustomStateMachine.Instance;
        var newState = new State
        {
            id = "tempState" + StateMachine.GetSize()
        };

        //Get the index of state1 in the StatesInTimeline dictionary
        int state1Index = StatesDict.Keys.ToList().IndexOf(state1);
        //Find the state before state1 in the StatesInTimeline dictionary if the index of state1 is not 0
        State previousState = null;
        if (state1Index != 0)
        {
            previousState = StatesDict.Keys.ToList()[state1Index - 1];
            //Add a transition from the previous state to the new state
            previousState.ModifyTransitionTo(newState);
            //previousState.transitions.Clear();  
        }

        //Copy the OnEnterActions of state1 to the OnEnterActions of the new state
        newState.OnEnterActions += state1.OnEnterActions;
        //Copy the OnEnterActions of state2 to the OnExitActions of the new state
        //newState.OnExitActions += state2.OnEnterActions;
        //Copy the OnExitActions of state1 to the OnExitActions of the new state
        newState.OnExitActions += state1.OnExitActions;
        //Copy the OnExitActions of state2 to the OnExitActions of the new state
        newState.OnExitActions += state2.OnExitActions;
        newState.CopyTransitionFromState(state2);

        //Assign state.id as "State" plus the digits present in state1.id and state2.id
        string state1ID = state1.id;
        string state2ID = state2.id;
        string stateID = "State";
        foreach (char c in state1ID)
        {
            if (char.IsDigit(c))
            {
                stateID += c;
            }
        }
        foreach (char c in state2ID)
        {
            if (char.IsDigit(c))
            {
                stateID += c;
            }
        }
        newState.id = stateID;

        var stateUI = StateTimelineUIElement.CreateStateTimelineElement(stateTimelineElementPrefab, stateTimelinePanel.GetComponent<RectTransform>(),
                                                        state1UIElement.StartIndex, state1UIElement.Length + state2UIElement.Length, GetSizeOfMainRecordedData(), newState);

        state1UIElement = stateUI.GetComponent<StateTimelineUIElement>();

        DebugLogger.Instance.Log("Merging states " + state1.id + " and " + state2.id + " to create state " + newState.id);
        DebugLogger.Instance.Log("Size of new state: " + state1UIElement.Length);

        //Remove state1 and state2 from the state machine
        StateMachine.DeleteState(state1.id);
        StateMachine.DeleteState(state2.id);
        //Remove state1 and state2 from the state timeline
        Destroy(StatesDict[state1].gameObject);
        Destroy(StatesDict[state2].gameObject);
        StatesDict.Remove(state1);
        StatesDict.Remove(state2);

        //Insert the new state into the location of state1

        //StatesInTimeline.Add(state, stateUI.GetComponent<StateTimelineUIElement>());

        //Iterate through the StatesInTimeline dictionary keys and change the key to be "State" + index of the key
        /*Dictionary<State, StateTimelineUIElement> newStatesInTimeline = new Dictionary<State, StateTimelineUIElement>();
        foreach (var stateInTimeline in StatesInTimeline)
        {
            State newState = new State();
            newState.id = "State" + newStatesInTimeline.Count;
            newStatesInTimeline.Add(newState, stateInTimeline.Value);
        }
        StatesInTimeline = newStatesInTimeline;*/

        //state.id = "State" + StatesInTimeline.Count;
        StatesDict.Add(newState, stateUI.GetComponent<StateTimelineUIElement>());
        StateMachine.AddState(newState.id, newState);

        StateMachine.SetInitialState(StatesDict.First().Key.id);

        return newState;
    }


    public void ResetStateMachine()
    {
        StatesDict.Clear();

        //Delete all existing states
        CustomStateMachine.Instance.DeleteAllStates();

        //Delete all existing state timeline UI elements
        for (int i = 2; i < stateTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(stateTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }
    }

    public void CreateStates(List<GestureSequence> gestures, List<AssetAction> collisions, int recordedFramesTotal)
    {
        ResetStateMachine();

        // Create a list of events (start or end of a sequence)
        var events = new List<(int Index, string Type, GestureSequence Gesture, AssetAction Asset)>();

        foreach (var gesture in gestures)
        {
            events.Add((gesture.StartIndex, "start", gesture, null));
        }

        foreach (var collision in collisions)
        {
            events.Add((collision.StartIndex, "start", null, collision));
        }

        // Sort the events by their index
        events = events.OrderBy(e => e.Index).ToList();

        int lastIndex = 0;

        // Iterate over events to create states
        for (int i = 0; i < events.Count; i++)
        {
            var currentEvent = events[i];

            if (lastIndex != currentEvent.Index)
            {
                var state = CreateState(lastIndex, currentEvent.Index - lastIndex);

                if (i > 0)
                {
                    var prevEvent = events[i - 1];
                    state.Gesture = prevEvent.Gesture;
                    state.Collision = prevEvent.Asset;
                }
            }

            lastIndex = currentEvent.Index;
        }

        // Last state
        if (lastIndex < recordedFramesTotal)
        {
            var state = CreateState(lastIndex, recordedFramesTotal - lastIndex);

            if (events.Count > 0)
            {
                var lastEvent = events.Last();
                state.Gesture = lastEvent.Gesture;
                state.Collision = lastEvent.Asset;
            }
        }
    }


    public void CreateStateMachine()
    {
        RefreshAssetsTimeline(); 
        CreateStates(gestureSequences, collisionSequencesLists.SelectMany(x => x).ToList(), recordedFramesTotal);

        //Print all states and the gesture and asset sequences they contain


        AddEventsAndTransitionsToStates();
    }

    public void AddEventsAndTransitionsToStates()
    {
        DebugLogger.Instance.Log("Adding events and transitions to states");
        //var firstState = StatesInTimeline.First().Key;
        //Add events to states
        //
        List<State> orderedKeys = new List<State>(StatesDict.Keys);

        for (int i = 0; i < orderedKeys.Count - 1; i++)
        {
            State prevState = StatesDict[orderedKeys[i]].state;
            State stateInTimeline = StatesDict[orderedKeys[i + 1]].state;

            DebugLogger.Instance.Log("Adding transition from " + prevState.id + " to " + stateInTimeline.id);
            //prevState.AddTransitionTo(state, (frame) => { return true; });


            //Iterate through gesture sequences and check if GestureType is equal to InputManager.Gesture.RIGHTHANDPINCH or InputManager.Gesture.LEFTHANDPINCH
            foreach (var gesture in gestureSequences)
            {
                if (gesture.StartIndex == StatesDict[stateInTimeline].StartIndex)
                {
                    //Add the gesture to the state
                    stateInTimeline.Gesture = gesture;
                    DebugLogger.Instance.Log("Found gesture " + InputManager.Instance.GestureToString(gesture.GestureType) + " at the beginning of state " + stateInTimeline.id);
                }
            }

            //Iterate through collision sequences and check if the start index is within the first 5 frames of the state
            foreach (var collisionSequences in collisionSequencesLists)
            {
                foreach (var collisionSequence in collisionSequences)
                {
                    if (collisionSequence.StartIndex == StatesDict[stateInTimeline].StartIndex)
                    {
                        //Add the collision to the state
                        stateInTimeline.Collision = collisionSequence;
                        DebugLogger.Instance.Log("Added collision " + collisionSequence.Action + " to state " + stateInTimeline.id);
                    }
                }
            }

            if (stateInTimeline.Gesture == null && stateInTimeline.Collision == null)
            {
                DebugLogger.Instance.Log("State " + stateInTimeline.id + " does not have any gesture or collision");
            }
            else if (stateInTimeline.Gesture != null && stateInTimeline.Collision == null)
            {
                //DebugLogger.Instance.Log("State " + state.id + " has gesture " + InputManager.Instance.GestureToString(state.Gesture.GestureType));
                if (stateInTimeline.Gesture.GestureType == InputManager.Gesture.RIGHTHANDPINCH)
                {
                    DebugLogger.Instance.Log("Adding transition from " + prevState.id + " to " + stateInTimeline.id + ": frame.rightHandGesture == InputManager.Gesture.RIGHTHANDPINCH; ");

                    prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.rightHandGesture == InputManager.Gesture.RIGHTHANDPINCH; }, "" + prevState.id + "->" + stateInTimeline.id + ": frame.rightHandGesture == InputManager.Gesture.RIGHTHANDPINCH; ");
                }
                else if (stateInTimeline.Gesture.GestureType == InputManager.Gesture.RIGHTHANDOPEN)
                {
                    DebugLogger.Instance.Log("Adding transition from " + prevState.id + " to " + stateInTimeline.id + ": frame.rightHandGesture == InputManager.Gesture.RIGHTHANDOPEN; ");

                    prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.rightHandGesture == InputManager.Gesture.RIGHTHANDOPEN; }, "" + prevState.id + "->" + stateInTimeline.id + ": frame.rightHandGesture == InputManager.Gesture.RIGHTHANDOPEN; ");
                }
                else if (stateInTimeline.Gesture.GestureType == InputManager.Gesture.LEFTHANDPINCH || stateInTimeline.Gesture.GestureType == InputManager.Gesture.LEFTHANDOPEN)
                {
                    DebugLogger.Instance.Log("Adding transition from " + prevState.id + " to " + stateInTimeline.id + ": frame.lefthandgesture == InputManager.Gesture.LEFTHANDOPEN/LEFTHANDPINCH; ");

                    prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.leftHandGesture == stateInTimeline.Gesture.GestureType; }, "" + prevState.id + "->" + stateInTimeline.id + ": frame.lefthandgesture == state.Gesture.GestureType; ");
                }

            }
            else if (stateInTimeline.Gesture == null && stateInTimeline.Collision != null)
            {
                DebugLogger.Instance.Log("Adding transition from " + prevState.id + " to " + stateInTimeline.id + ": frame.IsColliding(" + stateInTimeline.Collision.CollidingObject1.name + ", " + stateInTimeline.Collision.CollidingObject2.name + "); ");
                prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.IsColliding(stateInTimeline.Collision.CollidingObject1, stateInTimeline.Collision.CollidingObject2); }, "" + prevState.id + "->" + stateInTimeline.id + ": frame.IsColliding(" + stateInTimeline.Collision.CollidingObject1.name + ", " + stateInTimeline.Collision.CollidingObject2.name + "); ");
            }
            else
            {
                //DebugLogger.Instance.Log("State " + state.id + " has gesture " + InputManager.Instance.GestureToString(state.Gesture.GestureType) + " and collision " + state.Collision.Action);
                if (stateInTimeline.Gesture.GestureType == InputManager.Gesture.RIGHTHANDPINCH)
                {
                    DebugLogger.Instance.Log("Adding transition from " + prevState.id + " to " + stateInTimeline.id + ": frame.rightHandGesture == InputManager.Gesture.RIGHTHANDPINCH && frame.IsColliding(" + stateInTimeline.Collision.CollidingObject1.name + ", " + stateInTimeline.Collision.CollidingObject2.name + "); ");
                    prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.rightHandGesture == stateInTimeline.Gesture.GestureType && frame.IsColliding(stateInTimeline.Collision.CollidingObject1, stateInTimeline.Collision.CollidingObject2); }, "" + prevState.id + "->" + stateInTimeline.id + ": frame.rightHandGesture == InputManager.Gesture.RIGHTHANDPINCH && frame.IsColliding(" + stateInTimeline.Collision.CollidingObject1.name + ", " + stateInTimeline.Collision.CollidingObject2.name + "); ");
                }
                else if (stateInTimeline.Gesture.GestureType == InputManager.Gesture.LEFTHANDPINCH)
                {
                    DebugLogger.Instance.Log("Adding transition from " + prevState.id + " to " + stateInTimeline.id + ": frame.lefthandgesture == InputManager.Gesture.LEFTHANDPINCH && frame.IsColliding(" + stateInTimeline.Collision.CollidingObject1.name + ", " + stateInTimeline.Collision.CollidingObject2.name + "); ");
                    prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.leftHandGesture == stateInTimeline.Gesture.GestureType && frame.IsColliding(stateInTimeline.Collision.CollidingObject1, stateInTimeline.Collision.CollidingObject2); }, "" + prevState.id + "->" + stateInTimeline.id + ": frame.lefthandgesture == InputManager.Gesture.LEFTHANDPINCH && frame.IsColliding(" + stateInTimeline.Collision.CollidingObject1.name + ", " + stateInTimeline.Collision.CollidingObject2.name + "); ");
                }
            }

            foreach (var assetSequences in assetSequencesLists)
            {
                foreach (var assetSequence in assetSequences)
                {
                    int distanceToStateStart = assetSequence.StartIndex - StatesDict[stateInTimeline].StartIndex;
                    int distanceToStateEnd = StatesDict[stateInTimeline].StartIndex + StatesDict[stateInTimeline].Length - assetSequence.StartIndex - 1;
                    if (distanceToStateStart >= 0 && distanceToStateEnd >= 0)
                    {
                        if(distanceToStateStart < distanceToStateEnd)
                        {
                            DebugLogger.Instance.Log("Adding action " + assetSequence.Action + " for state " + stateInTimeline.id + " in OnEnterActions");
                            stateInTimeline.OnEnterActions += () => assetSequence.ActionDelegate();
                        }
                        else
                        {
                            DebugLogger.Instance.Log("Adding action " + assetSequence.Action + " for state " + stateInTimeline.id + " in OnExitActions");
                            stateInTimeline.OnExitActions += () => assetSequence.ActionDelegate();
                        }
                    }

                }
            }
    }


    //Set the initial state of the state machine to be the first state in the StatesInTimeline dictionary
    CustomStateMachine.Instance.SetInitialState(StatesDict.First().Key.id);



    }

public void PrintDetailsOfStateMachine()
{
    DebugLogger.Instance.ClearVRDebugText();
    DebugLogger.Instance.Log("Printing details of state machine");
    foreach (var state in StatesDict.Keys)
    {
        state.PrintDetailsOfState(VRConsoleEnabled: true);
    }
}

public void MergeToLeftState(StateTimelineUIElement selectedStateUIElement)
{
    var selectedState = StatesDict.FirstOrDefault(x => x.Value == selectedStateUIElement).Key;
    //Find the state to the left of the selected state
    if (StatesDict.Keys.ToList().IndexOf(selectedState) > 0)
    {
        var precedingState = StatesDict.ElementAt(StatesDict.Keys.ToList().IndexOf(selectedState) - 1).Key;
        var precedingStateUIElement = StatesDict.ElementAt(StatesDict.Keys.ToList().IndexOf(selectedState) - 1).Value;
        DebugLogger.Instance.Log("Merging state " + selectedState.id + " with state " + precedingState.id);
        MergeStates(precedingState, selectedState);
        //precedingStateUIElement.MergeWithStateUIElement(selectedStateUIElement);
    }
    else
    {
        DebugLogger.Instance.Log("Cannot merge state " + selectedState.id + " with state to the left because it is the first state");
    }

    //var precedingKey = StatesInTimeline.ElementAt(StatesInTimeline.Keys.ToList().IndexOf(selectedState) - 1);
}

public void MergeToRightState(StateTimelineUIElement selectedStateUIElement)
{
    var selectedState = StatesDict.FirstOrDefault(x => x.Value == selectedStateUIElement).Key;
    //Find the state to the right of the selected state
    if (StatesDict.Keys.ToList().IndexOf(selectedState) < StatesDict.Count - 1)
    {
        var succeedingState = StatesDict.ElementAt(StatesDict.Keys.ToList().IndexOf(selectedState) + 1).Key;
        var succeedingStateUIElement = StatesDict.ElementAt(StatesDict.Keys.ToList().IndexOf(selectedState) + 1).Value;
        DebugLogger.Instance.Log("Merging state " + selectedState.id + " with state " + succeedingState.id);
        MergeStates(selectedState, succeedingState);
        //selectedStateUIElement.MergeWithStateUIElement(succeedingStateUIElement);
    }
    else
    {
        DebugLogger.Instance.Log("Cannot merge state " + selectedState.id + " with state to the right because it is the last state");
    }
}

int frameCount = 0;

private void FixedUpdate()
{
    if (isMainRecordingOn)
    {
        ++frameCount;

        head.Record(frameCount);
        leftHand.Record(frameCount);
        rightHand.Record(frameCount);

    }
    else if (isMainPlaybackOn)
    {

        if (isAutomaticPlayback)
        {
            playbackSlider.value += 1;//Time.deltaTime;
            if (playbackSlider.value >= recordedFramesTotal)
            {
                playbackSlider.value = 0;
            }
        }

        int currentFrameNum = (int)playbackSlider.value;


        if (head.playbackObject != null)
        {
            head.playbackObject.transform.localPosition = head.recordedData[currentFrameNum].rootPosition;
            head.playbackObject.transform.localRotation = head.recordedData[currentFrameNum].rootRotation;

            head.playbackFocusSquare.transform.position = head.recordedData[currentFrameNum].focusSquarePosition;
            head.playbackFocusSquare.transform.rotation = head.recordedData[currentFrameNum].focusSquareRotation;
        }

        if (leftHand.playbackObject != null)
        {
            leftHand.playbackObject.transform.localPosition = leftHand.recordedData[currentFrameNum].rootPosition;
            leftHand.playbackObject.transform.localRotation = leftHand.recordedData[currentFrameNum].rootRotation * Quaternion.Euler(leftHand.rotationCorrection);//Modify the rotation in the recorded data to add 180 degrees to the  axis

            if (leftHand.playbackObject.GetComponent<HandPlaybackObjectScript>() != null)
            {
                leftHand.playbackObject.GetComponent<HandPlaybackObjectScript>().SetPoseForAllFingerJoints(leftHand.recordedData[currentFrameNum]);
                leftHand.playbackGestureText.text = InputManager.Instance.GestureToString(leftHand.recordedData[currentFrameNum].gesture);
            }

            leftHand.playbackFocusSquare.transform.position = leftHand.recordedData[currentFrameNum].focusSquarePosition;
            leftHand.playbackFocusSquare.transform.rotation = leftHand.recordedData[currentFrameNum].focusSquareRotation;
        }

        if (rightHand.playbackObject != null)
        {
            rightHand.playbackObject.transform.localPosition = rightHand.recordedData[currentFrameNum].rootPosition;
            rightHand.playbackObject.transform.localRotation = rightHand.recordedData[currentFrameNum].rootRotation * Quaternion.Euler(rightHand.rotationCorrection);

            if (rightHand.playbackObject.GetComponent<HandPlaybackObjectScript>() != null)
            {
                rightHand.playbackObject.GetComponent<HandPlaybackObjectScript>().SetPoseForAllFingerJoints(rightHand.recordedData[currentFrameNum]);
                rightHand.playbackGestureText.text = InputManager.Instance.GestureToString(rightHand.recordedData[currentFrameNum].gesture);
            }

            rightHand.playbackFocusSquare.transform.position = rightHand.recordedData[currentFrameNum].focusSquarePosition;
            rightHand.playbackFocusSquare.transform.rotation = rightHand.recordedData[currentFrameNum].focusSquareRotation;
        }

    }

}
}





