using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugLogger : MonoBehaviour
{
    private static DebugLogger instance = null;
    private static readonly object padlock = new object();
    private static TMPro.TMP_Text debugText;
    
    DebugLogger()
    {
                  
    }

    public static DebugLogger Instance
    {
        get
        {
            lock (padlock)
            {
                if (instance == null)
                {
                    instance = new DebugLogger();
                }
                return instance;
            }
        }
    }
    
    void Start()
    {
        debugText = GetComponent<Manager>().VRDebugText;   
    }

    public void Log(string message)
    {
        Debug.Log("ProtoXR_Test" + message);
    }

    //Log in debug text
    public void LogInVR(string message)
    {
        debugText.text+= message+"\n";
    }

    public void LogException(System.Exception e)
    {
        Debug.LogException(e);
    }
}