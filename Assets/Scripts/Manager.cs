using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Dependencies.NCalc;
using System.ComponentModel;

//using Assets.OVR.Scripts;
//using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UI;

public class Manager : MonoBehaviour
{
    public GameObject leftHandMenu;
    public AppState currAppState;
    public SpeechToText speechToTextEngine;
    public GameObject spherePrefab;
    public GameObject cubePrefab;
    public GameObject textAssetPrefab;

    public enum AppState
    {
        INIT,
        RECORDING,
        RECORDING_DURING_PLAYBACK,
        ASSETRECORDING,
        PLAYBACK,
        LIVE,
        EDITCOLLIDERS
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
        currAppState = Manager.AppState.INIT;
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
        //AssetManager.Instance.HideMiscObjs();
        AssetManager.Instance.SetAllAssetMenusPokeable(false);
        Recorder.Instance.CreateStateMachine();
        // Recorder.Instance.ResetStateMachine();
        InputManager.Instance.NotifyCollision(null,null);
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

        Recorder.Instance.RefreshTimelineGestures();
    }
    public void ToggleLeftHandEventRow(Boolean leftHandEventsOn) {
        foreach (var eachLeftHandData in Recorder.Instance.currentActiveExample.leftHandFrames)
        {
            eachLeftHandData.isActive = !eachLeftHandData.isActive;
        }

        Recorder.Instance.RefreshTimelineGestures();
    }

