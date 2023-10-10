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

    public GameObject statesTimelinePanel;

    public GameObject mergesStatePanel;

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
        if (obj.name.StartsWith("Sphere"))
        {
            AssetPoseRecorder.Instance.SpawnSphere(obj.transform);
        }
        else if (obj.name.StartsWith("Cube"))
        {
            AssetPoseRecorder.Instance.SpawnCube(obj.transform);
        }
        else if (obj.name.StartsWith("Text"))
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
                Action = currentAction,
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
            if (recordable.recordedData[sequence.StartIndex].ActionDelegate != null)
            {
                sequence.ActionDelegate = recordable.recordedData[sequence.StartIndex].ActionDelegate;
                //DebugLogger.Instance.Log("Action delegate: " + recordable.recordedData[sequence.StartIndex].ActionDelegate.Method.Name + ", Parameters: " + string.Join(", ", recordable.recordedData[sequence.StartIndex].ActionDelegate.Method.GetParameters().Select(x => x.Name)));
            }

            if (sequence.Action.StartsWith("Hide"))
            {
                GameObject hideElement = Instantiate(hideTimelinePanelPrefab, timelinePanel);
                hideElement.SetActive(true);
                hideElement.GetComponent<RectTransform>().anchoredPosition = new Vector2(TimelineUIElement.MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex, GetSizeOfMainRecordedData(0)), hideElement.GetComponent<RectTransform>().anchoredPosition.y);
            }
            else if (sequence.Action.StartsWith("Show"))
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

                if (recordable.recordedData[sequence.StartIndex].CollisionDelegate != null)
                    DebugLogger.Instance.Log("Collision delegate: " + recordable.recordedData[sequence.StartIndex].CollisionDelegate.Method.Name + ", Parameters: " + string.Join(", ", recordable.recordedData[sequence.StartIndex].CollisionDelegate.Method.GetParameters().Select(x => x.Name)));
                if (recordable.recordedData[sequence.StartIndex].CollidedObject != null)
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


    public List<GestureSequence> gestureSequences = new();
    public List<List<AssetSequence>> assetSequencesLists = new();
    public List<List<AssetSequence>> collisionSequencesLists = new();

    public List<InputTimelineSequence> inputSequences = new();

    Dictionary<State, StateTimelineUIElement> StatesInTimeline = new();
    State CreateState(int startIndex, int length)
    {
        var StateMachine = CustomStateMachine.Instance;
        var state = new State
        {
            id = "State" + StateMachine.GetSize()
        };
        StateMachine.AddState(state.id, state);
        //var previousState = StatesInTimeline.LastOrDefault().Key;


        var stateUI = StateTimelineUIElement.CreateStateTimelineElement(stateTimelineElementPrefab, stateTimelinePanel.GetComponent<RectTransform>(),
                                                        startIndex, length, GetSizeOfMainRecordedData(0), state);

        StatesInTimeline.Add(state, stateUI.GetComponent<StateTimelineUIElement>());

        return state;
    }

    State MergeStates(State state1, State state2)
    {
        var state1UIElement = StatesInTimeline[state1];
        var state2UIElement = StatesInTimeline[state2];


        var StateMachine = CustomStateMachine.Instance;
        var newState = new State
        {
            id = "tempState" + StateMachine.GetSize()
        };

        //Get the index of state1 in the StatesInTimeline dictionary
        int state1Index = StatesInTimeline.Keys.ToList().IndexOf(state1);
        //Find the state before state1 in the StatesInTimeline dictionary if the index of state1 is not 0
        State previousState = null;
        if (state1Index != 0)
        {
            previousState = StatesInTimeline.Keys.ToList()[state1Index - 1];
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
                                                        state1UIElement.StartIndex, state1UIElement.Length + state2UIElement.Length, GetSizeOfMainRecordedData(0), newState);

        state1UIElement = stateUI.GetComponent<StateTimelineUIElement>();

        DebugLogger.Instance.Log("Merging states " + state1.id + " and " + state2.id + " to create state " + newState.id);
        DebugLogger.Instance.Log("Size of new state: " + state1UIElement.Length);

        //Remove state1 and state2 from the state machine
        StateMachine.DeleteState(state1.id);
        StateMachine.DeleteState(state2.id);
        //Remove state1 and state2 from the state timeline
        Destroy(StatesInTimeline[state1].gameObject);
        Destroy(StatesInTimeline[state2].gameObject);
        StatesInTimeline.Remove(state1);
        StatesInTimeline.Remove(state2);

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
        StatesInTimeline.Add(newState, stateUI.GetComponent<StateTimelineUIElement>());
        StateMachine.AddState(newState.id, newState);

        StateMachine.SetInitialState(StatesInTimeline.First().Key.id);

        return newState;
    }


    public void ResetStateMachine()
    {
        StatesInTimeline.Clear();

        //Delete all existing states
        CustomStateMachine.Instance.DeleteAllStates();

        //Delete all existing state timeline UI elements
        for (int i = 2; i < stateTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(stateTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }
    }

    public void CreateStates(List<GestureSequence> gestures, List<AssetSequence> collisions, int recordedFramesTotal)
    {
        ResetStateMachine();

        // Create a list of events (start or end of a sequence)
        var events = new List<(int Index, string Type, GestureSequence? Gesture, AssetSequence? Asset)>();

        foreach (var gesture in gestures)
        {
            events.Add((gesture.StartIndex, "start", gesture, null));
            //events.Add((gesture.StartIndex + gesture.Length, "end", gesture, null));
        }

        foreach (var collision in collisions)
        {
            events.Add((collision.StartIndex, "start", null, collision));
            if (collision.Length > 1)
            {
                //events.Add((collision.StartIndex + collision.Length, "end", null, collision));
            }
            else
            {
                // For AssetSequence with a length of 1, treat the StartIndex as the end index as well
                //events.Add((collision.StartIndex, "end", null, collision));
            }
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
                /*   var state = new State
                    {
                        StartIndex = lastIndex,
                        Length = currentEvent.Index - lastIndex
                    };*/

                if (i > 0)
                {
                    var prevEvent = events[i - 1];
                    state.Gesture = prevEvent.Gesture;
                    state.Collision = prevEvent.Asset;
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
                state.Collision = lastEvent.Asset;
            }

            //states.Add(state);
        }

        //return StatesInTimeline;
    }


    public void CreateStateMachine()
    {
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
        List<State> orderedKeys = new List<State>(StatesInTimeline.Keys);

        for (int i = 0; i < orderedKeys.Count - 1; i++)
        {
            State prevState = StatesInTimeline[orderedKeys[i]].state;
            State stateInTimeline = StatesInTimeline[orderedKeys[i + 1]].state;

            DebugLogger.Instance.Log("Adding transition from " + prevState.id + " to " + stateInTimeline.id);
            //prevState.AddTransitionTo(state, (frame) => { return true; });


            //Iterate through gesture sequences and check if GestureType is equal to InputManager.Gesture.RIGHTHANDPINCH or InputManager.Gesture.LEFTHANDPINCH
            foreach (var gesture in gestureSequences)
            {
                if (gesture.StartIndex == StatesInTimeline[stateInTimeline].StartIndex)
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
                    if (collisionSequence.StartIndex == StatesInTimeline[stateInTimeline].StartIndex)
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


        //Add OnEnterActions to state
        foreach (var assetSequences in assetSequencesLists)
        {
            foreach (var assetSequence in assetSequences)
            {
                if (assetSequence.StartIndex == StatesInTimeline[stateInTimeline].StartIndex)
                {
                    //Add the action to the OnEnterActions of state
                    stateInTimeline.OnEnterActions += () => assetSequence.ActionDelegate();
                    DebugLogger.Instance.Log("Added action " + assetSequence.Action + " for state " + stateInTimeline.id + " in OnEnterActions");
                }
            }
        }
        //Add OnExitActions to state
        foreach (var assetSequences in assetSequencesLists)
        {
            foreach (var assetSequence in assetSequences)
            {
                if (assetSequence.StartIndex == StatesInTimeline[stateInTimeline].StartIndex + StatesInTimeline[stateInTimeline].Length - 1)
                {
                    //Add the action to the OnExitActions of state
                    stateInTimeline.OnExitActions += () => assetSequence.ActionDelegate();
                    DebugLogger.Instance.Log("Added action " + assetSequence.Action + " for state " + stateInTimeline.id + " in OnExitActions");
                }
            }
        }


    }


    //Set the initial state of the state machine to be the first state in the StatesInTimeline dictionary
    CustomStateMachine.Instance.SetInitialState(StatesInTimeline.First().Key.id);

        //Iterate through the StateTimelineUIElements in the StatesInTimeline, then add a transition from the previous state to the current state by checking if there are gesture and collision sequences starting within the first 5 frames of the current state
        //foreach(st)

    }

public void PrintDetailsOfStateMachine()
{
    DebugLogger.Instance.ClearVRDebugText();
    DebugLogger.Instance.Log("Printing details of state machine");
    foreach (var state in StatesInTimeline.Keys)
    {
        state.PrintDetailsOfState(VRConsoleEnabled: true);
    }
}

public void MergeToLeftState(StateTimelineUIElement selectedStateUIElement)
{
    var selectedState = StatesInTimeline.FirstOrDefault(x => x.Value == selectedStateUIElement).Key;
    //Find the state to the left of the selected state
    if (StatesInTimeline.Keys.ToList().IndexOf(selectedState) > 0)
    {
        var precedingState = StatesInTimeline.ElementAt(StatesInTimeline.Keys.ToList().IndexOf(selectedState) - 1).Key;
        var precedingStateUIElement = StatesInTimeline.ElementAt(StatesInTimeline.Keys.ToList().IndexOf(selectedState) - 1).Value;
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
    var selectedState = StatesInTimeline.FirstOrDefault(x => x.Value == selectedStateUIElement).Key;
    //Find the state to the right of the selected state
    if (StatesInTimeline.Keys.ToList().IndexOf(selectedState) < StatesInTimeline.Count - 1)
    {
        var succeedingState = StatesInTimeline.ElementAt(StatesInTimeline.Keys.ToList().IndexOf(selectedState) + 1).Key;
        var succeedingStateUIElement = StatesInTimeline.ElementAt(StatesInTimeline.Keys.ToList().IndexOf(selectedState) + 1).Value;
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
        foreach (var recordable in objectsToRecord)
        {
            //recordable.Record(Time.time - recordStartTime);
            recordable.Record(frameCount);
        }
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





