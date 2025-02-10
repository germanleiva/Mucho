using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Dependencies.NCalc;
using System.ComponentModel;
using System.Reflection;
using Assets.OVR.Scripts;
using Oculus.Interaction;
using Unity.VisualScripting;

//using Assets.OVR.Scripts;
//using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UI;

public class Manager : MonoBehaviour
{
    public GameObject leftHandMenu;

    private AppState _currAppState;
    public AppState currAppState
    {
        get
        {
            return _currAppState;
        }
        set
        {
            _currAppState = value;
        }
    }
    public SpeechToText speechToTextEngine;
    public GameObject spherePrefab;
    public GameObject cubePrefab;
    public GameObject textAssetPrefab;

    public enum AppState
    {
        RECORDING,
        PLAYBACK,
        SIMULATING,
        LIVE,
        EDIT_BOUNDING_SPHERE
    }

    public static Manager Instance { get; private set; }

    private void Awake()
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


    // Start is called before the first frame update
    public TMPro.TMP_Text VRDebugText;
    void Start()
    {
        currAppState = Manager.AppState.PLAYBACK;
    }

    public void ClearDebugText()
    {
        VRDebugText.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeToLiveMode()
    {
        if(currAppState == Manager.AppState.LIVE)
        {
            DebugLogger.Instance.Log("I am already in Live Mode");
            return;
        }

        if(Recorder.Instance.examples.Count == 0)
        {
            DebugLogger.Instance.Log("No examples to change to live mode");
            return;
        }
        
        Recorder.Instance.playbackSlider.value = 0;
        Recorder.Instance.SetPlaybackObjectsVisibility(false);
        AssetManager.Instance.HideMiscObjs();
        AssetManager.Instance.SetAllAssetMenusPokeable(false);
        
        StateMachineModel.CombinedStateMachine(Recorder.Instance.examples);
        
        // Recorder.Instance.ResetStateMachine();
        InputManager.Instance.SaveCurrentCollision(null,null);
        StateMachineModel.Instance.InvokeOnEnterActionsOfInitialState();
        speechToTextEngine.StartListening();
        
        currAppState = Manager.AppState.LIVE;
    }

    public void ChangeToAssetRecordingMode()
    {
        currAppState = Manager.AppState.RECORDING;
    }

    public void ChangeToPlaybackMode()
    {
        
        
        currAppState = Manager.AppState.PLAYBACK; 
        AssetManager.Instance.ResetMeshRendererForAllAssets();       
        Recorder.Instance.SetPlaybackObjectsVisibility(true);
        //AssetManager.Instance.ShowMiscObjs();
        AssetManager.Instance.SetAllAssetMenusPokeable(true);
        AssetManager.Instance.ResetPhysicsForAllAssetsAndStopFollowing();

        speechToTextEngine.StopListening();
    }

    public void CreateCopyOfObject(GameObject objectDragged)
    {
        GameObject objectThatWillStayInTheMenu = Instantiate(objectDragged);
        objectThatWillStayInTheMenu.transform.SetParent(objectDragged.transform.parent);
        objectThatWillStayInTheMenu.transform.SetLocalPositionAndRotation(objectDragged.transform.localPosition, objectDragged.transform.localRotation);
        objectThatWillStayInTheMenu.transform.localScale = objectDragged.transform.localScale;
        //objectThatWillStayInTheMenu.name = objectDragged.name + "Copy";
        DetachFromAllParents(objectDragged.transform);
    }

    //public void Create

    public void DetachFromAllParents(Transform transform)
    {
        transform.SetParent(null);
        DebugLogger.Instance.Log("Detached " + transform.name + " from all parents");
        //controlUI.transform.SetParent(null);
    }

    public void DestroyCopyAndSpawnAsset(GameObject copyObjectDragged)
    {
        if(Recorder.Instance.examples.Count == 0)
        {
            DebugLogger.Instance.Log("No examples to spawn asset in");
            Destroy(copyObjectDragged);
            return;
        }

        //TODO: only one prefab and change the parameter (for god sake)
        if (copyObjectDragged.name.StartsWith("Sphere"))
        {
            var meshSelected = copyObjectDragged.GetComponent<MeshFilter>().sharedMesh;
            var assetCount = Recorder.Instance.allAssets.Count(asset => asset.GetComponent<MeshFilter>().sharedMesh == meshSelected);
            AssetManager.Instance.CreateAsset(copyObjectDragged.transform.position, spherePrefab, $"Sphere {assetCount+1}", meshSelected);
        }
        else if (copyObjectDragged.name.StartsWith("Cube"))
        {
            var meshSelected = copyObjectDragged.GetComponent<MeshFilter>().sharedMesh;
            var assetCount = Recorder.Instance.allAssets.Count(asset => asset.GetComponent<MeshFilter>().sharedMesh == meshSelected);
            AssetManager.Instance.CreateAsset(copyObjectDragged.transform.position, spherePrefab, $"Cube {assetCount+1}", meshSelected);
        }
        else if (copyObjectDragged.name.StartsWith("Text"))
        {
            AssetManager.Instance.CreateAsset(copyObjectDragged.transform.position, textAssetPrefab, "Text");
        }
        else if (copyObjectDragged.name.StartsWith("Premade"))
        {
            //We were dragging a PremadeAsset so we need to execute it's meshCopy component code
            var meshCopyComponent = copyObjectDragged.GetComponent<MeshCopy>();
            if (meshCopyComponent != null)
            {
                meshCopyComponent.ApplyMeshChange();
                // Update the timeline asset row name
                Recorder.Instance.RecreateTimelineUI_AssetRows();
            }
        }

        Destroy(copyObjectDragged);
    }

    public string CleanAssetName(string str)
    {        
        //If the string ends with the substring "Anchor", remove it
        if (str.EndsWith("Anchor"))
        {
            str = str.Substring(0, str.Length - 6);
            return str;
        }
        if (str.IndexOf('-') == -1)
        {
            return str;
        }
        return str.Substring(0, str.IndexOf('-'));
    }

    public void OpenLeftHandMenu()
    {
        leftHandMenu.SetActive(true);
    }

    public void CloseLeftHandMenu()
    {
        leftHandMenu.SetActive(false);
    }

    public void ToggleRightHandEventRow(Boolean rightHandEventsOn) {
        foreach (var eachRightHandData in Recorder.Instance.currentActiveExample.rightHandFrames)
        {
            eachRightHandData.isActive = !eachRightHandData.isActive;
        }

        Recorder.Instance.RecreateTimelineUI_Gestures();
    }
    public void ToggleLeftHandEventRow(Boolean leftHandEventsOn) {
        foreach (var eachLeftHandData in Recorder.Instance.currentActiveExample.leftHandFrames)
        {
            eachLeftHandData.isActive = !eachLeftHandData.isActive;
        }

        Recorder.Instance.RecreateTimelineUI_Gestures();
    }
    
    /*public void ToggleCollisionEventRow(Boolean focusAreaCollisionsOn)
    {
        Recorder.Instance.currentActiveExample.areFocusAreaCollisionsActivated = !Recorder.Instance.currentActiveExample.areFocusAreaCollisionsActivated;
        foreach (var collisionModel in Recorder.Instance.currentActiveExample.CollisionModels)
        {
            if (collisionModel.IsACollisionWithAFocusArea()) {
                collisionModel.IsActive = !collisionModel.IsActive;
            }
        }

        Recorder.Instance.RecreateTimelineUI_Collisions();
    }*/
    
    public void ToggleCollisionFilters(GameObject gameObject)
    {
        // Put the gameobject in the hierarchy as the last element
        gameObject.transform.SetAsLastSibling();
        gameObject.SetActive(!gameObject.activeSelf);
    }

    public void PressedRecreateStatePlaceholders() {
       Recorder.Instance.RecreateTimelineUI_StatePlaceholders();
    }
    
    public void FilterFocusAreaCollisionEventRow(string collisionType)
    {
        switch (collisionType)
        {
            case "LeftHand":
                Recorder.Instance.currentActiveExample.isLeftHandFocusAreaActivated = !Recorder.Instance.currentActiveExample.isLeftHandFocusAreaActivated;
                break;
            case "RightHand":
                Recorder.Instance.currentActiveExample.isRightHandFocusAreaActivated = !Recorder.Instance.currentActiveExample.isRightHandFocusAreaActivated;
                break;
            case "F_G":
                Recorder.Instance.currentActiveExample.isGazeFocusAreaActivated = !Recorder.Instance.currentActiveExample.isGazeFocusAreaActivated;
                break;
            case "Head":
                Recorder.Instance.currentActiveExample.isHeadActivated = !Recorder.Instance.currentActiveExample.isHeadActivated;
                break;
            case "F_L":
                Recorder.Instance.currentActiveExample.isLeftHandPinchActivated = !Recorder.Instance.currentActiveExample.isLeftHandPinchActivated;
                break;
            case "F_R":
                Recorder.Instance.currentActiveExample.isRightHandPinchActivated = !Recorder.Instance.currentActiveExample.isRightHandPinchActivated;
                break;
        }

        foreach (var eachCollisionData in Recorder.Instance.currentActiveExample.CollisionModels)
        {
            if (eachCollisionData.IsACollisionWith(collisionType))
            {
                eachCollisionData.IsActive = !eachCollisionData.IsActive;
            }
        }

        Recorder.Instance.RecreateTimelineUI_Collisions();
    }
    
    

}

public class Example
{
    public static int exampleCount = 0;
    public int exampleId { get; private set; }
    public List<HandFrame> leftHandFrames;
    // Define the read-only property to return filtered elements
    public List<HandFrame> activeLeftHandFrames
    {
        get
        {
            // Filter the list based on the isActive property
            return leftHandFrames.Where(handFrame => handFrame.isActive).ToList();
        }
    }
    public List<HandFrame> rightHandFrames;

