using System;
using System.Collections;
using UnityEngine;
using Whisper.Utils;
using Whisper;
using System.Text.RegularExpressions;

public class SpeechToText : MonoBehaviour
{
    public static SpeechToText Instance { get; set; }

    public WhisperManager whisper;
    public MicrophoneRecord microphoneRecord;

    public TMPro.TMP_Text outputText;
    private WhisperStream _stream;

    public event Action<bool> OnListeningStateChanged;

    private bool _isListening;

    public bool IsListening
    {
        get => _isListening;
        private set
        {
            if (_isListening == value)
            {
                return;
            }

            _isListening = value;
            OnListeningStateChanged?.Invoke(_isListening);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    async void Start()
    {
        _stream = await whisper.CreateStream(microphoneRecord);
        _stream.OnResultUpdated += OnResult;
        _stream.OnSegmentUpdated += OnSegmentUpdated;
        _stream.OnSegmentFinished += OnSegmentFinished;
        _stream.OnStreamFinished += OnFinished;
    }

    public void StartListening()
    {
        if (IsListening) return;
        if (_stream == null) return;

        _stream.StartStream();
        microphoneRecord.StartRecord();
        DebugLogger.Instance.Log("Start Recording");

        IsListening = true;
    }


    public void StopListening()
    {
        if (!IsListening) return;


        microphoneRecord.StopRecord();
        DebugLogger.Instance.Log("Stop Recording");

        IsListening = false;
    }

    private void OnApplicationQuit()
    {
        StopAllCoroutines();
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
        outputText.text = Regex.Replace(segment.Result, "[!><.]", "").ToLower();
        //Remove first character if it is a space
        if (outputText.text.Length > 0)
        {
            if (outputText.text[0] == ' ')
                outputText.text = outputText.text.Substring(1);
        }

        print($"Segment finished: {segment.Result}");
        InputManager.Instance.NotifyVoiceCommand(outputText.text);

        //Call coroutine to clear outputText.text after 2 seconds
        StartCoroutine(ClearOutputText());
    }

    IEnumerator ClearOutputText()
    {
        yield return new WaitForSeconds(2f);
        InputManager.Instance.NotifyVoiceCommand("");
        outputText.text = "";
    }

    public void SendSyntheticVoiceCommand(string _fakeVoiceCommand)
    {
        InputManager.Instance.NotifyVoiceCommand(_fakeVoiceCommand);
    }

    private void OnFinished(string finalResult)
    {
        print("Stream finished!");
    }
}