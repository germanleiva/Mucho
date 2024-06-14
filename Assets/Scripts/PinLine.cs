using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinLine : MonoBehaviour
{
    public Transform pinLineStart;
    public Transform pinGuide;

    public Action UndoHighlightingOfAssetIfAny;

    //public Transform fo

    //Transform targetTransform;
    public GameObject asset;

    bool isInitialized = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(isInitialized)
        {
            PositionAndScaleLineBody();
        }
    }

    public void TriggerPinAction()
    {
        asset.GetComponent<Asset>().RecordPin(pinGuide.position);
        
        if(UndoHighlightingOfAssetIfAny != null)
        {
            //DebugLogger.Instance.Log("Calling PinLine: TriggerPinAction");
            UndoHighlightingOfAssetIfAny?.Invoke();
        }    
        else
        {
            //DebugLogger.Instance.Log("PinLine: UndoHighlightingOfAssetIfAny is null");
        }
        gameObject.SetActive(false);
        pinGuide.transform.position = pinLineStart.position;
        //pinGuide.gameObject.SetActive(false);
    }

    public void ResetPinLine()
    {
        //targetTransform = _targetTransform;
        isInitialized = true;
        gameObject.SetActive(true);
        pinGuide.gameObject.SetActive(true);
        pinGuide.position = pinLineStart.position;
    }

    public void DeactivatePinLine()
    {
        isInitialized = false;
        gameObject.SetActive(false);
    }

    private void PositionAndScaleLineBody()
    {
        //lineHead.position = targetTransform.position;

        // Position the cylinder
        transform.position = Vector3.Lerp(pinLineStart.position, pinGuide.position, 0.5f);

        // Scale the cylinder
        float distance = Vector3.Distance(pinLineStart.position, pinGuide.position);
        transform.localScale = new Vector3(transform.localScale.x, distance / 2, transform.localScale.z);

        // Rotate the cylinder
        Vector3 direction = pinGuide.position - pinLineStart.position;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
        transform.rotation = rotation;
    }
}