    public List<HandFrame> activeRightHandFrames
    {
        get
        {
            // Filter the list based on the isActive property
            return rightHandFrames.Where(handFrame => handFrame.isActive).ToList();
        }
    }
    public List<HeadFrame> headFrames;


    public bool isLeftHandFocusAreaActivated = false;
    public bool isRightHandFocusAreaActivated = false;
    public bool isGazeFocusAreaActivated = false;
    public bool isHeadActivated = true;
    public bool isLeftHandPinchActivated = true;
    public bool isRightHandPinchActivated = true;
    
    public List<CollisionSequence> activeCollisionModels
    {
        get
        {
            return CollisionModels.Where(collisionModel => collisionModel.IsActive).ToList();
        }
    }

    public RectTransform examplePlaybackPanel;
    public RectTransform rightHandTimelinePanel;    
    public RectTransform leftHandTimelinePanel;    
    public RectTransform voiceTimelinePanel;
    public GameObject collisionTimelinePanel;
    public GameObject assetTimelinePanelPrefab;
    public GameObject statesTimelinePanel;

    public GameObject stateTimelineElementPrefab;
    public GameObject handTimelineElementPrefab;
    public GameObject voiceCommandTimelineElementPrefab;
    public GameObject assetTimelineElementPrefab;
    public GameObject collisionTimelineElementPrefab;    
    public GameObject hideTimelinePanelPrefab;   
    public GameObject showTimelinePanelPrefab;
    /*public GameObject filterCollisionElements;*/

