using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextAsset : MonoBehaviour
{
    //textmesh pro text object
    public TMP_Text text;
    public TMP_InputField inputField;

    public GameObject AssetMenu;
    // Start is called before the first frame update
    
    void Awake()
    {
        
    }

    void Start()
    {
        
    }

    public void CopyTextFromInputField()
    {
        DebugLogger.Instance.Log("Copying text from input field");
        text.text = inputField.text;
    }


}
