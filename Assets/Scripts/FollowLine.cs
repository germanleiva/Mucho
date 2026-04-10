using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowLine : MonoBehaviour
{
    public Transform followLineStart;
    public Transform followGuide;

    // private Action FollowLineAction;

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
        if (isInitialized)
        {
            PositionAndScaleLineBody();
        }
    }

    private List<FollowLineTrigger> FLT_List = new();
    FollowLineTrigger winningTrigger = null;

    public void TriggerFollowLineAction()
    {
        // if (FollowLineAction != null)  
        if (winningTrigger != null)
        {
            //DebugLogger.Instance.Log("Calling FollowLine: TriggerFollowLineAction");
            var assetScript = this.asset.GetComponent<Asset>();

            // this.FollowLineAction = () =>
            // {
            assetScript.RecordFollow(winningTrigger.followTargetType);
            winningTrigger.SetHighLightThisTrigger(false);
            // };
            // FollowLineAction?.Invoke();
        }
        else
        {
            //DebugLogger.Instance.Log("FollowLine: FollowLineAction is null");
        }

        gameObject.SetActive(false);
        followGuide.transform.position = followLineStart.position;
        //followGuide.gameObject.SetActive(false);
    }

    private void CalculateNewBestTrigger()
    {
        winningTrigger = null;
        // FollowLineTrigger have this enum:     public enum FollowTargetType { LEFTHAND, RIGHTHAND, LEFTFOCUS, RIGHTFOCUS, GAZEFOCUS};
        // You have to loop over the FLT_List and find the trigger with the highest priority (LEFTHAND < RIGHTHAND < LEFTFOCUS < RIGHTFOCUS < GAZEFOCUS)
        foreach (var FLTrigger in FLT_List)
        {
            if (winningTrigger == null)
            {
                winningTrigger = FLTrigger;
            }
            else
            {
                if (FLTrigger.followTargetType > winningTrigger.followTargetType)
                {
                    // winningTrigger.UnHighLightThisTrigger();
                    winningTrigger = FLTrigger;
                }
                // else
                // {
                // FLTrigger.UnHighLightThisTrigger();
                // }
            }
        }
    }

    public bool Register_FLT(FollowLineTrigger followLineTrigger)
    {
        // If the trigger is already in the list, return false
        if (FLT_List.Contains(followLineTrigger))
            return false;

        // De-highlight old triggers
        foreach (var FLTrigger in FLT_List)
            FLTrigger.SetHighLightThisTrigger(false);
        
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
            if (FLT_List[i] == followLineTrigger)
            {
                FLT_List[i].SetHighLightThisTrigger(false);
                FLT_List.RemoveAt(i);

                if (FLT_List.Count > 0)
                {
                    CalculateNewBestTrigger();  // This updates the internal variable winningTrigger to the new best trigger, but does not highlight it
                    winningTrigger.SetHighLightThisTrigger(true);
                }

                return true;
            }
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