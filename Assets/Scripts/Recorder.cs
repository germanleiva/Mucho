using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Recorder : MonoBehaviour
{

    [Header("Record & Playback")]
    public GameObject rootPlaybackArea;
    //public GameObject playbackUI;
    public GameObject controlUI;
    public Slider playbackSlider;
    public Recordable[] objectsToRecord;
    public bool isMainRecordingOn = false;
    public bool isMainPlaybackOn = false;
    public int recordStartFrame;
    public int recordedFramesTotal;

    private bool isAutomaticPlayback = false;

    readonly List<float> handGuideTimePoints = new();

    void Awake()
    {
        
    }

    void Start()
    {
        initialize();
    }

    public int GetSizeOfMainRecordedData()
    {
        return objectsToRecord[0].recordedData.Count;
    }

    void initialize()
    {
        rootPlaybackArea.SetActive(false);
    }

    // Start recording.
    public void StartRecording()
    {
        Manager.Instance.currAppState = Manager.AppState.RECORDING;
        rootPlaybackArea.SetActive(false);
        DebugLogger.Instance.Log("StartRecording");
        AssetPoseRecorder.Instance.DisableGrabForAllAssets();
        foreach (var recordable in objectsToRecord)
        {
            recordable.ResetData();
        }
        isMainRecordingOn = true;
        recordStartFrame = 0;//Time.time;
    }

    //Reset recording
    public void ResetRecording()
    {
        //currAppState = Manager.AppState.NONE;
        rootPlaybackArea.SetActive(false);
        DebugLogger.Instance.Log("ResetRecording");
        foreach (var recordable in objectsToRecord)
        {
            //recordable.playbackObject.GetComponent<QuickTransformDebug>().enabled = false;
            //recordable.GetComponent<QuickTransformDebug>().enabled = true;
            recordable.playbackObject.SetActive(false);
            //Turn off the line renderer
            //recordable.lineObject.GetComponent<LineRenderer>().enabled = false;            
            recordable.ResetData();
        }
        isMainRecordingOn = false;
        isMainPlaybackOn = false;
        isAutomaticPlayback = false;
    }

    // Stop recording.
    public void StopRecording()
    {
        rootPlaybackArea.SetActive(true);
        DebugLogger.Instance.Log("Size of recordedData head: " + objectsToRecord[0].recordedData.Count);
        DebugLogger.Instance.Log("Size of recordedData leftHand: " + objectsToRecord[1].recordedData.Count);
        DebugLogger.Instance.Log("Size of recordedData rightHand: " + objectsToRecord[2].recordedData.Count);

        isMainRecordingOn = false;

        AssetPoseRecorder.Instance.EnableGrabForAllAssets();
        AssetPoseRecorder.Instance.InitializeRecordFramesForAssets();

        VisualizePath();
        StartPlayback();
    }

    public void SetAutomaticPlayMode(bool isAutomatic)
    {
        isAutomaticPlayback = isAutomatic;
    }

    void VisualizePath()
    {
        foreach (var recordable in objectsToRecord)
        {
            //recordable.VisualizePath();
        }
    }

    // Start playback.
    public void StartPlayback()
    {
        try
        {
            if (isMainRecordingOn) return; // Don't allow playback while recording.
            Manager.Instance.currAppState = Manager.AppState.PLAYBACK;    
            DebugLogger.Instance.Log("StartPlayback");
            // Determine the duration of the recording.
            int framesTotal = 0;
            foreach (var recordable in objectsToRecord)
            {
                recordable.playbackObject.SetActive(true);
                //recordable.playbackObject.GetComponent<QuickTransformDebug>().enabled = true;
                //recordable.GetComponent<QuickTransformDebug>().enabled = false;
                //recordable.lineObject.GetComponent<LineRenderer>().enabled = true;
                if (recordable.recordedData.Count > 0)
                {
                    //duration = Mathf.Max(duration, recordable.recordedData.Last().timestamp);
                    //Find last element of recordedData and get its timestamp
                    framesTotal = Mathf.Max(framesTotal, recordable.recordedData[recordable.recordedData.Count - 1].frameNumber);
                }
            }

            // Set up the slider.
            playbackSlider.minValue = 0;
            playbackSlider.maxValue = framesTotal;
            playbackSlider.value = 0;
            recordedFramesTotal = framesTotal;
            DebugLogger.Instance.Log("Duration of recording: " + recordedFramesTotal);

            isMainPlaybackOn = true;
            isAutomaticPlayback = false;
            recordStartFrame = 0;//Time.time;
            //CalculateHandGuideTimePoints();
        }
        catch (System.Exception e)
        {
            DebugLogger.Instance.LogException(e);
        }
    }

    private void CalculateHandGuideTimePoints()
    {
        //Find 20 time points equally spaced out from 0 to recordingDuration
        float timeInterval = recordedFramesTotal / 100;
        float currentTime = 0f;
        
        while (currentTime < recordedFramesTotal)
        {
            handGuideTimePoints.Add(currentTime);
            currentTime += timeInterval;
        }  
        DebugLogger.Instance.Log("Size of handGuideTimePoints: " + handGuideTimePoints.Count);
    }    

    // Stop playback.
    public void StopPlayback()
    {
        DebugLogger.Instance.Log("StopPlayback");
        isMainPlaybackOn = false;
    }

    public void SetTestMode()
    {
        DebugLogger.Instance.Log("Start Testing");
        Manager.Instance.currAppState = Manager.AppState.TEST;
        rootPlaybackArea.SetActive(false);
        //playbackUI.SetActive(false);
    }

    public void DetachFromAllParents(Transform transform)
    {
        transform.SetParent(null);
        DebugLogger.Instance.Log("Detached " + transform.name + " from all parents");
        //controlUI.transform.SetParent(null);
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

    public void DestroyCopyAndSpawnAsset(GameObject obj)
    {
        if(obj.name.StartsWith("Sphere"))
        {
            AssetPoseRecorder.Instance.SpawnSphere(obj.transform);
        }
        else if(obj.name.StartsWith("Cube"))
        {
            AssetPoseRecorder.Instance.SpawnCube(obj.transform);
        }
        else if(obj.name.StartsWith("Text"))
        {
            AssetPoseRecorder.Instance.SpawnText(obj.transform);
        }
        Destroy(obj);
    }
 
    /*private void FindPrevandNextFrames(List<RecordFrameData> recordedData, float currentTime, out RecordFrameData previousFrame, out RecordFrameData nextFrame)
    {
        previousFrame = null;
        nextFrame = null;
        for (int i = 0; i < recordedData.Count; i++) //TODO: Replace with binary search?
        {
            var data = recordedData[i];
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
    }*/

    int frameCount = 0;

    private void FixedUpdate()
    {
        if (isMainRecordingOn)
        {
            ++frameCount;
            foreach (var recordable in objectsToRecord)
            {
                //recordable.Record(Time.time - recordStartTime);
                recordable.Record(frameCount);
            }
        }
        else if (isMainPlaybackOn)
        {

            if(isAutomaticPlayback)
            {
                playbackSlider.value += 1;//Time.deltaTime;
                if(playbackSlider.value >= recordedFramesTotal)
                {
                    playbackSlider.value = 0;
                }
            }

            int currentFrameNum = (int)playbackSlider.value;

            //Find the first two items in handGuideTimePoints that are greater than currentTime
            //var result = handGuideTimePoints.Where(x => x > currentTime).Take(4).ToList();

            foreach (var recordable in objectsToRecord)
            {
                if (recordable.playbackObject != null)
                {
                    recordable.playbackObject.transform.localPosition = recordable.recordedData[currentFrameNum].rootPosition;
                    recordable.playbackObject.transform.localRotation = recordable.recordedData[currentFrameNum].rootRotation * Quaternion.Euler(recordable.rotationCorrection);//Modify the rotation in the recorded data to add 180 degrees to the  axis
                    
                    // check if the HandPlaybackObjectScript is null and call setPoseForAllFingerJoints only if it is not null
                    if (recordable.playbackObject.GetComponent<HandPlaybackObjectScript>() != null)
                    {    
                        recordable.playbackObject.GetComponent<HandPlaybackObjectScript>().SetPoseForAllFingerJoints(recordable.recordedData[currentFrameNum]);
                        recordable.playbackGestureText.text = GestureManager.Instance.GestureToString(recordable.recordedData[currentFrameNum].gesture);  
                    }
                    //Focus Square
                    recordable.playbackFocusSquare.transform.position = recordable.recordedData[currentFrameNum].focusSquarePosition;
                    recordable.playbackFocusSquare.transform.rotation = recordable.recordedData[currentFrameNum].focusSquareRotation;

                }
            }
        }

    }
}