    public void PressedRecreateStatePlaceholders() {
       Recorder.Instance.CreateStatePlaceholders();
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

    public List<GestureSequence> LeftHandGestureSequences = new();
    public List<GestureSequence> RightHandGestureSequences = new();

    public List<VoiceSequence> VoiceCommandSequences = new();

    public List<GestureSequence> AllGestureSequences = new();
    public List<List<AssetActionSequence>> assetSequencesLists = new();
    public List<List<CollisionSequence>> collisionSequencesLists = new();
    
    public GameObject startRecordingButton, stopRecordingButton;


    public int numberOfRecordedFrames = 0;
    public Button button;
    //Create a dictionary matching assets to the list of their recordable frames
    public Dictionary<Asset, List<AssetFrame>> assetFramesDict;   

    //public List<Recordable> assets;
    public List<StateTimelineUIElement> StatePlaceholders;
    // public Dictionary<State, StateTimelineUIElement> StatesDict;

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

        LeftHandGestureSequences = new();
        RightHandGestureSequences = new();
        VoiceCommandSequences = new();
        AllGestureSequences = new();
        assetSequencesLists = new();
        collisionSequencesLists = new();

        assetFramesDict = new Dictionary<Asset, List<AssetFrame>>();
        StatePlaceholders = new();
        // StatesDict = new Dictionary<State, StateTimelineUIElement>();
        //Copy assetsInScene to assets
        foreach (Asset recordable in Recorder.Instance.assetsInScene)
        {            
            //Create a new list of recordable frames for each asset
            assetFramesDict.Add(recordable, new List<AssetFrame>());            
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

        assetFramesDict = new Dictionary<Asset, List<AssetFrame>>();
        foreach (var entry in example.assetFramesDict)
        {
            assetFramesDict.Add(entry.Key, new List<AssetFrame>(entry.Value.Select(item => (AssetFrame)item.Clone())));
        }
    }

    public void RefreshAssetsInExample()
    {
        //Copy assetsInScene to assets
        foreach (Asset recordable in Recorder.Instance.assetsInScene)
        {
            //Create a new list of recordable frames for each asset
            if (!assetFramesDict.ContainsKey(recordable))
            {
                assetFramesDict.Add(recordable, new List<AssetFrame>());
            }
        }

        //Remove assets that are no longer in the scene
        List<Asset> assetsToRemove = new List<Asset>();
        foreach (Asset recordable in assetFramesDict.Keys)
        {
            if (!Recorder.Instance.assetsInScene.Contains(recordable))
            {
                assetsToRemove.Add(recordable);
            }
        }
        foreach (Asset recordable in assetsToRemove)
        {
            assetFramesDict.Remove(recordable);
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

    public void ResetData()
    {
        DebugLogger.Instance.Log("Resetting data for example " + exampleId);
        leftHandFrames.Clear();
        rightHandFrames.Clear();
        headFrames.Clear();
        //assets.Clear();
        foreach(Asset recordable in assetFramesDict.Keys)
        {
            assetFramesDict[recordable].Clear();
            foreach (GameObject obj in recordable.forceArrows)
            {
                UnityEngine.Object.Destroy(obj);
            }
        }
    }

    public void DeleteExample()
    {
        //Delete the example from the list of examples
    }

}

public abstract class Sequence {
    public int StartIndex { get; set; }
    public int Length { get; set; }

    abstract public Boolean CanTriggerAt(int stateStartIndex);
    abstract public Func<Frame,bool> AddConditionToFunction(Func<Frame,bool> conditionFunction);
}

public enum COLLISION_ENUM { NONE, COLLIDE, UNDEFINED};

public class CollisionSequence : Sequence {
    public COLLISION_ENUM CollisionType { get; set; } 

    public GameObject CollidingObject1 { get; set; }
    public GameObject CollidingObject2 { get; set; }

    public override Func<Frame, bool> AddConditionToFunction(Func<Frame, bool> conditionFunction)
    {
        return (Frame frame) => { return conditionFunction(frame) && frame.IsColliding(CollidingObject1,CollidingObject2);};
    }

    public override bool CanTriggerAt(int stateStartIndex)
    {
        return StartIndex < stateStartIndex && stateStartIndex < (StartIndex + Length);
    }

    public override string ToString() {
        return $"{CollidingObject1.tag},{CollidingObject2.tag}";
    }
}
public enum ACTION_ENUM { 
    [Description("None")]
    NONE, 
    [Description("Follow(Left hand)")]
    FOLLOW_LEFT_HAND,
    [Description("Follow(Right hand)")]
    FOLLOW_RIGHT_HAND,
    [Description("Follow(L-focus)")]
    FOLLOW_L_FOCUS,
    [Description("Follow(R-focus)")]
    FOLLOW_R_FOCUS,
    [Description("Follow(G-focus)")]
    FOLLOW_G_FOCUS,
    [Description("ApplyForce()")]
    APPLY_FORCE,
    [Description("Show()")]
    SHOW, 
    [Description("Hide()")]
    HIDE, 
    [Description("ChangeColor()")]
    CHANGE_COLOR,
    [Description("Pin()")]
    PIN,
    [Description("Unfollow()")]
    UNFOLLOW,
    [Description("ResetPhysics()")]
    RESET_PHYSICS,
    UNDEFINED,
    };

public class AssetActionSequence : Sequence
{

    public ACTION_ENUM ActionType { get; set; } //None, Physics, Follow, Show, Hide

    public Action ActionDelegate { get; set; }

    public override Func<Frame, bool> AddConditionToFunction(Func<Frame, bool> conditionFunction)
    {
        throw new NotImplementedException();
    }

    public override bool CanTriggerAt(int stateStartIndex)
    {
        throw new NotImplementedException();
    }
    
    public override string ToString() {
        return $"{ActionType}";
    }


    //public GestureManager.Gesture GestureType { get; set; }


}

public class GestureSequence : Sequence
{
    public InputManager.Gesture GestureType { get; set; }
    override public Boolean CanTriggerAt(int stateStartIndex) {
        return StartIndex == stateStartIndex;
    }

    public override Func<Frame, bool> AddConditionToFunction(Func<Frame, bool> conditionFunction) {
        switch (GestureType) {
            case InputManager.Gesture.LEFTHANDGRAB:
            case InputManager.Gesture.LEFTHANDPINCH:
            case InputManager.Gesture.LEFTHANDOPEN:
                return (Frame frame) => { return conditionFunction(frame) && frame.leftHandGesture == GestureType;};
            case InputManager.Gesture.RIGHTHANDGRAB:
            case InputManager.Gesture.RIGHTHANDPINCH:
            case InputManager.Gesture.RIGHTHANDOPEN:
                return (Frame frame) => { return conditionFunction(frame) && frame.rightHandGesture == GestureType;};
            case InputManager.Gesture.LEFTHANDNONE:
            case InputManager.Gesture.RIGHTHANDNONE:
            case InputManager.Gesture.LEFTHANDMENUOPEN: 
            default:
                DebugLogger.Instance.Log("Ignoring GestureType in GestureSequence >> addConditionToFunction for " + GestureType);
                return conditionFunction;
        }
    }
    public override string ToString() {
        return $"GestureSequence {GestureType.ToString()}";
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
}


