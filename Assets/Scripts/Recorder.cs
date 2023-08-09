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
    private bool isRecording = false;
    private bool isPlayingBack = false;
    public float recordStartTime;
    public float recordingDuration;

    public GameObject triggerStartObj;
    public GameObject triggerStopObj;
    public GameObject CylinderPrefab;

    private bool isAutomaticPlayback = false;

    List<float> handGuideTimePoints = new List<float>();

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
        //currAppState = Manager.AppState.NONE;
        rootPlaybackArea.SetActive(false);
        //playbackUI.SetActive(false);
        //DebugLogger.Instance.Log("Initialize");
        //foreach (var recordable in objectsToRecord)
        //{
        //    recordable.ResetData();
        //}
        //isRecording = false;
        //isPlayingBack = false;
    }

    // Start recording.
    public void StartRecording()
    {
        //currAppState = Manager.AppState.RECORD;
        rootPlaybackArea.SetActive(false);
        DebugLogger.Instance.Log("StartRecording");
        AssetPoseRecorder.Instance.DisableGrabForAllAssets();
        foreach (var recordable in objectsToRecord)
        {
            recordable.ResetData();
        }
        isRecording = true;
        recordStartTime = Time.time;
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
        isRecording = false;
        isPlayingBack = false;
        isAutomaticPlayback = false;
    }

    // Stop recording.
    public void StopRecording()
    {
        rootPlaybackArea.SetActive(true);
        DebugLogger.Instance.Log("Size of recordedData head: " + objectsToRecord[0].recordedData.Count);
        DebugLogger.Instance.Log("Size of recordedData leftHand: " + objectsToRecord[1].recordedData.Count);
        DebugLogger.Instance.Log("Size of recordedData rightHand: " + objectsToRecord[2].recordedData.Count);
        DebugLogger.Instance.Log("StopRecording");
        isRecording = false;

        AssetPoseRecorder.Instance.EnableGrabForAllAssets();

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
            if (isRecording) return; // Don't allow playback while recording.
            Manager.Instance.currAppState = Manager.AppState.PLAYBACK;    
            DebugLogger.Instance.Log("StartPlayback");
            // Determine the duration of the recording.
            float duration = 0f;
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
                    duration = Mathf.Max(duration, recordable.recordedData[recordable.recordedData.Count - 1].timestamp);
                }
            }

            // Set up the slider.
            playbackSlider.minValue = 0f;
            playbackSlider.maxValue = duration;
            playbackSlider.value = 0f;
            recordingDuration = duration;

            isPlayingBack = true;
            isAutomaticPlayback = false;
            recordStartTime = Time.time;
            CalculateHandGuideTimePoints();
        }
        catch (System.Exception e)
        {
            DebugLogger.Instance.LogException(e);
        }
    }

    private void CalculateHandGuideTimePoints()
    {
        //Find 20 time points equally spaced out from 0 to recordingDuration
        float timeInterval = recordingDuration / 100;
        float currentTime = 0f;
        
        while (currentTime < recordingDuration)
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
        isPlayingBack = false;
    }

    public void SetTestMode()
    {
        DebugLogger.Instance.Log("Start Testing");
        Manager.Instance.currAppState = Manager.AppState.TEST;
        rootPlaybackArea.SetActive(false);
        //playbackUI.SetActive(false);
    }

    public void SetRecordMode()
    {
        DebugLogger.Instance.Log("Set Record Mode");
        Manager.Instance.currAppState = Manager.AppState.RECORDING;
        rootPlaybackArea.SetActive(false);
        //playbackUI.SetActive(true);        
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

    public void DestroyCopyAndSpawnSphere(GameObject obj)
    {
        if(obj.name.StartsWith("Sphere"))
        {
            AssetPoseRecorder.Instance.SpawnSphere(obj.transform);
        }
        Destroy(obj);
    }

    public void DestroyCopyAndSpawnCube(GameObject obj)
    {
        if(obj.name.StartsWith("Cube"))
        {
            AssetPoseRecorder.Instance.SpawnCube(obj.transform);
        }
        Destroy(obj);
    }
    


    private void FindPrevandNextFrames(List<RecordFrameData> recordedData, float currentTime, out RecordFrameData previousFrame, out RecordFrameData nextFrame)
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
    }

    private void Update()
    {
        if (isRecording)
        {
            foreach (var recordable in objectsToRecord)
            {
                recordable.Record(Time.time - recordStartTime);
            }
        }
        else if (isPlayingBack)
        {
            if(isAutomaticPlayback)
            {
                playbackSlider.value += Time.deltaTime;
                if(playbackSlider.value >= recordingDuration)
                {
                    playbackSlider.value = 0f;
                }
            }
            
            //float currentTime = Time.time - recordStartTime;
            float currentTime = playbackSlider.value;

            //Find the first two items in handGuideTimePoints that are greater than currentTime
            var result = handGuideTimePoints.Where(x => x > currentTime).Take(4).ToList();

            foreach (var recordable in objectsToRecord)
            {
                // Find the two frames to interpolate between.
                //foreach (var data in recordable.recordedData) 
                RecordFrameData previousFrame = null, nextFrame = null, previousFrame2 = null, nextFrame2 = null, previousFrame3 = null, nextFrame3 = null, previousFrame4 = null, nextFrame4 = null, previousFrame5 = null, nextFrame5 = null;
                FindPrevandNextFrames(recordable.recordedData, currentTime, out previousFrame, out nextFrame);
                if(result.Count == 4)
                {
                    FindPrevandNextFrames(recordable.recordedData, result[0], out previousFrame2, out nextFrame2);  
                    FindPrevandNextFrames(recordable.recordedData, result[1], out previousFrame3, out nextFrame3); 
                    FindPrevandNextFrames(recordable.recordedData, result[2], out previousFrame4, out nextFrame4);
                    FindPrevandNextFrames(recordable.recordedData, result[3], out previousFrame5, out nextFrame5);
                }
                if (recordable.playbackObject != null)
                {
                    if (previousFrame != null && nextFrame != null)
                    {
                        // Interpolate between the two frames.
                        float t = (currentTime - previousFrame.timestamp) / (nextFrame.timestamp - previousFrame.timestamp);
                        if(recordable.playbackObject.GetComponent<HandPlaybackObjectScript>() != null) //TODO:Can also check if the child objects to record is not null
                        {
                            //Playback hand 1
                            //Hand root
                            recordable.playbackObject.transform.localPosition = Vector3.Lerp(previousFrame.rootPosition, nextFrame.rootPosition, t);
                            recordable.playbackObject.transform.localRotation = Quaternion.Lerp(previousFrame.rootRotation, nextFrame.rootRotation, t) * Quaternion.Euler(recordable.rotationCorrection);//Modify the rotation in the recorded data to add 180 degrees to the  axis
                            recordable.playbackObject.GetComponent<HandPlaybackObjectScript>().InterpolatePoseForAllFingerJoints(previousFrame, nextFrame, t);

                            if(result.Count == 4)
                            {
                                //Playback hand 2
                                recordable.playbackObject2.transform.localPosition = Vector3.Lerp(nextFrame2.rootPosition, nextFrame2.rootPosition, t);
                                recordable.playbackObject2.transform.localRotation = Quaternion.Lerp(nextFrame2.rootRotation, nextFrame2.rootRotation, t) * Quaternion.Euler(recordable.rotationCorrection);//Modify the rotation in the recorded data to add 180 degrees to the  axis
                                recordable.playbackObject2.GetComponent<HandPlaybackObjectScript>().InterpolatePoseForAllFingerJoints(nextFrame2, nextFrame2, t);

                                //Playback hand 3
                                recordable.playbackObject3.transform.localPosition = Vector3.Lerp(nextFrame3.rootPosition, nextFrame3.rootPosition, t);
                                recordable.playbackObject3.transform.localRotation = Quaternion.Lerp(nextFrame3.rootRotation, nextFrame3.rootRotation, t) * Quaternion.Euler(recordable.rotationCorrection);//Modify the rotation in the recorded data to add 180 degrees to the  axis
                                recordable.playbackObject3.GetComponent<HandPlaybackObjectScript>().InterpolatePoseForAllFingerJoints(nextFrame3, nextFrame3, t);

                                //Playback hand 4
                                recordable.playbackObject4.transform.localPosition = Vector3.Lerp(nextFrame4.rootPosition, nextFrame4.rootPosition, t);
                                recordable.playbackObject4.transform.localRotation = Quaternion.Lerp(nextFrame4.rootRotation, nextFrame4.rootRotation, t) * Quaternion.Euler(recordable.rotationCorrection);//Modify the rotation in the recorded data to add 180 degrees to the  axis
                                recordable.playbackObject4.GetComponent<HandPlaybackObjectScript>().InterpolatePoseForAllFingerJoints(nextFrame4, nextFrame4, t);

                                //Playback hand 5
                                recordable.playbackObject5.transform.localPosition = Vector3.Lerp(nextFrame5.rootPosition, nextFrame5.rootPosition, t);
                                recordable.playbackObject5.transform.localRotation = Quaternion.Lerp(nextFrame5.rootRotation, nextFrame5.rootRotation, t) * Quaternion.Euler(recordable.rotationCorrection);//Modify the rotation in the recorded data to add 180 degrees to the  axis
                                recordable.playbackObject5.GetComponent<HandPlaybackObjectScript>().InterpolatePoseForAllFingerJoints(nextFrame5, nextFrame5, t);

                            }                    
                            
                            //Focus Square
                            recordable.playbackFocusSquare.transform.position = Vector3.Lerp(previousFrame.focusSquarePosition, nextFrame.focusSquarePosition, t);
                            recordable.playbackFocusSquare.transform.rotation = Quaternion.Lerp(previousFrame.focusSquareRotation, nextFrame.focusSquareRotation, t); 

                            //Gestures
                            recordable.playbackGestureText.text = GestureManager.Instance.GestureToString(previousFrame.gesture);                       

                        }
                        else 
                        {
                            recordable.playbackObject.transform.localPosition = Vector3.Lerp(previousFrame.rootPosition, nextFrame.rootPosition, t);
                            recordable.playbackObject.transform.localRotation = Quaternion.Lerp(previousFrame.rootRotation, nextFrame.rootRotation, t) * Quaternion.Euler(recordable.rotationCorrection);//Modify the rotation in the recorded data to add 180 degrees to the  axis
                            recordable.playbackFocusSquare.transform.position = Vector3.Lerp(previousFrame.focusSquarePosition, nextFrame.focusSquarePosition, t);
                            recordable.playbackFocusSquare.transform.rotation = Quaternion.Lerp(previousFrame.focusSquareRotation, nextFrame.focusSquareRotation, t);
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




