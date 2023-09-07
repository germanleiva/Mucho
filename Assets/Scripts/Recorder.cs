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
    public GameObject playButton;
    public Recordable[] objectsToRecord;
    public bool isMainRecordingOn = false;
    public bool isMainPlaybackOn = false;
    public int recordStartFrame;
    public int recordedFramesTotal;
  
    private bool isAutomaticPlayback = false;

    readonly List<float> handGuideTimePoints = new();
    [Header("Timeline UI")]
    [SerializeField]
    RectTransform rightHandTimelinePanel;
    [SerializeField]
    RectTransform leftHandTimelinePanel;
    [SerializeField]
    GameObject handTimelineElementPrefab;
    [SerializeField]
    GameObject assetTimelineElementPrefab;
    [SerializeField]
     RectTransform playbackPanelTransform;
     [SerializeField]
    GameObject hideTimelinePanelPrefab;
    [SerializeField]
    GameObject showTimelinePanelPrefab;
     public GameObject assetTimelinePanelPrefab;
     public GameObject collisionTimelinePanel;
    

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

        GenerateGestureSequences(leftHandTimelinePanel, objectsToRecord[1]);
        GenerateGestureSequences(rightHandTimelinePanel, objectsToRecord[2]);

        //VisualizePath();
        PreparePlayback();
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
    public void PreparePlayback()
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

            playButton.SetActive(true);

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

    public int GetSizeOfMainRecordedData(int index)
    {
        return objectsToRecord[index].recordedData.Count;
    }

    public void ExpandRecordedData(int size)
    {
        // For each recordable object, expand the recordedData list to the given size by copying the last element
        foreach (var recordable in objectsToRecord)
        {
            RecordFrameData lastElement = recordable.recordedData[recordable.recordedData.Count - 1];
            for (int i = 0; i < size - recordable.recordedData.Count; i++)
            {
                recordable.recordedData.Add(lastElement);
            }
        }
        AssetPoseRecorder.Instance.ExpandRecordFramesForAssets(size);
        AssetPoseRecorder.Instance.DoRecordSizesMatch();
    }

    public List<GestureSequence> GetContinuousGestureSequences(List<GestureManager.Gesture> gestures)
    {
        List<GestureSequence> sequences = new List<GestureSequence>();

        int startIndex = -1;
        GestureManager.Gesture? currentGesture = null;

        for (int i = 0; i < gestures.Count; i++)
        {
            if (gestures[i] != GestureManager.Gesture.LEFTHANDNONE && gestures[i] != GestureManager.Gesture.RIGHTHANDNONE)
            {
                if (currentGesture == null || currentGesture == gestures[i])
                {
                    if (currentGesture == null)
                    {
                        currentGesture = gestures[i];
                        startIndex = i;
                    }
                }
                else
                {
                    sequences.Add(new GestureSequence
                    {
                        StartIndex = startIndex,
                        Length = i - startIndex,
                        GestureType = currentGesture.Value
                    });

                    startIndex = i;
                    currentGesture = gestures[i];
                }
            }
            else if (currentGesture != null)
            {
                sequences.Add(new GestureSequence
                {
                    StartIndex = startIndex,
                    Length = i - startIndex,
                    GestureType = currentGesture.Value
                });

                startIndex = -1;
                currentGesture = null;
            }
        }

        if (currentGesture != null)
        {
            sequences.Add(new GestureSequence
            {
                StartIndex = startIndex,
                Length = gestures.Count - startIndex,
                GestureType = currentGesture.Value
            });
        }

        return sequences;
    }

    public void GenerateGestureSequences(RectTransform timelinePanel, Recordable recordable)
    {
        List<GestureManager.Gesture> gestures = recordable.recordedData.Select(x => x.gesture).ToList();
        List<GestureSequence> sequences = GetContinuousGestureSequences(gestures);
        foreach (GestureSequence sequence in sequences)
        {
            DebugLogger.Instance.Log("Sequence name: " + GestureManager.Instance.GestureToString(sequence.GestureType) + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
            DebugLogger.Instance.Log("Start x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex) + ", End x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex + sequence.Length));
            GameObject timelineElement = Instantiate(handTimelineElementPrefab, timelinePanel);
            timelineElement.SetActive(true);
            timelineElement.GetComponent<RectTransform>().anchoredPosition = new Vector2(MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex), timelineElement.GetComponent<RectTransform>().anchoredPosition.y);
            timelineElement.GetComponent<RectTransform>().sizeDelta = new Vector2(MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex + sequence.Length) - MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex), timelineElement.GetComponent<RectTransform>().sizeDelta.y);
            timelineElement.GetComponent<TimelineUIElement>().SetEvent(GestureManager.Instance.GestureToString(sequence.GestureType));
        }
    }    

    public List<AssetChangeSequence> GetContinuousChangeSequences(List<string> changes)
    {
        List<AssetChangeSequence> sequences = new List<AssetChangeSequence>();

        int startIndex = -1;
        string currentChange = null;

        for (int i = 0; i < changes.Count; i++)
        {
            if (changes[i] != "None")
            {
                if (currentChange == null || currentChange == changes[i])
                {
                    if (currentChange == null)
                    {
                        currentChange = changes[i];
                        startIndex = i;
                    }
                }
                else
                {
                    sequences.Add(new AssetChangeSequence
                    {
                        StartIndex = startIndex,
                        Length = i - startIndex,
                        SourceOfAssetChange = currentChange
                    });

                    startIndex = i;
                    currentChange = changes[i];
                }
            }
            else if (currentChange != null)
            {
                sequences.Add(new AssetChangeSequence
                {
                    StartIndex = startIndex,
                    Length = i - startIndex,
                    SourceOfAssetChange = currentChange
                });

                startIndex = -1;
                currentChange = null;
            }
        }

        if (currentChange != null)
        {
            sequences.Add(new AssetChangeSequence
            {
                StartIndex = startIndex,
                Length = changes.Count - startIndex,
                SourceOfAssetChange = currentChange
            });
        }

        return sequences;
    }

    public void GenerateAssetChangeSequences(RectTransform timelinePanel, Recordable recordable)
    {
        List<string> changes = recordable.recordedData.Select(x => x.SourceOfAssetChange).ToList();
        List<AssetChangeSequence> sequences = GetContinuousChangeSequences(changes);
        foreach (AssetChangeSequence sequence in sequences)
        {
            DebugLogger.Instance.Log("Sequence name: " + sequence.SourceOfAssetChange + ", StartIndex : " + sequence.StartIndex + ", Length:" + sequence.Length);
            DebugLogger.Instance.Log("Start x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex) + ", End x: " + MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex + sequence.Length));
            if(sequence.SourceOfAssetChange.StartsWith("Collide"))
            {
                DebugLogger.Instance.Log("Collide event found");
                RectTransform collisionTimelinePanelTransform = collisionTimelinePanel.GetComponent<RectTransform>();
                GameObject timelineElement = Instantiate(assetTimelineElementPrefab, collisionTimelinePanelTransform);
                timelineElement.SetActive(true);
                timelineElement.GetComponent<RectTransform>().anchoredPosition = new Vector2(MapIndexToTimelinePosition(collisionTimelinePanelTransform, sequence.StartIndex), timelineElement.GetComponent<RectTransform>().anchoredPosition.y);
                timelineElement.GetComponent<RectTransform>().sizeDelta = new Vector2(MapIndexToTimelinePosition(collisionTimelinePanelTransform, sequence.StartIndex + sequence.Length) - MapIndexToTimelinePosition(collisionTimelinePanelTransform, sequence.StartIndex), timelineElement.GetComponent<RectTransform>().sizeDelta.y);
                timelineElement.GetComponent<TimelineUIElement>().SetEvent(sequence.SourceOfAssetChange);
            }
            else if(sequence.SourceOfAssetChange.StartsWith("Hide"))
            {
                GameObject hideElement = Instantiate(hideTimelinePanelPrefab, timelinePanel);
                hideElement.SetActive(true);
                hideElement.GetComponent<RectTransform>().anchoredPosition = new Vector2(MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex), hideElement.GetComponent<RectTransform>().anchoredPosition.y);
                //hideElement.GetComponent<RectTransform>().sizeDelta = new Vector2(MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex + sequence.Length) - MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex), timelineElement.GetComponent<RectTransform>().sizeDelta.y);
                //hideElement.GetComponent<TimelineUIElement>().SetEvent(sequence.SourceOfAssetChange);
            }
            else if(sequence.SourceOfAssetChange.StartsWith("Show"))
            {
                GameObject showElement = Instantiate(showTimelinePanelPrefab, timelinePanel);
                showElement.SetActive(true);
                showElement.GetComponent<RectTransform>().anchoredPosition = new Vector2(MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex), showElement.GetComponent<RectTransform>().anchoredPosition.y);
                //showElement.GetComponent<RectTransform>().sizeDelta = new Vector2(MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex + sequence.Length) - MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex), timelineElement.GetComponent<RectTransform>().sizeDelta.y);
                //showElement.GetComponent<TimelineUIElement>().SetEvent(sequence.SourceOfAssetChange);
            }
            else //Other types of events - physics, attach etc
            {
                GameObject timelineElement = Instantiate(assetTimelineElementPrefab, timelinePanel);
                timelineElement.SetActive(true);
                timelineElement.GetComponent<RectTransform>().anchoredPosition = new Vector2(MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex), timelineElement.GetComponent<RectTransform>().anchoredPosition.y);
                timelineElement.GetComponent<RectTransform>().sizeDelta = new Vector2(MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex + sequence.Length) - MapIndexToTimelinePosition(timelinePanel, sequence.StartIndex), timelineElement.GetComponent<RectTransform>().sizeDelta.y);
                timelineElement.GetComponent<TimelineUIElement>().SetEvent(sequence.SourceOfAssetChange);
            }
        }
    }

    //List of asset timelines
    List<GameObject> assetTimelines = new List<GameObject>();

    public void RefreshAssetsTimeline(GameObject timelinePanelPrefab)
    {
        //Delete all existing asset timelines
        foreach (var timeline in assetTimelines)
        {
            Destroy(timeline);
        }
        //GenerateGestureSequences(timelinePanel, recordable);
        int recordableCounter = 0;
        foreach (var recordable in AssetPoseRecorder.Instance.recordableAssets)
        {
            ++recordableCounter;
            GameObject timelinePanel = Instantiate(timelinePanelPrefab, playbackPanelTransform);
            assetTimelines.Add(timelinePanel);
            timelinePanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(timelinePanel.GetComponent<RectTransform>().anchoredPosition.x, timelinePanel.GetComponent<RectTransform>().anchoredPosition.y - recordableCounter * 100);
            timelinePanel.SetActive(true);
            if (recordable.recordedData.Count > 0)
            {
                GenerateAssetChangeSequences(timelinePanel.GetComponent<RectTransform>(), recordable);
            }   
        }
    }

    public float MapIndexToTimelinePosition(RectTransform _rectTransform, int index)
    {
        float rectStartX = 0;
        float rectEndX =  _rectTransform.GetComponent<RectTransform>().rect.width;
        int indexStart = 0;
        int indexEnd = GetSizeOfMainRecordedData(0);
        //Map index to value scaled between rectStartX and rectEndX
        float mappedValue = Map(index, indexStart, indexEnd, rectStartX, rectEndX);
        return mappedValue;
    }

    public float Map(float x, float in_min, float in_max, float out_min, float out_max) //From https://forum.unity.com/threads/mapping-or-scaling-values-to-a-new-range.180090/#post-2241099
    {
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }
 
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




