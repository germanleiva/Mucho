using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public class PinLineTrigger : MonoBehaviour
{
    public enum PinTargetType { LEFTHAND, RIGHTHAND, LEFTFOCUS, RIGHTFOCUS, GAZEFOCUS, WALL};

    public PinTargetType pinTargetType;

    public new Renderer renderer;
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
            //The PinLineTrigger component is attached to a potential object that can be highlighted (playback_focus_square_gaze, playback_focus_square_left, playback_focus_square_right, OVRLeftHandVisual_Playback, OVRRightHandVisual_Playback)
            renderer.material = highlightMaterial;
            
            pinLine.UndoHighlightingOfAssetIfAny = () => { renderer.material = defaultMaterial;};
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
            pinLine.UndoHighlightingOfAssetIfAny = null;
        }
    }
}
