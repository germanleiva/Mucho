using UnityEngine;

public class ForceArrow : MonoBehaviour
{
    public Transform asset;
    public Transform arrowHead;
    public Transform arrowBody;
    public LineRenderer lineRenderer;
    public int numberOfPoints = 10;
    public float timeInterval = 0.5f;
    public bool isConnectedToAsset = false;
    public Vector3 initialVelocity = new(0, 0, 0);
    public GameObject connectedAsset;
    public Material arrowTranslucentMaterial;

    readonly int layerMask = 1 << 6;
    Vector3 previousArrowHeadPosition;

    //public Transform ArrowEnd;


    //TODO: Make this event driven from grab
    void Update()
    {
        //Call DrawTrajectory() when the current position of arrowHead is different from the previous position
        if ((arrowHead.position-previousArrowHeadPosition).magnitude > 0.0001f)
        {
            ReOrientArrow();
            DrawTrajectory();
        }
        previousArrowHeadPosition = arrowHead.position;        
    }

    public void ReOrientArrow()
    {
        // Position and Scale the cylinder
        PositionAndScaleArrowBody();

        // Rotate the arrow to point towards asset
        ReOrientArrowHead();
    }

    private void PositionAndScaleArrowBody()
    {
        // Position the cylinder
        arrowBody.position = Vector3.Lerp(asset.position, arrowHead.position, 0.5f);

        // Scale the cylinder
        float distance = Vector3.Distance(asset.position, arrowHead.position);
        arrowBody.localScale = new Vector3(arrowBody.localScale.x, distance / 2, arrowBody.localScale.z);

        // Rotate the cylinder
        Vector3 direction = arrowHead.position - asset.position;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
        arrowBody.rotation = rotation;
    }

    private void ReOrientArrowHead()
    {
        // Calculate the direction from asset to the arrow (this)
        Vector3 direction = arrowHead.position - asset.position;

        // Calculate the rotation to align the arrow with this direction
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);

        // Apply the rotation to the arrow
        arrowHead.rotation = rotation;
    }
    

    public void DrawTrajectory()
    {
        lineRenderer.enabled = true;
        
        Vector3 position1 = asset.position;
        //DebugLogger.Instance.Log("Arrow end position: " + position1);
        Vector3 position2 = arrowHead.position;
        //DebugLogger.Instance.Log("Arrow head position: " + position2);
        Vector3 direction = (position2 - position1);

        // Initial velocity is just the direction
        initialVelocity = direction * 10f;

        // Set the number of points in the LineRenderer
        lineRenderer.positionCount = numberOfPoints;

        // Initialize the previous point position to the start point
        Vector3 previousPointPosition = position1;

        // Calculate and set the position of each point in the trajectory
        for (int i = 0; i < numberOfPoints; i++)
        {
            float t = i * timeInterval;
            Vector3 pointPosition = CalculateTrajectoryPoint(position1, initialVelocity, t);

            // Perform raycast from the previous point to the current point
            Vector3 raycastDirection = (pointPosition - previousPointPosition).normalized;
            float raycastDistance = Vector3.Distance(pointPosition, previousPointPosition);

            if (Physics.Raycast(previousPointPosition, raycastDirection, raycastDistance, layerMask))
            {
                //DebugLogger.Instance.Log("Hit surface, stopping trajectory generation");
                // If there is a hit, set the number of points to i + 1 (since i starts from 0)
                lineRenderer.positionCount = i + 1;

                // Set the current point in the LineRenderer and then break
                lineRenderer.SetPosition(i, pointPosition);
                break;
            }
            else
            {
                //DebugLogger.Instance.Log("No hit, continuing trajectory generation");
                // If there is no hit, set the current point in the LineRenderer
                lineRenderer.SetPosition(i, pointPosition);

                // Update the previous point position
                previousPointPosition = pointPosition;
            }
        }
    }

    private Vector3 CalculateTrajectoryPoint(Vector3 startPoint, Vector3 initialVelocity, float time)
    {
        Vector3 gravity = Physics.gravity;
        Vector3 position = startPoint + initialVelocity * time + 0.5f * time * time * gravity;
        return position;
    }

    public void HideTrajectoryAndArrow()
    {
        lineRenderer.enabled = false;
        arrowHead.gameObject.SetActive(false);
        arrowBody.gameObject.SetActive(false);
    }

    public void ShowTrajectoryAndArrow()
    {
        lineRenderer.enabled = true;
        arrowHead.gameObject.SetActive(true);
        arrowBody.gameObject.SetActive(true);
    }


    public void ThrowAsset()
    {
        Manager.Instance.currAppState = Manager.AppState.ASSETRECORDING;
        GameObject throwableAsset = asset.gameObject;
        throwableAsset.GetComponent<Recordable>().PrepareForceSimulation(initialVelocity);
    }

}

