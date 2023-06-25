using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RecordedLinePointer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        DebugLogger.Instance.Log("Trigger collision with " + other.gameObject.name);
    }

    void OnCollisionEnter(Collision collision)
    {
        DebugLogger.Instance.Log("Collision with " + collision.gameObject.name);
    }
}
