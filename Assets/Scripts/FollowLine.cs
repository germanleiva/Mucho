using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowLine : MonoBehaviour
{
    public Transform followLineStart;
    public Transform followGuide;

    public Action FollowLineAction;

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

    public void TriggerFollowLineAction()
    {
        if(FollowLineAction != null)
        {
            //DebugLogger.Instance.Log("Calling FollowLine: TriggerFollowLineAction");
            FollowLineAction?.Invoke();
        }    
        else
        {
            //DebugLogger.Instance.Log("FollowLine: FollowLineAction is null");
        }
        gameObject.SetActive(false);
        followGuide.transform.position = followLineStart.position;
        //followGuide.gameObject.SetActive(false);
    }

    public void ResetFollowLine()
    {
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
