using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public class PinLineTrigger : MonoBehaviour
{
    public enum PinTargetType { LEFTHAND, RIGHTHAND, LEFTFOCUS, RIGHTFOCUS, GAZEFOCUS};

    public PinTargetType pinTargetType;

    new public Renderer renderer;
    public Material highlightMaterial;
    Material defaultMaterial;

    // Start is called before the first frame update
    void Start()
    {
        defaultMaterial = renderer.material;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void OnTriggerEnter(Collider other)
    {
        //DebugLogger.Instance.Log("FollowLineTrigger: OnTriggerEnter, collision with " + other.gameObject.name);
        //Check if the parent of the other collider has the FollowLine component
        if(other.gameObject.name != "PinGuideSphere")
        {
            //DebugLogger.Instance.Log("PinGuideSphere not found");
            return;
        }

        if(other.gameObject.transform.parent?.GetComponentInChildren<PinLine>() == null)
        {
            //DebugLogger.Instance.Log("PinLineTrigger: No PinLine component found for collider " + other.gameObject.name);
            return;
        }
        
        PinLine pinLine = other.gameObject.transform.parent.GetComponentInChildren<PinLine>();

        if(pinLine != null)
        {
            //followLine.DeactivateFollowLine();
            renderer.material = highlightMaterial;  
            if(pinTargetType == PinTargetType.LEFTHAND)
            {                
                pinLine.PinAction = () => { pinLine.asset.GetComponent<Recordable>().RecordPinToLeftHand(); renderer.material = defaultMaterial;};             
                //followLine.asset.GetComponent<Recordable>().AttachToLeftHand();
            }
            else if(pinTargetType == PinTargetType.RIGHTHAND)
            {                
                pinLine.PinAction = () => { pinLine.asset.GetComponent<Recordable>().RecordPinToRightHand(); renderer.material = defaultMaterial;};
                //followLine.asset.GetComponent<Recordable>().AttachToRightHand();
            }
            else if(pinTargetType == PinTargetType.LEFTFOCUS)
            {
                pinLine.PinAction = () => { pinLine.asset.GetComponent<Recordable>().RecordPinToLeftFocus(); renderer.material = defaultMaterial;}; 
            }
            else if(pinTargetType == PinTargetType.RIGHTFOCUS)
            {
                pinLine.PinAction = () => { pinLine.asset.GetComponent<Recordable>().RecordPinToRightFocus(); renderer.material = defaultMaterial;};
            }
            else if(pinTargetType == PinTargetType.GAZEFOCUS)
            {
                pinLine.PinAction = () => { pinLine.asset.GetComponent<Recordable>().RecordPinToGazeFocus(); renderer.material = defaultMaterial;};
            }
            else
            {
                DebugLogger.Instance.Log("PinLineTrigger: No PinTargetType found for " + pinTargetType);
            }
            
        }
    }

    void OnTriggerExit(Collider other)
    {
        //DebugLogger.Instance.Log("FollowLineTrigger: OnTriggerExit, collision with " + other.gameObject.name);
        //Check if the parent of the other collider has the FollowLine component

        if(other.gameObject.name != "PinGuideSphere")
        {
           //DebugLogger.Instance.Log("PinGuideSphere not found");
            return;
        }

        if(other.gameObject.transform.parent?.GetComponentInChildren<PinLine>() == null)
        {
            //DebugLogger.Instance.Log("PinLineTrigger: No PinLine component found for collider " + other.gameObject.name);
            return;
        }
        
        PinLine pinLine = other.gameObject.transform.parent.GetComponentInChildren<PinLine>();

        if(pinLine != null)
        {
            //followLine.ResetFollowLine();
            renderer.material = defaultMaterial;
            pinLine.PinAction = null;
        }
    }
}