    public List<GestureSequence> LeftHandGestureSequences {
        get
        {
            List<InputManager.Gesture> gestures = activeLeftHandFrames.Select(x => x.gesture).ToList();
            return Sequence.GetContinuousGestureSequences(gestures);
        }
    }
    
    public List<GestureSequence> RightHandGestureSequences {
        get
        {
            List<InputManager.Gesture> gestures = activeRightHandFrames.Select(x => x.gesture).ToList();
            return Sequence.GetContinuousGestureSequences(gestures);
        }
    }

    public List<VoiceSequence> VoiceCommandSequences
    {
        get
        {
            List<string> voiceCommands = headFrames.Select(x => x.voiceCommand).ToList();
            return VoiceSequence.GetVoiceCommandSequences(voiceCommands);
        }
    }

    public List<GestureSequence> AllGestureSequences {
        get
        {
            return LeftHandGestureSequences.Concat(RightHandGestureSequences).ToList();
        }
    }

    public int RecordedDataCount
    {
        get
        {
            return headFrames.Count;
        }
    }
    
    public GameObject startRecordingButton, stopRecordingButton;


    public int numberOfRecordedFrames = 0;
    public Button button;
    //Create a dictionary matching assets to the list of their recordable frames
    // public Dictionary<Asset, List<AssetFrame>> assetFramesDict;   

    //public List<Recordable> assets;
    public List<StateTimelineUIElement> StatePlaceholders;
    // public Dictionary<State, StateTimelineUIElement> StatesDict;
    
    public readonly List<CollisionSequence> CollisionModels = new();
    // public readonly Dictionary<Asset,List<AssetActionSequence>> AssetActions = new ();
    
    public readonly Dictionary<Asset, (List<AssetFrame> assetFrames, List<AssetActionSequence> assetActions)> assetsDict = new();

