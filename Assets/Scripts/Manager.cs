using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Dependencies.NCalc;
using System.ComponentModel;
using System.Reflection;
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

        currAppState = Manager.AppState.LIVE;
        Recorder.Instance.playbackSlider.value = 0;
        Recorder.Instance.SetPlaybackObjectsVisibility(false);
        AssetManager.Instance.HideMiscObjs();
        AssetManager.Instance.SetAllAssetMenusPokeable(false);
        
        CustomStateMachine.CombinedStateMachine(Recorder.Instance.examples);
        
        // Recorder.Instance.ResetStateMachine();
        InputManager.Instance.SaveCurrentCollision(null,null);
        CustomStateMachine.Instance.InvokeOnEnterActionsOfInitialState();
        speechToTextEngine.StartListening();
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
        AssetManager.Instance.ResetPhysicsForAllAssets();
        Recorder.Instance.ResetStateMachine();
        speechToTextEngine.StopListening();
    }

    public void CreateCopyOfObject(GameObject obj)
    {
        GameObject newObj = Instantiate(obj);
        newObj.transform.SetParent(obj.transform.parent);
        newObj.transform.SetLocalPositionAndRotation(obj.transform.localPosition, obj.transform.localRotation);
        newObj.transform.localScale = obj.transform.localScale;
        newObj.name = obj.name + "Copy";
        DetachFromAllParents(obj.transform);
    }

    //public void Create

    public void DetachFromAllParents(Transform transform)
    {
        transform.SetParent(null);
        DebugLogger.Instance.Log("Detached " + transform.name + " from all parents");
        //controlUI.transform.SetParent(null);
    }

    public void DestroyCopyAndSpawnAsset(GameObject obj)
    {
        if(Recorder.Instance.examples.Count == 0)
        {
            DebugLogger.Instance.Log("No examples to spawn asset in");
            Destroy(obj);
            return;
        }

        //TODO: only one prefab and change the parameter (for god sake)
        if (obj.name.StartsWith("Sphere"))
        {
            AssetManager.Instance.CreateAsset(obj.transform, spherePrefab);
        }
        else if (obj.name.StartsWith("Cube"))
        {
            AssetManager.Instance.CreateAsset(obj.transform, cubePrefab);
        }
        else if (obj.name.StartsWith("Text"))
        {
            AssetManager.Instance.CreateAsset(obj.transform, textAssetPrefab);
        }        
        
        //if (obj.GetComponent<MeshCopy>() == null)
        {
           //Destroy(obj);
        }
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

    public void PressedRecreateStatePlaceholders() {
       Recorder.Instance.RecreateTimelineUI_StatePlaceholders();
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
            
            assetsDict.Add(entry.Key, (assetFrames, assetActions));
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
        if (CollisionModels.Exists(collision => collision.StartIndex == frameStart && collision.isCollidingWith(assetGameObject,anotherGameObject)))
        {
            //We have a similar collision, so we ignore it
            return;
        }
        var newCollisionModel = new CollisionSequence();
        newCollisionModel.StartIndex = frameStart;
        newCollisionModel.CollisionType = COLLISION_ENUM.COLLIDE; //TODO needed?
        newCollisionModel.CollidingObject1 = assetGameObject;
        newCollisionModel.CollidingObject2 = anotherGameObject;

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
        
        newCollisionModel.collisionDelegate = (Frame frame) => { frame.IsColliding(assetGameObject, collidedObjectOnLiveMode);};
        
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
}

public class CollisionModel
{
    public Action<Frame> collisionDelegate;

    public CollisionModel(int frameStart, GameObject assetGameObject, GameObject anotherGameObject)
    {
        FrameStart = frameStart;
        AssetGameObject = assetGameObject;
        ColliderGameObject = anotherGameObject;
    }

    public GameObject ColliderGameObject { get; }

    public GameObject AssetGameObject { get; }

    public int FrameStart { get; }
    public int FrameEnd { get; set;  }
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
}

public enum COLLISION_ENUM { NONE, COLLIDE, UNDEFINED};

public class CollisionSequence : Sequence {
    public COLLISION_ENUM CollisionType { get; set; } 

    public GameObject CollidingObject1 { get; set; }
    public GameObject CollidingObject2 { get; set; }

    public Action<Frame> collisionDelegate { get; set; }

    public override Func<Frame, bool> AddConditionToFunction(Func<Frame, bool> conditionFunction)
    {
        return (Frame frame) => { return conditionFunction(frame) && frame.IsColliding(CollidingObject1,CollidingObject2);};
    }

    public override bool CanTriggerAt(int stateStartIndex)
    {
        return StartIndex == stateStartIndex || (stateStartIndex > StartIndex && stateStartIndex < (StartIndex + Length));
    }

    public override string ToString() {
        return $"{CollidingObject1.tag} hit {CollidingObject2.tag}";
    }
    
    
    public override object Clone()
    {
        // Create a new instance of the class
        CollisionSequence clonedCollision = new();
        
        // Copy the properties of the current object
        clonedCollision.StartIndex = StartIndex;
        clonedCollision.Length = Length;
        clonedCollision.CollisionType = CollisionType;
        clonedCollision.CollidingObject1 = CollidingObject1;
        clonedCollision.CollidingObject2 = CollidingObject2;

        // Return the cloned object
        return clonedCollision;
    }

    public bool isCollidingWith(GameObject assetGameObject, GameObject anotherGameObject)
    {
        return (CollidingObject1 == assetGameObject && CollidingObject2 == anotherGameObject) || (CollidingObject1 == anotherGameObject && CollidingObject2 == assetGameObject);
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
    };

public class AssetActionSequence : Sequence
{
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
    
    public static bool IsAddForceAction(string actionName)
    {
        return actionName.Equals("ApplyForce()", StringComparison.OrdinalIgnoreCase);
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
}


