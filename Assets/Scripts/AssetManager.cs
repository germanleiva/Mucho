using System;
using System.Collections;
using System.Collections.Generic;
using Assets.OVR.Scripts;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.UI;

public class AssetManager : MonoBehaviour
{   
    public static AssetManager Instance { get; private set; }

    //public List <Recordable> assets = new();

    public Recorder mainRecorder;

    // Start is called before the first frame update


    public GameObject forceArrowPrefab;
    //public GameObject cylinderPrefab;

    public GameObject hmd;

    public Material transparentMaterial, translucentMaterial;


    
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
    
    void Start()
    {
        //lastAssetPosition = transform.position;
    }



    public bool DoRecordSizesMatch()
    {
        int size = mainRecorder.GetSizeOfMainRecordedData(); //Get size of head in main recorder
        foreach(Recordable recordable in Recorder.Instance.currentActiveExample.assetDataDict.Keys)
        {
            if(Recorder.Instance.currentActiveExample.assetDataDict[recordable].Count != size)
            {
                DebugLogger.Instance.Log("Asset recording sizes are different from main recording size");
                return false;
            }
        }
        DebugLogger.Instance.Log("Asset recording sizes are same as main recording size");
        return true;
    }

    public void ExpandRecordFramesForAssets(int size)
    {
        /*foreach(Recordable recordable in Recorder.Instance.currentActiveExample.assets)
        {
            recordable.recordedData.Capacity = size;
        }*/
    }

   public float movementRecordThreshold;
   public float playbackSpeed;

   public void SetPlaybackSpeed(int speed)
    {
         playbackSpeed = speed;
    }

    public void SetDistanceFromHand(float distance)
    {
        movementRecordThreshold = distance;
    }

    private void FixedUpdate()
    {
        if(Manager.Instance.currAppState == Manager.AppState.LIVE) return;
        
        if(Recorder.Instance.currentActiveExample == null) return;

        //DebugLogger.Instance.Log("Size of assetDataDict: " + Recorder.Instance.currentActiveExample.assetDataDict.Count);
        

        if(Manager.Instance.currAppState == Manager.AppState.RECORDING)
        {
            foreach(Recordable recordable in Recorder.Instance.currentActiveExample.assetDataDict.Keys)
            {
                recordable.RecordAssetFrame();
                
            }
        }
        else if (Manager.Instance.currAppState == Manager.AppState.PLAYBACK)
        {
            foreach(Recordable recordable in Recorder.Instance.currentActiveExample.assetDataDict.Keys)
            {
                recordable.PlaybackAssetFrame();
            }
        }
    }

    public void CreateAsset(Transform target, GameObject prefab)
    {
        DebugLogger.Instance.Log("Spawned Asset");
        GameObject obj = Instantiate(prefab, target.position, Quaternion.identity);

        Destroy(target.gameObject);

        obj.SetActive(true);
        Recorder.Instance.assetsInScene.Add(obj.GetComponentInChildren<Recordable>());
        Recorder.Instance.currentActiveExample.RefreshAssetsInExample();
        mainRecorder.RefreshTimelineAndStates();
        if(mainRecorder.GetSizeOfMainRecordedData() > 0) //Recording already exists
        {
            //Manager.Instance.currAppState = Manager.AppState.RECORDING;
            mainRecorder.playbackSlider.value = 0;
            //InputManager.Instance.SetPlaybackContactSphereActive(true);
   
            if (obj != null)
            {
                Recordable recordable = obj.GetComponentInChildren<Recordable>();
                if (recordable != null)
                {
                    recordable.SetColor(Color.gray);
                }
                {
                    DebugLogger.Instance.Log("SpawnAsset: No Recordable component attached to obj");
                }
            }
            else
            {
                DebugLogger.Instance.Log("SpawnAsset: obj is null");
            }
            StartCoroutine(AddAssetFrameToAssetDataDict(obj.GetComponentInChildren<Recordable>()));
        }
    }


