using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public GameObject recordStartButton;
    public GameObject recordStopButton;
    public GameObject playButton;
    public GameObject stopButton;
    public GameObject pauseButton;
    public GameObject head, leftHand, rightHand;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnRecordStartClick()
    {
        DebugLogger.Instance.Log("Record start Clicked");
    }

    public void OnRecordStopClick()
    {
        DebugLogger.Instance.Log("Record stop Clicked");
    }

    public void OnPlayClick()
    {
        DebugLogger.Instance.Log("Play Clicked");
    }

    public void OnPauseClick()
    {
        DebugLogger.Instance.Log("Pause Clicked");
    }

}


