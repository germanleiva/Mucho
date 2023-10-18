using System.Collections;
using System.Collections.Generic;
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
    int exampleId;
    List<HandFrame> leftHandFrames;
    List<HandFrame> rightHandFrames;

    List<HeadFrame> headFrames;
    //Create a dictionary matching assets to the list of their recordable frames
    Dictionary<Recordable, List<RecordableFrame>> assetDataDict;

    Dictionary<State, StateTimelineUIElement> StatesDict = new();

    public void CreateExample(Button exampleSelectionButton)
    {
        ++exampleCount;
        exampleId = exampleCount;
        exampleSelectionButton.GetComponentInChildren<TMPro.TMP_Text>().text = exampleId.ToString();
        leftHandFrames = new List<HandFrame>();
        rightHandFrames = new List<HandFrame>();
        headFrames = new List<HeadFrame>();
        //Add a new example to the list of examples
        //Create a new example object
        //Add the example object to the list of examples
    }

    public void DeleteExample()
    {
        //Delete the example from the list of examples
    }

}


