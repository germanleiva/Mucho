using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Recorder : MonoBehaviour
{
    public Slider playbackSlider;
    public Recordable[] objectsToRecord;
    private bool isRecording = false;
    private bool isPlayingBack = false;
    private float recordStartTime;

    public GameObject rootPlaybackArea;


    // Start recording.
    public void StartRecording()
    {
        rootPlaybackArea.SetActive(false);
        DebugLogger.Instance.Log("StartRecording");
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
        rootPlaybackArea.SetActive(false);
        DebugLogger.Instance.Log("ResetRecording");
        foreach (var recordable in objectsToRecord)
        {
            //recordable.playbackObject.GetComponent<QuickTransformDebug>().enabled = false;
            //recordable.GetComponent<QuickTransformDebug>().enabled = true;
            recordable.playbackObject.SetActive(false);
            //Turn off the line renderer
            recordable.lineObject.GetComponent<LineRenderer>().enabled = false;            
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

        VisualizePath();
        StartPlayback();
    }

    void VisualizePath()
    {
        foreach (var recordable in objectsToRecord)
        {
            recordable.VisualizePath();
        }
    }

    // Start playback.
    public void StartPlayback()
    {
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
                recordable.lineObject.GetComponent<LineRenderer>().enabled = true;
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




