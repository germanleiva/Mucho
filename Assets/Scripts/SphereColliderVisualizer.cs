using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereColliderVisualizer : MonoBehaviour
{
    public GameObject visualizerSphere;
    //private GameObject sphere;

    private SphereCollider sphereCollider;

    public GameObject colliderBoundaryGizmo;

    void Start()
    {
        sphereCollider = GetComponent<SphereCollider>();
    }

    void Update()
    {
        /*if (visualizerSphere != null && sphereCollider != null && colliderBoundaryGizmo != null)
        {
            float visualizerRadius = Vector3.Distance(visualizerSphere.transform.localPosition, colliderBoundaryGizmo.transform.localPosition) - colliderBoundaryGizmo.transform.localScale.x / 2;
            visualizerSphere.transform.localScale = new Vector3(visualizerRadius * 2, visualizerRadius * 2, visualizerRadius * 2);

            //float adjustedScale = visualizerSphere.transform.localScale.x / transform.localScale.x;

            //float radius = adjustedScale / 2;
            sphereCollider.radius = visualizerSphere.transform.localScale.x / 2;
            sphereCollider.center = visualizerSphere.transform.localPosition;
            //visualizerSphere.transform.position = transform.position;
        }*/
    }

}