    IEnumerator AddAssetFrameToAssetDataDict(Recordable recordable)
    {
        //Call recordable.RecordAssetFrame() every 0.1 seconds until the size of the assetDataDict is equal to the size of the main recording
        while(Recorder.Instance.currentActiveExample.assetDataDict[recordable].Count < mainRecorder.GetSizeOfMainRecordedData())
        {
            mainRecorder.playbackSlider.value = Recorder.Instance.currentActiveExample.assetDataDict[recordable].Count;
            recordable.RecordAssetFrame();
            yield return new WaitForSeconds(0.01f);
        }
        //Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        //InputManager.Instance.SetPlaybackContactSphereActive(false);
        DebugLogger.Instance.Log("AddAssetFrameToAssetDataDict: DoRecordSizesMatch() - " + DoRecordSizesMatch());
    }

    /*public void SpawnCube(Transform target)
    {
        DebugLogger.Instance.Log("Spawned Cube");
        GameObject obj = Instantiate(cubePrefab, target.position, Quaternion.identity);
        obj.SetActive(true);
        Recorder.Instance.assetsInScene.Add(obj.GetComponentInChildren<Recordable>());
        Recorder.Instance.currentActiveExample.RefreshAssetsInExample();
        mainRecorder.RefreshTimelineAndStates();
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }

    public void SpawnText(Transform target)
    {
        DebugLogger.Instance.Log("Spawned Text");
        GameObject obj = Instantiate(textAsset, target.position, Quaternion.identity);
        obj.SetActive(true);
        Recorder.Instance.assetsInScene.Add(obj.GetComponentInChildren<Recordable>());
        Recorder.Instance.currentActiveExample.RefreshAssetsInExample();
        mainRecorder.RefreshTimelineAndStates();
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }*/

    public void DeleteAsset(GameObject obj)
    {
        DebugLogger.Instance.Log("Deleted " + obj.name);        
        Recorder.Instance.assetsInScene.Remove(obj.GetComponentInChildren<Recordable>());
        Recorder.Instance.currentActiveExample.RefreshAssetsInExample();
        mainRecorder.RefreshTimelineAndStates();
        Destroy(obj);
    }

    public void HideMiscObjs()
    {
        foreach (Recordable recordable in Recorder.Instance.currentActiveExample.assetDataDict.Keys)
        {
            recordable.assetMenu.SetActive(false);
        }
    }

    public void ShowMiscObjs()
    {
        foreach (Recordable recordable in Recorder.Instance.currentActiveExample.assetDataDict.Keys)
        {
            recordable.assetMenu.SetActive(true);
        }
    }

    public void ResetMeshRendererForAllAssets()
    {
        foreach (Recordable recordable in Recorder.Instance.currentActiveExample.assetDataDict.Keys)
        {
            recordable.GetComponent<MeshRenderer>().enabled = true;
        }
    }

    public void SetAllAssetMenusPokeable(bool status)
    {
        foreach (Recordable recordable in Recorder.Instance.currentActiveExample.assetDataDict.Keys)
        {
            recordable.GetComponent<PokeInteractable>().enabled = status;
        }
    }

    public void AddForceArrowToAsset(Recordable recordable)
    {
        /*foreach (GameObject obj in recordable.forceArrows)
        {
            Destroy(obj);
        }*/ 

        //GameObject obj = Instantiate(cubePrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
        GameObject forceArrow = Instantiate(forceArrowPrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);   
        forceArrow.SetActive(true);     
        ForceArrow forceArrowScript = forceArrow.GetComponent<ForceArrow>();
        forceArrowScript.indexWhereArrowIsVisible = (int) Recorder.Instance.playbackSlider.value;
        forceArrowScript.associatedExample = Recorder.Instance.currentActiveExample;
        recordable.forceArrows.Add(forceArrow);
        forceArrowScript.asset = recordable.gameObject.transform;
        //forceArrowScript.arrowHead should be positioned 1 unit above the arrowEnd in the y axis
        forceArrowScript.arrowHead.position = recordable.gameObject.transform.position + new Vector3(0f,0.2f,0.2f);
        forceArrowScript.ReOrientArrow();
        forceArrowScript.DrawTrajectory();     
    }
   
}


