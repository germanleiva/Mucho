using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
//using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Serialization;
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

    public int recordStartFrame;

    public bool isAutomaticPlayback = false;

    readonly List<float> handGuideTimePoints = new();
    [Header("Timeline UI")]
    [SerializeField]
    public RectTransform examplePlaybackPanelPrefab;

    [SerializeField]
    RectTransform stateGraphPanel;
    [SerializeField]
    GameObject stateGraphElementPrefab;   

    // Everytime this variable changes we need to update timelineAssetsRow and assetFramesDict
    [FormerlySerializedAs("assetsInScene")] public List<Asset> allAssets = new();

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
        examplePlaybackPanelPrefab.gameObject.SetActive(false);
        AddExample();
    }


    public void SelectExample(Example _currentActiveExample)
    {
        DebugLogger.Instance.Log("Selecting example " + _currentActiveExample.exampleId);
        
        currentActiveExample = _currentActiveExample;
        foreach (var ex in examples)
        {
            ex.button.GetComponent<Image>().color = Color.white;
            ex.examplePlaybackPanel.gameObject.SetActive(false);
        }

        currentActiveExample.examplePlaybackPanel.gameObject.SetActive(true);
        currentActiveExample.button.GetComponent<Image>().color = Color.green;
        
        RecreateTimeline();
    }
    
    public void RecreateTimeline()
    {
        //Delete all existing timeline elements in the input and create the new ones
        RecreateTimelineInputs();
        
        //Delete all existing asset rows and create the new ones
        RecreateTimelineAssetRows();
    }

    public void RecreateTimelineInputs()
    {
        RefreshTimelineGestures();
        RefreshTimelineVoiceSequences();
        RefreshTimelineCollisions();
    }
    
    public void AddExample()
    {        
        var numberButton = Instantiate(firstExampleButtonObj, firstExampleButtonObj.transform.parent);
        numberButton.SetActive(true);
        //clone _examplePlaybackPanel
        RectTransform examplePlaybackPanel_clone = Instantiate(examplePlaybackPanelPrefab, examplePlaybackPanelPrefab.parent);
        
        Example example = new(numberButton.GetComponent<Button>(), examplePlaybackPanel_clone);
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
        //CreateTimelineAndStatePlaceholders(false); 

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
        //AssetManager.Instance.ShowMiscObjs();
        AssetManager.Instance.SetAllAssetMenusPokeable(false);

        recordStartFrame = 0;//Time.time;
        frameCount = 0;
        //RefreshTimelineAndStates(); 
    }



    // Stop recording.
    public void StopRecording()
    {
        rootPlaybackArea.SetActive(true);

        DebugLogger.Instance.Log("Size of recordedData head: " + currentActiveExample.headFrames.Count);
        DebugLogger.Instance.Log("Size of recordedData leftHand: " + currentActiveExample.leftHandFrames.Count);
        DebugLogger.Instance.Log("Size of recordedData rightHand: " + currentActiveExample.rightHandFrames.Count);

        foreach(var asset in currentActiveExample.assetFramesDict.Keys)
        {
            DebugLogger.Instance.Log("Size of recordedData asset: " + asset.name + ", " + currentActiveExample.assetFramesDict[asset].Count);
        }
        
        //TODO: If sizes do no match raise an error
        

        Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        
        //Stop voice record if active
        if (head.voiceRecordStarted)
        {
            head.StopVoiceRecord();
        }

        //AssetManager.Instance.ShowMiscObjs();
        AssetManager.Instance.SetAllAssetMenusPokeable(true);
 
        AssetManager.Instance.DoRecordSizesMatch();
        CreateTimelineAndStatePlaceholders();   

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

            playButton.SetActive(true);

            // Set up the slider.
            playbackSlider.minValue = 0;
            playbackSlider.maxValue = GetSizeOfMainRecordedData(); //framesTotal;
            playbackSlider.value = 0;


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

        foreach (var sequence in currentActiveExample.RightHandGestureSequences)
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

        foreach (var sequence in currentActiveExample.LeftHandGestureSequences)
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

    public void SetPlaybackObjectsVisibility(bool status)
    {
        rootPlaybackArea.SetActive(status);
    }
    
    public int GetSizeOfMainRecordedData()
    {
        //return objectsToRecord[0].recordedData.Count;
        return currentActiveExample.headFrames.Count;
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



    public List<GestureSequence> GenerateGestureSequences(RectTransform timelinePanel, List<HandFrame> handFrames)
    {
        //DebugLogger.Instance.Log("Generating gesture sequences for " + handStr);
        List<InputManager.Gesture> gestures = handFrames.Select(x => x.gesture).ToList();
        List<GestureSequence> GestureSequences = GetContinuousGestureSequences(gestures);
        foreach (GestureSequence sequence in GestureSequences)
        {
            //DebugLogger.Instance.Log("Sequence name: " + InputManager.Instance.GestureToString(sequence.GestureType) + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
            //DebugLogger.Instance.Log("Start x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex) + ", End x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex + sequence.Length));
            TimelineUIElement.CreateTimelineElement(currentActiveExample.handTimelineElementPrefab, timelinePanel, sequence.StartIndex, sequence.Length, GetSizeOfMainRecordedData(), InputManager.Instance.GestureToString(sequence.GestureType));
        }
        return GestureSequences;
    }

    public List<VoiceSequence> GenerateVoiceCommandSequences(RectTransform timelinePanel)
    {        
        DebugLogger.Instance.Log("Generating voice command sequences");
        List<string> voiceCommands = Recorder.Instance.currentActiveExample.headFrames.Select(x => x.voiceCommand).ToList();
        List<VoiceSequence> voiceSequences = GetVoiceCommandSequences(voiceCommands);
        foreach (VoiceSequence sequence in voiceSequences)
        {
            DebugLogger.Instance.Log("Voice sequence name: " + sequence.VoiceCommand + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
            GameObject timelineElement = TimelineUIElement.CreateTimelineElement(currentActiveExample.voiceCommandTimelineElementPrefab, timelinePanel, sequence.StartIndex, sequence.Length, GetSizeOfMainRecordedData(), sequence.VoiceCommand);
            TimelineUIElement.SetTimeLineElementWidthAccordingToText(timelineElement);
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
    
    public List<AssetActionSequence> GetSequences(List<ACTION_ENUM> actions)
    {
        List<AssetActionSequence> sequences = new();

        int startIndex = -1;
        ACTION_ENUM currentAction = ACTION_ENUM.UNDEFINED;

        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i] != ACTION_ENUM.NONE)
            {
                if (currentAction == ACTION_ENUM.UNDEFINED || currentAction == actions[i])
                {
                    if (currentAction == ACTION_ENUM.UNDEFINED)
                    {
                        currentAction = actions[i];
                        startIndex = i;
                    }
                }
                else
                {
                    sequences.Add(new AssetActionSequence
                    {
                        StartIndex = startIndex,
                        Length = i - startIndex,
                        ActionType = currentAction,
                    });

                    startIndex = i;
                    currentAction = actions[i];
                }
            }
            else if (currentAction != ACTION_ENUM.UNDEFINED)
            {
                sequences.Add(new AssetActionSequence
                {
                    StartIndex = startIndex,
                    Length = i - startIndex,
                    ActionType = currentAction
                });

                startIndex = -1;
                currentAction = ACTION_ENUM.UNDEFINED;
            }
        }

        if (currentAction != ACTION_ENUM.UNDEFINED)
        {
            sequences.Add(new AssetActionSequence
            {
                StartIndex = startIndex,
                Length = actions.Count - startIndex,
                ActionType = currentAction,
            });
        }

        return sequences;
    }

    public List<CollisionSequence> GetSequences(List<COLLISION_ENUM> actions)
    {
        List<CollisionSequence> sequences = new();

        int startIndex = -1;
        COLLISION_ENUM currentAction = COLLISION_ENUM.UNDEFINED;

        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i] != COLLISION_ENUM.NONE)
            {
                if (currentAction == COLLISION_ENUM.UNDEFINED || currentAction == actions[i])
                {
                    if (currentAction == COLLISION_ENUM.UNDEFINED)
                    {
                        currentAction = actions[i];
                        startIndex = i;
                    }
                }
                else
                {
                    sequences.Add(new CollisionSequence
                    {
                        StartIndex = startIndex,
                        Length = i - startIndex,
                        CollisionType = currentAction,
                    });

                    startIndex = i;
                    currentAction = actions[i];
                }
            }
            else if (currentAction != COLLISION_ENUM.UNDEFINED)
            {
                sequences.Add(new CollisionSequence
                {
                    StartIndex = startIndex,
                    Length = i - startIndex,
                    CollisionType = currentAction
                });

                startIndex = -1;
                currentAction = COLLISION_ENUM.UNDEFINED;
            }
        }

        if (currentAction != COLLISION_ENUM.UNDEFINED)
        {
            sequences.Add(new CollisionSequence
            {
                StartIndex = startIndex,
                Length = actions.Count - startIndex,
                CollisionType = currentAction,
            });
        }

        return sequences;
    }

    public List<AssetActionSequence> GenerateAssetActionSequences(RectTransform timelinePanel, Asset asset)
    {

        List<ACTION_ENUM> followActions = new()
            {
                ACTION_ENUM.FOLLOW_RIGHT_HAND,
                ACTION_ENUM.FOLLOW_LEFT_HAND,
                ACTION_ENUM.FOLLOW_L_FOCUS,
                ACTION_ENUM.FOLLOW_R_FOCUS,
                ACTION_ENUM.FOLLOW_G_FOCUS
        };
            
        DebugLogger.Instance.Log("Generating asset action sequences for " + asset.name);

        var recordedAssetFrames = currentActiveExample.assetFramesDict[asset]; 

        List<ACTION_ENUM> actionTypes = recordedAssetFrames.Select(x => x.ActionType).ToList();
        List<AssetActionSequence> sequences = GetSequences(actionTypes);
        foreach (AssetActionSequence sequence in sequences)
        {
            DebugLogger.Instance.Log("Sequence name: " + sequence.ActionType + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
            //DebugLogger.Instance.Log("Start x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex) + ", End x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex + sequence.Length));
            if (recordedAssetFrames[sequence.StartIndex].ActionDelegate != null)
            {
                sequence.ActionDelegate = recordedAssetFrames[sequence.StartIndex].ActionDelegate;
                DebugLogger.Instance.Log("Action delegate found: " + recordedAssetFrames[sequence.StartIndex].ActionDelegate.Method.Name + ", Parameters: " + string.Join(", ", recordedAssetFrames[sequence.StartIndex].ActionDelegate.Method.GetParameters().Select(x => x.Name)));
            }

            if (sequence.ActionType == ACTION_ENUM.HIDE)
            {
                GameObject hideElement = Instantiate(currentActiveExample.hideTimelinePanelPrefab, timelinePanel);
                hideElement.SetActive(true);
                hideElement.GetComponent<RectTransform>().anchoredPosition = new Vector2(TimelineUIElement.MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex, GetSizeOfMainRecordedData()), hideElement.GetComponent<RectTransform>().anchoredPosition.y);
            }
            else if (sequence.ActionType == ACTION_ENUM.SHOW)
            {
                GameObject showElement = Instantiate(currentActiveExample.showTimelinePanelPrefab, timelinePanel);
                showElement.SetActive(true);
                showElement.GetComponent<RectTransform>().anchoredPosition = new Vector2(TimelineUIElement.MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex, GetSizeOfMainRecordedData()), showElement.GetComponent<RectTransform>().anchoredPosition.y);
            }
            
            if(followActions.Contains(sequence.ActionType) || sequence.ActionType == ACTION_ENUM.APPLY_FORCE || sequence.ActionType == ACTION_ENUM.CHANGE_COLOR || sequence.ActionType == ACTION_ENUM.PIN) //Other types of events - physics, attach etc
            {                
                TimelineUIElement.CreateTimelineElement(currentActiveExample.assetTimelineElementPrefab, timelinePanel, sequence.StartIndex, sequence.Length, GetSizeOfMainRecordedData(), sequence.ToString());
            }
        }
        return sequences;
    }

    public List<CollisionSequence> GenerateCollisionSequences(RectTransform collisionTimelinePanelTransform, Asset asset) //Strong assumption that all sources of action come from collision
    {
        DebugLogger.Instance.Log("Generating collision sequences for " + asset.name);
        var recordedAssetFrames = currentActiveExample.assetFramesDict[asset]; 
        try
        {
            List<COLLISION_ENUM> collisionTypes = recordedAssetFrames.Select(x => x.CollisionType).ToList();
            List<CollisionSequence> sequences = GetSequences(collisionTypes);
            foreach (CollisionSequence sequence in sequences)
            {
                //DebugLogger.Instance.Log("Collision sequence name: " + sequence.Action + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
                //DebugLogger.Instance.Log("Start x: " + MapIndexToTimelinePosition(collisionTimelinePanelTransform, sequence.StartIndex) + ", End x: " + MapIndexToTimelinePosition(collisionTimelinePanelTransform, sequence.StartIndex + sequence.Length));

                if (recordedAssetFrames[sequence.StartIndex].CollisionDelegate != null)
                    DebugLogger.Instance.Log("Collision delegate found: " + recordedAssetFrames[sequence.StartIndex].CollisionDelegate.Method.Name + ", Parameters: " + string.Join(", ", recordedAssetFrames[sequence.StartIndex].CollisionDelegate.Method.GetParameters().Select(x => x.Name)));
                if (recordedAssetFrames[sequence.StartIndex].CollidedObject != null)
                {
                    DebugLogger.Instance.Log("Collided object: " + recordedAssetFrames[sequence.StartIndex].CollidedObject.name);
                    sequence.CollidingObject1 = asset.gameObject;
                    sequence.CollidingObject2 = recordedAssetFrames[sequence.StartIndex].CollidedObject;
                    sequence.CollisionType = recordedAssetFrames[sequence.StartIndex].CollisionType;
                }

                TimelineUIElement.CreateTimelineElement(currentActiveExample.collisionTimelineElementPrefab, collisionTimelinePanelTransform, sequence.StartIndex, sequence.Length, GetSizeOfMainRecordedData(), sequence.ToString());
            }
            return sequences;
        }
        catch (System.Exception e)
        {
            DebugLogger.Instance.LogException(e);
        }
        return null;
    }

    public void CreateTimelineRowsAndActions()
    {
        DebugLogger.Instance.Log("Creating assets timeline for example " + currentActiveExample.exampleId);

        int assetsCounter = 0;
        currentActiveExample.assetSequencesLists.Clear();
        foreach (var asset in currentActiveExample.assetFramesDict.Keys)
        {
            ++assetsCounter;
            GameObject timelinePanel = Instantiate(currentActiveExample.assetTimelinePanelPrefab, currentActiveExample.examplePlaybackPanel);
            timelinePanel.GetComponent<TimelineAssetRow>().AssetInstanceID = asset.GetInstanceID();
            timelinePanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(timelinePanel.GetComponent<RectTransform>().anchoredPosition.x, timelinePanel.GetComponent<RectTransform>().anchoredPosition.y - assetsCounter * 100);
            timelinePanel.SetActive(true);
            timelinePanel.GetComponent<RectTransform>().GetChild(0).GetComponent<TMPro.TMP_Text>().text = Manager.Instance.CleanAssetName(asset.name); //Assign asset name

            if (currentActiveExample.assetFramesDict[asset].Count > 0)
            {
                currentActiveExample.assetSequencesLists.Add(GenerateAssetActionSequences(timelinePanel.GetComponent<RectTransform>(), asset));

            }
        }
    }

    public void CreateTimelineAndStatePlaceholders(bool startHidden=true)
    {
        RecreateTimelineInputs();
        CreateTimelineRowsAndActions();
        
        CreateStatePlaceholders(startHidden);
    }

    public void RefreshTimelineVoiceSequences()
    {
        // refresh voice command sequences
        //TODO: not creating them all over again but just add the new one
        //Delete all existing voice command timeline elements
        for (int i = 4; i < currentActiveExample.voiceTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(currentActiveExample.voiceTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }
        
        currentActiveExample.VoiceCommandSequences = GenerateVoiceCommandSequences(currentActiveExample.voiceTimelinePanel);
        
        //refresh state placeholders 
        RefreshStatePlaceholders();
    }

    public void RecreateTimelineAssetRows()
    {
        //Refresh action events in the timeline
        DebugLogger.Instance.Log("Refresh assets timeline for example " + currentActiveExample.exampleId);

        foreach (var timelineAssetRow in currentActiveExample.GetTimelineAssetRows())
        {
            Destroy(timelineAssetRow);
        }

        int assetsCounter = 0;
        currentActiveExample.assetSequencesLists.Clear();
        foreach (var asset in currentActiveExample.assetFramesDict.Keys)
        {
            ++assetsCounter;
            CreateTimelineAssetRow(asset, assetsCounter);
        }
    }

    public void CreateTimelineAssetRow(Asset asset, int assetsCounter)
    {
        GameObject assetRow = Instantiate(currentActiveExample.assetTimelinePanelPrefab, currentActiveExample.examplePlaybackPanel);
        assetRow.GetComponent<TimelineAssetRow>().AssetInstanceID = asset.GetInstanceID();
        assetRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(assetRow.GetComponent<RectTransform>().anchoredPosition.x, assetRow.GetComponent<RectTransform>().anchoredPosition.y - assetsCounter * 100);
        assetRow.SetActive(true);
        assetRow.GetComponent<RectTransform>().GetChild(0).GetComponent<TMPro.TMP_Text>().text = Manager.Instance.CleanAssetName(asset.name); //Assign asset name

        CreateTimelineActionsForAsset(asset);
    }

    public void CreateTimelineActionsForAsset(Asset asset)
    {
        if (currentActiveExample.assetFramesDict[asset].Count > 0)
        {
            var assetRow = currentActiveExample.GetTimelineRowFor(asset);
            currentActiveExample.assetSequencesLists.Add(GenerateAssetActionSequences(assetRow.GetComponent<RectTransform>(), asset));
        }
        RefreshTimelineCollisions();
    }

    public void RefreshTimelineCollisions()
    {
        //Delete all existing collision timeline elements
        for (int i = 2; i < currentActiveExample.collisionTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(currentActiveExample.collisionTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }
        
        //Generate collision sequences
        currentActiveExample.collisionSequencesLists.Clear();
        foreach (var recordable in currentActiveExample.assetFramesDict.Keys)
        {
            if (currentActiveExample.assetFramesDict[recordable].Count > 0)
            {
                currentActiveExample.collisionSequencesLists.Add(GenerateCollisionSequences(currentActiveExample.collisionTimelinePanel.GetComponent<RectTransform>(), recordable));
            }
        }
    }

    public void RefreshTimelineGestures()
    {
        for (int i = 1; i < currentActiveExample.leftHandTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(currentActiveExample.leftHandTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }
        for (int i = 2; i < currentActiveExample.rightHandTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(currentActiveExample.rightHandTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }
        //Generate gesture sequences for left and right hand
        currentActiveExample.LeftHandGestureSequences = GenerateGestureSequences(currentActiveExample.leftHandTimelinePanel, currentActiveExample.activeLeftHandFrames);
        currentActiveExample.RightHandGestureSequences = GenerateGestureSequences(currentActiveExample.rightHandTimelinePanel,
                currentActiveExample.activeRightHandFrames);
    }

    /*
    public void RefreshTimelineAndStates()
    { 
        RefreshTimelineInputSequences();
        RecreateTimelineAssetRows();
        CreateStates();
        
        //TODO: call them only in live mode
        AddActionsToAllStates();
        CombineExamples();       
    } */

    public void CreateStateMachine()
    {
        int recordedFramesTotal = GetSizeOfMainRecordedData();

        var StateMachine = CustomStateMachine.Instance;
        StateMachine.DeleteAllStates();
        
        var localStatesDict = new Dictionary<StateTimelineUIElement,State>();

        foreach (var statePlaceholder in currentActiveExample.StatePlaceholders) {
            var newState = new State {
                name = "State " + StateMachine.GetSize()
            };
            StateMachine.AddState(newState.name, newState);

            localStatesDict.Add(statePlaceholder, newState);
        }

        var gestures = currentActiveExample.AllGestureSequences;
        var collisions = currentActiveExample.collisionSequencesLists.SelectMany(x => x).ToList();
        var voiceCommands = currentActiveExample.VoiceCommandSequences;

        List<Sequence> allPotentialTriggers = gestures.Cast<Sequence>()
                                  .Concat(collisions.Cast<Sequence>())
                                  .Concat(voiceCommands.Cast<Sequence>())
                                  .ToList();

        var allActions = currentActiveExample.assetSequencesLists;

        State firstState = null;

        for (int i = 0; i < currentActiveExample.StatePlaceholders.Count; i++) {
            var currentStatePlaceholder = currentActiveExample.StatePlaceholders[i];
            var nexStatePlaceholder = i < currentActiveExample.StatePlaceholders.Count - 1 ? currentActiveExample.StatePlaceholders[i + 1] : null;

            var currentState = localStatesDict[currentStatePlaceholder];
            if (i == 0) {
                firstState = currentState;
            }

            if (nexStatePlaceholder != null) {
                var nextState = localStatesDict[nexStatePlaceholder];

                //Find transitions between currentStatePlaceholder to nexStatePlaceholder
                var actualTriggers = allPotentialTriggers.FindAll(x => x.CanTriggerAt(nexStatePlaceholder.StartIndex));
                if (actualTriggers.Count > 0) {
                    //Let's build the transition
                    Func<Frame, bool> transitionConditionFunction = (Frame frame) => {return true;};
                    string transitionDescription = "" + currentState.name + "->" + nextState.name + ":";
                    foreach (var trigger in actualTriggers) {
                        transitionConditionFunction = trigger.AddConditionToFunction(transitionConditionFunction);
                        transitionDescription = transitionDescription + " && " + trigger.ToString();
                    }
                    //Let's add a transition between currentState and nextState
                    currentState.AddTransitionTo(nextState, transitionConditionFunction, transitionDescription);
                }
            } else {
                //currentStatePlaceholder is the last statePlaceholder
            }

            DebugLogger.Instance.Log("Adding OnEnter and OnExit actions to state " + currentState.name);

            //var stateInTimeline = currentActiveExample.StatesDict.First().Key;
            foreach (var assetSequences in allActions)
            {
                foreach (var assetSequence in assetSequences)
                {
                    if (assetSequence.StartIndex >= currentStatePlaceholder.StartIndex && 
                        assetSequence.StartIndex < currentStatePlaceholder.StartIndex+currentStatePlaceholder.Length)
                    {
                        int distanceToStateStart = Math.Abs(assetSequence.StartIndex - currentStatePlaceholder.StartIndex);
                        int distanceToStateEnd = Math.Abs(currentStatePlaceholder.StartIndex + currentStatePlaceholder.Length - assetSequence.StartIndex - 1);

                        if(distanceToStateStart < distanceToStateEnd)
                        {
                            DebugLogger.Instance.Log("Adding action(s) " + assetSequence.ActionType + " for state " + currentState.name + " in OnEnterActions");
                            currentState.OnEnterActions += () => assetSequence.ActionDelegate();
                            currentState.OnEnterActionsStr += assetSequence.ActionType.ToString()+ " ";
                        }
                        else
                        {
                            DebugLogger.Instance.Log("Adding action(s) " + assetSequence.ActionType + " for state " + currentState.name + " in OnExitActions");
                            currentState.OnExitActions += () => assetSequence.ActionDelegate();
                            currentState.OnExitActionsStr += assetSequence.ActionType.ToString() + " ";
                        }
                    }
                }
            }
        }

        CustomStateMachine.Instance.SetInitialState(firstState);

        PrintDetailsOfStateMachine(localStatesDict.Values.ToList());    
    }

    // public void AddActionsToAllStates()
    // {
    //     DebugLogger.Instance.Log("Adding OnEnter and OnExit actions to states");

    //     List<State> orderedKeys = new(currentActiveExample.StatesDict.Keys);

    //     if (orderedKeys.Count == 0)
    //     {
    //         DebugLogger.Instance.Log("No states found");
    //         return;
    //     }

    //     var firstState = currentActiveExample.StatesDict[orderedKeys[0]].state;
    //     AddActionsToState(firstState);

    //     for (int i = 0; i < orderedKeys.Count - 1; i++)
    //     {   
    //         State currStateProcessed = currentActiveExample.StatesDict[orderedKeys[i + 1]].state;

    //         AddActionsToState(currStateProcessed);
    //     }

    // }

    // public void AddActionsToState(State stateInTimeline)
    // {
    //     var actions = currentActiveExample.assetSequencesLists;
    //     //var stateInTimeline = currentActiveExample.StatesDict.First().Key;
    //     foreach (var assetSequences in actions)
    //     {
    //         foreach (var assetSequence in assetSequences)
    //         {
    //             int distanceToStateStart = assetSequence.StartIndex - currentActiveExample.StatesDict[stateInTimeline].StartIndex;
    //             int distanceToStateEnd = currentActiveExample.StatesDict[stateInTimeline].StartIndex + currentActiveExample.StatesDict[stateInTimeline].Length - assetSequence.StartIndex - 1;
    //             if (distanceToStateStart >= 0 && distanceToStateEnd >= 0)
    //             {
    //                 if(distanceToStateStart < distanceToStateEnd)
    //                 {
    //                     DebugLogger.Instance.Log("Adding action(s) " + assetSequence.ActionType + " for state " + stateInTimeline.name + " in OnEnterActions");
    //                     stateInTimeline.OnEnterActions = () => assetSequence.ActionDelegate();
    //                     stateInTimeline.OnEnterActionsStr = assetSequence.ActionType.ToString();
    //                 }
    //                 else
    //                 {
    //                     DebugLogger.Instance.Log("Adding action(s) " + assetSequence.ActionType + " for state " + stateInTimeline.name + " in OnExitActions");
    //                     stateInTimeline.OnExitActions = () => assetSequence.ActionDelegate();
    //                     stateInTimeline.OnExitActionsStr = assetSequence.ActionType.ToString();
    //                 }
    //             }
    //         }
    //     }
    // }



    public void CombineExamples()
    {
        // DebugLogger.Instance.ClearVRDebugText();

        // //DebugLogger.Instance.Log("Combining examples");
        // List<List<State>> allStatesInExamples = new();
        // foreach (var example in examples)
        // {
        //     allStatesInExamples.Add(example.StatesDict.Keys.ToList());
        // }
       
        // allStatesInExamples = allStatesInExamples.OrderBy(x => x.Count).ToList();

        // var shortestList = allStatesInExamples.First();
        // int shortestListLength = shortestList.Count;
        // DebugLogger.Instance.Log("CombineExamples: The shortest list of states has " + shortestList.Count + " states");

        // List<State> commonStates = new List<State>();
        // int lastCommonStateIndex = 0;
        // for (int i = 0; i < shortestListLength; i++)
        // {
        //     lastCommonStateIndex = i;
        //     bool allStatesEqual = true;
        //     for (int j = 0; j < allStatesInExamples.Count; j++)
        //     {
        //         if (!shortestList[i].IsStateEqualTo(allStatesInExamples[j][i]))
        //         {
        //             allStatesEqual = false;
        //             break;
        //         }
        //     }
            
        //     if (allStatesEqual)
        //     {
        //         commonStates.Add(shortestList[i]);
        //     }
        //     else
        //     {
        //         break;
        //     }
        // }

        // if(lastCommonStateIndex == 0)
        // {
        //     DebugLogger.Instance.Log("CombineExamples: No common states found");
        //     return;
        // }

        // DebugLogger.Instance.Log("CombineExamples: The last common state is " + commonStates.Last().name + " at index " + lastCommonStateIndex);

        // if (lastCommonStateIndex < shortestListLength)
        // { 
        //     //Printing the states at lastCommonStateIndex-1 for each List<State> in allStatesInExamples except the first
        //     DebugLogger.Instance.Log("CombineExamples: States at index " + (lastCommonStateIndex - 1) + " for each example except the first: ");
        //     for (int i = 1; i < allStatesInExamples.Count; i++) //Except the first list. i.e. the shortest list which has already been added
        //     {
        //         DebugLogger.Instance.Log("CombineExamples: Inside for loop");
        //         commonStates.Add(shortestList[lastCommonStateIndex]); //Add the state after the last common state
        //         //allStatesInExamples[i][lastCommonStateIndex].PrintDetailsOfState(VRConsoleEnabled : true);            
        //         commonStates.Last().CopyTransitionFromState(allStatesInExamples[i][lastCommonStateIndex]);
        //         //DebugLogger.Instance.Log("Transition copied from " + allStatesInExamples[i][lastCommonStateIndex].id + " to " + commonStates.Last().id);
        //         //DebugLogger.Instance.Log("Details of allStatesInExamples[" + i + "][" + lastCommonStateIndex + "]: ");
        //         //allStatesInExamples[i][lastCommonStateIndex].PrintDetailsOfState(VRConsoleEnabled : true);
        //         //DebugLogger.Instance.Log("Details of commonStates.Last(): ", VRConsoleEnabled : true);
        //         //commonStates.Last().PrintDetailsOfState(VRConsoleEnabled : true);

        //     }

        //     CustomStateMachine.Instance.DeleteAllStates();
        //     int newId = 0;
        //     foreach (var state in commonStates)
        //     {
        //         CustomStateMachine.Instance.AddState("State " + newId.ToString(), state);
        //         newId++;
        //     }

        //     for (int i = 0; i < allStatesInExamples.Count; i++)
        //     {
        //         //Copy all states from lastCommonStateIndex to the end of each list
        //         for (int j = lastCommonStateIndex + 1; j < allStatesInExamples[i].Count; j++)
        //         {
        //             CustomStateMachine.Instance.AddState("State " + newId.ToString(), allStatesInExamples[i][j]);
        //             newId++;
        //         }
        //     }


        //     //Print the common states          
            

        //     CustomStateMachine.Instance.CreateStateGraph(stateGraphElementPrefab, stateGraphPanel.GetComponent<RectTransform>());

        // }
        
        // CustomStateMachine.Instance.PrintDetailsOfStateMachine(VRConsoleEnabled : true);

        // //CustomStateMachine.Instance.SetInitialState(commonStates.First().id);

    }

    public void RefreshStatePlaceholders()
    {
        //TODO
        CreateStatePlaceholders();
    }
    
    public void CreateStatePlaceholders(bool startHidden=false) {
        DeleteStatePlaceholders();
        int recordedFramesTotal = GetSizeOfMainRecordedData();
        var gestures = currentActiveExample.AllGestureSequences;
        var collisions = currentActiveExample.collisionSequencesLists.SelectMany(x => x).ToList();
        var voiceCommands = currentActiveExample.VoiceCommandSequences;
        // Create a list of events (start or end of a sequence)
        var eventsThatStartStates = new List<(int Index, string Type, GestureSequence Gesture, CollisionSequence Collision, VoiceSequence VoiceCommand)>();
        foreach (var gesture in gestures)
        {
            eventsThatStartStates.Add((gesture.StartIndex, "start", gesture, null, null));
        }
        foreach (var collision in collisions)
        {
            eventsThatStartStates.Add((collision.StartIndex, "start", null, collision, null));
        }
        foreach (var voiceCommand in voiceCommands)
        {
            eventsThatStartStates.Add((voiceCommand.StartIndex, "start", null, null, voiceCommand));
        }
        // Sort the events by their index
        eventsThatStartStates = eventsThatStartStates.OrderBy(e => e.Index).ToList();
        int lastIndex = 0;
        // Iterate over events to create states
        for (int i = 0; i < eventsThatStartStates.Count; i++) {
            var currentEvent = eventsThatStartStates[i];
            if (lastIndex != currentEvent.Index) {
                var startIndex = lastIndex;
                var length = currentEvent.Index - lastIndex;
                var stateTimelineElement = StateTimelineUIElement.CreateStateTimelineElement(
                    currentActiveExample.stateTimelineElementPrefab,
                    currentActiveExample.statesTimelinePanel.GetComponent<RectTransform>(),
                    startIndex,
                    length,
                    GetSizeOfMainRecordedData(),
                    "State " + currentActiveExample.StatePlaceholders.Count);
                this.currentActiveExample.StatePlaceholders.Add(stateTimelineElement.GetComponent<StateTimelineUIElement>());
            }
            lastIndex = currentEvent.Index;
        }
        // Last state
        if (lastIndex < recordedFramesTotal) {
            var startIndex = lastIndex;
            var length = recordedFramesTotal - lastIndex;
            var stateTimelineElement = StateTimelineUIElement.CreateStateTimelineElement(
                currentActiveExample.stateTimelineElementPrefab,
                currentActiveExample.statesTimelinePanel.GetComponent<RectTransform>(),
                startIndex,
                length,
                GetSizeOfMainRecordedData(),
                "State " + currentActiveExample.StatePlaceholders.Count);
            this.currentActiveExample.StatePlaceholders.Add(stateTimelineElement.GetComponent<StateTimelineUIElement>());
        }
        if (startHidden)
        {
            foreach (var statePlaceholder in  this.currentActiveExample.StatePlaceholders)
            {
                statePlaceholder.gameObject.SetActive(false);
            }
        }

    }        

    //Dictionary<State, StateTimelineUIElement> StatesDict = new();
    // State CreateState(int startIndex, int length)
    // {
    //     var StateMachine = CustomStateMachine.Instance;
    //     var state = new State
    //     {
    //         name = "State " + StateMachine.GetSize()
    //     };
    //     StateMachine.AddState(state.name, state);

    //     var stateUI = StateTimelineUIElement.CreateStateTimelineElement(currentActiveExample.stateTimelineElementPrefab, currentActiveExample.statesTimelinePanel.GetComponent<RectTransform>(),
    //                                                     startIndex, length, GetSizeOfMainRecordedData(), state);
        
    //     state.timelineElement = stateUI;

    //     currentActiveExample.StatesDict.Add(state, stateUI.GetComponent<StateTimelineUIElement>());

    //     return state;
    // }

    State MergeStates(State state1, State state2)
    {
        return null;
    //     var state1UIElement = currentActiveExample.StatesDict[state1];
    //     var state2UIElement = currentActiveExample.StatesDict[state2];


    //     var StateMachine = CustomStateMachine.Instance;
    //     var newState = new State
    //     {
    //         name = "tempState" + StateMachine.GetSize()
    //     };

    //     //Get the index of state1 in the StatesInTimeline dictionary
    //     int state1Index = currentActiveExample.StatesDict.Keys.ToList().IndexOf(state1);
    //     //Find the state before state1 in the StatesInTimeline dictionary if the index of state1 is not 0
    //     State previousState = null;
    //     if (state1Index > 0)
    //     {
    //         previousState = currentActiveExample.StatesDict.Keys.ToList()[state1Index - 1];
    //         DebugLogger.Instance.Log("The state before state " + state1.name + " is state " + previousState.name);
    //         //Add a transition from the previous state to the new state
    //         previousState.ModifyTransitionTo(newState);
    //         //previousState.transitions.Clear();  
    //     }
    //     else
    //     {
    //         DebugLogger.Instance.Log("State " + state1.name + " is the first state");
    //     }

    //     State nextState = null;
    //     //Find the state after state2 in the StatesInTimeline dictionary if the index of state2 is not the last index
    //     if (state1Index < currentActiveExample.StatesDict.Keys.Count - 1)
    //     {
    //         nextState = currentActiveExample.StatesDict.Keys.ToList()[state1Index + 1];
    //         DebugLogger.Instance.Log("The state after state " + state2.name + " is state " + nextState.name);

    //         newState.ClearTransitions();
    //         newState.CopyTransitionFromState(state2);

    //         //Add a transition from the new state to the next state
    //         newState.ModifyTransitionTo(nextState);
    //         //newState.transitions.Clear();
    //     }
    //     else
    //     {
    //         DebugLogger.Instance.Log("State " + state2.name + " is the last state");
    //     }


    //     newState.OnEnterActions = state1.OnEnterActions;
    //     newState.OnEnterActions = state2.OnEnterActions;
    //     newState.OnExitActions = state1.OnExitActions; //We only have unfollow so this is fine for now TODO: Fix this for other use cases
    //     newState.OnExitActions = state2.OnExitActions;


    //     //Assign state.id as "State" plus the digits present in state1.id and state2.id
    //     string state1ID = state1.name;
    //     string state2ID = state2.name;
    //     string stateID = "State";
    //     foreach (char c in state1ID)
    //     {
    //         if (char.IsDigit(c))
    //         {
    //             stateID += c;
    //         }
    //     }
    //     foreach (char c in state2ID)
    //     {
    //         if (char.IsDigit(c))
    //         {
    //             stateID += c;
    //         }
    //     }
    //     newState.name = stateID;

    //     var stateUI = StateTimelineUIElement.CreateStateTimelineElement(currentActiveExample.stateTimelineElementPrefab, currentActiveExample.statesTimelinePanel.GetComponent<RectTransform>(),
    //                                                     state1UIElement.StartIndex, state1UIElement.Length + state2UIElement.Length, GetSizeOfMainRecordedData(), newState);

    //     newState.timelineElement = stateUI;
    //     state1UIElement = stateUI.GetComponent<StateTimelineUIElement>();

    //     DebugLogger.Instance.Log("Merged states " + state1.name + " and " + state2.name + " to create state " + newState.name);
    //     //DebugLogger.Instance.Log("Size of new state: " + state1UIElement.Length);

    //     //Remove state1 and state2 from the state machine
    //     StateMachine.DeleteState(state1.name);
    //     StateMachine.DeleteState(state2.name);
    //     //Remove state1 and state2 from the state timeline
    //     Destroy(currentActiveExample.StatesDict[state1].gameObject);
    //     Destroy(currentActiveExample.StatesDict[state2].gameObject);
    //     currentActiveExample.StatesDict.Remove(state1);
    //     currentActiveExample.StatesDict.Remove(state2);

    //     //Insert the new state into the location of state1

    //     //StatesInTimeline.Add(state, stateUI.GetComponent<StateTimelineUIElement>());

    //     //Iterate through the StatesInTimeline dictionary keys and change the key to be "State" + index of the key
    //     /*Dictionary<State, StateTimelineUIElement> newStatesInTimeline = new Dictionary<State, StateTimelineUIElement>();
    //     foreach (var stateInTimeline in StatesInTimeline)
    //     {
    //         State newState = new State();
    //         newState.id = "State" + newStatesInTimeline.Count;
    //         newStatesInTimeline.Add(newState, stateInTimeline.Value);
    //     }
    //     StatesInTimeline = newStatesInTimeline;*/

    //     //state.id = "State" + StatesInTimeline.Count;
    //     currentActiveExample.StatesDict.Add(newState, stateUI.GetComponent<StateTimelineUIElement>());
    //     StateMachine.AddState(newState.name, newState);

    //     //StateMachine.SetInitialState(currentActiveExample.StatesDict.First().Key.id);
    //     AddActionsToAllStates();
    //     CombineExamples();
    //     StateMachine.SetInitialState(currentActiveExample.StatesDict.First().Key.name);

    //     return newState;
    }


    public void ResetExampleStateMachine()
    {
        // currentActiveExample.StatesDict.Clear();

        //Delete all existing states
        CustomStateMachine.Instance.DeleteAllStates();

        //Delete all existing state timeline UI elements
        DeleteStatePlaceholders();
    }

    public void DeleteStatePlaceholders()
    {
        for (int i = 2; i < currentActiveExample.statesTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(currentActiveExample.statesTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }
        currentActiveExample.StatePlaceholders.Clear();
    }





    public void ResetStateMachine()
    {
        //Find the id of the first state in the state machine
        
        //CustomStateMachine.Instance.SetInitialState("State 0");
        // CustomStateMachine.Instance.SetInitialState(currentActiveExample.StatesDict.First().Key.name);
    }


    public void PrintDetailsOfStateMachine(List<State> states)
    {
        DebugLogger.Instance.ClearVRDebugText();

        CustomStateMachine.Instance.PrintDetailsOfStateMachine(VRConsoleEnabled : true);

        DebugLogger.Instance.Log("Printing details of state machine");

        // foreach(var example in examples)
        // {
        //     DebugLogger.Instance.Log("Example " + example.exampleId);
        //     foreach (var state in states)
        //     {
        //         state.PrintDetailsOfState(VRConsoleEnabled: true);
        //     }
        // }
        foreach (var state in states)
        {
            state.PrintDetailsOfState(VRConsoleEnabled: true);
        }
    }

    public void MergeToLeftState(StateTimelineUIElement selectedStateUIElement)
    {
        var previousIndex = currentActiveExample.StatePlaceholders.IndexOf(selectedStateUIElement) - 1;

        if (previousIndex > 0 && previousIndex < currentActiveExample.StatePlaceholders.Count) {
            var previousStateUIElement = currentActiveExample.StatePlaceholders[previousIndex];
            DebugLogger.Instance.Log("Merging state " + selectedStateUIElement.name + " with state " + previousStateUIElement.name);
            previousStateUIElement.MergeWithStateUIElement(selectedStateUIElement);
        } else {
            DebugLogger.Instance.Log("Cannot merge state " + selectedStateUIElement.name + " with state to the left because it is the first state");
        }
    }
 
    public void MergeToRightState(StateTimelineUIElement selectedStateUIElement)
    {
        var nextIndex = currentActiveExample.StatePlaceholders.IndexOf(selectedStateUIElement) + 1;

        if (nextIndex > 0 && nextIndex < currentActiveExample.StatePlaceholders.Count) {
            var nextStateUIElement = currentActiveExample.StatePlaceholders[nextIndex];
            DebugLogger.Instance.Log("Merging state " + selectedStateUIElement.name + " with state " + nextStateUIElement.name);
            selectedStateUIElement.MergeWithStateUIElement(nextStateUIElement);

        } else {
            DebugLogger.Instance.Log("Cannot merge state " + selectedStateUIElement.name + " with state to the left because it is the first state");
        }
    }


    int frameCount = 0;

    private void FixedUpdate()
    {
        if (Manager.Instance.currAppState == Manager.AppState.RECORDING)
        {
            ++frameCount;

            head.Record(frameCount);
            leftHand.Record(frameCount, currentActiveExample.leftHandFrames);
            rightHand.Record(frameCount, currentActiveExample.rightHandFrames);

        }
        else if (Manager.Instance.currAppState == Manager.AppState.PLAYBACK || Manager.Instance.currAppState == Manager.AppState.RECORDING_DURING_PLAYBACK)
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
                head.playbackObject.transform.SetLocalPositionAndRotation(currentActiveExample.headFrames[currentFrameNum].rootPosition, currentActiveExample.headFrames[currentFrameNum].rootRotation);
                head.playbackFocusSquare.transform.SetPositionAndRotation(currentActiveExample.headFrames[currentFrameNum].focusSquarePosition, currentActiveExample.headFrames[currentFrameNum].focusSquareRotation);
            }

            if (leftHand.playbackObject != null)
            {
                leftHand.playbackObject.transform.SetLocalPositionAndRotation(currentActiveExample.leftHandFrames[currentFrameNum].rootPosition, currentActiveExample.leftHandFrames[currentFrameNum].rootRotation * Quaternion.Euler(leftHand.rotationCorrection));
                if (leftHand.playbackObject.GetComponent<HandPlaybackObjectScript>() != null)
                {
                    leftHand.playbackObject.GetComponent<HandPlaybackObjectScript>().SetPoseForAllFingerJoints(currentActiveExample.leftHandFrames[currentFrameNum]);
                    leftHand.playbackGestureText.text = InputManager.Instance.GestureToString(currentActiveExample.leftHandFrames[currentFrameNum].gesture);
                }

                leftHand.playbackFocusSquare.transform.SetPositionAndRotation(currentActiveExample.leftHandFrames[currentFrameNum].focusSquarePosition, currentActiveExample.leftHandFrames[currentFrameNum].focusSquareRotation);
            }

            if (rightHand.playbackObject != null)
            {
                rightHand.playbackObject.transform.SetLocalPositionAndRotation(currentActiveExample.rightHandFrames[currentFrameNum].rootPosition, currentActiveExample.rightHandFrames[currentFrameNum].rootRotation * Quaternion.Euler(rightHand.rotationCorrection));
                if (rightHand.playbackObject.GetComponent<HandPlaybackObjectScript>() != null)
                {
                    rightHand.playbackObject.GetComponent<HandPlaybackObjectScript>().SetPoseForAllFingerJoints(currentActiveExample.rightHandFrames[currentFrameNum]);
                    rightHand.playbackGestureText.text = InputManager.Instance.GestureToString(currentActiveExample.rightHandFrames[currentFrameNum].gesture);
                }

                rightHand.playbackFocusSquare.transform.SetPositionAndRotation(currentActiveExample.rightHandFrames[currentFrameNum].focusSquarePosition, currentActiveExample.rightHandFrames[currentFrameNum].focusSquareRotation);
            }

        }

    }
}





