using System;
using TMPro;
using UnityEngine;

public class SpeechToTextUIManager : MonoBehaviour
{
    private static SpeechToTextUIManager Instance { get; set; }
    
    [SerializeField] private TMP_Text speechToTextStatusText;
    
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
        
        if (speechToTextStatusText == null)
        {
            throw new Exception("UI Text component 'currentExampleText' is not assigned in the inspector.");
        }
    }
    
    private void OnEnable()
    {
        if (SpeechToText.Instance != null) 
            SpeechToText.Instance.OnListeningStateChanged -= HandleWhisperChangeInListening;
    }

    private void OnDisable()
    {
        if (SpeechToText.Instance != null) 
            SpeechToText.Instance.OnListeningStateChanged -= HandleWhisperChangeInListening;
    }

    private void HandleWhisperChangeInListening(bool whisperIsListening)
    {
        Debug.Log($"Whisper is listening: {whisperIsListening}");
        speechToTextStatusText.text = whisperIsListening
            ? "<color=\"green\">ON"
            : "<color=\"red\">OFF";
    }
}
