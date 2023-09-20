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

    public Material defaultMaterial, transparentMaterial, translucentMaterial;

    int firstFrameOfManualRecording, lastFrameOfManualRecording;
    
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

    public void StartRecording(Recordable recordable)
    {
        Manager.Instance.currAppState = Manager.AppState.ASSETRECORDING;
        DebugLogger.Instance.Log("StartRecording in " + recordable.playbackObject.name);
        firstFrameOfManualRecording = (int)mainRecorder.playbackSlider.value;
        //currentActiveRecordable = recordable;
        recordable.currentRecordingMode = Recordable.RecordingType.ManualAnimation;
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
                //recordable.recordedData.Add(new RecordFrameData(recordable.playbackObject.transform.position, recordable.playbackObject.transform.rotation, recordable.showStatus, "None", data.frameNumber));
                recordable.recordedData.Add(new RecordableFrame(recordable.playbackObject.transform.position, recordable.playbackObject.transform.rotation, recordable.showStatus, "None", "None", Recordable.RecordingType.None, null, data.frameNumber));
            }
            recordable.InsertAssetRecordFrame(0, action: "None", collision: "None", Recordable.RecordingType.ManualAnimation);
        }
    }

    public void StopRecording(Recordable recordable)
    {
        Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        recordable.currentRecordingMode = Recordable.RecordingType.None;
        DebugLogger.Instance.Log("StopRecording in " + recordable.playbackObject.name);
        lastFrameOfManualRecording = (int)mainRecorder.playbackSlider.value;
        DebugLogger.Instance.Log("First frame: " + firstFrameOfManualRecording + " Last frame: " + lastFrameOfManualRecording);
        //CheckIfAssetIsFollowingAnything(recordable);
        mainRecorder.RefreshAssetsTimeline(mainRecorder.assetTimelinePanelPrefab);
    }

    public void CheckIfAssetIsFollowingAnything(Recordable recordable)
    {
        // Initialize variables to keep track of the count of the closest objects
        int leftHandCount = 0;
        int rightHandCount = 0;
        int leftFocusSquareCount = 0;
        int rightFocusSquareCount = 0;
        int headFocusSquareCount = 0;

        // Iterate over the frames from firstFrameOfManualRecording to lastFrameOfManualRecording
        for (int i = firstFrameOfManualRecording; i <= lastFrameOfManualRecording; i++)
        {
            // Get the RecordFrameData for the current frame
            var assetFrameData = recordable.recordedData[i];
            var headFrameData = mainRecorder.objectsToRecord[0].recordedData[i];
            var leftHandFrameData = mainRecorder.objectsToRecord[1].recordedData[i];
            var rightHandFrameData = mainRecorder.objectsToRecord[2].recordedData[i];

            // Calculate the distances to the left hand, right hand, left focus square, and right focus square
            // Note: We need to replace the placeholders below with the actual way to access the positions of these objects
            float distanceToLeftHand = Vector3.Distance(assetFrameData.rootPosition, leftHandFrameData.rootPosition);
            float distanceToRightHand = Vector3.Distance(assetFrameData.rootPosition, rightHandFrameData.rootPosition);
            float distanceToLeftFocusSquare = Vector3.Distance(assetFrameData.rootPosition, leftHandFrameData.focusSquarePosition);
            float distanceToRightFocusSquare = Vector3.Distance(assetFrameData.rootPosition, rightHandFrameData.focusSquarePosition);
            float distanceToHeadFocusSquare = Vector3.Distance(assetFrameData.rootPosition, headFrameData.focusSquarePosition);

            // Find the minimum distance and increment the count for the corresponding object
            float minDistance = Mathf.Min(distanceToLeftHand, distanceToRightHand, distanceToLeftFocusSquare, distanceToRightFocusSquare, distanceToHeadFocusSquare);
            if (minDistance == distanceToLeftHand) leftHandCount++;
            else if (minDistance == distanceToRightHand) rightHandCount++;
            else if (minDistance == distanceToLeftFocusSquare) leftFocusSquareCount++;
            else if (minDistance == distanceToRightFocusSquare) rightFocusSquareCount++;
            else headFocusSquareCount++;
        }

        // Determine which object was closest most frequently and return that information
        int maxCount = Mathf.Max(leftHandCount, rightHandCount, leftFocusSquareCount, rightFocusSquareCount, headFocusSquareCount);
        if (maxCount == leftHandCount)
        {
            DebugLogger.Instance.Log("Asset is following left hand");            
            recordable.CopyPoseFromRecordable(mainRecorder.objectsToRecord[1], firstFrameOfManualRecording, lastFrameOfManualRecording, copyFirstRecord:false, copyRotation:false);
            recordable.CopyPoseFromRecordable(recordable, lastFrameOfManualRecording, copyFirstRecord:true, copyRotation:false);
        }
        else if (maxCount == rightHandCount)
        {
            DebugLogger.Instance.Log("Asset is following right hand");
            recordable.CopyPoseFromRecordable(mainRecorder.objectsToRecord[2], firstFrameOfManualRecording, lastFrameOfManualRecording, copyFirstRecord:false, copyRotation:false);
            recordable.CopyPoseFromRecordable(recordable, lastFrameOfManualRecording, copyFirstRecord:true, copyRotation:false);
        }
        else if (maxCount == leftFocusSquareCount)
        {
            DebugLogger.Instance.Log("Asset is following left focus square");
            recordable.CopyPoseFromFocusSquare(mainRecorder.objectsToRecord[1], firstFrameOfManualRecording, lastFrameOfManualRecording, copyFirstRecord:false, copyRotation:false);
            recordable.CopyPoseFromFocusSquare(recordable, lastFrameOfManualRecording, copyFirstRecord:true, copyRotation:false);
        }
        else if (maxCount == rightFocusSquareCount)
        {
            DebugLogger.Instance.Log("Asset is following right focus square");
            recordable.CopyPoseFromFocusSquare(mainRecorder.objectsToRecord[2], firstFrameOfManualRecording, lastFrameOfManualRecording, copyFirstRecord:false, copyRotation:false);
            recordable.CopyPoseFromFocusSquare(recordable, lastFrameOfManualRecording, copyFirstRecord:true, copyRotation:false);
        }
        else
        {
            DebugLogger.Instance.Log("Asset is following head focus square");
            recordable.CopyPoseFromFocusSquare(mainRecorder.objectsToRecord[0], firstFrameOfManualRecording, lastFrameOfManualRecording, copyFirstRecord:false, copyRotation:false);
            recordable.CopyPoseFromFocusSquare(recordable, lastFrameOfManualRecording, copyFirstRecord:true, copyRotation:false);
        }
  
        //Finally, refresh the timeline
        mainRecorder.RefreshAssetsTimeline(mainRecorder.assetTimelinePanelPrefab);
    }



    public void ResetRecording(Recordable recordable)
    {
        DebugLogger.Instance.Log("ResetRecording in " + recordable.playbackObject.name);
        recordable.currentRecordingMode = Recordable.RecordingType.None;
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
            if(recordable.currentRecordingMode == Recordable.RecordingType.ManualAnimation)
            {
                if (Vector3.Distance(recordable.playbackObject.transform.position, lastAssetPosition) > movementRecordThreshold)
                {
                    DebugLogger.Instance.Log("Added new frame data for " + recordable.playbackObject.name + " at " + mainRecorder.playbackSlider.value);

                    //recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, "Collide(" + Manager.Instance.CleanString(recordable.gameObject.name) + ", hand)", true);
                    recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, action: "None", collision: "None", Recordable.RecordingType.ManualAnimation, propagateValueToSubsequentFrames: true);
                    //Increment the slider value by a small value proportional to the total recording time
                    mainRecorder.playbackSlider.value += playbackSpeed;
                }
                lastAssetPosition = recordable.playbackObject.transform.position;
                
            }
            else if(recordable.currentRecordingMode == Recordable.RecordingType.Physics)
            {
                //recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, "ApplyForce()", true);
                //recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, action: "ApplyForce()", sourceOfAction: "None", Recordable.RecordingType.Physics, propagateValueToSubsequentFrames: true);
                //Increment the slider value by frame duration
                mainRecorder.playbackSlider.value += 1;//Time.deltaTime;
            }
            else if(recordable.currentRecordingMode == Recordable.RecordingType.Follow)
            {
                //recordable.InsertAssetRecordFrame((int)mainRecorder.playbackSlider.value, action: "Follow()", sourceOfAction: "None", propagateValueToSubsequentFrames: true);
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
                    recordable.recordedData[currentFrameNum].ActionDelegate?.Invoke();
                    if(recordable.recordedData[currentFrameNum].recordingMode == Recordable.RecordingType.Physics || recordable.recordedData[currentFrameNum].recordingMode == Recordable.RecordingType.Follow || recordable.recordedData[currentFrameNum].recordingMode == Recordable.RecordingType.Visibility)
                    {
                        recordable.recordedData[currentFrameNum].ActionDelegate?.Invoke();
                    }
                    else if(recordable.recordedData[currentFrameNum].recordingMode == Recordable.RecordingType.ManualAnimation)
                    {
                        recordable.playbackObject.transform.position = recordable.recordedData[currentFrameNum].rootPosition;
                        recordable.playbackObject.transform.rotation = recordable.recordedData[currentFrameNum].rootRotation * Quaternion.Euler(recordable.rotationCorrection);

                    }

                    //DebugLogger.Instance.Log("Playing back " + recordable.playbackObject.name + " at " + currentFrameNum);
                    /*recordable.playbackObject.transform.position = recordable.recordedData[currentFrameNum].rootPosition;
                    recordable.playbackObject.transform.rotation = recordable.recordedData[currentFrameNum].rootRotation * Quaternion.Euler(recordable.rotationCorrection);

                    if (recordable.recordedData[currentFrameNum].showStatusForThisFrame)
                    {
                        //DebugLogger.Instance.Log("Showing " + recordable.playbackObject.name + " at " + currentFrameNum);
                        recordable.playbackObject.GetComponent<MeshRenderer>().material = defaultMaterial;
                    }
                    else
                    {
                        //DebugLogger.Instance.Log("Hiding " + recordable.playbackObject.name + " at " + currentFrameNum);
                        if(mainRecorder.isAutomaticPlayback) 
                            recordable.playbackObject.GetComponent<MeshRenderer>().material = transparentMaterial;
                        else 
                            recordable.playbackObject.GetComponent<MeshRenderer>().material = translucentMaterial;
                    }*/
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

    public void PokeTest()
    {
        Debug.Log("Poke test called");
    }


    public void SpawnSphere(Transform target)
    {
        DebugLogger.Instance.Log("Spawned Sphere");
        GameObject obj = Instantiate(spherePrefab, target.position, Quaternion.identity);
        obj.SetActive(true);
        recordableAssets.Add(obj.GetComponentInChildren<Recordable>());
        mainRecorder.RefreshAssetsTimeline(mainRecorder.assetTimelinePanelPrefab);
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }

    public void SpawnCube(Transform target)
    {
        DebugLogger.Instance.Log("Spawned Cube");
        GameObject obj = Instantiate(cubePrefab, target.position, Quaternion.identity);
        obj.SetActive(true);
        recordableAssets.Add(obj.GetComponentInChildren<Recordable>());
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }

    public void SpawnText(Transform target)
    {
        DebugLogger.Instance.Log("Spawned Text");
        GameObject obj = Instantiate(textAsset, target.position, Quaternion.identity);
        obj.SetActive(true);
        recordableAssets.Add(obj.GetComponentInChildren<Recordable>());
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }

    public void DeleteAsset(GameObject obj)
    {
        DebugLogger.Instance.Log("Deleted " + obj.name);        
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

public class AssetSequence
{
    public string Action { get; set; } //None, Physics, Follow, Show, Hide
    public int StartIndex { get; set; }
    public int Length { get; set; }
    //public GestureManager.Gesture GestureType { get; set; }
}
