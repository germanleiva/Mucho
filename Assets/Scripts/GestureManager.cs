using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GestureManager : MonoBehaviour
{

    public Recordable leftHandRecordable, rightHandRecordable;

    public TMPro.TMP_Text rightHandGestureText, leftHandGestureText;
    // Start is called before the first frame update

    public OVRHand leftHand, rightHand;

    private bool setRightHandNone = false;
    private bool setLeftHandNone = false;

    public static GestureManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update() 
    {

        
    }






}



