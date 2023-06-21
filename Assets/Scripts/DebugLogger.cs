using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugLogger : MonoBehaviour
{
    private static DebugLogger instance = null;
    private static readonly object padlock = new object();
    
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

    public void Log(string message)
    {
        Debug.Log("ProtoXR_Test" + message);
    }
}