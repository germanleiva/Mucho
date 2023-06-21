using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuickTransformDebug : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        DebugLogger.Instance.Log(gameObject.name + ", Position: " + transform.position);
    }
}
