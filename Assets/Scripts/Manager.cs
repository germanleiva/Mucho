using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.FullSerializer;
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
            DontDestroyOnLoad(gameObject);
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
        currAppState = Manager.AppState.LIVE;
        Recorder.Instance.SetPlaybackObjectsVisibility(false);
        AssetManager.Instance.HideMiscObjs();
        AssetManager.Instance.SetAllAssetMenusPokeable(false);
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
        Recorder.Instance.SetPlaybackObjectsVisibility(true);
        AssetManager.Instance.ShowMiscObjs();
        AssetManager.Instance.SetAllAssetMenusPokeable(true);
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

    public void DetachFromAllParents(Transform transform)
    {
        transform.SetParent(null);
        DebugLogger.Instance.Log("Detached " + transform.name + " from all parents");
        //controlUI.transform.SetParent(null);
    }

    public void DestroyCopyAndSpawnAsset(GameObject obj)
    {
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
        Destroy(obj);
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

    public Button button;
    //Create a dictionary matching assets to the list of their recordable frames
    public Dictionary<Recordable, List<RecordableFrame>> assetDataDict;

    //public List<Recordable> assets;

    Dictionary<State, StateTimelineUIElement> StatesDict = new();

    public Example(Button _button)
    {
        ++exampleCount;
        exampleId = exampleCount;
        _button.GetComponentInChildren<TMPro.TMP_Text>().text = exampleId.ToString();
        button = _button;
        //exampleId = int.Parse(button.GetComponentInChildren<TMPro.TMP_Text>().text);
        //exampleSelectionButton.onClick.AddListener(() => { DebugLogger.Instance.Log("Example " + exampleId + " selected"); });
        leftHandData = new List<HandFrame>();
        rightHandData = new List<HandFrame>();
        headData = new List<HeadFrame>();
        //assets = new List<Recordable>();
        assetDataDict = new Dictionary<Recordable, List<RecordableFrame>>();
        //Copy assetsInScene to assets
        foreach (Recordable recordable in Recorder.Instance.assetsInScene)
        {            
            //Create a new list of recordable frames for each asset
            assetDataDict.Add(recordable, new List<RecordableFrame>());            
        }
        //Add a new example to the list of examples
        //Create a new example object
        //Add the example object to the list of examples

        DebugLogger.Instance.Log("Created example " + exampleId);
    }

    public void CopyExampleDataFrom(Example example)
    {
        DebugLogger.Instance.Log("Copying data from example " + example.exampleId + " to example " + exampleId);
        leftHandData = example.leftHandData.ToList();
        rightHandData = example.rightHandData.ToList();
        headData = example.headData.ToList();
        //assets = new List<Recordable>(example.assets);
        assetDataDict = new Dictionary<Recordable, List<RecordableFrame>>(example.assetDataDict);
        //Print details of assetDataDict
        foreach (Recordable recordable in assetDataDict.Keys)
        {
            DebugLogger.Instance.Log("Asset " + recordable.name + " has " + assetDataDict[recordable].Count + " frames");
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
                assetDataDict.Add(recordable, new List<RecordableFrame>());
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
        }
    }

    public void DeleteExample()
    {
        //Delete the example from the list of examples
    }

}


