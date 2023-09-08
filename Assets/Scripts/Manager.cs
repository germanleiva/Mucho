using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public GameObject leftHandMenu;

    public AppState currAppState;

    public enum AppState
    {
        NONE,
        RECORDING,
        PLAYBACK,
        ASSETRECORDING,
        TEST,
        LIVE
    }

    public static Manager Instance { get; private set; }

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


    // Start is called before the first frame update
    public TMPro.TMP_Text VRDebugText;
    void Start()
    {
        currAppState = Manager.AppState.NONE;
    }

    public void ClearDebugText()
    {
        VRDebugText.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public string CleanString(string str)
    {        
        if (str.IndexOf('-') == -1)
        {
            return str;
        }
        return str.Substring(0, str.IndexOf('-'));
    }

    public void OpenLeftHandMenu()
    {
        leftHandMenu.SetActive(true);
    }

    public void CloseLeftHandMenu()
    {
        leftHandMenu.SetActive(false);
    }

}


