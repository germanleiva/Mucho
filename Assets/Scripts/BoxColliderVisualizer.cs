using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxColliderVisualizer : MonoBehaviour
{
    public GameObject visualizerCube;
    //private GameObject sphere;

    private BoxCollider boxCollider;

    public GameObject colliderBoundaryGizmo1, colliderBoundaryGizmo2;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
    }


void Update()
{
    // Calculate new size and position based on corner cubes
    Vector3 newSize = colliderBoundaryGizmo2.transform.position - colliderBoundaryGizmo1.transform.position;
    Vector3 newCenter = colliderBoundaryGizmo1.transform.position + newSize / 2;

    // Update the visualization cube
    visualizerCube.transform.localScale = newSize;
    visualizerCube.transform.position = newCenter;

    Vector3 adjustedScale = new Vector3(visualizerCube.transform.localScale.x / transform.localScale.x, visualizerCube.transform.localScale.y / transform.localScale.y, visualizerCube.transform.localScale.z / transform.localScale.z);

    // Update the BoxCollider
    if (boxCollider != null)
    {
        boxCollider.size = adjustedScale;

        // Convert the visualizer cube's world position to the local coordinate space of this GameObject
        Vector3 localCenter = transform.InverseTransformPoint(visualizerCube.transform.position);

        boxCollider.center = localCenter;
    }
}

}