    public Example(Button _button, RectTransform _examplePlaybackPanel)
    {
        ++exampleCount;
        exampleId = exampleCount;
        _button.GetComponentInChildren<TMPro.TMP_Text>().text = exampleId.ToString();
        button = _button;

        leftHandFrames = new List<HandFrame>();
        rightHandFrames = new List<HandFrame>();
        headFrames = new List<HeadFrame>();

        examplePlaybackPanel = _examplePlaybackPanel;
        //examplePlaybackPanel.gameObject.SetActive(true);
        examplePlaybackPanel.SetAsFirstSibling();

        rightHandTimelinePanel = examplePlaybackPanel.Find("RightHandEventsPanel").GetComponent<RectTransform>();
        leftHandTimelinePanel = examplePlaybackPanel.Find("LeftHandEventsPanel").GetComponent<RectTransform>();
        voiceTimelinePanel = examplePlaybackPanel.Find("VoiceInputPanel").GetComponent<RectTransform>();
        collisionTimelinePanel = examplePlaybackPanel.Find("CollisionEventsPanel").gameObject;
        assetTimelinePanelPrefab = examplePlaybackPanel.Find("AssetEventsPanel").gameObject;
        assetTimelinePanelPrefab.SetActive(false);
        statesTimelinePanel = examplePlaybackPanel.Find("StatesPanel").gameObject;

        stateTimelineElementPrefab = statesTimelinePanel.transform.GetChild(0).gameObject;
        handTimelineElementPrefab = rightHandTimelinePanel.transform.GetChild(0).gameObject;
        voiceCommandTimelineElementPrefab = voiceTimelinePanel.transform.GetChild(0).gameObject;
        assetTimelineElementPrefab = assetTimelinePanelPrefab.transform.GetChild(1).gameObject;
        collisionTimelineElementPrefab = collisionTimelinePanel.transform.GetChild(0).gameObject;
        hideTimelinePanelPrefab = assetTimelinePanelPrefab.transform.GetChild(2).gameObject;
        showTimelinePanelPrefab = assetTimelinePanelPrefab.transform.GetChild(3).gameObject;
        /*filterCollisionElements = collisionTimelinePanel.transform.GetChild(2).gameObject;*/
        
        StatePlaceholders = new();
        // StatesDict = new Dictionary<State, StateTimelineUIElement>();
        //Copy allAssets to assets
        foreach (Asset recordable in Recorder.Instance.allAssets)
        {            
            //Create a new list of recordable frames for each asset
            assetsDict.Add(recordable, (new List<AssetFrame>(), new List<AssetActionSequence>()));            
        }
        
        //find in the children of the voicetimelinepanel a child called StartAudio
        Transform  [] recordButtons= voiceTimelinePanel.transform.GetComponentsInChildren<Transform>();
        foreach (Transform g in recordButtons)
        {
            if (g.gameObject.name.Equals("StartAudioRecord"))
            {
                startRecordingButton = g.gameObject;
            }else if (g.gameObject.name.Equals("StopAudioRecord"))
            {
                stopRecordingButton = g.gameObject;
            }
        }
        
        //startRecordingButton = voiceTimelinePanel.transform.GetChild()

        DebugLogger.Instance.Log("Created example " + exampleId);
    }
    
    /*public void ToggleCollisionFilters()
    {
        filterCollisionElements.SetActive(!filterCollisionElements.activeSelf);
    }*/

    public List<GameObject> GetTimelineAssetRows()
    {
        List<GameObject> result = new();
        foreach (var timelineRow in examplePlaybackPanel.GetComponentsInChildren<TimelineAssetRow>())
        {
            result.Add(timelineRow.gameObject);
        }

        return result;
    }
    public GameObject GetTimelineRowFor(Asset asset)
    {
        foreach (var timelineAssetRow in examplePlaybackPanel.GetComponentsInChildren<TimelineAssetRow>())
        {
            if (timelineAssetRow.AssetInstanceID == asset.GetInstanceID())
            {
                return timelineAssetRow.gameObject;
            }
        }

        return null;
    }

    public void CopyExampleDataFrom(Example example)
    {
        DebugLogger.Instance.Log("Copying data from example " + example.exampleId + " to example " + exampleId);
        leftHandFrames = new List<HandFrame>(example.leftHandFrames.Select(item => (HandFrame)item.Clone()));
        rightHandFrames = new List<HandFrame>(example.rightHandFrames.Select(item => (HandFrame)item.Clone()));
        headFrames = new List<HeadFrame>(example.headFrames.Select(item => (HeadFrame)item.Clone()));
        foreach (var data in headFrames) // Clear the voice commands
        {
            data.voiceCommand = null;
        }
        
        foreach ( var entry in example.assetsDict)
        {
            var assetFrames = new List<AssetFrame>(entry.Value.assetFrames.Select(item => (AssetFrame)item.Clone()));
            var assetActions = new List<AssetActionSequence>(entry.Value.assetActions.Select(item => (AssetActionSequence)item.Clone()));

            assetsDict[entry.Key] = (assetFrames, assetActions);
        }
        
    }

