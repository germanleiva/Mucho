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

    void LateUpdate()
    {
        Vector3 position1 = object1.transform.position;
        Vector3 position2 = object2.transform.position;
        Vector3 direction = (position2 - position1);

        // Initial velocity is just the direction
        Vector3 initialVelocity = direction * 10f;

        // Set the number of points in the LineRenderer
        lineRenderer.positionCount = numberOfPoints;

        // Calculate and set the position of each point in the trajectory
        for (int i = 0; i < numberOfPoints; i++)
        {
            float t = i * timeInterval;
            Vector3 pointPosition = CalculateTrajectoryPoint(position2, initialVelocity, t);
            lineRenderer.SetPosition(i, pointPosition);
        }
    }

    private Vector3 CalculateTrajectoryPoint(Vector3 startPoint, Vector3 initialVelocity, float time)
    {
        Vector3 gravity = Physics.gravity;
        Vector3 position = startPoint + initialVelocity * time + 0.5f * gravity * time * time;
        return position;
    }
}

