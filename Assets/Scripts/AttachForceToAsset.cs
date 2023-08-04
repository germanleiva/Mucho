using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachForceToAsset : MonoBehaviour
{
    public TrajectoryVisualizer trajectoryVisualizer;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Check if an object from user layer 7 (Assets) is trigger colliding with this object
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 7)
        {
            //Attach the object to this object
            //other.transform.parent = transform;
            DebugLogger.Instance.Log("Object attached to asset");
            //trajectoryVisualizer.connectedAsset = other.gameObject;
            //trajectoryVisualizer.isConnectedToAsset = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == 7)
        {
            //Detach the object from this object
            //other.transform.parent = null;
            DebugLogger.Instance.Log("Object detached from asset");
            //trajectoryVisualizer.connectedAsset = null;
            //trajectoryVisualizer.isConnectedToAsset = false;
        }
    }
}
