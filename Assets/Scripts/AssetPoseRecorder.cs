using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.UI;

public class AssetPoseRecorder : MonoBehaviour
{   
    public static AssetPoseRecorder Instance { get; private set; }

    public List <Recordable> recordableAssets = new();
    
    public Recordable currentActiveRecordable;

    public Recorder mainRecorder;

    //List of all force arrow components
    public List<ForceArrow> forceArrowsInScene = new();

    Vector3 lastLocalPosition = Vector3.zero;

    //public Recordable[] objectsToRecord;

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
        lastLocalPosition = transform.localPosition;
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
            recordable.PropagateShowStatusToSubsequentFrames(mainRecorder.playbackSlider.value, false);
            //recordable.InsertAssetRecordFrame(mainRecorder.playbackSlider.value,true);
            //recordable.Record(mainRecorder.playbackSlider.value);
        }
    }

    public void Show(Recordable recordable)
    {
        //Turn the material in recordable.playbackObject to 1 alpha
        recordable.playbackObject.GetComponent<MeshRenderer>().material = defaultMaterial;
        recordable.showStatus = true;
        //if (recordable.isAssetRecordingOn)
        if(mainRecorder.GetSizeOfMainRecordedData() > 0)
        {
            DebugLogger.Instance.Log("Recorded show for " + recordable.playbackObject.name);
            recordable.PropagateShowStatusToSubsequentFrames(mainRecorder.playbackSlider.value, true);
            //recordable.InsertAssetRecordFrame(mainRecorder.playbackSlider.value,true);
            //recordable.Record(mainRecorder.playbackSlider.value);
            //Re
        }
    }

    public void StartRecording(Recordable recordable)
    {
        Manager.Instance.currAppState = Manager.AppState.ASSETRECORDING;
        //currAppState = Manager.AppState.RECORD;
        //rootPlaybackArea.SetActive(false);
        DebugLogger.Instance.Log("StartRecording in " + recordable.playbackObject.name);
        currentActiveRecordable = recordable;
        currentActiveRecordable.recordingMode = Recordable.RecordingMode.ManualAnimation;
        currentActiveRecordable.recordedData ??= new List<RecordFrameData>(mainRecorder.GetSizeOfMainRecordedData());
        DebugLogger.Instance.Log("Size of main recorded data: " + mainRecorder.GetSizeOfMainRecordedData());
        CopyTimeStampsFromMainRecorder(currentActiveRecordable);

        //currentActiveRecordable.ResetData();        
        recordable.isAssetRecordingOn = true;
        recordable.isAssetPlaybackOn = false;

    }

    private void CopyTimeStampsFromMainRecorder(Recordable recordable)
    {
        foreach(var data in mainRecorder.objectsToRecord[0].recordedData)
        {
            recordable.recordedData.Add(new RecordFrameData(recordable.playbackObject.transform.localPosition, recordable.playbackObject.transform.localRotation, true, data.timestamp));
        }
    }

    public void StopRecording(Recordable recordable)
    {
        Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        DebugLogger.Instance.Log("StopRecording in " + recordable.playbackObject.name);
        recordable.isAssetRecordingOn = false;
        recordable.isAssetPlaybackOn = true;
        //mainRecorder.playbackSlider.value = 0;

    }

    public void ResetRecording(Recordable recordable)
    {
        DebugLogger.Instance.Log("ResetRecording in " + recordable.playbackObject.name);
        recordable.isAssetRecordingOn = false;
        recordable.isAssetPlaybackOn = false;
        recordable.ResetData();
        //mainRecorder.playbackSlider.value = 0;
    }
   
   public float movementRecordThreshold = 0.0001f;
   public float playbackSpeed = 0.003f;

   public void SetPlaybackSpeed(float speed)
    {
         playbackSpeed = speed;
    }

    public void SetDistanceFromHand(float distance)
    {
        movementRecordThreshold = distance;
    }

    private void Update()
    {
        if(currentActiveRecordable != null)
        {
            if (currentActiveRecordable.isAssetRecordingOn)
            {
                if(currentActiveRecordable.recordingMode == Recordable.RecordingMode.ManualAnimation)
                {
                    //DebugLogger.Instance.Log("Current Position: " + currentActiveRecordable.playbackObject.transform.localPosition + " Last Recorded Position: " + lastLocalPosition);
                    //Check if there is a difference between the current position and the last recorded position and if the difference is greater than 0.01 then record the current position
                    //DebugLogger.Instance.Log("Distance: " + Vector3.Distance(currentActiveRecordable.playbackObject.transform.localPosition, lastLocalPosition));
                    if (Vector3.Distance(currentActiveRecordable.playbackObject.transform.localPosition, lastLocalPosition) > movementRecordThreshold)            
                    {
                        
                        //print into debugger.instance.log both the current position and the last recorded position
                        
                        DebugLogger.Instance.Log("Added new frame data for " + currentActiveRecordable.playbackObject.name + " at " + mainRecorder.playbackSlider.value);
                        //recordable.Record(Time.time - recordStartTime);
                        //currentActiveRecordable.Record(mainRecorder.playbackSlider.value);
                        currentActiveRecordable.InsertAssetRecordFrame(mainRecorder.playbackSlider.value);
                        //Increment the slider value by a small value proportional to the total recording time
                        mainRecorder.playbackSlider.value += playbackSpeed;// / (mainRecorder.recordingDuration * 100);
                        
                    }
                    lastLocalPosition = currentActiveRecordable.playbackObject.transform.localPosition;
                }
                else if(currentActiveRecordable.recordingMode == Recordable.RecordingMode.Physics)
                {
                    currentActiveRecordable.InsertAssetRecordFrame(mainRecorder.playbackSlider.value);
                    //Increment the slider value by frame duration
                    mainRecorder.playbackSlider.value += Time.deltaTime;

                }
                else if(currentActiveRecordable.recordingMode == Recordable.RecordingMode.Attach)
                {
                    
                }
                //recordable.Record(Time.time - recordStartTime);
            }
            else if (currentActiveRecordable.isAssetPlaybackOn)
            {
                //float currentTime = Time.time - recordStartTime;
                float currentTime = mainRecorder.playbackSlider.value;
                
                foreach (var recordable in recordableAssets)
                {
                    //DebugLogger.Instance.Log("Playing back " + recordable.playbackObject.name + " at " + currentTime);
                    // Find the two frames to interpolate between.
                    RecordFrameData previousFrame = null;
                    RecordFrameData nextFrame = null;
                    foreach (var data in recordable.recordedData)
                    {
                        if (data.timestamp <= currentTime)
                        {
                            previousFrame = data;
                        }
                        else
                        {
                            nextFrame = data;
                            break;
                        }
                    }

                    if (recordable.playbackObject != null)
                    {
                        if (previousFrame != null && nextFrame != null)
                        {
                            // Interpolate between the two frames.
                            float t = (currentTime - previousFrame.timestamp) / (nextFrame.timestamp - previousFrame.timestamp);
                            recordable.playbackObject.transform.localPosition = Vector3.Lerp(previousFrame.rootPosition, nextFrame.rootPosition, t);
                            recordable.playbackObject.transform.localRotation = Quaternion.Lerp(previousFrame.rootRotation, nextFrame.rootRotation, t) * Quaternion.Euler(recordable.rotationCorrection);//Modify the rotation in the recorded data to add 180 degrees to the  axis
                            if (previousFrame.showStatusForThisFrame)
                            {
                                //DebugLogger.Instance.Log("Playing back show for " + recordable.playbackObject.name);
                                recordable.playbackObject.GetComponent<MeshRenderer>().material = defaultMaterial;
                            }
                            else
                            {
                                //DebugLogger.Instance.Log("Playing back hide for " + recordable.playbackObject.name);
                                recordable.playbackObject.GetComponent<MeshRenderer>().material = transparentMaterial;
                            } 
                        }
                        else if (previousFrame != null)
                        {
                            // If there's no next frame, use the data from the previous frame.
                            recordable.playbackObject.transform.localPosition = previousFrame.rootPosition;
                            recordable.playbackObject.transform.localRotation = previousFrame.rootRotation;
                        }
                    }
                }
            }
        }
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

    public void SpawnSphere()
    {
        DebugLogger.Instance.LogInVR("Spawned Sphere");
        GameObject obj = Instantiate(spherePrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
        obj.SetActive(true);
        recordableAssets.Add(obj.GetComponentInChildren<Recordable>());
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }

    public void SpawnSphere(Transform target)
    {
        DebugLogger.Instance.LogInVR("Spawned Sphere");
        GameObject obj = Instantiate(spherePrefab, target.position, Quaternion.identity);
        obj.SetActive(true);
        recordableAssets.Add(obj.GetComponentInChildren<Recordable>());
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }

    public void SpawnCube()
    {
        DebugLogger.Instance.LogInVR("Spawned Cube");
        GameObject obj = Instantiate(cubePrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
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

    public void SpawnText()
    {
        DebugLogger.Instance.LogInVR("Spawned Text");
        GameObject obj = Instantiate(textAsset, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
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
