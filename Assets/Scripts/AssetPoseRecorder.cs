using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.UI;

public class AssetPoseRecorder : MonoBehaviour
{   
    public static AssetPoseRecorder Instance { get; private set; }
    //public Slider playbackSlider;
    private float recordStartTime;

    private bool isRecording = false;
    private bool isPlayingBack = false;

    //public Recordable recordable; //Should be a list of recordables

    public List <Recordable> recordableAssets = new List<Recordable>();
    
    public Recordable currentActiveRecordable;

    public Recorder mainRecorder;

    Vector3 lastLocalPosition = Vector3.zero;

    //public Recordable[] objectsToRecord;

    // Start is called before the first frame update

    public GameObject spherePrefab;
    public GameObject cubePrefab;
    public GameObject cylinderPrefab;
    public GameObject hmd;
    
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

    public void StartRecording(Recordable recordable)
    {
        //currAppState = Manager.AppState.RECORD;
        //rootPlaybackArea.SetActive(false);
        DebugLogger.Instance.Log("StartRecording in " + recordable.playbackObject.name);
        currentActiveRecordable = recordable;
        currentActiveRecordable.ResetData();        
        isRecording = true;
        isPlayingBack = false;
        //recordStartTime = mainRecorder.playbackSlider.value;
    }

    public void StopRecording(Recordable recordable)
    {
        DebugLogger.Instance.Log("StopRecording in " + recordable.playbackObject.name);
        isRecording = false;
        isPlayingBack = true;
        //mainRecorder.playbackSlider.value = 0;

    }

    public void ResetRecording(Recordable recordable)
    {
        DebugLogger.Instance.Log("ResetRecording in " + recordable.playbackObject.name);
        isRecording = false;
        isPlayingBack = false;
        recordable.ResetData();
        //mainRecorder.playbackSlider.value = 0;
    }
   

    private void Update()
    {
        if (isRecording)
        {
            //DebugLogger.Instance.Log("Current Position: " + currentActiveRecordable.playbackObject.transform.localPosition + " Last Recorded Position: " + lastLocalPosition);
            //Check if there is a difference between the current position and the last recorded position and if the difference is greater than 0.01 then record the current position
            //DebugLogger.Instance.Log("Distance: " + Vector3.Distance(currentActiveRecordable.playbackObject.transform.localPosition, lastLocalPosition));
            if (Vector3.Distance(currentActiveRecordable.playbackObject.transform.localPosition, lastLocalPosition) > 0.0001f)            
            {
                
                //print into debugger.instance.log both the current position and the last recorded position
                
                //DebugLogger.Instance.Log("Added new frame data for " + currentActiveRecordable.playbackObject.name + " at " + Time.time);
                //recordable.Record(Time.time - recordStartTime);
                currentActiveRecordable.Record(mainRecorder.playbackSlider.value);
                //Increment the slider value by a small value proportional to the total recording time
                mainRecorder.playbackSlider.value += 0.003f;// / (mainRecorder.recordingDuration * 100);
                
            }
            lastLocalPosition = currentActiveRecordable.playbackObject.transform.localPosition;

            //recordable.Record(Time.time - recordStartTime);
        }
        else if (isPlayingBack)
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

    public void enableGrabForAllAssets()
    {
        DebugLogger.Instance.Log("Enabling grab for all assets");
        foreach (var recordable in recordableAssets)
        {
            DebugLogger.Instance.Log("Enabling grab for " + recordable.playbackObject.name);
            recordable.playbackObject.GetComponent<BoxCollider>().enabled = true;
        }
    }

    public void disableGrabForAllAssets()
    {
        DebugLogger.Instance.Log("Disabling grab for all assets");
        foreach (var recordable in recordableAssets)
        {
            DebugLogger.Instance.Log("Disabling grab for " + recordable.playbackObject.name);
            recordable.playbackObject.GetComponent<BoxCollider>().enabled = false;
        }
    }

    public void SpawnSphere()
    {
        DebugLogger.Instance.LogInVR("Spawned Sphere");
        GameObject obj = Instantiate(spherePrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
        obj.SetActive(true);
        recordableAssets.Add(obj.GetComponent<Recordable>());
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }

    public void SpawnCube()
    {
        DebugLogger.Instance.LogInVR("Spawned Cube");
        GameObject obj = Instantiate(cubePrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
        obj.SetActive(true);
        recordableAssets.Add(obj.GetComponent<Recordable>());
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }
   
}
