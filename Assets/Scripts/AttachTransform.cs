using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttachTransform : MonoBehaviour
{
    public GameObject targetObject;  // The object to follow
    private Vector3 offsetPosition;  // The initial offset from the target
    private Quaternion offsetRotation; // The initial rotation offset from the target

    void Start()
    {
        // Calculate the initial offset.
        offsetPosition = transform.position - targetObject.transform.position;
        offsetRotation = Quaternion.Inverse(targetObject.transform.rotation) * transform.rotation;
    }

    void Update()
    {
        // Update the position of the object to follow the target while preserving the offset.
        transform.position = targetObject.transform.position + offsetPosition;

        // Update the rotation of the object to follow the target while preserving the offset.
        transform.rotation = targetObject.transform.rotation * offsetRotation;
    }
}
