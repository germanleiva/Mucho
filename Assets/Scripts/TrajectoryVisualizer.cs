using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrajectoryVisualizer : MonoBehaviour
{
    public GameObject object1;
    public GameObject object2;
    public LineRenderer lineRenderer;
    public float mass = 0.01f;
    public int numberOfPoints = 10;
    public float timeInterval = 5f;

    int layerMask = 1 << 6;
    

    void DrawTrajectory()
    {
        lineRenderer.enabled = true;
        
        Vector3 position1 = object1.transform.position;
        Vector3 position2 = object2.transform.position;
        Vector3 direction = (position2 - position1);

        // Initial velocity is just the direction
        Vector3 initialVelocity = direction * 10f;

        // Set the number of points in the LineRenderer
        lineRenderer.positionCount = numberOfPoints;

        // Initialize the previous point position to the start point
        Vector3 previousPointPosition = position2;

        // Calculate and set the position of each point in the trajectory
        for (int i = 0; i < numberOfPoints; i++)
        {
            float t = i * timeInterval;
            Vector3 pointPosition = CalculateTrajectoryPoint(position2, initialVelocity, t);

            // Perform raycast from the previous point to the current point
            Vector3 raycastDirection = (pointPosition - previousPointPosition).normalized;
            float raycastDistance = Vector3.Distance(pointPosition, previousPointPosition);

            if (Physics.Raycast(previousPointPosition, raycastDirection, raycastDistance, layerMask))
            {
                DebugLogger.Instance.Log("Hit surface, stopping trajectory generation");
                // If there is a hit, set the number of points to i + 1 (since i starts from 0)
                lineRenderer.positionCount = i + 1;

                // Set the current point in the LineRenderer and then break
                lineRenderer.SetPosition(i, pointPosition);
                break;
            }
            else
            {
                DebugLogger.Instance.Log("No hit, continuing trajectory generation");
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
        Vector3 position = startPoint + initialVelocity * time + 0.5f * gravity * time * time;
        return position;
    }

    public void ArrowSelected()
    {
        DebugLogger.Instance.Log("Arrow selected");
        DrawTrajectory();
    }
}