    public void RefreshAssetsInExample()
    {
        //Copy allAssets to assets
        foreach (Asset asset in Recorder.Instance.allAssets)
        {
            //Create a new list of recordable frames for each asset
            if (!assetsDict.ContainsKey(asset))
            {
                assetsDict.Add(asset, (assetFrames:new List<AssetFrame>(),assetActions:new List<AssetActionSequence>()));
            }
        }

        //Remove assets that are no longer in the scene
        List<Asset> assetsToRemove = new List<Asset>();
        foreach (Asset asset in assetsDict.Keys)
        {
            if (!Recorder.Instance.allAssets.Contains(asset))
            {
                assetsToRemove.Add(asset);
            }
        }
        foreach (Asset asset in assetsToRemove)
        {
            assetsDict.Remove(asset);
        }
    }

    public void Render()
    {
        //Render the states
        // foreach (State state in StatesDict.Keys)
        // {
            //StatesDict[state].Render();
        // }
    }

    public void CleanDataBeforeRecording()
    {
        DebugLogger.Instance.Log("Resetting data for example " + exampleId);
        leftHandFrames.Clear();
        rightHandFrames.Clear();
        headFrames.Clear();
        
        foreach(Asset asset in assetsDict.Keys)
        {
            assetsDict[asset].assetFrames.Clear();
            foreach (ForceArrow forceArrow in asset.forceArrows)
            {
                UnityEngine.Object.Destroy(forceArrow.gameObject);
            } 
        }
    }

    public void DeleteExample()
    {
        //Delete the example from the list of examples
    }

    public void AddNewCollision(int frameStart, GameObject assetGameObject, GameObject anotherGameObject)
    {
        
        if (CollisionModels.Exists(existingCollision => (existingCollision.StartIndex == frameStart && existingCollision.isCollidingWith(assetGameObject,anotherGameObject))))
        {
            DebugLogger.Instance.Log($"We have a similar collision, so we ignore it: Frame{frameStart}, {assetGameObject.name} vs {anotherGameObject.name}"); 
            return;
        }
     
        var newCollisionModel = new CollisionSequence();
        newCollisionModel.StartIndex = frameStart;
        newCollisionModel.CollidingObject1 = assetGameObject; //This is generally an asset
        newCollisionModel.CollidingObject2 = anotherGameObject;  //This is a playback object   
        
        GameObject collidedObjectOnLiveMode;

        if (anotherGameObject == InputManager.Instance.leftHandPinchObj || anotherGameObject == InputManager.Instance.playbackLeftHandPinchObj)
        {
            collidedObjectOnLiveMode = InputManager.Instance.leftHandPinchObj;
            
        } else if (anotherGameObject == InputManager.Instance.rightHandPinchObj || anotherGameObject == InputManager.Instance.playbackRightHandPinchObj)
        {
            collidedObjectOnLiveMode = InputManager.Instance.rightHandPinchObj;
            
        } else if (anotherGameObject == InputManager.Instance.headContactObj || anotherGameObject == InputManager.Instance.playbackHeadContactObj)
        {
            collidedObjectOnLiveMode = InputManager.Instance.headContactObj;
            
        } else if (anotherGameObject == InputManager.Instance.leftFocus || anotherGameObject == InputManager.Instance.playbackLeftFocus)
        {
            collidedObjectOnLiveMode = InputManager.Instance.leftFocus;
            
        } else if (anotherGameObject == InputManager.Instance.rightFocus || anotherGameObject == InputManager.Instance.playbackRightFocus)
        {
            collidedObjectOnLiveMode = InputManager.Instance.rightFocus;
            
        } else if (anotherGameObject == InputManager.Instance.gazeFocus || anotherGameObject == InputManager.Instance.playbackGazeFocus)
        {
            collidedObjectOnLiveMode = InputManager.Instance.gazeFocus;
            
        } else
        {
            //TODO what happen with collisions with other assets or the floor?
            collidedObjectOnLiveMode = anotherGameObject;
        }
        
        newCollisionModel.collisionDelegate = (Frame frame) => frame.IsColliding(assetGameObject, collidedObjectOnLiveMode);

        if (newCollisionModel.IsACollisionWith("LeftHand") && !isLeftHandPinchActivated)
        {
            newCollisionModel.IsActive = false;
        }
        if (newCollisionModel.IsACollisionWith("RightHand") && !isRightHandPinchActivated)
        {
            newCollisionModel.IsActive = false;
        }
        if (newCollisionModel.IsACollisionWith("F_G") && !isGazeFocusAreaActivated)
        {
            newCollisionModel.IsActive = false;
        }
        if (newCollisionModel.IsACollisionWith("Head") && !isHeadActivated)
        {
            newCollisionModel.IsActive = false;
        }
        if (newCollisionModel.IsACollisionWith("F_L") && !isLeftHandFocusAreaActivated)
        {
            newCollisionModel.IsActive = false;
        }
        if (newCollisionModel.IsACollisionWith("F_R") && !isRightHandFocusAreaActivated)
        {
            newCollisionModel.IsActive = false;
        }

        
        CollisionModels.Add(newCollisionModel);
    }

