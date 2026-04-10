using System;
using System.Collections.Generic;
using UnityEngine;

public class FollowLine : MonoBehaviour
{
    public Transform followLineStart;
    public Transform followGuide;

    [SerializeField] private Asset assetScript;

    bool isInitialized = false;

    // Update is called once per frame
    void Update()
    {
        if (isInitialized)
        {
            PositionAndScaleLineBody();
        }
        
        if (assetScript == null)
            throw new Exception("FollowLine: assetScript is not assigned in the inspector. Add 'Asset' component.");
    }

    private List<FollowLineTrigger> FLT_List = new();
    FollowLineTrigger winningTrigger = null;

    public void TriggerFollowLineAction()
    {
        if (winningTrigger != null)
        {
            assetScript.RecordFollow(winningTrigger.followTargetType);
            winningTrigger.SetHighLightThisTrigger(false);
        }

        gameObject.SetActive(false);
        followGuide.transform.position = followLineStart.position;
    }

    private void CalculateNewBestTrigger()
    {
        winningTrigger = null;
        // FollowLineTrigger have this enum:     public enum FollowTargetType { LEFTHAND, RIGHTHAND, LEFTFOCUS, RIGHTFOCUS, GAZEFOCUS};
        // We have to loop over the FLT_List and find the trigger with the highest priority (LEFTHAND < RIGHTHAND < LEFTFOCUS < RIGHTFOCUS < GAZEFOCUS)
        foreach (var FLT in FLT_List)
        {
            if (winningTrigger == null)
            {
                winningTrigger = FLT;
            }
            else
            {
                if (FLT.followTargetType > winningTrigger.followTargetType)
                {
                    winningTrigger = FLT;
                }
            }
        }
    }

    public bool Register_FLT(FollowLineTrigger followLineTrigger)
    {
        // If the trigger is already in the list, return false
        if (FLT_List.Contains(followLineTrigger))
            return false;

        // De-highlight old triggers
        foreach (var FLT in FLT_List)
            FLT.SetHighLightThisTrigger(false);
        
        // Add the new trigger to the list and highlight it
        FLT_List.Add(followLineTrigger);

        CalculateNewBestTrigger(); // This updates the internal variable winningTrigger to the new best trigger, but does not highlight it
        winningTrigger.SetHighLightThisTrigger(true);
        
        return true;
    }

    public bool UnRegister_FLT(FollowLineTrigger followLineTrigger)
    {
        // Find the trigger in the list, remove it and de-highlight it
        for (int i = 0; i < FLT_List.Count; i++)
        {
            if (FLT_List[i] != followLineTrigger) continue;
            
            FLT_List[i].SetHighLightThisTrigger(false);
            FLT_List.RemoveAt(i);

            if (FLT_List.Count > 0)
            {
                CalculateNewBestTrigger();  // This updates the internal variable winningTrigger to the new best trigger, but does not highlight it
                winningTrigger.SetHighLightThisTrigger(true);
            }

            return true;
        }
        // Didn't find the trigger in the list, return false
        return false;
    }

    public void InitializeFollowLine()
    {
        FLT_List = new();
        winningTrigger = null;

        //targetTransform = _targetTransform;
        isInitialized = true;
        gameObject.SetActive(true);
        followGuide.gameObject.SetActive(true);
        followGuide.position = followLineStart.position;
    }

    //TODO J Is this used?
    public void DeactivateFollowLine()
    {
        isInitialized = false;
        gameObject.SetActive(false);
    }

    private void PositionAndScaleLineBody()
    {
        //lineHead.position = targetTransform.position;

        // Position the cylinder
        transform.position = Vector3.Lerp(followLineStart.position, followGuide.position, 0.5f);

        // Scale the cylinder
        float distance = Vector3.Distance(followLineStart.position, followGuide.position);
        transform.localScale = new Vector3(transform.localScale.x, distance / 2, transform.localScale.z);

        // Rotate the cylinder
        Vector3 direction = followGuide.position - followLineStart.position;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
        transform.rotation = rotation;
    }
}