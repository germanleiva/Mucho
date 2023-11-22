using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Recorder : MonoBehaviour
{
    public static Recorder Instance { get; private set; }

    [Header("Record & Playback")]
    public GameObject rootPlaybackArea;
    public Slider playbackSlider;
    public GameObject playButton;
    public Head head;
    public Hand leftHand;
    public Hand rightHand;

    
    //public bool isMainRecordingOn = false;
    //public bool isMainPlaybackOn = false;
    public int recordStartFrame;
    //public int recordedFramesTotal;

    public bool isAutomaticPlayback = false;

    readonly List<float> handGuideTimePoints = new();
    [Header("Timeline UI")]
    [SerializeField]
    RectTransform stateTimelinePanel;
    [SerializeField]
    RectTransform stateGraphPanel;
    [SerializeField]
    RectTransform rightHandTimelinePanel;
    [SerializeField]
    RectTransform leftHandTimelinePanel;
    [SerializeField]
    RectTransform voiceTimelinePanel;
    [SerializeField]
    GameObject stateTimelineElementPrefab;
    [SerializeField]
    GameObject stateGraphElementPrefab;    
    [SerializeField]
    GameObject handTimelineElementPrefab;
     [SerializeField]
    GameObject voiceCommandTimelineElementPrefab;
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


    public List<GestureSequence> LeftHandGestureSequences = new();
    public List<GestureSequence> RightHandGestureSequences = new();

    public List<VoiceSequence> VoiceCommandSequences = new();

    public List<GestureSequence> AllGestureSequences = new();
    public List<List<AssetAction>> assetSequencesLists = new();
    public List<List<AssetAction>> collisionSequencesLists = new();

    public List<Recordable> assetsInScene = new();

    public GameObject firstExampleButtonObj;
    public Button addExampleButton;

    public Example currentActiveExample;

    public List<Example> examples = new();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        SetPlaybackObjectsVisibility(false);
        AddExample();
        /*Example example = new(firstExampleButton);
        firstExampleButton.onClick.AddListener(() => SelectExample(example));
        examples.Add(example);
        currentActiveExample = example;*/
    }

    public void HighlightAllFollowTargets()
    {

    }

    public void HighlightSelectedFollowTarget()
    {

    }


    public void SelectExample(Example example)
    {
        DebugLogger.Instance.Log("Selecting example " + example.exampleId);
        currentActiveExample = example;
        foreach (var ex in examples)
        {
            ex.button.GetComponent<Image>().color = Color.white;
        }
        currentActiveExample.button.GetComponent<Image>().color = Color.green;
        //PreparePlayback();
        RefreshTimelineAndStates();
    }

    public void AddExample()
    {        
        var obj = Instantiate(firstExampleButtonObj, firstExampleButtonObj.transform.parent);
        obj.SetActive(true);
        Example example = new(obj.GetComponent<Button>());
        examples.Add(example);
        //example.button.GetComponentInChildren<TMPro.TMP_Text>().text = (examples.Count + 1).ToString();
        //Place the button 20 units below the previous button
        example.button.GetComponent<RectTransform>().anchoredPosition = new Vector2(example.button.GetComponent<RectTransform>().anchoredPosition.x, example.button.GetComponent<RectTransform>().anchoredPosition.y - examples.Count * 20);
        addExampleButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(addExampleButton.GetComponent<RectTransform>().anchoredPosition.x, addExampleButton.GetComponent<RectTransform>().anchoredPosition.y - 20);
        example.button.onClick.AddListener(() => SelectExample(example));       
        

        if (examples.Count >= 2)
        {
            //Copy data from previous example
            example.CopyExampleDataFrom(examples[examples.Count - 2]);
            //PreparePlayback();
        }

        SelectExample(example);        
    }

    // Start recording.
    public void StartRecording()
    {
        if(examples.Count == 0)
        {
            return;
        }
        Manager.Instance.currAppState = Manager.AppState.RECORDING;
        rootPlaybackArea.SetActive(false);
        DebugLogger.Instance.Log("StartRecording");

        currentActiveExample.ResetData();
        //rightHand.ResetData();
        //head.ResetData();
        //AssetManager.Instance.ResetAssetRecordings();
        //RefreshAssetsTimeline(); 

        //isMainRecordingOn = true;
        //isMainPlaybackOn = false;
        recordStartFrame = 0;//Time.time;
        frameCount = 0;
        RefreshTimelineAndStates(); 
    }



    // Stop recording.
    public void StopRecording()
    {
        rootPlaybackArea.SetActive(true);

        DebugLogger.Instance.Log("Size of recordedData head: " + currentActiveExample.headData.Count);
        DebugLogger.Instance.Log("Size of recordedData leftHand: " + currentActiveExample.leftHandData.Count);
        DebugLogger.Instance.Log("Size of recordedData rightHand: " + currentActiveExample.rightHandData.Count);

        foreach(var asset in currentActiveExample.assetDataDict.Keys)
        {
            DebugLogger.Instance.Log("Size of recordedData asset: " + asset.name + ", " + currentActiveExample.assetDataDict[asset].Count);
        }
        
        //TODO: If sizes do no match raise an error
        

        //isMainRecordingOn = false;
        Manager.Instance.currAppState = Manager.AppState.PLAYBACK;

        //AssetPoseRecorder.Instance.EnableGrabForAllAssets();
        //AssetManager.Instance.InitializeRecordFramesForAssets();
        AssetManager.Instance.DoRecordSizesMatch();
        RefreshTimelineAndStates();   

        PreparePlayback();
    }

    public void SetAutomaticPlayMode(bool isAutomatic)
    {
        isAutomaticPlayback = isAutomatic;
        AssetManager.Instance.HideMiscObjs();
        Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
    }

    // Start playback.
    public void PreparePlayback()
    {
        try
        {
            if (Manager.Instance.currAppState == Manager.AppState.RECORDING) return; // Don't allow playback while recording.
            
            DebugLogger.Instance.Log("StartPlayback");
            // Determine the duration of the recording.
            //int framesTotal = 0;

            head.playbackObject.SetActive(true);
            leftHand.playbackObject.SetActive(true);
            rightHand.playbackObject.SetActive(true);

            /*if (currentActiveExample.headData.Count > 0)
            {
                framesTotal = Mathf.Max(framesTotal, currentActiveExample.headData[currentActiveExample.headData.Count - 1].frameNumber);
            }*/

            playButton.SetActive(true);

            // Set up the slider.
            playbackSlider.minValue = 0;
            playbackSlider.maxValue = GetSizeOfMainRecordedData(); //framesTotal;
            playbackSlider.value = 0;
            //recordedFramesTotal = framesTotal;
            //DebugLogger.Instance.Log("Duration of recording: " + recordedFramesTotal);

            //isMainPlaybackOn = true;
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
        int threshHold = (int)GetSizeOfMainRecordedData() / 50;

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
        //isMainPlaybackOn = false;
    }

    public void SetTestMode()
    {
        //DebugLogger.Instance.Log("Start Testing");
        Manager.Instance.currAppState = Manager.AppState.TEST;
        rootPlaybackArea.SetActive(false);
        //playbackUI.SetActive(false);
    }

    public void SetPlaybackObjectsVisibility(bool status)
    {
        rootPlaybackArea.SetActive(status);
    }
    
    public int GetSizeOfMainRecordedData()
    {
        //return objectsToRecord[0].recordedData.Count;
        return currentActiveExample.headData.Count;
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



    public List<GestureSequence> GenerateGestureSequences(RectTransform timelinePanel, string handStr)
    {
        var hand = handStr.Equals("lefthand") ? Recorder.Instance.currentActiveExample.leftHandData : Recorder.Instance.currentActiveExample.rightHandData;
        //DebugLogger.Instance.Log("Generating gesture sequences for " + handStr);
        List<InputManager.Gesture> gestures = hand.Select(x => x.gesture).ToList();
        List<GestureSequence> GestureSequences = GetContinuousGestureSequences(gestures);
        foreach (GestureSequence sequence in GestureSequences)
        {
            //DebugLogger.Instance.Log("Sequence name: " + InputManager.Instance.GestureToString(sequence.GestureType) + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
            //DebugLogger.Instance.Log("Start x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex) + ", End x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex + sequence.Length));
            TimelineUIElement.CreateTimelineElement(handTimelineElementPrefab, timelinePanel, sequence.StartIndex, sequence.Length, GetSizeOfMainRecordedData(), InputManager.Instance.GestureToString(sequence.GestureType));
        }
        return GestureSequences;
    }

    public List<VoiceSequence> GenerateVoiceCommandSequences(RectTransform timelinePanel)
    {        
        DebugLogger.Instance.Log("Generating voice command sequences");
        List<string> voiceCommands = Recorder.Instance.currentActiveExample.headData.Select(x => x.voiceCommand).ToList();
        List<VoiceSequence> voiceSequences = GetVoiceCommandSequences(voiceCommands);
        foreach (VoiceSequence sequence in voiceSequences)
        {
            DebugLogger.Instance.Log("Voice sequence name: " + sequence.VoiceCommand + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
            TimelineUIElement.CreateTimelineElement(voiceCommandTimelineElementPrefab, timelinePanel, sequence.StartIndex, sequence.Length, GetSizeOfMainRecordedData(), sequence.VoiceCommand);
        }
        return voiceSequences;
    }
    
    public List<VoiceSequence> GetVoiceCommandSequences(List<string> voiceCommands)
    {
        List<VoiceSequence> sequences = new();

        int startIndex = -1;
        string currentVoiceCommand = null;

        for (int i = 0; i < voiceCommands.Count; i++)
        {
            if (voiceCommands[i] != null)
            {
                if (currentVoiceCommand == null || currentVoiceCommand == voiceCommands[i])
                {
                    if (currentVoiceCommand == null)
                    {
                        currentVoiceCommand = voiceCommands[i];
                        startIndex = i;
                    }
                }
                else
                {
                    sequences.Add(new VoiceSequence
                    {
                        StartIndex = startIndex,
                        Length = i - startIndex,
                        VoiceCommand = currentVoiceCommand
                    });

                    startIndex = i;
                    currentVoiceCommand = voiceCommands[i];
                }
            }
            else if (currentVoiceCommand != null)
            {
                sequences.Add(new VoiceSequence
                {
                    StartIndex = startIndex,
                    Length = i - startIndex,
                    VoiceCommand = currentVoiceCommand
                });

                startIndex = -1;
                currentVoiceCommand = null;
            }
        }

        if (currentVoiceCommand != null)
        {
            sequences.Add(new VoiceSequence
            {
                StartIndex = startIndex,
                Length = voiceCommands.Count - startIndex,
                VoiceCommand = currentVoiceCommand
            });
        }

        return sequences;
    }
    
    public List<AssetAction> GetContinuousChangeSequences(List<string> actions)
    {
        List<AssetAction> sequences = new();

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
                        ActionStr = currentAction,
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
                    ActionStr = currentAction
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
                ActionStr = currentAction,
            });
        }

        return sequences;
    }

    public List<AssetAction> GenerateAssetActionSequences(RectTransform timelinePanel, Recordable recordable)
    {
        DebugLogger.Instance.Log("Generating asset action sequences for " + recordable.name);

        var recordedData = currentActiveExample.assetDataDict[recordable]; 

        List<string> changes = recordedData.Select(x => x.ActionStr).ToList();
        List<AssetAction> sequences = GetContinuousChangeSequences(changes);
        foreach (AssetAction sequence in sequences)
        {
            //DebugLogger.Instance.Log("Sequence name: " + sequence.Action + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
            //DebugLogger.Instance.Log("Start x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex) + ", End x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex + sequence.Length));
            if (recordedData[sequence.StartIndex].ActionDelegate != null)
            {
                sequence.ActionDelegate = recordedData[sequence.StartIndex].ActionDelegate;
                DebugLogger.Instance.Log("Action delegate found: " + recordedData[sequence.StartIndex].ActionDelegate.Method.Name + ", Parameters: " + string.Join(", ", recordedData[sequence.StartIndex].ActionDelegate.Method.GetParameters().Select(x => x.Name)));
            }

            if (sequence.ActionStr.Contains("Hide"))
            {
                GameObject hideElement = Instantiate(hideTimelinePanelPrefab, timelinePanel);
                hideElement.SetActive(true);
                hideElement.GetComponent<RectTransform>().anchoredPosition = new Vector2(TimelineUIElement.MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex, GetSizeOfMainRecordedData()), hideElement.GetComponent<RectTransform>().anchoredPosition.y);
            }
            else if (sequence.ActionStr.Contains("Show"))
            {
                GameObject showElement = Instantiate(showTimelinePanelPrefab, timelinePanel);
                showElement.SetActive(true);
                showElement.GetComponent<RectTransform>().anchoredPosition = new Vector2(TimelineUIElement.MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex, GetSizeOfMainRecordedData()), showElement.GetComponent<RectTransform>().anchoredPosition.y);
            }

            if(sequence.ActionStr.Contains("Follow") || sequence.ActionStr.Contains("ApplyForce") || sequence.ActionStr.Contains("ChangeColor") || sequence.ActionStr.Contains("Pin")) //Other types of events - physics, attach etc
            {                
                TimelineUIElement.CreateTimelineElement(assetTimelineElementPrefab, timelinePanel, sequence.StartIndex, sequence.Length, GetSizeOfMainRecordedData(), sequence.ActionStr);
            }
        }
        return sequences;
    }

    /*public void GenerateVoiceCommandSequences(RectTransform timelinePanel)
    {
        DebugLogger.Instance.Log("Generating voice command sequences");
        List<string> voiceCommands = currentActiveExample.headData.Select(x => x).ToList();
        List<AssetAction> sequences = GetContinuousChangeSequences(voiceCommands);
        foreach (AssetAction sequence in sequences)
        {
            //DebugLogger.Instance.Log("Sequence name: " + sequence.Action + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
            //DebugLogger.Instance.Log("Start x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex) + ", End x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex + sequence.Length));
            TimelineUIElement.CreateTimelineElement(assetTimelineElementPrefab, timelinePanel, sequence.StartIndex, sequence.Length, GetSizeOfMainRecordedData(), sequence.ActionStr);
        }
    }*/

    public List<AssetAction> GenerateCollisionSequences(RectTransform collisionTimelinePanelTransform, Recordable recordable) //Strong assumption that all sources of action come from collision
    {
        DebugLogger.Instance.Log("Generating collision sequences for " + recordable.name);
        var recordedData = currentActiveExample.assetDataDict[recordable]; 
        try
        {
            List<string> sourcesOfChanges = recordedData.Select(x => x.CollisionStr).ToList();
            List<AssetAction> sequences = GetContinuousChangeSequences(sourcesOfChanges);
            foreach (AssetAction sequence in sequences)
            {
                //DebugLogger.Instance.Log("Collision sequence name: " + sequence.Action + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
                //DebugLogger.Instance.Log("Start x: " + MapIndexToTimelinePosition(collisionTimelinePanelTransform, sequence.StartIndex) + ", End x: " + MapIndexToTimelinePosition(collisionTimelinePanelTransform, sequence.StartIndex + sequence.Length));

                if (recordedData[sequence.StartIndex].CollisionDelegate != null)
                    DebugLogger.Instance.Log("Collision delegate found: " + recordedData[sequence.StartIndex].CollisionDelegate.Method.Name + ", Parameters: " + string.Join(", ", recordedData[sequence.StartIndex].CollisionDelegate.Method.GetParameters().Select(x => x.Name)));
                if (recordedData[sequence.StartIndex].CollidedObject != null)
                {
                    DebugLogger.Instance.Log("Collided object: " + recordedData[sequence.StartIndex].CollidedObject.name);
                    sequence.CollidingObject1 = recordable.gameObject;
                    sequence.CollidingObject2 = recordedData[sequence.StartIndex].CollidedObject;
                    sequence.CollisionStr = recordedData[sequence.StartIndex].CollisionStr;
                }

                TimelineUIElement.CreateTimelineElement(collisionTimelineElementPrefab, collisionTimelinePanelTransform, sequence.StartIndex, sequence.Length, GetSizeOfMainRecordedData(), sequence.ActionStr);
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

    public void RefreshTimelineAndStates()
    {
        DebugLogger.Instance.Log("Refreshing assets timeline for example " + currentActiveExample.exampleId);
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
        for (int i = 1; i < leftHandTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(leftHandTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }
        for (int i = 2; i < rightHandTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(rightHandTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }
        for (int i = 4; i < voiceTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(voiceTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }

        //Generate gesture sequences for left and right hand
        LeftHandGestureSequences = GenerateGestureSequences(leftHandTimelinePanel, "lefthand");
        RightHandGestureSequences = GenerateGestureSequences(rightHandTimelinePanel, "righthand");
        AllGestureSequences = LeftHandGestureSequences.Concat(RightHandGestureSequences).ToList();

        //Generate voice command sequences
        VoiceCommandSequences = GenerateVoiceCommandSequences(voiceTimelinePanel);


        //GenerateGestureSequences(timelinePanel, recordable);
        int assetsCounter = 0;
        assetSequencesLists.Clear();
        collisionSequencesLists.Clear();
        foreach (var recordable in currentActiveExample.assetDataDict.Keys)
        {
            ++assetsCounter;
            GameObject timelinePanel = Instantiate(assetTimelinePanelPrefab, playbackPanelTransform);
            assetTimelines.Add(timelinePanel);
            timelinePanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(timelinePanel.GetComponent<RectTransform>().anchoredPosition.x, timelinePanel.GetComponent<RectTransform>().anchoredPosition.y - assetsCounter * 100);
            timelinePanel.SetActive(true);
            timelinePanel.GetComponent<RectTransform>().GetChild(0).GetComponent<TMPro.TMP_Text>().text = Manager.Instance.CleanAssetName(recordable.name); //Assign asset name

            if (currentActiveExample.assetDataDict[recordable].Count > 0)
            {
                assetSequencesLists.Add(GenerateAssetActionSequences(timelinePanel.GetComponent<RectTransform>(), recordable));
                collisionSequencesLists.Add(GenerateCollisionSequences(collisionTimelinePanel.GetComponent<RectTransform>(), recordable));
            }
        }
        CreateStateMachine();
       
    }

    public void CreateStateMachine()
    {
        //RefreshAssetsTimeline(); 
        CreateStates();
        CombineExamples();
    }


    public void CreateStates()
    {
        ResetExampleStateMachine();

        int recordedFramesTotal = GetSizeOfMainRecordedData();

        var gestures = AllGestureSequences;
        var collisions = collisionSequencesLists.SelectMany(x => x).ToList();
        var voiceCommands = VoiceCommandSequences;

        // Create a list of events (start or end of a sequence)
        var events = new List<(int Index, string Type, GestureSequence Gesture, AssetAction Collision, VoiceSequence VoiceCommand)>();

        foreach (var gesture in gestures)
        {
            events.Add((gesture.StartIndex, "start", gesture, null, null));
        }

        foreach (var collision in collisions)
        {
            events.Add((collision.StartIndex, "start", null, collision, null));
        }

        foreach (var voiceCommand in voiceCommands)
        {
            events.Add((voiceCommand.StartIndex, "start", null, null, voiceCommand));
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
                    state.Collision = prevEvent.Collision;
                    state.VoiceSequence = prevEvent.VoiceCommand;
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
                state.Collision = lastEvent.Collision;
                state.VoiceSequence = lastEvent.VoiceCommand;
            }
        }


        DebugLogger.Instance.Log("Adding events and transitions to states");
        //var firstState = StatesInTimeline.First().Key;
        //Add events to states
        //
        List<State> orderedKeys = new(currentActiveExample.StatesDict.Keys);

        if (orderedKeys.Count == 0)
        {
            DebugLogger.Instance.Log("No states found");
            return;
        }
        //Add actions to the first state
        var firstState = currentActiveExample.StatesDict[orderedKeys[0]].state;
        AddActionsToState(firstState);


        for (int i = 0; i < orderedKeys.Count - 1; i++)
        {
            State prevState = currentActiveExample.StatesDict[orderedKeys[i]].state;
            State stateInTimeline = currentActiveExample.StatesDict[orderedKeys[i + 1]].state;

            DebugLogger.Instance.Log("Adding transition from " + prevState.id + " to " + stateInTimeline.id);
            //prevState.AddTransitionTo(state, (frame) => { return true; });


            //Iterate through gesture sequences and check if GestureType is equal to InputManager.Gesture.RIGHTHANDPINCH or InputManager.Gesture.LEFTHANDPINCH
            foreach (var gesture in gestures)
            {
                if (gesture.StartIndex == currentActiveExample.StatesDict[stateInTimeline].StartIndex)
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
                    if (collisionSequence.StartIndex == currentActiveExample.StatesDict[stateInTimeline].StartIndex)
                    {
                        //Add the collision to the state
                        stateInTimeline.Collision = collisionSequence;
                        DebugLogger.Instance.Log("Added collision " + collisionSequence.ActionStr + " to state " + stateInTimeline.id);
                    }
                }
            }

            //Iterate through voice command sequences
            foreach (var voiceCommand in voiceCommands)
            {
                if (voiceCommand.StartIndex == currentActiveExample.StatesDict[stateInTimeline].StartIndex)
                {
                    //Add the voice command to the state
                    stateInTimeline.VoiceSequence = voiceCommand;
                    DebugLogger.Instance.Log("Found voice command " + voiceCommand.VoiceCommand + " at the beginning of state " + stateInTimeline.id);
                }
            }

            string transitionDescription = "";

            if (stateInTimeline.Gesture == null && stateInTimeline.Collision == null && stateInTimeline.VoiceSequence == null)//No gesture, collision or voice command
            {
                DebugLogger.Instance.Log("State " + stateInTimeline.id + " does not have any gesture, collision or voice command");
            }
            else if (stateInTimeline.Gesture != null && stateInTimeline.Collision == null && stateInTimeline.VoiceSequence == null) //Gesture only
            {
                //DebugLogger.Instance.Log("State " + state.id + " has gesture " + InputManager.Instance.GestureToString(state.Gesture.GestureType));
                if (stateInTimeline.Gesture.GestureType == InputManager.Gesture.RIGHTHANDPINCH || stateInTimeline.Gesture.GestureType == InputManager.Gesture.RIGHTHANDOPEN)
                {                    
                    transitionDescription = "" + prevState.id + "->" + stateInTimeline.id + ": frame.rightHandGesture == " + InputManager.Instance.GestureToString(stateInTimeline.Gesture.GestureType) + "; ";
                    DebugLogger.Instance.Log("Adding transition from " + transitionDescription);
                    prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.rightHandGesture == stateInTimeline.Gesture.GestureType; }, transitionDescription);
                }
                else if (stateInTimeline.Gesture.GestureType == InputManager.Gesture.LEFTHANDPINCH || stateInTimeline.Gesture.GestureType == InputManager.Gesture.LEFTHANDOPEN)
                {
                    transitionDescription = "" + prevState.id + "->" + stateInTimeline.id + ": frame.lefthandgesture == " + InputManager.Instance.GestureToString(stateInTimeline.Gesture.GestureType) + "; ";
                    DebugLogger.Instance.Log("Adding transition from " + transitionDescription);
                    prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.leftHandGesture == stateInTimeline.Gesture.GestureType; }, transitionDescription);
                }

            }
            else if (stateInTimeline.Gesture == null && stateInTimeline.Collision != null && stateInTimeline.VoiceSequence == null)//Collision only
            {
                transitionDescription = "" + prevState.id + "->" + stateInTimeline.id + ": frame.IsColliding(" + stateInTimeline.Collision.CollidingObject1.name + ", " + stateInTimeline.Collision.CollidingObject2.name + "); ";
                DebugLogger.Instance.Log("Adding transition from " + transitionDescription);
                prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.IsColliding(stateInTimeline.Collision.CollidingObject1, stateInTimeline.Collision.CollidingObject2); }, transitionDescription);
            }
            else if (stateInTimeline.Gesture == null && stateInTimeline.Collision == null && stateInTimeline.VoiceSequence != null)//Voice command only
            {
                transitionDescription = "" + prevState.id + "->" + stateInTimeline.id + ": frame.voiceCommand.Contains(" + stateInTimeline.VoiceSequence.VoiceCommand + "); ";
                DebugLogger.Instance.Log("Adding transition from " + transitionDescription);
                prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.voiceCommand.Contains(stateInTimeline.VoiceSequence.VoiceCommand); }, transitionDescription);
            }
            else if (stateInTimeline.Gesture != null && stateInTimeline.Collision != null && stateInTimeline.VoiceSequence == null)//Gesture and collision
            {
                //DebugLogger.Instance.Log("State " + state.id + " has gesture " + InputManager.Instance.GestureToString(state.Gesture.GestureType) + " and collision " + state.Collision.Action);
                if (stateInTimeline.Gesture.GestureType == InputManager.Gesture.RIGHTHANDPINCH || stateInTimeline.Gesture.GestureType == InputManager.Gesture.RIGHTHANDOPEN)
                {
                    transitionDescription = "" + prevState.id + "->" + stateInTimeline.id + ": frame.rightHandGesture == " + InputManager.Instance.GestureToString(stateInTimeline.Gesture.GestureType) + " && frame.IsColliding(" + stateInTimeline.Collision.CollidingObject1.name + ", " + stateInTimeline.Collision.CollidingObject2.name + "); ";
                    DebugLogger.Instance.Log("Adding transition from " + transitionDescription);
                    prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.rightHandGesture == stateInTimeline.Gesture.GestureType && frame.IsColliding(stateInTimeline.Collision.CollidingObject1, stateInTimeline.Collision.CollidingObject2); }, transitionDescription);
                }
                else if (stateInTimeline.Gesture.GestureType == InputManager.Gesture.LEFTHANDPINCH || stateInTimeline.Gesture.GestureType == InputManager.Gesture.LEFTHANDOPEN)
                {
                    transitionDescription = "" + prevState.id + "->" + stateInTimeline.id + ": frame.lefthandgesture == " + InputManager.Instance.GestureToString(stateInTimeline.Gesture.GestureType) + " && frame.IsColliding(" + stateInTimeline.Collision.CollidingObject1.name + ", " + stateInTimeline.Collision.CollidingObject2.name + "); ";
                    prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.leftHandGesture == stateInTimeline.Gesture.GestureType && frame.IsColliding(stateInTimeline.Collision.CollidingObject1, stateInTimeline.Collision.CollidingObject2); }, transitionDescription);
                }
            }
            else if (stateInTimeline.Gesture != null && stateInTimeline.Collision == null && stateInTimeline.VoiceSequence != null)//Gesture and voice command
            {
                //DebugLogger.Instance.Log("State " + state.id + " has gesture " + InputManager.Instance.GestureToString(state.Gesture.GestureType) + " and collision " + state.Collision.Action);
                if (stateInTimeline.Gesture.GestureType == InputManager.Gesture.RIGHTHANDPINCH || stateInTimeline.Gesture.GestureType == InputManager.Gesture.RIGHTHANDOPEN)
                {
                    transitionDescription = "" + prevState.id + "->" + stateInTimeline.id + ": frame.rightHandGesture == " + InputManager.Instance.GestureToString(stateInTimeline.Gesture.GestureType) + " && frame.voiceCommand.Contains(" + stateInTimeline.VoiceSequence.VoiceCommand + "); ";
                    DebugLogger.Instance.Log("Adding transition from " + transitionDescription);
                    prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.rightHandGesture == stateInTimeline.Gesture.GestureType && frame.voiceCommand.Contains(stateInTimeline.VoiceSequence.VoiceCommand); }, transitionDescription);
                }
                else if (stateInTimeline.Gesture.GestureType == InputManager.Gesture.LEFTHANDPINCH || stateInTimeline.Gesture.GestureType == InputManager.Gesture.LEFTHANDOPEN)
                {
                    transitionDescription = "" + prevState.id + "->" + stateInTimeline.id + ": frame.lefthandgesture == " + InputManager.Instance.GestureToString(stateInTimeline.Gesture.GestureType) + " && frame.voiceCommand.Contains(" + stateInTimeline.VoiceSequence.VoiceCommand + "); ";
                    DebugLogger.Instance.Log("Adding transition from " + transitionDescription);
                    prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.leftHandGesture == stateInTimeline.Gesture.GestureType && frame.voiceCommand.Contains(stateInTimeline.VoiceSequence.VoiceCommand); }, transitionDescription);
                }
            }
            else if (stateInTimeline.Gesture == null && stateInTimeline.Collision != null && stateInTimeline.VoiceSequence != null)//Collision and voice command
            {
                transitionDescription = "" + prevState.id + "->" + stateInTimeline.id + ": frame.IsColliding(" + stateInTimeline.Collision.CollidingObject1.name + ", " + stateInTimeline.Collision.CollidingObject2.name + ") && frame.voiceCommand.Contains(" + stateInTimeline.VoiceSequence.VoiceCommand + "); ";
                DebugLogger.Instance.Log("Adding transition from " + transitionDescription);                
                prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.voiceCommand.Contains(stateInTimeline.VoiceSequence.VoiceCommand) && frame.IsColliding(stateInTimeline.Collision.CollidingObject1, stateInTimeline.Collision.CollidingObject2); }, transitionDescription);
            }
            else if (stateInTimeline.Gesture != null && stateInTimeline.Collision != null && stateInTimeline.VoiceSequence != null)//Gesture, collision and voice command
            {
                //DebugLogger.Instance.Log("State " + state.id + " has gesture " + InputManager.Instance.GestureToString(state.Gesture.GestureType) + " and collision " + state.Collision.Action);
                if (stateInTimeline.Gesture.GestureType == InputManager.Gesture.RIGHTHANDPINCH || stateInTimeline.Gesture.GestureType == InputManager.Gesture.RIGHTHANDOPEN)
                {
                    transitionDescription = "" + prevState.id + "->" + stateInTimeline.id + ": frame.rightHandGesture == " + InputManager.Instance.GestureToString(stateInTimeline.Gesture.GestureType) + " && frame.IsColliding(" + stateInTimeline.Collision.CollidingObject1.name + ", " + stateInTimeline.Collision.CollidingObject2.name + ") && frame.voiceCommand.Contains(" + stateInTimeline.VoiceSequence.VoiceCommand + "); ";
                    DebugLogger.Instance.Log("Adding transition from " + transitionDescription);
                    prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.rightHandGesture == stateInTimeline.Gesture.GestureType && frame.IsColliding(stateInTimeline.Collision.CollidingObject1, stateInTimeline.Collision.CollidingObject2) && frame.voiceCommand.Contains(stateInTimeline.VoiceSequence.VoiceCommand); }, transitionDescription);
                }
                else if (stateInTimeline.Gesture.GestureType == InputManager.Gesture.LEFTHANDPINCH || stateInTimeline.Gesture.GestureType == InputManager.Gesture.LEFTHANDOPEN)
                {
                    transitionDescription = "" + prevState.id + "->" + stateInTimeline.id + ": frame.lefthandgesture == " + InputManager.Instance.GestureToString(stateInTimeline.Gesture.GestureType) + " && frame.IsColliding(" + stateInTimeline.Collision.CollidingObject1.name + ", " + stateInTimeline.Collision.CollidingObject2.name + ") && frame.voiceCommand.Contains(" + stateInTimeline.VoiceSequence.VoiceCommand + "); ";
                    DebugLogger.Instance.Log("Adding transition from " + transitionDescription);
                    prevState.AddTransitionTo(stateInTimeline, (frame) => { return frame.leftHandGesture == stateInTimeline.Gesture.GestureType && frame.IsColliding(stateInTimeline.Collision.CollidingObject1, stateInTimeline.Collision.CollidingObject2) && frame.voiceCommand.Contains(stateInTimeline.VoiceSequence.VoiceCommand); }, transitionDescription);
                }
            }
            else
            {
                DebugLogger.Instance.Log("No transition added for state " + stateInTimeline.id);
            }

            //Add actions to states
            AddActionsToState(stateInTimeline);
        }

        //Set the initial state of the state machine to be the first state in the StatesInTimeline dictionary
        //CustomStateMachine.Instance.SetInitialState(currentActiveExample.StatesDict.First().Key.id);
     
    }


    public void CombineExamples()
    {
        DebugLogger.Instance.ClearVRDebugText();

        //DebugLogger.Instance.Log("Combining examples");
        List<List<State>> allStatesInExamples = new();
        foreach (var example in examples)
        {
            allStatesInExamples.Add(example.StatesDict.Keys.ToList());
        }
       
        allStatesInExamples = allStatesInExamples.OrderBy(x => x.Count).ToList();

        var shortestList = allStatesInExamples.First();
        int shortestListLength = shortestList.Count;
        DebugLogger.Instance.Log("CombineExamples: The shortest list of states has " + shortestList.Count + " states");

        List<State> commonStates = new List<State>();
        int lastCommonStateIndex = 0;
        for (int i = 0; i < shortestListLength; i++)
        {
            lastCommonStateIndex = i;
            bool allStatesEqual = true;
            for (int j = 0; j < allStatesInExamples.Count; j++)
            {
                if (!shortestList[i].IsStateEqualTo(allStatesInExamples[j][i]))
                {
                    allStatesEqual = false;
                    break;
                }
            }
            
            if (allStatesEqual)
            {
                commonStates.Add(shortestList[i]);
            }
            else
            {
                break;
            }
        }

        if(lastCommonStateIndex == 0)
        {
            DebugLogger.Instance.Log("CombineExamples: No common states found");
            return;
        }

        DebugLogger.Instance.Log("CombineExamples: The last common state is " + commonStates.Last().id + " at index " + (lastCommonStateIndex-1));

        if (lastCommonStateIndex < shortestListLength)
        { 
            //Printing the states at lastCommonStateIndex-1 for each List<State> in allStatesInExamples except the first
            DebugLogger.Instance.Log("CombineExamples: States at index " + (lastCommonStateIndex - 1) + " for each example except the first: ");
            for (int i = 1; i < allStatesInExamples.Count; i++)
            {
                commonStates.Add(shortestList[lastCommonStateIndex]); //Add the state after the last common state
                //allStatesInExamples[i][lastCommonStateIndex].PrintDetailsOfState(VRConsoleEnabled : true);            
                commonStates.Last().CopyTransitionFromState(allStatesInExamples[i][lastCommonStateIndex]);
                //DebugLogger.Instance.Log("Transition copied from " + allStatesInExamples[i][lastCommonStateIndex].id + " to " + commonStates.Last().id);
                //DebugLogger.Instance.Log("Details of allStatesInExamples[" + i + "][" + lastCommonStateIndex + "]: ");
                //allStatesInExamples[i][lastCommonStateIndex].PrintDetailsOfState(VRConsoleEnabled : true);
                //DebugLogger.Instance.Log("Details of commonStates.Last(): ", VRConsoleEnabled : true);
                //commonStates.Last().PrintDetailsOfState(VRConsoleEnabled : true);

            }

            CustomStateMachine.Instance.DeleteAllStates();
            int newId = 0;
            foreach (var state in commonStates)
            {
                CustomStateMachine.Instance.AddState("State " + newId.ToString(), state);
                newId++;
            }

            for (int i = 0; i < allStatesInExamples.Count; i++)
            {
                //Copy all states from lastCommonStateIndex to the end of each list
                for (int j = lastCommonStateIndex + 1; j < allStatesInExamples[i].Count; j++)
                {
                    CustomStateMachine.Instance.AddState("State " + newId.ToString(), allStatesInExamples[i][j]);
                    newId++;
                }
            }


            //Print the common states          
            

            CustomStateMachine.Instance.CreateStateGraph(stateGraphElementPrefab, stateGraphPanel.GetComponent<RectTransform>());

        }
        
        CustomStateMachine.Instance.PrintDetailsOfStateMachine(VRConsoleEnabled : true);

        //CustomStateMachine.Instance.SetInitialState(commonStates.First().id);

    }

    //Dictionary<State, StateTimelineUIElement> StatesDict = new();
    State CreateState(int startIndex, int length)
    {
        var StateMachine = CustomStateMachine.Instance;
        var state = new State
        {
            id = "State " + StateMachine.GetSize()
        };
        StateMachine.AddState(state.id, state);

        var stateUI = StateTimelineUIElement.CreateStateTimelineElement(stateTimelineElementPrefab, stateTimelinePanel.GetComponent<RectTransform>(),
                                                        startIndex, length, GetSizeOfMainRecordedData(), state);
        
        state.timelineElement = stateUI;

        currentActiveExample.StatesDict.Add(state, stateUI.GetComponent<StateTimelineUIElement>());

        return state;
    }

    State MergeStates(State state1, State state2)
    {
        var state1UIElement = currentActiveExample.StatesDict[state1];
        var state2UIElement = currentActiveExample.StatesDict[state2];


        var StateMachine = CustomStateMachine.Instance;
        var newState = new State
        {
            id = "tempState" + StateMachine.GetSize()
        };

        //Get the index of state1 in the StatesInTimeline dictionary
        int state1Index = currentActiveExample.StatesDict.Keys.ToList().IndexOf(state1);
        //Find the state before state1 in the StatesInTimeline dictionary if the index of state1 is not 0
        State previousState = null;
        if (state1Index != 0)
        {
            previousState = currentActiveExample.StatesDict.Keys.ToList()[state1Index - 1];
            //Add a transition from the previous state to the new state
            //previousState.ModifyTransitionTo(newState);
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
        //newState.CopyTransitionFromState(state2);

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
        Destroy(currentActiveExample.StatesDict[state1].gameObject);
        Destroy(currentActiveExample.StatesDict[state2].gameObject);
        currentActiveExample.StatesDict.Remove(state1);
        currentActiveExample.StatesDict.Remove(state2);

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
        currentActiveExample.StatesDict.Add(newState, stateUI.GetComponent<StateTimelineUIElement>());
        StateMachine.AddState(newState.id, newState);

        //StateMachine.SetInitialState(currentActiveExample.StatesDict.First().Key.id);

        return newState;
    }


    public void ResetExampleStateMachine()
    {
        currentActiveExample.StatesDict.Clear();

        //Delete all existing states
        CustomStateMachine.Instance.DeleteAllStates();

        //Delete all existing state timeline UI elements
        for (int i = 2; i < stateTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(stateTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }
    }

    public void AddActionsToState(State stateInTimeline)
    {
        var actions = assetSequencesLists;
        //var stateInTimeline = currentActiveExample.StatesDict.First().Key;
        foreach (var assetSequences in actions)
        {
            foreach (var assetSequence in assetSequences)
            {
                int distanceToStateStart = assetSequence.StartIndex - currentActiveExample.StatesDict[stateInTimeline].StartIndex;
                int distanceToStateEnd = currentActiveExample.StatesDict[stateInTimeline].StartIndex + currentActiveExample.StatesDict[stateInTimeline].Length - assetSequence.StartIndex - 1;
                if (distanceToStateStart >= 0 && distanceToStateEnd >= 0)
                {
                    if(distanceToStateStart < distanceToStateEnd)
                    {
                        DebugLogger.Instance.Log("Adding action(s) " + assetSequence.ActionStr + " for state " + stateInTimeline.id + " in OnEnterActions");
                        stateInTimeline.OnEnterActions += () => assetSequence.ActionDelegate();
                        stateInTimeline.OnEnterActionsStr.Add(assetSequence.ActionStr);
                    }
                    else
                    {
                        DebugLogger.Instance.Log("Adding action(s) " + assetSequence.ActionStr + " for state " + stateInTimeline.id + " in OnExitActions");
                        stateInTimeline.OnExitActions += () => assetSequence.ActionDelegate();
                        stateInTimeline.OnExitActionsStr.Add(assetSequence.ActionStr);
                    }
                }

            }
        }
    }




    public void ResetStateMachine()
    {
        CustomStateMachine.Instance.SetInitialState("State 0");
    }


    public void PrintDetailsOfStateMachine()
    {
        DebugLogger.Instance.ClearVRDebugText();

        CustomStateMachine.Instance.PrintDetailsOfStateMachine(VRConsoleEnabled : true);

        /*
        DebugLogger.Instance.Log("Printing details of state machine");

        foreach(var example in examples)
        {
            DebugLogger.Instance.Log("Example " + example.exampleId);
            foreach (var state in example.StatesDict.Keys)
            {
                state.PrintDetailsOfState(VRConsoleEnabled: true);
            }
        }
        /*foreach (var state in currentActiveExample.StatesDict.Keys)
        {
            state.PrintDetailsOfState(VRConsoleEnabled: true);
        }*/
    }

    public void MergeToLeftState(StateTimelineUIElement selectedStateUIElement)
    {
        var selectedState = currentActiveExample.StatesDict.FirstOrDefault(x => x.Value == selectedStateUIElement).Key;
        //Find the state to the left of the selected state
        if (currentActiveExample.StatesDict.Keys.ToList().IndexOf(selectedState) > 0)
        {
            var precedingState = currentActiveExample.StatesDict.ElementAt(currentActiveExample.StatesDict.Keys.ToList().IndexOf(selectedState) - 1).Key;
            var precedingStateUIElement = currentActiveExample.StatesDict.ElementAt(currentActiveExample.StatesDict.Keys.ToList().IndexOf(selectedState) - 1).Value;
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
        var selectedState = currentActiveExample.StatesDict.FirstOrDefault(x => x.Value == selectedStateUIElement).Key;
        //Find the state to the right of the selected state
        if (currentActiveExample.StatesDict.Keys.ToList().IndexOf(selectedState) < currentActiveExample.StatesDict.Count - 1)
        {
            var succeedingState = currentActiveExample.StatesDict.ElementAt(currentActiveExample.StatesDict.Keys.ToList().IndexOf(selectedState) + 1).Key;
            var succeedingStateUIElement = currentActiveExample.StatesDict.ElementAt(currentActiveExample.StatesDict.Keys.ToList().IndexOf(selectedState) + 1).Value;
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
        if (Manager.Instance.currAppState == Manager.AppState.RECORDING)
        {
            ++frameCount;

            head.Record(frameCount);
            leftHand.Record(frameCount, "lefthand");
            rightHand.Record(frameCount, "righthand");

        }
        else if (Manager.Instance.currAppState == Manager.AppState.PLAYBACK)
        {

            if (isAutomaticPlayback)
            {
                playbackSlider.value += 1;//Time.deltaTime;
                if (playbackSlider.value >= GetSizeOfMainRecordedData())
                {
                    playbackSlider.value = 0;
                }
            }

            int currentFrameNum = (int)playbackSlider.value;


            if (head.playbackObject != null)
            {
                head.playbackObject.transform.SetLocalPositionAndRotation(currentActiveExample.headData[currentFrameNum].rootPosition, currentActiveExample.headData[currentFrameNum].rootRotation);
                head.playbackFocusSquare.transform.SetPositionAndRotation(currentActiveExample.headData[currentFrameNum].focusSquarePosition, currentActiveExample.headData[currentFrameNum].focusSquareRotation);
            }

            if (leftHand.playbackObject != null)
            {
                leftHand.playbackObject.transform.SetLocalPositionAndRotation(currentActiveExample.leftHandData[currentFrameNum].rootPosition, currentActiveExample.leftHandData[currentFrameNum].rootRotation * Quaternion.Euler(leftHand.rotationCorrection));
                if (leftHand.playbackObject.GetComponent<HandPlaybackObjectScript>() != null)
                {
                    leftHand.playbackObject.GetComponent<HandPlaybackObjectScript>().SetPoseForAllFingerJoints(currentActiveExample.leftHandData[currentFrameNum]);
                    leftHand.playbackGestureText.text = InputManager.Instance.GestureToString(currentActiveExample.leftHandData[currentFrameNum].gesture);
                }

                leftHand.playbackFocusSquare.transform.SetPositionAndRotation(currentActiveExample.leftHandData[currentFrameNum].focusSquarePosition, currentActiveExample.leftHandData[currentFrameNum].focusSquareRotation);
            }

            if (rightHand.playbackObject != null)
            {
                rightHand.playbackObject.transform.SetLocalPositionAndRotation(currentActiveExample.rightHandData[currentFrameNum].rootPosition, currentActiveExample.rightHandData[currentFrameNum].rootRotation * Quaternion.Euler(rightHand.rotationCorrection));
                if (rightHand.playbackObject.GetComponent<HandPlaybackObjectScript>() != null)
                {
                    rightHand.playbackObject.GetComponent<HandPlaybackObjectScript>().SetPoseForAllFingerJoints(currentActiveExample.rightHandData[currentFrameNum]);
                    rightHand.playbackGestureText.text = InputManager.Instance.GestureToString(currentActiveExample.rightHandData[currentFrameNum].gesture);
                }

                rightHand.playbackFocusSquare.transform.SetPositionAndRotation(currentActiveExample.rightHandData[currentFrameNum].focusSquarePosition, currentActiveExample.rightHandData[currentFrameNum].focusSquareRotation);
            }

        }

    }
}