    public void EndPreviousCollision(int frameIndex, GameObject assetGameObject, GameObject anotherGameObject)
    {       
        //We need to find the corresponding CollisionModel and set its end frame
        var oldestUnclosedCollision = CollisionModels.Find(collision => collision.isCollidingWith(assetGameObject,anotherGameObject) && collision.Length == 0);
        if (oldestUnclosedCollision != null)
        {
            oldestUnclosedCollision.Length = frameIndex - oldestUnclosedCollision.StartIndex;
        }
    }

    public List<CollisionSequence> CollisionModelsWithoutFrameEnd()
    {
        return CollisionModels.FindAll(collisionModel => collisionModel.Length == 0);
    }

    public void prepareForSimulation()
    {
        //This method reset/initialize/remove things that will be calculated during the simulation
        
        foreach (var keyValuePair in assetsDict.Values)
        {
            foreach (var action in keyValuePair.assetActions)
            {
                //We need to reopen the actions that are ApplyForce because their end is calculated from collisions
                if (action.ActionType == ACTION_ENUM.APPLY_FORCE_START)
                {
                    action.Length = 0;
                    action.associatedEndAction = null;
                }
            }
            
            //We need to remove all the actions ApplyForceEnd, they need to be recalculated
            keyValuePair.assetActions.RemoveAll(action => action.ActionType == ACTION_ENUM.APPLY_FORCE_END);
        }
    }
}

public abstract class Sequence: ICloneable {
    public int StartIndex { get; set; }
    public int Length { get; set; }

    abstract public Boolean CanTriggerAt(int stateStartIndex);
    abstract public Func<Frame,bool> AddConditionToFunction(Func<Frame,bool> conditionFunction);
    public virtual object Clone()
    {
        throw new NotImplementedException();
    }
    
    public static List<GestureSequence> GetContinuousGestureSequences(List<InputManager.Gesture> gestures)
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

    public abstract bool IsEquivalentSequence(Sequence other);
}

public class CollisionSequence : Sequence
{
    public bool IsActive = true;
    public GameObject CollidingObject1 { get; set; }
    public GameObject CollidingObject2 { get; set; }

    public Func<Frame, bool> collisionDelegate { get; set; }

    public override Func<Frame, bool> AddConditionToFunction(Func<Frame, bool> conditionFunction)
    {
        return (Frame frame) => { return conditionFunction(frame) && collisionDelegate(frame); };
    }

    //We check if stateStartIndex is within the boundaries of this sequence 
    public override bool CanTriggerAt(int stateStartIndex)
    {
        //TODO Does it need to be <= the last sign?
        return StartIndex == stateStartIndex || (stateStartIndex > StartIndex && stateStartIndex < (StartIndex + Length));
    }

    public override string ToString() {
        var text1 = GetCollidingObject1Name();
        var text2 = GetCollidingObject2Name();
        
        return $"{text1} hit {text2}";
    }
    
    public string GetCollidingObject1Name()
    {
        return CollidingObject1.GetComponent<Asset>() ? CollidingObject1.name : CollidingObject1.tag;
    }
    
    public string GetCollidingObject2Name()
    {
        return CollidingObject2.GetComponent<Asset>() ? CollidingObject2.name : CollidingObject2.tag;
    }
    
    public override object Clone()
    {
        // Create a new instance of the class
        CollisionSequence clonedCollision = new();
        
        // Copy the properties of the current object
        clonedCollision.StartIndex = StartIndex;
        clonedCollision.Length = Length;
        clonedCollision.CollidingObject1 = CollidingObject1;
        clonedCollision.CollidingObject2 = CollidingObject2;

        // Return the cloned object
        return clonedCollision;
    }

    public bool isCollidingWith(GameObject assetGameObject, GameObject anotherGameObject)
    {
        return (CollidingObject1 == assetGameObject && CollidingObject2 == anotherGameObject) || (CollidingObject1 == anotherGameObject && CollidingObject2 == assetGameObject);
    }
    
