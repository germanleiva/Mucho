using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.OVR.Scripts;
using UnityEngine;
using UnityEngine.UI;

public class Manager : MonoBehaviour
{
    public GameObject leftHandMenu;

    public AppState currAppState;

    public enum AppState
    {
        INIT,
        RECORDING,
        ASSETRECORDING,
        PLAYBACK,
        TEST,
        LIVE
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
        Recorder.Instance.ResetStateMachine();
        InputManager.Instance.NotifyCollision(null,null);
        CustomStateMachine.Instance.InvokeOnEnterActionsOfInitialState();
    }

    public void ChangeToTestMode()
    {
        currAppState = Manager.AppState.TEST;
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
        AssetManager.Instance.ShowMiscObjs();
        AssetManager.Instance.SetAllAssetMenusPokeable(true);
        Recorder.Instance.ResetStateMachine();
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

        if (obj.name.StartsWith("Sphere"))
        {
            AssetManager.Instance.SpawnSphere(obj.transform);
        }
        else if (obj.name.StartsWith("Cube"))
        {
            AssetManager.Instance.SpawnCube(obj.transform);
        }
        else if (obj.name.StartsWith("Text"))
        {
            AssetManager.Instance.SpawnText(obj.transform);
        }        
        
        if (obj.GetComponent<MeshCopy>() == null)
        {
           Destroy(obj);
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

}

public class Example
{
    public static int exampleCount = 0;
    public int exampleId { get; private set; }
    public List<HandFrame> leftHandData;
    public List<HandFrame> rightHandData;
    public List<HeadFrame> headData;

    public int numberOfRecordedFrames = 0;

    public Button button;
    //Create a dictionary matching assets to the list of their recordable frames
    public Dictionary<Recordable, List<AssetFrame>> assetDataDict;   

    //public List<Recordable> assets;

    public Dictionary<State, StateTimelineUIElement> StatesDict;

    public Example(Button _button)
    {
        ++exampleCount;
        exampleId = exampleCount;
        _button.GetComponentInChildren<TMPro.TMP_Text>().text = exampleId.ToString();
        button = _button;

        leftHandData = new List<HandFrame>();
        rightHandData = new List<HandFrame>();
        headData = new List<HeadFrame>();

        assetDataDict = new Dictionary<Recordable, List<AssetFrame>>();
        StatesDict = new Dictionary<State, StateTimelineUIElement>();
        //Copy assetsInScene to assets
        foreach (Recordable recordable in Recorder.Instance.assetsInScene)
        {            
            //Create a new list of recordable frames for each asset
            assetDataDict.Add(recordable, new List<AssetFrame>());            
        }

        DebugLogger.Instance.Log("Created example " + exampleId);
    }

    public void CopyExampleDataFrom(Example example)
    {
        DebugLogger.Instance.Log("Copying data from example " + example.exampleId + " to example " + exampleId);
        leftHandData = new List<HandFrame>(example.leftHandData.Select(item => (HandFrame)item.Clone()));
        rightHandData = new List<HandFrame>(example.rightHandData.Select(item => (HandFrame)item.Clone()));
        headData = new List<HeadFrame>(example.headData.Select(item => (HeadFrame)item.Clone()));
        foreach (var data in headData) // Clear the voice commands
        {
            data.voiceCommand = null;
        }

        assetDataDict = new Dictionary<Recordable, List<AssetFrame>>();
        foreach (var entry in example.assetDataDict)
        {
            assetDataDict.Add(entry.Key, new List<AssetFrame>(entry.Value.Select(item => (AssetFrame)item.Clone())));
        }
    }

    public void RefreshAssetsInExample()
    {
        //Copy assetsInScene to assets
        foreach (Recordable recordable in Recorder.Instance.assetsInScene)
        {
            //Create a new list of recordable frames for each asset
            if (!assetDataDict.ContainsKey(recordable))
            {
                assetDataDict.Add(recordable, new List<AssetFrame>());
            }
        }

        //Remove assets that are no longer in the scene
        List<Recordable> assetsToRemove = new List<Recordable>();
        foreach (Recordable recordable in assetDataDict.Keys)
        {
            if (!Recorder.Instance.assetsInScene.Contains(recordable))
            {
                assetsToRemove.Add(recordable);
            }
        }
        foreach (Recordable recordable in assetsToRemove)
        {
            assetDataDict.Remove(recordable);
        }
    }

    public void Render()
    {
        //Render the states
        foreach (State state in StatesDict.Keys)
        {
            //StatesDict[state].Render();
        }
    }

    public void ResetData()
    {
        DebugLogger.Instance.Log("Resetting data for example " + exampleId);
        leftHandData.Clear();
        rightHandData.Clear();
        headData.Clear();
        //assets.Clear();
        foreach(Recordable recordable in assetDataDict.Keys)
        {
            assetDataDict[recordable].Clear();
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


