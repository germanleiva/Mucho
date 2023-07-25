using UnityEngine;

public class ForceArrow : MonoBehaviour
{
    public Transform ArrowEnd;
    public Transform ArrowBody;


    //TODO: Make this event driven from grab
    void Update()
    {
        // Position and Scale the cylinder
        PositionAndScaleCylinder();

        // Rotate the arrow to point towards object1
        ReOrientArrow();
    }

    private void PositionAndScaleCylinder()
    {
        // Position the cylinder
        ArrowBody.position = Vector3.Lerp(ArrowEnd.position, transform.position, 0.5f);

        // Scale the cylinder
        float distance = Vector3.Distance(ArrowEnd.position, transform.position);
        ArrowBody.localScale = new Vector3(ArrowBody.localScale.x, distance / 2, ArrowBody.localScale.z);

        // Rotate the cylinder
        Vector3 direction = transform.position - ArrowEnd.position;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
        ArrowBody.rotation = rotation;
    }

    private void ReOrientArrow()
    {
        // Calculate the direction from object1 to the arrow (this)
        Vector3 direction = transform.position - ArrowEnd.position;

        // Calculate the rotation to align the arrow with this direction
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);

        // Apply the rotation to the arrow
        transform.rotation = rotation;
    }
}