    public bool IsOverridenBy(CollisionSequence anotherCollision)
    {
        if (!anotherCollision.CanTriggerAt(StartIndex) || !anotherCollision.CanTriggerAt(StartIndex+Length))
        {
            return false;
        }

        //If we have a collision between an asset and the hand,
        //that overrides a collision between the same asset and the focus area of the same hand
        
        if (CollidingObject1 == anotherCollision.CollidingObject1 || CollidingObject2 == anotherCollision.CollidingObject1)
        {
            //That means we are talking about the same asset
            //It only overrides if the parameter collision is a hand and I am a focus area hand
            if ( (CollidingObject2.CompareTag("F_L") && anotherCollision.CollidingObject2.CompareTag("LeftHand")) ||
                 (CollidingObject2.CompareTag("F_R") && anotherCollision.CollidingObject2.CompareTag("RightHand") ||
                 (CollidingObject1.CompareTag("F_L") && anotherCollision.CollidingObject2.CompareTag("LeftHand"))) ||
                 (CollidingObject1.CompareTag("F_R") && anotherCollision.CollidingObject2.CompareTag("RightHand")))
            {
                Debug.Log("Detected collision override: " + anotherCollision.CollidingObject1.name + " and " + anotherCollision.CollidingObject2.name);
                return true;
            }
        }
        return false;
    }
    
    public override bool IsEquivalentSequence(Sequence other)
    {
        var otherCollisionSequence = other as CollisionSequence;
        if (otherCollisionSequence == null)
        {
            return false;
        }
        return otherCollisionSequence.isCollidingWith(CollidingObject1,CollidingObject2);
    }

    public bool IsACollisionWithAFocusArea()
    {
        foreach (var collidingObject in new List<GameObject> { CollidingObject1, CollidingObject2 })
        {
            if ((new List<string> { "F_L","F_R","F_G" }).Exists(x => collidingObject.CompareTag(x)))
            {
                return true;
            }
        }

        return false;
    }
    
    public bool IsACollisionWith(string colliderTag)
    {
        foreach (var collidingObject in new List<GameObject> { CollidingObject1, CollidingObject2 })
        {
            if (collidingObject.CompareTag(colliderTag))
            {
                return true;
            }
        }
        return false;
    }
}
public enum ACTION_ENUM { 
    [Description("ApplyFollow(Left hand)")]
    FOLLOW_LEFT_HAND,
    [Description("ApplyFollow(Right hand)")]
    FOLLOW_RIGHT_HAND,
    [Description("ApplyFollow(L-focus)")]
    FOLLOW_L_FOCUS,
    [Description("ApplyFollow(R-focus)")]
    FOLLOW_R_FOCUS,
    [Description("ApplyFollow(G-focus)")]
    FOLLOW_G_FOCUS,
    [Description("ApplyFollowEnd()")]
    FOLLOW_END,
    [Description("Show()")]
    SHOW,
    [Description("Hide()")]
    HIDE,
    [Description("ChangeColor()")]
    CHANGE_COLOR,
    [Description("Pin()")]
    PIN,
    [Description("ApplyForce()")]
    APPLY_FORCE_START,
    [Description("ApplyForceEnd()")]
    APPLY_FORCE_END,
    [Description("Animate()")]
    ANIMATE,
    [Description("StopAnimate()")]
    STOP_ANIMATE
    };

public class AssetActionSequence : Sequence
{
    public Asset TargetAsset { get; set; }
    public ACTION_ENUM ActionType { get; set; } //None, Physics, ApplyFollow, Show, Hide

    public Action ActionDelegate { get; set; }

    public AssetActionSequence associatedEndAction;

    public override Func<Frame, bool> AddConditionToFunction(Func<Frame, bool> conditionFunction)
    {
        throw new NotImplementedException();
    }

    public override bool CanTriggerAt(int stateStartIndex)
    {
        throw new NotImplementedException();
    }
    
    public override string ToString() {
        return GetDescription(ActionType);
    }

    public static string GetDescription(ACTION_ENUM value)
    {
        Type type = value.GetType();
        string name = Enum.GetName(type, value);
        if (name != null)
        {
            FieldInfo field = type.GetField(name);
            if (field != null)
            {
                DescriptionAttribute attr = 
                    Attribute.GetCustomAttribute(field, 
                        typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attr != null)
                {
                    return attr.Description;
                }
            }
        }
        return null;
    }

