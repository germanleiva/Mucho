using System.Linq;
using UnityEditor;
using UnityEngine;

/** TODO J - This script is attached to initially disabled objects [HMDPlaybackLine, LeftControllerPlaybackLine, LeftHandLine, RightControllerPlaybackLine, RightHandLine].
Do they get enabled soon or later?*/
[RequireComponent(typeof(LineRenderer))]
public class LineRendererSmoother : MonoBehaviour
{
    public LineRenderer Line;
    //public Vector3[] InitialState = new Vector3[1];
    public float SmoothingLength = 2f;
    public int SmoothingSections = 10;
    

    public void SimplifyAndSmooth()
    {
        Line.Simplify(0.05f);
        /*
        BezierCurve[] curves = new BezierCurve[Line.positionCount - 1];
        for (int i = 0; i < curves.Length; i++)
        {
            curves[i] = new BezierCurve();
        }

        for (int i = 0; i < curves.Length; i++)
        {
            Vector3 position = Line.GetPosition(i);
            Vector3 lastPosition = i == 0 ? Line.GetPosition(0) : Line.GetPosition(i - 1);
            Vector3 nextPosition = Line.GetPosition(i + 1);

            Vector3 lastDirection = (position - lastPosition).normalized;
            Vector3 nextDirection = (nextPosition - position).normalized;

            Vector3 startTangent = (lastDirection + nextDirection) * SmoothingLength;
            Vector3 endTangent = (nextDirection + lastDirection) * -1 * SmoothingLength;


            curves[i].Points[0] = position; // Start Position (P0)
            curves[i].Points[1] = position + startTangent; // Start Tangent (P1)
            curves[i].Points[2] = nextPosition + endTangent; // End Tangent (P2)
            curves[i].Points[3] = nextPosition; // End Position (P3)
        }

        // Apply look-ahead for first curve and retroactively apply the end tangent
        {
            Vector3 nextDirection = (curves[1].EndPosition - curves[1].StartPosition).normalized;
            Vector3 lastDirection = (curves[0].EndPosition - curves[0].StartPosition).normalized;

            curves[0].Points[2] = curves[0].Points[3] +
                (nextDirection + lastDirection) * -1 * SmoothingLength;
        }

        Line.positionCount = curves.Length * SmoothingSections;
        int index = 0;
        for (int i = 0; i < curves.Length; i++)
        {
            Vector3[] segments = curves[i].GetSegments(SmoothingSections);
            for (int j = 0; j < segments.Length; j++)
            {
                Line.SetPosition(index, segments[j]);
                index++;
            }
        }*/
    }
}
