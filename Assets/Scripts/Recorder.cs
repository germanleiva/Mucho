using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
//using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

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
        CustomStateMachine.Instance.stateMachineModel = new StateMachineModel(currentActiveExample);
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
        RecreateTimelineUI_Inputs();
        
        //Delete all existing asset rows and create the new ones
        RecreateTimelineUI_AssetRows();
    }

    public void RecreateTimelineUI_Inputs()
    {
        RecreateTimelineUI_Gestures();
        RecreateTimelineUI_VoiceCommands();
        RecreateTimelineUI_Collisions();
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

        currentActiveExample.CleanDataBeforeRecording();
        //AssetManager.Instance.ShowMiscObjs();
        AssetManager.Instance.SetAllAssetMenusPokeable(false);

        recordStartFrame = 0;//Time.time;
        _latestRecordedFrameIndex = 0;
        //RefreshTimelineAndStates(); 
    }



    // Stop recording.
    public void StopRecording()
    {
        rootPlaybackArea.SetActive(true);

        DebugLogger.Instance.Log("Size of recordedData head: " + currentActiveExample.headFrames.Count);
        DebugLogger.Instance.Log("Size of recordedData leftHand: " + currentActiveExample.leftHandFrames.Count);
        DebugLogger.Instance.Log("Size of recordedData rightHand: " + currentActiveExample.rightHandFrames.Count);

        foreach(var asset in currentActiveExample.assetsDict.Keys)
        {
            DebugLogger.Instance.Log("Size of recordedData asset: " + asset.name + ", " + currentActiveExample.assetsDict[asset].assetFrames.Count);
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
        RecreateTimelineUIAndStatePlaceholders();   

        PreparePlayback();
        
        UpdateAllAssetFramesAndCollisions(0, currentActiveExample, RecreateTimelineUI_Collisions);
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
        int threshHold = currentActiveExample.RecordedDataCount / 50;

        int nearestStartIndex = int.MaxValue;
        int nearestEndIndex = int.MaxValue;

        foreach (Sequence sequence in currentActiveExample.AllGestureSequences.Concat<Sequence>(currentActiveExample.activeCollisionModels.Concat<Sequence>(currentActiveExample.VoiceCommandSequences)))
        {
            if (Mathf.Abs(currentFrameNum - sequence.StartIndex) < threshHold)
            {
                nearestStartIndex = sequence.StartIndex;
            }
            if (Mathf.Abs(currentFrameNum - (sequence.StartIndex + sequence.Length)) < threshHold)
            {
                nearestEndIndex = sequence.StartIndex + sequence.Length;
            }
        }

        // Determine if the start or end index is closer to the current frame number
        int nearestIndex = Mathf.Abs(currentFrameNum - nearestStartIndex) < Mathf.Abs(currentFrameNum - nearestEndIndex)
                        ? nearestStartIndex
                        : nearestEndIndex;

        if (nearestIndex == int.MaxValue)
        {
            nearestIndex = currentFrameNum;
        }

        playbackSlider.value = nearestIndex;
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
        return currentActiveExample.RecordedDataCount;
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

    public void RecreateTimelineUIAndStatePlaceholders(bool startHidden=true)
    {
        RecreateTimelineUI_Inputs();
        RecreateTimelineUI_AssetRows();
        
        RecreateTimelineUI_StatePlaceholders(startHidden);
    }

    public void RecreateTimelineUI_VoiceCommands()
    {
        // refresh voice command sequences
        //TODO: not creating them all over again but just add the new one
        //Delete all existing voice command timeline elements
        for (int i = 4; i < currentActiveExample.voiceTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(currentActiveExample.voiceTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }
        
        foreach (VoiceSequence sequence in currentActiveExample.VoiceCommandSequences)
        {
            DebugLogger.Instance.Log("Voice sequence name: " + sequence.VoiceCommand + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
            TimelineUIElement.CreateTimelineElement(currentActiveExample.voiceCommandTimelineElementPrefab, currentActiveExample.voiceTimelinePanel, GetSizeOfMainRecordedData(), sequence);
        }
    }
    
    public void UpdateAllAssetFramesAndCollisions(int updateFrameStart, Example example, Action onCompletionDelegate = null)
    {
        //TODO for now this update should focus on the receiver asset values and not other assets, but physic simulations might make this action to affect other assets
        //TODO use the updateFrameStart so we update only the FrameStart > updateFrameStart
        
        //Create a dictionary where the key is an indexFrame and the value is the corresponding assetAction
        var allActionsGroupedByFrames = new Dictionary<int, List<AssetActionSequence>>();

        example.prepareForSimulation();
        
        allAssets.ForEach(asset => {
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
        
        StartCoroutine(SimulateAssetFramesAndCollisions(example, allActionsGroupedByFrames, onCompletionDelegate));
        
    }
    
    private IEnumerator SimulateAssetFramesAndCollisions(Example example,
        Dictionary<int, List<AssetActionSequence>> actionsToPerformGroupedByFrames, Action onCompletionDelegate)
    {
        var oldState = Manager.Instance.currAppState;
        var oldActiveExample = currentActiveExample;
        var oldTimeScale = Time.timeScale;
        var oldPlaybackSliderValue = playbackSlider.value;
        var oldGravity = Physics.gravity;

        Time.timeScale = 10f; // Moderate speed boost (10x)
        Time.fixedDeltaTime = 0.02f / Time.timeScale; // Adjust physics step
        Physics.gravity = oldGravity * (0.02f / Time.fixedDeltaTime) * Time.timeScale;
        
        currentActiveExample = example;
        
        Manager.Instance.currAppState = Manager.AppState.SIMULATING;
        
        InputManager.Instance.SetPlaybackObjectsActive(true);
        AssetManager.Instance.HideMiscObjs();

        for (int frameIndex = 0; frameIndex < example.RecordedDataCount; frameIndex++)
        {
            //DebugLogger.Instance.Log("Updating slider from SIMULATING " + frameIndex);
            playbackSlider.value = frameIndex;

            if (actionsToPerformGroupedByFrames.TryGetValue(frameIndex, out var actionsToSimulate))
            {
                foreach (var anAction in actionsToSimulate)
                {
                    // if (frameIndex == anAction.FrameStart) This check should be unnecesary
                    anAction.ActionDelegate(); //This will execute the action and any related collision
                }
            } else 
            {
                //Debug.Log("There were no actions to simulate on frame " + frameIndex);
            }
            //We pause the execution of this routine to let Unity send the collision events: OnCollisionEnter, OnCollisionStay, OnCollisionExit
            //Collisions are saved in the corresponding model Example
            
            yield return new WaitForFixedUpdate(); //new WaitForSeconds(0.01f);
            
            allAssets.ForEach(asset =>
            {
                asset.SaveMainVisualValuesIn(example.assetsDict[asset].assetFrames[frameIndex]);
            });
        }
        
        yield return new WaitForFixedUpdate(); //new WaitForSeconds(0.01f);
        //TODO Set the frameEnd of all the unclosed collisions to the final frame of the recorded data
        // foreach (var collisionModelWithoutFrameEnd in example.CollisionModelsWithoutFrameEnd())
        // {
        //     collisionModelWithoutFrameEnd.Length = (example.RecordedDataCount - 1) - collisionModelWithoutFrameEnd.StartIndex;
        // }
        
        InputManager.Instance.SetPlaybackObjectsActive(false);
        Manager.Instance.currAppState = oldState;
        currentActiveExample = oldActiveExample;

        Time.timeScale = oldTimeScale; // Default (1x)
        Time.fixedDeltaTime = 0.02f / Time.timeScale; // Adjust physics step
        Physics.gravity = oldGravity;
        
        playbackSlider.value = oldPlaybackSliderValue;
        
        //Let's clean up collision that override other collisions
        var CollisionsToRemove = new List<CollisionSequence>();
        foreach (var currentCollision in currentActiveExample.CollisionModels)
        {
            if ( currentActiveExample.CollisionModels.Exists(existingCollision => existingCollision != currentCollision && currentCollision.IsOverridenBy(existingCollision)))
            {
                CollisionsToRemove.Add(currentCollision);
            }
        }

        foreach (var collisionToRemove in CollisionsToRemove)
        {
            currentActiveExample.CollisionModels.Remove(collisionToRemove);
        }
        
        if (onCompletionDelegate != null)
        { 
            onCompletionDelegate();
        }
        // Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        // InputManager.Instance.SetPlaybackObjectsActive(false);
        // //DebugLogger.Instance.Log("AddAssetFrameToAssetFramesDict: DoRecordSizesMatch() - " + DoRecordSizesMatch());
        //     
        // Recorder.Instance.RecreateTimelineAssetRows();
        //     
        // Recorder.Instance.RecreateCollisionsInTimeline();

    }

    public void RecreateTimelineUI_AssetRowsAndCollisions()
    {
        UpdateAllAssetFramesAndCollisions(0, currentActiveExample, () =>
        {
            RecreateTimelineUI_Collisions();
            RecreateTimelineUI_AssetRows();
        });
    }
    
    public void RecreateTimelineUI_AssetRows()
    {
        //Refresh action events in the timeline
        DebugLogger.Instance.Log("Deleting assets row in timeline for example " + currentActiveExample.exampleId);

        foreach (var timelineAssetRow in currentActiveExample.GetTimelineAssetRows())
        {
            //Destroy() is async so we need to disable the usage of timelineAssetRow, so it is not confused with the new one
            timelineAssetRow.GetComponent<TimelineAssetRow>().AssetInstanceID = 0;
            Destroy(timelineAssetRow);
        }

        DebugLogger.Instance.Log("Recreating assets row in timeline for example " + currentActiveExample.exampleId);

        for (int i = 0; i < allAssets.Count; i++)
        {
            CreateTimelineUI_AssetRow(allAssets[i], i + 1);
        }
    }

    public void CreateTimelineUI_AssetRow(Asset asset, int assetsCounter)
    {
        GameObject assetRow = Instantiate(currentActiveExample.assetTimelinePanelPrefab, currentActiveExample.examplePlaybackPanel);
        assetRow.GetComponent<TimelineAssetRow>().AssetInstanceID = asset.GetInstanceID();
        assetRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(assetRow.GetComponent<RectTransform>().anchoredPosition.x, assetRow.GetComponent<RectTransform>().anchoredPosition.y - assetsCounter * 100);
        assetRow.SetActive(true);
        assetRow.GetComponent<RectTransform>().GetChild(0).GetComponent<TMPro.TMP_Text>().text = Manager.Instance.CleanAssetName(asset.name); //Assign asset name

        CreateTimelineUI_ActionsForAsset(asset, currentActiveExample.assetsDict[asset].assetActions);
    }

    public void RecreateTimelineUI_ActionsForAsset(Asset asset)
    {
        var assetRow = currentActiveExample.GetTimelineRowFor(asset);
        
        //Delete all existing action timeline elements
        for (int i = 4; i < assetRow.transform.childCount; i++)
        {
            Destroy(assetRow.transform.GetChild(i).gameObject);
        }

        CreateTimelineUI_ActionsForAsset(asset, currentActiveExample.assetsDict[asset].assetActions);
    }

    public void CreateTimelineUI_ActionsForAsset(Asset asset, List<AssetActionSequence> assetActions)
    {
        var assetRow = currentActiveExample.GetTimelineRowFor(asset);
        
        var timelinePanel = assetRow.GetComponent<RectTransform>();
        foreach (var assetAction in assetActions)
        {
            if (assetAction.shouldShowInTimeline)
            {
                TimelineUIElement.CreateTimelineElement(currentActiveExample.assetTimelineElementPrefab, timelinePanel,
                    currentActiveExample.RecordedDataCount, assetAction);
            }
        }
    }

    public void RecreateTimelineUI_Collisions()
    {
        //Delete all existing collision timeline elements
        for (int i = 2; i < currentActiveExample.collisionTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(currentActiveExample.collisionTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }
        
        //Generate collision sequences
        foreach (var collisionModel in currentActiveExample.activeCollisionModels)
        {
            var collisionTimelinePanelTransform =
                    currentActiveExample.collisionTimelinePanel.GetComponent<RectTransform>();
                TimelineUIElement.CreateTimelineElement(currentActiveExample.collisionTimelineElementPrefab, collisionTimelinePanelTransform, GetSizeOfMainRecordedData(), collisionModel);
        }
    }

    public void RecreateTimelineUI_Gestures()
    {
        for (int i = 1; i < currentActiveExample.leftHandTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(currentActiveExample.leftHandTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }
        for (int i = 2; i < currentActiveExample.rightHandTimelinePanel.GetComponent<RectTransform>().childCount; i++)
        {
            Destroy(currentActiveExample.rightHandTimelinePanel.GetComponent<RectTransform>().GetChild(i).gameObject);
        }
        
        foreach (GestureSequence sequence in currentActiveExample.LeftHandGestureSequences)
        {
            TimelineUIElement.CreateTimelineElement(currentActiveExample.handTimelineElementPrefab, currentActiveExample.leftHandTimelinePanel, GetSizeOfMainRecordedData(), sequence);
        }
        foreach (GestureSequence sequence in currentActiveExample.RightHandGestureSequences)
        {
            TimelineUIElement.CreateTimelineElement(currentActiveExample.handTimelineElementPrefab, currentActiveExample.rightHandTimelinePanel, GetSizeOfMainRecordedData(), sequence);
        }
    }

    public void CreateStateMachine()
    {
        var stateMachineModel = StateMachineModel.CreateStateMachine(currentActiveExample);
        CustomStateMachine.Instance.stateMachineModel = stateMachineModel;
        StateMachineModel.Instance = stateMachineModel;

        // int recordedFramesTotal = GetSizeOfMainRecordedData();
        //
        // var StateMachine = StateMachineModel.Instance;
        // StateMachine.states.Clear();
        //
        // var localStatesDict = new Dictionary<StateTimelineUIElement,State>();
        //
        // foreach (var statePlaceholder in currentActiveExample.StatePlaceholders)
        // {
        //     var newState = new State("State " + StateMachine.states.Count(), StateMachine);
        //     StateMachine.AddState(newState);
        //
        //     localStatesDict.Add(statePlaceholder, newState);
        // }
        //
        // var gestures = currentActiveExample.AllGestureSequences;
        // var collisions = currentActiveExample.activeCollisionModels; //TODO check if it needs to be only the active collisions or all
        // var voiceCommands = currentActiveExample.VoiceCommandSequences;
        //
        // List<Sequence> allPotentialTriggers = gestures.Cast<Sequence>()
        //                           .Concat(collisions.Cast<Sequence>())
        //                           .Concat(voiceCommands.Cast<Sequence>())
        //                           .ToList();
        //
        // var allActions = currentActiveExample.assetsDict.Select(keyValuePair => keyValuePair.Value.assetActions).ToList();
        //
        // State firstState = null;
        //
        // for (int i = 0; i < currentActiveExample.StatePlaceholders.Count; i++) {
        //     var currentStatePlaceholder = currentActiveExample.StatePlaceholders[i];
        //     var nexStatePlaceholder = i < currentActiveExample.StatePlaceholders.Count - 1 ? currentActiveExample.StatePlaceholders[i + 1] : null;
        //
        //     var currentState = localStatesDict[currentStatePlaceholder];
        //     if (i == 0) {
        //         firstState = currentState;
        //     }
        //
        //     if (nexStatePlaceholder != null) {
        //         var nextState = localStatesDict[nexStatePlaceholder];
        //
        //         //Find transitions between currentStatePlaceholder to nexStatePlaceholder
        //         var actualTriggers = allPotentialTriggers.FindAll(x => x.CanTriggerAt(nexStatePlaceholder.StartIndex));
        //         if (actualTriggers.Count > 0) {
        //             //Let's build the transition
        //             Func<Frame, bool> transitionConditionFunction = (Frame frame) => {return true;};
        //             string transitionDescription = "" + currentState.name + "->" + nextState.name + ":";
        //             foreach (var trigger in actualTriggers) {
        //                 transitionConditionFunction = trigger.AddConditionToFunction(transitionConditionFunction);
        //                 transitionDescription = transitionDescription + " && " + trigger.ToString();
        //             }
        //             //Let's add a transition between currentState and nextState
        //             currentState.AddTransitionTo(nextState, transitionConditionFunction, transitionDescription);
        //         }
        //     } else {
        //         //currentStatePlaceholder is the last statePlaceholder
        //     }
        //
        //     DebugLogger.Instance.Log("Adding OnEnter and OnExit actions to state " + currentState.name);
        //
        //     //var stateInTimeline = currentActiveExample.StatesDict.First().Key;
        //     foreach (var assetSequences in allActions)
        //     {
        //         foreach (var assetSequence in assetSequences)
        //         {
        //             if (assetSequence.StartIndex >= currentStatePlaceholder.StartIndex && 
        //                 assetSequence.StartIndex < currentStatePlaceholder.StartIndex+currentStatePlaceholder.Length)
        //             {
        //                 int distanceToStateStart = Math.Abs(assetSequence.StartIndex - currentStatePlaceholder.StartIndex);
        //                 int distanceToStateEnd = Math.Abs(currentStatePlaceholder.StartIndex + currentStatePlaceholder.Length - assetSequence.StartIndex - 1);
        //
        //                 if(distanceToStateStart < distanceToStateEnd)
        //                 {
        //                     DebugLogger.Instance.Log("Adding action(s) " + assetSequence.ActionType + " for state " + currentState.name + " in OnEnterActions");
        //                     currentState.OnEnterActionsSequences.Add(assetSequence);
        //                 }
        //                 else
        //                 {
        //                     DebugLogger.Instance.Log("Adding action(s) " + assetSequence.ActionType + " for state " + currentState.name + " in OnExitActions");
        //                     currentState.OnExitActionsSequences.Add(assetSequence);
        //                 }
        //             }
        //         }
        //     }
        // }
        //
        // StateMachineModel.Instance.SetInitialState(firstState);
        //
        // PrintDetailsOfStateMachine(localStatesDict.Values.ToList()); 
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
        //         if (!shortestList[i].IsStateEquivalentTo(allStatesInExamples[j][i]))
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
        RecreateTimelineUI_StatePlaceholders();
    }
    
    public void RecreateTimelineUI_StatePlaceholders(bool startHidden=false) {
        DeleteStatePlaceholders();
        int recordedFramesTotal = GetSizeOfMainRecordedData();
        var gestures = currentActiveExample.AllGestureSequences;
        var collisions = currentActiveExample.activeCollisionModels;
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
        StateMachineModel.Instance.states.Clear();

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
    
    int _latestRecordedFrameIndex = 0;

    private void FixedUpdate()
    {
        switch (Manager.Instance.currAppState)
        {
            case Manager.AppState.RECORDING:
            {
                _latestRecordedFrameIndex++;

                head.Record(_latestRecordedFrameIndex);
                leftHand.Record(_latestRecordedFrameIndex, currentActiveExample.leftHandFrames);
                rightHand.Record(_latestRecordedFrameIndex, currentActiveExample.rightHandFrames);
                break;
            }
            case Manager.AppState.PLAYBACK:
            case Manager.AppState.SIMULATING:
            {
                if (isAutomaticPlayback)
                {
                    playbackSlider.value += 1;//Time.deltaTime;
                    if (playbackSlider.value >= currentActiveExample.RecordedDataCount)
                    {
                        playbackSlider.value = 0;
                    }
                }

                UpdatePlaybackObjects((int)playbackSlider.value);
                
                break;
            }
            default:
            {
                DebugLogger.Instance.Log("Ignoring currAppState " + Manager.Instance.currAppState + " in Recorder >> FixedUpdate");
                return;
            }
        }

    }

    private void UpdatePlaybackObjects(int currentFrameNum)
    {
        if (currentActiveExample.RecordedDataCount == 0)
        {
            return;
        }
        
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

    public void HighlightStateTimelineUIElement(string stateModelId)
    {
        //Unhighlight all the other states and highlight this one
        foreach (var example in examples)
        {
            foreach (var stateTimelineUIElement in example.StatePlaceholders)
            {
                var imageComponent = stateTimelineUIElement.GetComponent<Image>();
                if (stateTimelineUIElement.stateModelId == stateModelId)
                {
                    //Highlight this one
                    imageComponent.color = Color.red;
                }
                else
                {
                    //Unhighlight all the others
                    imageComponent.color = new Color(255, 193,97);
                }
            }
        }
    }
}





