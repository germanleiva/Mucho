using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TextAsset : MonoBehaviour
{
    //textmesh pro text object
    public TMP_Text text;
    public TMP_InputField inputField;
    public Keyboard keyboard;
    public GameObject TextEditUI;
    //public InputField inputField;

    //public UnityEngine.UI.Image TextPanelBackground;

    public GameObject AssetMenu;
    // Start is called before the first frame update
    
    void Awake()
    {
        
    }

    void Start()
    {
        
    }

    public void ConnectToKeyboard()
    {    
        text.text = "";
        keyboard.ActivateKeyboard(inputField);
        keyboard.submitCallback = SubmitText;        
    }   

    public void SetTextVisibility(bool status)
    {
        text.gameObject.SetActive(status);
    }

    public void SubmitText()
    {
        DebugLogger.Instance.Log("Submitting text");
        text.text = inputField.text;
        inputField.text = "";
        keyboard.gameObject.SetActive(false);
        AssetMenu.SetActive(false);
        TextEditUI.SetActive(false);
    }

    public void CopyTextFromInputField()
    {
        DebugLogger.Instance.Log("Copying text from input field");
        text.text = inputField.text;
    }


}
