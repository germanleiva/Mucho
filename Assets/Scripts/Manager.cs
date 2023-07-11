using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public enum AppState
    {
        NONE,
        RECORD,
        PLAYBACK,
        TEST
    }

    // Start is called before the first frame update
    public TMPro.TMP_Text VRDebugText;
    void Start()
    {
        
    }

    public void ClearDebugText()
    {
        VRDebugText.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}


