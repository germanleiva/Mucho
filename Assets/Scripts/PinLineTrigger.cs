using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public class PinLineTrigger : MonoBehaviour
{
    public enum PinTargetType { LEFTHAND, RIGHTHAND, LEFTFOCUS, RIGHTFOCUS, GAZEFOCUS, WALL};

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
    
    void OnCollisionEnter(Collision collision)
    {
        //DebugLogger.Instance.Log("FollowLineTrigger: OnTriggerEnter, collision with " + other.gameObject.name);
        //Check if the parent of the other collider has the FollowLine component
        if(collision.gameObject.name != "PinGuideSphere")
        {
            //DebugLogger.Instance.Log("PinGuideSphere not found");
            return;
        }
        
        PinLine pinLine = collision.gameObject.transform.parent.parent.GetComponentInChildren<PinLine>();

        if(pinLine != null)
        {
            //followLine.DeactivateFollowLine();
            renderer.material = highlightMaterial;  
            if(pinTargetType == PinTargetType.LEFTHAND)
            {                
                pinLine.PinAction = () => { pinLine.asset.GetComponent<Asset>().RecordPinToLeftHand(); renderer.material = defaultMaterial;};             
                //followLine.asset.GetComponent<Recordable>().AttachToLeftHand();
            }
            else if(pinTargetType == PinTargetType.RIGHTHAND)
            {                
                pinLine.PinAction = () => { pinLine.asset.GetComponent<Asset>().RecordPinToRightHand(); renderer.material = defaultMaterial;};
                //followLine.asset.GetComponent<Recordable>().AttachToRightHand();
            }
            else if(pinTargetType == PinTargetType.LEFTFOCUS)
            {
                pinLine.PinAction = () => { pinLine.asset.GetComponent<Asset>().RecordPinToLeftFocus(); renderer.material = defaultMaterial;}; 
            }
            else if(pinTargetType == PinTargetType.RIGHTFOCUS)
            {
                pinLine.PinAction = () => { pinLine.asset.GetComponent<Asset>().RecordPinToRightFocus(); renderer.material = defaultMaterial;};
            }
            else if(pinTargetType == PinTargetType.GAZEFOCUS)
            {
                pinLine.PinAction = () => { pinLine.asset.GetComponent<Asset>().RecordPinToGazeFocus(); renderer.material = defaultMaterial;};
            }
            /*else if(pinTargetType == PinTargetType.WALL)
            {
                pinLine.PinAction = () => { pinLine.asset.GetComponent<Recordable>().RecordPinToWall(); renderer.material = defaultMaterial;};
            }*/
            else
            {
                DebugLogger.Instance.Log("PinLineTrigger: No PinTargetType found for " + pinTargetType);
            }
            
        }
    }

    void OnCollisionExit(Collision collision)
    {
        //DebugLogger.Instance.Log("FollowLineTrigger: OnTriggerExit, collision with " + other.gameObject.name);
        //Check if the parent of the other collider has the FollowLine component

        if(collision.gameObject.name != "PinGuideSphere")
        {
           //DebugLogger.Instance.Log("PinGuideSphere not found");
            return;
        }
        
        PinLine pinLine = collision.gameObject.transform.parent.parent.GetComponentInChildren<PinLine>();

        if(pinLine != null)
        {
            //followLine.ResetFollowLine();
            renderer.material = defaultMaterial;
            pinLine.PinAction = null;
        }
    }
}
