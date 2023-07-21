using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public GameObject spherePrefab;
    public GameObject cubePrefab;
    public GameObject cylinderPrefab;
    public GameObject hmd;

    public GameObject leftHandMenu;
    public enum AppState
    {
        NONE,
        RECORD,
        PLAYBACK,
        TEST,
        LIVE
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

    public void SpawnSphere()
    {
        DebugLogger.Instance.LogInVR("Spawned Sphere");
        Instantiate(spherePrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
    }

    public void SpawnCube()
    {
        DebugLogger.Instance.LogInVR("Spawned Cube");
        Instantiate(cubePrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
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


