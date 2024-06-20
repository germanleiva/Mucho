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

    public static Boolean isForceArrowGhostActive = false;


    //public List <Recordable> assets = new();

    //public Recorder Recorder.Instance.;

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
        int size = Recorder.Instance.GetSizeOfMainRecordedData(); //Get size of head in main recorder
        foreach(Asset asset in Recorder.Instance.currentActiveExample.assetsDict.Keys)
        {
            if(Recorder.Instance.currentActiveExample.assetsDict[asset].assetFrames.Count != size)
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
        
        switch (Manager.Instance.currAppState)
        {
            case Manager.AppState.RECORDING:
                foreach(Asset asset in Recorder.Instance.currentActiveExample.assetsDict.Keys)
                {
                    var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetsDict[asset].assetFrames;
                    currentAssetRecordedData.Add(new(asset.transform.position, asset.transform.rotation, asset.isVisible, asset.CurrentColor));
                }
                break;
            case Manager.AppState.PLAYBACK:
                foreach(Asset asset in Recorder.Instance.currentActiveExample.assetsDict.Keys)
                {
                    asset.PlaybackAssetFrame();
                }
                break;
        }
    }

    public void CreateAsset(GameObject copyObjectDragged, GameObject prefab, string name, Mesh mesh = null)
    {
        DebugLogger.Instance.Log("Spawned Asset");
        GameObject newAssetPrefabCopyObject = Instantiate(prefab, copyObjectDragged.transform.position, Quaternion.identity);
        
        Destroy(copyObjectDragged);

        newAssetPrefabCopyObject.SetActive(true);
        var newAsset = newAssetPrefabCopyObject.GetComponentInChildren<Asset>();
        newAsset.name = name;
        if (mesh != null) {
            newAsset.GetComponent<MeshFilter>().mesh = mesh;
        }
        newAsset.SetInitialVisualMainValues();
        Recorder.Instance.allAssets.Add(newAsset);
        
        foreach (var example in Recorder.Instance.examples)
        {
            example.RefreshAssetsInExample();
        }

        var currentAssetRecordedData = Recorder.Instance.currentActiveExample.assetsDict[newAsset].assetFrames;
        for (int i = 0; i < Recorder.Instance.currentActiveExample.RecordedDataCount; i++)
        {
            //We need to generate the asset frames of the new asset if we already have some recorded data
            currentAssetRecordedData.Add(new(newAsset.transform.position, newAsset.transform.rotation, newAsset.isVisible, newAsset.CurrentColor));
        }

        Recorder.Instance.RecreateTimelineUI_AssetRowsAndCollisions();

        //RefreshAssetsInAllExamples();
        //Recorder.Instance.RefreshTimelineAndStates();
        //Recorder.Instance.RecreateTimelineAssetRows();
        /*if(Recorder.Instance.GetSizeOfMainRecordedData() > 0) //Recording already exists, the main purpose is to check for new collisions
        {
            Manager.Instance.currAppState = Manager.AppState.RECORDING_DURING_PLAYBACK;
            Recorder.Instance.playbackSlider.value = 0;
            InputManager.Instance.SetPlaybackObjectsActive(true);

            if (newAssetGameObject != null)
            {
                Asset recordable = newAssetGameObject.GetComponentInChildren<Asset>();
                if (recordable != null)
                {
                    //This was added to fix a mysterious bug that made the asset not visible after adding it AFTER an input recording was done
                    recordable.CurrentColor = Color.gray;
                    recordable.InitialPosition = recordable.transform.position;
                    recordable.InitialRotation = recordable.transform.rotation;

                }
                {
                    DebugLogger.Instance.Log("SpawnAsset: No Recordable component attached to obj");
                }
            }
            else
            {
                //is this ever called?
                DebugLogger.Instance.Log("SpawnAsset: obj is null");
            }

            StartCoroutine(AddAssetFrameToAssetFramesDict(
                newAssetGameObject.GetComponentInChildren<Asset>()));
        }*/
    }

    public void DeleteAsset(GameObject obj)
    {
        DebugLogger.Instance.Log("Deleted " + obj.name);        
        Recorder.Instance.allAssets.Remove(obj.GetComponentInChildren<Asset>());
        
        //Update the model in all the examples
        for (int i = 0; i < Recorder.Instance.examples.Count; i++)
        {
            var example = Recorder.Instance.examples[i];
            example.RefreshAssetsInExample();

            Action onCompletion = null;
            if (i == Recorder.Instance.examples.Count - 1)
            {
                //Only for the last example
                onCompletion = Recorder.Instance.RecreateTimelineUI_AssetRowsAndCollisions;    
            }
            Recorder.Instance.UpdateAllAssetFramesAndCollisions(0, example, onCompletion);    

        }
        
        Destroy(obj);
    }

    /*public void RefreshAssetsInAllExamples()
    {
        foreach(Example example in Recorder.Instance.examples)
        {
            example.RefreshAssetsInExample();
        }
    }*/

    public void HideMiscObjs()
    {
        foreach (Asset asset in Recorder.Instance.currentActiveExample.assetsDict.Keys)
        {
            asset.assetMenu.SetActive(false);
        }
    }
    
    public void ResetMeshRendererForAllAssets()
    {
        foreach (Asset asset in Recorder.Instance.currentActiveExample.assetsDict.Keys)
        {
            asset.GetComponent<MeshRenderer>().enabled = true;
        }
    }

    public void SetAllAssetMenusPokeable(bool status)
    {
        foreach (Asset recordable in Recorder.Instance.currentActiveExample.assetsDict.Keys)
        {
            recordable.GetComponent<PokeInteractable>().enabled = status;
        }
    }

    public void ResetPhysicsForAllAssetsAndStopFollowing()
    {
        foreach (Asset asset in Recorder.Instance.currentActiveExample.assetsDict.Keys)
        {
            asset.ApplyUnfollow();
            asset.ResetPhysicsPropertiesInLiveMode();
        }
    }

    public void AddForceArrowToAsset(Asset asset)
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
        asset.forceArrows.Add(forceArrowScript);
        forceArrowScript.associatedAsset = asset;
        //forceArrowScript.arrowHead should be positioned 1 unit above the arrowEnd in the y axis
        if (!AssetManager.isForceArrowGhostActive) {
            forceArrowScript.arrowHeadReal.position = asset.gameObject.transform.position + new Vector3(0f,0.2f,0.2f);
            forceArrowScript.ReOrientArrow();
            forceArrowScript.DrawTrajectory(10);     
        } else {
            forceArrowScript.arrowHeadGhost.position = forceArrowScript.arrowHeadReal.position; // recordable.gameObject.transform.position + new Vector3(0f,0.2f,0.2f);
            forceArrowScript.OrientForceArrow();
        }
        //forceArrowScript.arrowHeadGhost.transform.position = forceArrowScript.arrowHeadReal.transform.position;
        //forceArrowScript.DrawTrajectory();     
        //TODO check if it is not really needed (as german said....) I don't think he has right though
        //Recorder.Instance.RecreateTimelineAssetRows(null);
        
    }
   
}


