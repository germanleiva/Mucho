using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereColliderVisualizer : MonoBehaviour
{
    public GameObject visualizerSphere;
    private SphereCollider sphereCollider;

    public GameObject colliderBoundaryGizmo;

    void Start()
    {
        sphereCollider = GetComponent<SphereCollider>();
    }

    void Update()
    {
        if (visualizerSphere != null && sphereCollider != null && colliderBoundaryGizmo != null)
        {
            float newRadius = Vector3.Distance(visualizerSphere.transform.position, colliderBoundaryGizmo.transform.position);
            visualizerSphere.transform.localScale = new Vector3(newRadius * 2, newRadius * 2, newRadius * 2);

            float radius = visualizerSphere.transform.localScale.x / 2;
            sphereCollider.radius = radius;
            sphereCollider.center = visualizerSphere.transform.localPosition;
        }
    }
}
