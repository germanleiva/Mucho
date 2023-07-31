using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public GameObject spherePrefab;
    public GameObject cubePrefab;
    public GameObject cylinderPrefab;
    public GameObject textAssetPrefab;

    public GameObject hmd;

    public GameObject leftHandMenu;

    public GameObject forceArrowPrefab;

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

    public void OpenLeftHandMenu()
    {
        leftHandMenu.SetActive(true);
    }

    public void CloseLeftHandMenu()
    {
        leftHandMenu.SetActive(false);
    }

    public void CreateForceArrow()
    {
        //GameObject obj = Instantiate(cubePrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
        GameObject forceArrow = Instantiate(forceArrowPrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
        forceArrow.SetActive(true);
    }

    public void CreateTextAsset()
    {
        GameObject textAsset = Instantiate(textAssetPrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
        textAsset.SetActive(true);
    }

}


