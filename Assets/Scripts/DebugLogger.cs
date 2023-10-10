using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugLogger : MonoBehaviour
{
    public static DebugLogger Instance { get; private set; }
    private static TMPro.TMP_Text debugText;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        debugText = GetComponent<Manager>().VRDebugText;   
    }

    public void Log(string message, bool VRConsoleEnabled = false)
    {
        Debug.Log(message);
        if (VRConsoleEnabled)
        {
            LogInVR(message);
        }
    }

    public void ClearVRDebugText()
    {
        debugText.text = "";
    }   

    //Log in debug text
    public void LogInVR(string message)
    {
        debugText.text+= message+"\n";
    }

    public void LogException(System.Exception e)
    {
        Debug.Log("ProtoXR_Test Exception : " + e.Message);
    }
}