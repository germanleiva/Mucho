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

    private string _buffer;
    // Start is called before the first frame update
    void Start()
    {
        microphoneRecord.vadStop = false;
        microphoneRecord.OnRecordStop += OnRecordStop;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartRecording()
    {
        microphoneRecord.StartRecord();
        DebugLogger.Instance.Log("Start Recording");
    }

    public void StopRecording()
    {
        microphoneRecord.StopRecord();
        DebugLogger.Instance.Log("Stop Recording");
    }

    private async void OnRecordStop(AudioChunk recordedAudio)
    {
        //buttonText.text = "Record";
        _buffer = "";

        var sw = new Stopwatch();
        sw.Start();
        
        var res = await whisper.GetTextAsync(recordedAudio.Data, recordedAudio.Frequency, recordedAudio.Channels);
        if (res == null || !outputText) 
            return;

        var time = sw.ElapsedMilliseconds;
        var rate = recordedAudio.Length / (time * 0.001f);
        string timeDetails = $"Time: {time} ms\nRate: {rate:F1}x";
        DebugLogger.Instance.Log("Speech to text details: " + timeDetails);

        var text = res.Result;

        DebugLogger.Instance.Log("Speech to text result: " + text);
                
        outputText.text = text;
    
    }

    //public void 
}
