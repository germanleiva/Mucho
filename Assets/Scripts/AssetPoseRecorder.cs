using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.UI;

public class AssetPoseRecorder : MonoBehaviour
{   
    public static AssetPoseRecorder Instance { get; private set; }

    public List <Recordable> recordableAssets = new();

    public Recorder mainRecorder;

    //List of all force arrow components
    public List<ForceArrow> forceArrowsInScene = new();

    Vector3 lastAssetPosition = Vector3.zero;

    // Start is called before the first frame update

    public GameObject spherePrefab;
    public GameObject cubePrefab;
    public GameObject forceArrowPrefab;
    //public GameObject cylinderPrefab;
    public GameObject textAsset;
    public GameObject hmd;

    public Material defaultMaterial, transparentMaterial;
    
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
    
    void Start()
    {
        lastAssetPosition = transform.position;
    }

    public void Hide(Recordable recordable)
    {
        //Turn the material in recordable.playbackObject to 0.5 alpha
        recordable.playbackObject.GetComponent<MeshRenderer>().material = transparentMaterial;
        recordable.showStatus = false;
        //if(recordable.isAssetRecordingOn)
        if(mainRecorder.GetSizeOfMainRecordedData() > 0)
        {
            DebugLogger.Instance.Log("Recorded hide for  " + recordable.playbackObject.name + " at " + mainRecorder.playbackSlider.value);
            recordable.RecordAndPropagateAssetShowStatus((int)mainRecorder.playbackSlider.value);
        }
    }

    public void Show(Recordable recordable)
    {
        //Turn the material in recordable.playbackObject to 1 alpha
        recordable.playbackObject.GetComponent<MeshRenderer>().material = defaultMaterial;
        recordable.showStatus = true;
        if(mainRecorder.GetSizeOfMainRecordedData() > 0)
        {
            DebugLogger.Instance.Log("Recorded show for " + recordable.playbackObject.name + " at " + mainRecorder.playbackSlider.value);
            recordable.RecordAndPropagateAssetShowStatus((int)mainRecorder.playbackSlider.value);
        }
    }

    public void AttachToLeftHand(Recordable recordable)
    {
        DebugLogger.Instance.Log("Attach called for " + recordable.playbackObject.name);
        recordable.recordingMode = Recordable.RecordingMode.Follow;
        recordable.CopyPoseFrom(mainRecorder.objectsToRecord[1], (int)mainRecorder.playbackSlider.value);
        //recordable.playbackObject.transform.SetParent(mainRecorder.objectsToRecord[1].playbackObject.transform);
    }

    public void AttachToRightHand(Recordable recordable)
    {
        DebugLogger.Instance.Log("Attach called for " + recordable.playbackObject.name);
        recordable.recordingMode = Recordable.RecordingMode.Follow;
        recordable.CopyPoseFrom(mainRecorder.objectsToRecord[2], (int)mainRecorder.playbackSlider.value);
        //recordable.playbackObject.transform.SetParent(mainRecorder.objectsToRecord[2].playbackObject.transform);
    }

    public void Detach(Recordable recordable)
    {
        DebugLogger.Instance.Log("Detach called for " + recordable.playbackObject.name);
        recordable.recordingMode = Recordable.RecordingMode.Follow;
        recordable.CopyPoseFrom(recordable, (int)mainRecorder.playbackSlider.value, true);
        //recordable.playbackObject.transform.SetParent(null);
    }

    public void StartRecording(Recordable recordable)
    {
        Manager.Instance.currAppState = Manager.AppState.ASSETRECORDING;

        DebugLogger.Instance.Log("StartRecording in " + recordable.playbackObject.name);
        //currentActiveRecordable = recordable;
        recordable.recordingMode = Recordable.RecordingMode.ManualAnimation;
    }

    public bool DoRecordSizesMatch()
    {
        int size = mainRecorder.GetSizeOfMainRecordedData(0); //Get size of head in main recorder
        foreach(Recordable recordable in recordableAssets)
        {
            if(recordable.recordedData.Count != size)
            {
                DebugLogger.Instance.Log("Asset recording sizes are different");
                return false;
            }
        }
        DebugLogger.Instance.Log("Asset recording sizes are same");
        return true;
    }

    public void ExpandRecordFramesForAssets(int size)
    {
        foreach(Recordable recordable in recordableAssets)
        {
            recordable.recordedData.Capacity = size;
        }
    }

    public void InitializeRecordFramesForAssets()
    {
        foreach(Recordable recordable in recordableAssets)
        {
            foreach(var data in mainRecorder.objectsToRecord[0].recordedData)
            {
                recordable.recordedData.Add(new RecordFrameData(recordable.playbackObject.transform.position, recordable.playbackObject.transform.rotation, recordable.showStatus, "None", data.frameNumber));
            }
        }
    }

    public void StopRecording(Recordable recordable)
    {
        Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        DebugLogger.Instance.Log("StopRecording in " + recordable.playbackObject.name);
        recordable.recordingMode = Recordable.RecordingMode.None;
    }

