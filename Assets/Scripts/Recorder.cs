using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Recorder : MonoBehaviour
{

    [Header("Record & Playback")]
    public GameObject rootPlaybackArea, playbackUI;
    public Slider playbackSlider;
    public Recordable[] objectsToRecord;
    private bool isRecording = false;
    private bool isPlayingBack = false;
    private float recordStartTime;
    public float recordingDuration;

    public GameObject triggerStartObj;
    public GameObject triggerStopObj;
    public GameObject CylinderPrefab;

    //[Header("Gesture Recognizers")]
    //public GestureRecognizer LeftHandGestureRecorder, RightHandGestureRecorder;

    //public GestureRecognizer LeftHandGestureRecognizer, RightHandGestureRecognizer;


    void Awake()
    {
        
    }

    void Start()
    {
        initialize();
    }

    void initialize()
    {
        //currAppState = Manager.AppState.NONE;
        rootPlaybackArea.SetActive(false);
        playbackUI.SetActive(false);
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
        AssetPoseRecorder.Instance.disableGrabForAllAssets();
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

        AssetPoseRecorder.Instance.enableGrabForAllAssets();

        VisualizePath();
        StartPlayback();
    }

    /*public void SavePlaybackLeftHandGesture()
    {
        DebugLogger.Instance.Log("Saving playback left hand gesture in record mode");
        //LeftHandGestureRecorder.SaveAsGesture();    
    }

    public void SavePlaybackRightHandGesture()
    {
        DebugLogger.Instance.Log("Saving playback right hand gesture in record mode");
        //RightHandGestureRecorder.SaveAsGesture();    
    }

    public void SaveRealtimeLeftHandGesture()
    {
        DebugLogger.Instance.Log("Saving realtime left hand gesture in record mode");
        //LeftHandGestureRecorder.SaveAsGesture();    
        LeftHandGestureRecognizer.SaveAsGesture(); 
    }

    public void CopyLeftHandGesture()
    {
        DebugLogger.Instance.Log("Copying left hand gesture");
        //LeftHandGestureRecognizer.CopySavedGestures(LeftHandGestureRecorder.GetSavedGestures());        
    }

    public void CopyRightHandGesture()
    {
        DebugLogger.Instance.Log("Copying right hand gesture");
        //RightHandGestureRecognizer.CopySavedGestures(RightHandGestureRecorder.GetSavedGestures());        
    }

    */

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
        Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
        try
        {
            if (isRecording) return; // Don't allow playback while recording.

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
            recordStartTime = Time.time;
        }
        catch (System.Exception e)
        {
            DebugLogger.Instance.LogException(e);
        }
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
        playbackUI.SetActive(false);
        //CopyLeftHandGesture();
        //CopyRightHandGesture();
    }

    public void SetRecordMode()
    {
        DebugLogger.Instance.Log("Set Record Mode");
        Manager.Instance.currAppState = Manager.AppState.RECORDING;
        rootPlaybackArea.SetActive(false);
        playbackUI.SetActive(true);        
    }

    /*public void SetStartTrigger()
    {
        DebugLogger.Instance.Log("Set StartTrigger");
        foreach (var recordable in objectsToRecord)
        {
            Instantiate(triggerStartObj, recordable.playbackObject.transform.position, Quaternion.identity);
        }
        
    }*/

    float startTriggerTime = 0f;
    float endTriggerTime = 0f;
    Vector3 startTriggerPos = Vector3.zero;
    Vector3 endTriggerPos = Vector3.zero;

    public void SetStartTrigger(GameObject _playbackObject)
    {
        DebugLogger.Instance.Log("Set StartTrigger for " + _playbackObject.name);
        Instantiate(triggerStartObj, _playbackObject.transform.position, Quaternion.identity);
        startTriggerTime = playbackSlider.value;
        startTriggerPos = _playbackObject.transform.position;
    }

    public void SetEndTrigger(GameObject _playbackObject)
    {
        DebugLogger.Instance.Log("Set EndTrigger for " + _playbackObject.name);
        Instantiate(triggerStopObj, _playbackObject.transform.position, Quaternion.identity);
        endTriggerTime = playbackSlider.value;
        endTriggerPos = _playbackObject.transform.position;
        DebugLogger.Instance.LogInVR("Start Trigger Time: " + startTriggerTime + " End Trigger Time: " + endTriggerTime);
        DebugLogger.Instance.LogInVR("Trigger Duration: " + (endTriggerTime - startTriggerTime));
        //Find direction between start and end trigger
        Vector3 direction = endTriggerPos - startTriggerPos;
        DebugLogger.Instance.LogInVR("Trigger Direction: " + direction);
        CreateCylinderBetweenTwoPoints(startTriggerPos, endTriggerPos, 0.01f);

        //Create a for loop which iterates from startTriggerTime to endTriggerTime and assigns the value to playbackSlider.value and in each iteration call the GestureDetectionLoop() function in the playback object's GestureRecognizer component
        DebugLogger.Instance.LogInVR("Gesture within trigger duration: ");
        for (float i = startTriggerTime; i <= endTriggerTime; i += 0.01f)
        {
            playbackSlider.value = i;
            //_playbackObject.GetComponent<GestureRecognizer>().GestureDetectionLoop();
        }
    }

    public void CreateCylinderBetweenTwoPoints(Vector3 pointA, Vector3 pointB, float width)
    {
        Vector3 offset = pointB - pointA;
        Vector3 scale = new Vector3(width, offset.magnitude / 2.0f, width);
        Vector3 position = pointA + (offset / 2.0f) + new Vector3(0, 0.03f, 0);

        GameObject cylinder = Instantiate(CylinderPrefab, position, Quaternion.identity);
        cylinder.transform.up = offset;
        cylinder.transform.localScale = scale;
        cylinder.SetActive(true);
    }

    /*public void SetEndTrigger()
    {
        DebugLogger.Instance.Log("Set EndTrigger");
        foreach (var recordable in objectsToRecord)
        {
            Instantiate(triggerStopObj, recordable.playbackObject.transform.position, Quaternion.identity);
        }
    }*/

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
            //float currentTime = Time.time - recordStartTime;
            float currentTime = playbackSlider.value;

            foreach (var recordable in objectsToRecord)
            {
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
                        if(recordable.playbackObject.GetComponent<HandPlaybackObjectScript>() != null) //TODO:Can also check if the child objects to record is not null
                        {

                            recordable.playbackObject.transform.localPosition = Vector3.Lerp(previousFrame.rootPosition, nextFrame.rootPosition, t);
                            recordable.playbackObject.transform.localRotation = Quaternion.Lerp(previousFrame.rootRotation, nextFrame.rootRotation, t) * Quaternion.Euler(recordable.rotationCorrection);//Modify the rotation in the recorded data to add 180 degrees to the  axis
                        
                            recordable.playbackObject.GetComponent<HandPlaybackObjectScript>().interpolatePoseForAllFingerJoints(previousFrame, nextFrame, t);

                        }
                        else 
                        {
                            recordable.playbackObject.transform.localPosition = Vector3.Lerp(previousFrame.rootPosition, nextFrame.rootPosition, t);
                            recordable.playbackObject.transform.localRotation = Quaternion.Lerp(previousFrame.rootRotation, nextFrame.rootRotation, t) * Quaternion.Euler(recordable.rotationCorrection);//Modify the rotation in the recorded data to add 180 degrees to the  axis
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




