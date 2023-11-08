using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Whisper.Utils;
using Whisper;
using System.Diagnostics;
public class SpeechToText : MonoBehaviour
{
    public WhisperManager whisper;
    public MicrophoneRecord microphoneRecord;

    public TMPro.TMP_Text outputText;
    private WhisperStream _stream;

    public bool isRecording = false;

    // Start is called before the first frame update
    async void Start()
    {
        //microphoneRecord.vadStop = false;
        //microphoneRecord.OnRecordStop += OnRecordStop;
        
        _stream = await whisper.CreateStream(microphoneRecord);
        _stream.OnResultUpdated += OnResult;
        _stream.OnSegmentUpdated += OnSegmentUpdated;
        _stream.OnSegmentFinished += OnSegmentFinished;
        _stream.OnStreamFinished += OnFinished;

        //microphoneRecord.OnRecordStop += OnRecordStop;
        //button.onClick.AddListener(OnButtonPressed);
        StartListening();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartListening()
    {
        _stream.StartStream();
        microphoneRecord.StartRecord();
        DebugLogger.Instance.Log("Start Recording");
    }

    public void StopListening()
    {
        microphoneRecord.StopRecord();
        DebugLogger.Instance.Log("Stop Recording");
    }

    private void OnApplicationQuit()
    {
        StopListening();
    }

    private void OnResult(string result)
    {
        //text.text = result;
        //UiUtils.ScrollDown(scroll);
    }
    
    private void OnSegmentUpdated(WhisperResult segment)
    {
        //print($"Segment updated: {segment.Result}");
    }
    
    private void OnSegmentFinished(WhisperResult segment)
    {
        outputText.text = segment.Result;
        print($"Segment finished: {segment.Result}");
        InputManager.Instance.NotifyVoiceCommand(segment.Result);
        //if(isRecording)
        {
            //currentRecognisedText = segment.Result;            
        }

        //Call coroutine to clear outputText.text after 2 seconds
        StartCoroutine(ClearOutputText());
    }

    IEnumerator ClearOutputText()
    {
        yield return new WaitForSeconds(2f);
        outputText.text = "";
    }

    
    
    private void OnFinished(string finalResult)
    {
        print("Stream finished!");
    }

    //public void 
}