    public static bool IsActionEnum(string actionName)
    {
        foreach (ACTION_ENUM action in Enum.GetValues(typeof(ACTION_ENUM)))
        {
            if (action.ToString().Equals(actionName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Check if the actionName matches the description of the enum value
            string description = GetDescription(action);
            if (description != null && description.Equals(actionName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

        }
        return false;
    }

    public override object Clone()
    {
        // Create a new instance of the class
        AssetActionSequence clonedAction = new();
        
        // Copy the properties of the current object
        clonedAction.StartIndex = StartIndex;
        clonedAction.Length = Length;
        clonedAction.ActionType = ActionType;
        clonedAction.ActionDelegate = ActionDelegate;
        clonedAction.TargetAsset = TargetAsset;

        // Return the cloned object
        return clonedAction;
    }

    public bool IsFollow()
    {
        var followActions = new List<ACTION_ENUM> {ACTION_ENUM.FOLLOW_LEFT_HAND, ACTION_ENUM.FOLLOW_RIGHT_HAND, ACTION_ENUM.FOLLOW_L_FOCUS, ACTION_ENUM.FOLLOW_R_FOCUS, ACTION_ENUM.FOLLOW_G_FOCUS};
        return followActions.Contains(ActionType);
    }

    public bool shouldShowInTimeline => ActionType != ACTION_ENUM.FOLLOW_END && ActionType != ACTION_ENUM.APPLY_FORCE_END;

    public void DeleteActionFrom(List<AssetActionSequence> actions)
    {
        //TODO (only Germán, Vittoria does not agree xD) for now the action is independent in every example, so we are not deleting this action from other examples than the currrentActiveExample
        actions.Remove(this);
        actions.Remove(this.associatedEndAction);

        if (this.ActionType == ACTION_ENUM.APPLY_FORCE_START)
        {
            //TODO We need to also remove the arrows!
        }
    }

    public override bool IsEquivalentSequence(Sequence other)
    {
        var otherAssetActionSequence = other as AssetActionSequence;
        if (otherAssetActionSequence == null)
        {
            return false;
        }
    
        return ActionType == otherAssetActionSequence.ActionType && TargetAsset == otherAssetActionSequence.TargetAsset;
    }
}

public class GestureSequence : Sequence
{
    public InputManager.Gesture GestureType { get; set; }
    public override Boolean CanTriggerAt(int stateStartIndex) {
        return StartIndex == stateStartIndex;
    }

    public override Func<Frame, bool> AddConditionToFunction(Func<Frame, bool> conditionFunction) {
        switch (GestureType) {
            case InputManager.Gesture.LEFTHANDGRAB:
            case InputManager.Gesture.LEFTHANDPINCH:
            case InputManager.Gesture.LEFTHANDOPEN:
                return (Frame frame) => conditionFunction(frame) && frame.leftHandGesture == GestureType;
            case InputManager.Gesture.RIGHTHANDGRAB:
            case InputManager.Gesture.RIGHTHANDPINCH:
            case InputManager.Gesture.RIGHTHANDOPEN:
                return (Frame frame) => conditionFunction(frame) && frame.rightHandGesture == GestureType;
            case InputManager.Gesture.LEFTHANDNONE:
            case InputManager.Gesture.RIGHTHANDNONE:
            case InputManager.Gesture.LEFTHANDMENUOPEN: 
            default:
                DebugLogger.Instance.Log("Ignoring GestureType in GestureSequence >> addConditionToFunction for " + GestureType);
                return conditionFunction;
        }
    }
    public override string ToString()
    {
        return InputManager.Instance.GestureToString(GestureType);
        //return $"GestureSequence {GestureType.ToString()}";
    }
    
    public override bool IsEquivalentSequence(Sequence other)
    {
        var otherGestureSequence = other as GestureSequence;
        if (otherGestureSequence == null)
        {
            return false;
        }
        return otherGestureSequence.GestureType == GestureType;
    }
}

public class VoiceSequence : Sequence
{
    public string VoiceCommand { get; set; }
    override public Boolean CanTriggerAt(int stateStartIndex) {
        return StartIndex == stateStartIndex;
    }

    public override Func<Frame, bool> AddConditionToFunction(Func<Frame, bool> conditionFunction) {
        return (Frame frame) => { return conditionFunction(frame) && frame.voiceCommand.Contains(VoiceCommand);};
    }

    public override string ToString() {
        return $"VoiceSequence {VoiceCommand}";
    }
    
    public static List<VoiceSequence> GetVoiceCommandSequences(List<string> voiceCommands)
    {
        List<VoiceSequence> sequences = new();

        int startIndex = -1;
        //TODO Virtual Museum forced
        string currentVoiceCommand = null;//= "red";

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
    
    public override bool IsEquivalentSequence(Sequence other)
    {
        var otherVoiceSequence = other as VoiceSequence;
        if (otherVoiceSequence == null)
        {
            return false;
        }
        return otherVoiceSequence.VoiceCommand == VoiceCommand;
    }
}


