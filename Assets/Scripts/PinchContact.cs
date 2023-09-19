using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinchContact : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Oncollisionstay
    void OnCollisionStay(Collision collision)
    {

        //DebugLogger.Instance.Log("Collision between " + gameObject.name + " and " + collision.collider.name);

    }

    void OnTriggerStay(Collider other)
    {
        //DebugLogger.Instance.Log("Trigger between " + gameObject.name + " and " + other.name);
    }
}