    public void ResetRecording(Recordable recordable)
    {
        DebugLogger.Instance.Log("ResetRecording in " + recordable.playbackObject.name);
        recordable.recordingMode = Recordable.RecordingMode.None;
        recordable.ResetData();
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
        foreach(Recordable recordable in recordableAssets)
        {
            if(recordable.recordingMode == Recordable.RecordingMode.ManualAnimation)
            {
                if (Vector3.Distance(recordable.playbackObject.transform.position, lastAssetPosition) > movementRecordThreshold)
                {
                    DebugLogger.Instance.Log("Added new frame data for " + recordable.playbackObject.name + " at " + mainRecorder.playbackSlider.value);

                    recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, "None", true);
                    //Increment the slider value by a small value proportional to the total recording time
                    mainRecorder.playbackSlider.value += playbackSpeed;
                }
                lastAssetPosition = recordable.playbackObject.transform.position;
                
            }
            else if(recordable.recordingMode == Recordable.RecordingMode.Physics)
            {
                recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, "Physics", true);
                //Increment the slider value by frame duration
                mainRecorder.playbackSlider.value += 1;//Time.deltaTime;
            }
            else if(recordable.recordingMode == Recordable.RecordingMode.Follow)
            {
                //recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, true);
                //Increment the slider value by frame duration
                //mainRecorder.playbackSlider.value += 1;//Time.deltaTime;
            }
            
        }

            //else if (currentActiveRecordable.isAssetPlaybackOn)
        if(mainRecorder.GetSizeOfMainRecordedData() > 0)
        {
            //float currentTime = Time.time - recordStartTime;
            int currentFrameNum = (int)mainRecorder.playbackSlider.value;
            
            foreach (var recordable in recordableAssets)
            {
                if (recordable.playbackObject != null && recordable.recordedData.Count > 0)
                {
                    //DebugLogger.Instance.Log("Playing back " + recordable.playbackObject.name + " at " + currentFrameNum);
                    recordable.playbackObject.transform.position = recordable.recordedData[currentFrameNum].rootPosition;
                    recordable.playbackObject.transform.rotation = recordable.recordedData[currentFrameNum].rootRotation * Quaternion.Euler(recordable.rotationCorrection);

                    if (recordable.recordedData[currentFrameNum].showStatusForThisFrame)
                    {
                        //DebugLogger.Instance.Log("Showing " + recordable.playbackObject.name + " at " + currentFrameNum);
                        recordable.playbackObject.GetComponent<MeshRenderer>().material = defaultMaterial;
                    }
                    else
                    {
                        //DebugLogger.Instance.Log("Hiding " + recordable.playbackObject.name + " at " + currentFrameNum);
                        recordable.playbackObject.GetComponent<MeshRenderer>().material = transparentMaterial;
                    }

                }
            }
        }
        //}
    }

    public void EnableGrabForAllAssets()
    {
        DebugLogger.Instance.Log("Enabling grab for all assets");
        foreach (var recordable in recordableAssets)
        {
            DebugLogger.Instance.Log("Enabling grab for " + recordable.playbackObject.name);
            recordable.grabCollider.enabled = true;
        }
    }

    public void DisableGrabForAllAssets()
    {
        DebugLogger.Instance.Log("Disabling grab for all assets");
        foreach (var recordable in recordableAssets)
        {
            DebugLogger.Instance.Log("Disabling grab for " + recordable.playbackObject.name);
            recordable.grabCollider.enabled = false;
        }
    }

    public void PokeTest()
    {
        Debug.Log("Poke test called");
    }


    public void SpawnSphere(Transform target)
    {
        DebugLogger.Instance.LogInVR("Spawned Sphere");
        GameObject obj = Instantiate(spherePrefab, target.position, Quaternion.identity);
        obj.SetActive(true);
        recordableAssets.Add(obj.GetComponentInChildren<Recordable>());
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }

    public void SpawnCube(Transform target)
    {
        DebugLogger.Instance.LogInVR("Spawned Cube");
        GameObject obj = Instantiate(cubePrefab, target.position, Quaternion.identity);
        obj.SetActive(true);
        recordableAssets.Add(obj.GetComponentInChildren<Recordable>());
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }

    public void SpawnText(Transform target)
    {
        DebugLogger.Instance.LogInVR("Spawned Text");
        GameObject obj = Instantiate(textAsset, target.position, Quaternion.identity);
        obj.SetActive(true);
        recordableAssets.Add(obj.GetComponentInChildren<Recordable>());
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }

    public void DeleteAsset(GameObject obj)
    {
        DebugLogger.Instance.LogInVR("Deleted " + obj.name);        
        recordableAssets.Remove(obj.GetComponentInChildren<Recordable>());
        Destroy(obj);
    }

    public void HideMiscObjs()
    {
        foreach (ForceArrow forceArrow in forceArrowsInScene)
        {
            forceArrow.HideTrajectoryAndArrow();
        }
        foreach (Recordable recordable in recordableAssets)
        {
            recordable.assetMenu.SetActive(false);
        }
    }

    public void ShowMiscObjs()
    {
        foreach (ForceArrow forceArrow in forceArrowsInScene)
        {
            forceArrow.ShowTrajectoryAndArrow();
        }
        foreach (Recordable recordable in recordableAssets)
        {
            recordable.assetMenu.SetActive(true);
        }
    }

    public void AddForceArrowToAsset(Recordable recordable)
    {
        //GameObject obj = Instantiate(cubePrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
        GameObject forceArrow = Instantiate(forceArrowPrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);   
        forceArrow.SetActive(true);     
        ForceArrow forceArrowScript = forceArrow.GetComponent<ForceArrow>();
        forceArrowsInScene.Add(forceArrowScript);
        forceArrowScript.asset = recordable.playbackObject.transform;
        //forceArrowScript.arrowHead should be positioned 1 unit above the arrowEnd in the y axis
        forceArrowScript.arrowHead.position = recordable.playbackObject.transform.position + new Vector3(0.2f,0.2f,0);
        forceArrowScript.ReOrientArrow();
             
    }
   
}

public class AssetChangeSequence
{
    public string SourceOfAssetChange { get; set; } //None, Physics, Follow
    public int StartIndex { get; set; }
    public int Length { get; set; }
    //public GestureManager.Gesture GestureType { get; set; }
}
