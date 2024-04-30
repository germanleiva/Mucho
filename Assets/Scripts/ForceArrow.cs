using UnityEngine;

public class ForceArrow : MonoBehaviour
{
    public Transform asset;
    public Transform arrowHeadGhost;
    public Transform arrowHeadReal;

    public Transform arrowBody;
    public LineRenderer lineRenderer;
    public int numberOfPoints = 20;
    public float timeInterval = 0.5f;
    public bool isConnectedToAsset = false;
    public Vector3 initialVelocity = new(0, 0, 0);
    public GameObject connectedAsset;
    public Material arrowTranslucentMaterial;

    public int indexWhereArrowIsVisible = 0;

    public Example associatedExample = null;
    public TMPro.TextMeshProUGUI forceMagnitudeText;

    public GameObject forceMagnitudeUI;

    readonly int layerMask = 1 << 6;
    Vector3 previousArrowHeadPosition;
    Vector3 previousArrowGhostHeadPosition;


    //public Transform ArrowEnd;
    void Start()
    {
        previousArrowGhostHeadPosition = arrowHeadGhost.position;
        previousArrowHeadPosition = arrowHeadReal.position;
        
    }

    //TODO: Make this event driven from grab
    void Update()
    {
        if((int) Recorder.Instance.playbackSlider.value == indexWhereArrowIsVisible && associatedExample == Recorder.Instance.currentActiveExample && !Recorder.Instance.isAutomaticPlayback)
        {
            ShowTrajectoryAndArrow();
        }
        else
        {
            HideTrajectoryAndArrow();
        }

        if (!AssetManager.isForceArrowGhostActive) {
            //Call DrawTrajectory() when the current position of arrowHead is different from the previous position
            if ((arrowHeadReal.position-previousArrowHeadPosition).magnitude > 0.0001f) {
                ReOrientArrow();
                DrawTrajectory(10);
            }
            previousArrowHeadPosition = arrowHeadReal.position;        
            return;
        }

        //Call DrawTrajectory() when the current position of arrowHead is different from the previous position
        if ((arrowHeadGhost.position-previousArrowGhostHeadPosition).magnitude > 0.0001f)
        {
            float calculatedMagnitude = OrientForceArrow();
            DrawTrajectory(calculatedMagnitude);
        }
        previousArrowGhostHeadPosition = arrowHeadGhost.position;        
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
        arrowBody.position = Vector3.Lerp(asset.position, arrowHeadReal.position, 0.5f);

        // Scale the cylinder
        float distance = Vector3.Distance(asset.position, arrowHeadReal.position);
        arrowBody.localScale = new Vector3(arrowBody.localScale.x, distance / 2, arrowBody.localScale.z);

        // Rotate the cylinder
        Vector3 direction = arrowHeadReal.position - asset.position;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
        arrowBody.rotation = rotation;
    }

    private void ReOrientArrowHead()
    {
        // Calculate the direction from asset to the arrow (this)
        Vector3 direction = arrowHeadReal.position - asset.position;

        // Calculate the rotation to align the arrow with this direction
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);

        // Apply the rotation to the arrow
        arrowHeadReal.rotation = rotation;
    }


    public float OrientForceArrow()
    {
        //Ghost arrow head
        Vector3 direction = arrowHeadGhost.position - asset.position;
        Quaternion ghostArrowRot = Quaternion.FromToRotation(Vector3.up, direction);
        arrowHeadGhost.rotation = ghostArrowRot;

        //Calculate magnitude and direction of force
        float magnitude = Vector3.Distance(arrowHeadGhost.position, asset.position) * 19;
        Vector3 forceDir = CalculateDirectionFromHand();
                
        //Real arrow head
        arrowHeadReal.position = asset.position + forceDir * magnitude;     
        Quaternion realArrowRot = Quaternion.FromToRotation(Vector3.up, forceDir);
        arrowHeadReal.rotation = realArrowRot;

        //Arrow body
        arrowBody.position = Vector3.Lerp(asset.position, arrowHeadReal.position, 0.5f);
        float distance = Vector3.Distance(asset.position, arrowHeadReal.position);
        arrowBody.localScale = new Vector3(arrowBody.localScale.x, distance / 2, arrowBody.localScale.z);
        Quaternion bodyRot = Quaternion.FromToRotation(Vector3.up, forceDir);
        arrowBody.rotation = bodyRot;

        return magnitude;
        
    }

    Vector3 CalculateDirectionFromHand()
    {
        Vector3 direction = Vector3.zero;
        float distanceToLeftHand = Vector3.Distance(asset.position, Recorder.Instance.leftHand.playbackObject.transform.position);
        float distanceToRightHand = Vector3.Distance(asset.position, Recorder.Instance.rightHand.playbackObject.transform.position);
        int currentIndex = (int) Recorder.Instance.playbackSlider.value;
        if (distanceToLeftHand < distanceToRightHand)
        {
            DebugLogger.Instance.Log("CalculateDirectionFromHand: Left hand is closer to asset");
            var leftHandData = Recorder.Instance.currentActiveExample.leftHandFrames;
            
            if (currentIndex - 2 >= 0)
            {
                //Find average of leftHandData[].rootPosition for the last 10 frames
                /*Vector3 averageRootPosition = Vector3.zero;
                for (int i = currentIndex - 10; i < currentIndex; i++)
                {
                    averageRootPosition += leftHandData[i].rootPosition;
                }
                averageRootPosition /= 10;                
                direction = leftHandData[currentIndex].rootPosition - averageRootPosition;*/
                direction = leftHandData[currentIndex].rootPosition - leftHandData[currentIndex - 2].rootPosition;
            }
            else
            {
                direction = Vector3.zero;
            }
        }
        else
        {
            DebugLogger.Instance.Log("CalculateDirectionFromHand: Right hand is closer to asset");
            var rightHandData = Recorder.Instance.currentActiveExample.rightHandFrames;
            
            if (currentIndex - 10 >= 0)
            {
                //Find average of rightHandData[].rootPosition for the last 10 frames
                /*Vector3 averageRootPosition = Vector3.zero;
                for (int i = currentIndex - 10; i < currentIndex; i++)
                {
                    averageRootPosition += rightHandData[i].rootPosition;
                }
                averageRootPosition /= 10;                
                direction = rightHandData[currentIndex].rootPosition - averageRootPosition;*/
                direction = rightHandData[currentIndex].rootPosition - rightHandData[currentIndex - 10].rootPosition;
            }
            else
            {
                direction = Vector3.zero;
            }


        }

        DebugLogger.Instance.Log("CalculateDirectionFromHand: Direction is " + direction);

        //Find the 
        return direction;

    }

    void PositionArrowHeadReal(Vector3 direction, float magnitude)
    {
        // Position the arrow head by a magnitude along the given direction with the asset as the origin
        arrowHeadReal.position = asset.position + direction * magnitude;

    }
    

    public void DrawTrajectory(float magnitude)
    {
        lineRenderer.enabled = true;
        
        Vector3 position1 = asset.position;
        //DebugLogger.Instance.Log("Arrow end position: " + position1);
        Vector3 position2 = arrowHeadReal.position;
        //DebugLogger.Instance.Log("Arrow head position: " + position2);
        Vector3 direction = (position2 - position1);

        // Initial velocity is just the direction
        initialVelocity = direction * magnitude;

        forceMagnitudeUI.transform.position = (arrowHeadReal.position + arrowBody.position) / 2;    
        forceMagnitudeUI.transform.position += new Vector3(0, 0.2f, 0);
        
        forceMagnitudeUI.transform.LookAt(Camera.main.transform.position);
        forceMagnitudeUI.transform.rotation = forceMagnitudeUI.transform.rotation * Quaternion.Euler(0, 180, 0);

        forceMagnitudeText.text = System.Math.Round(initialVelocity.magnitude * 10, 2).ToString("F1");

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
        arrowHeadGhost.gameObject.SetActive(false);
        arrowHeadReal.gameObject.SetActive(false);
        arrowBody.gameObject.SetActive(false);
        forceMagnitudeUI.SetActive(false);
    }

    public void ShowTrajectoryAndArrow()
    {
        lineRenderer.enabled = true;
        if (AssetManager.isForceArrowGhostActive) {
            arrowHeadGhost.gameObject.SetActive(true);
        }
        arrowHeadReal.gameObject.SetActive(true);
        arrowBody.gameObject.SetActive(true);
        forceMagnitudeUI.SetActive(true);
    }

    public void ThrowAsset()
    {
        Manager.Instance.currAppState = Manager.AppState.ASSETRECORDING;
        GameObject throwableAsset = asset.gameObject;
        throwableAsset.GetComponent<Asset>().isThisAssetThrown = true;
        throwableAsset.GetComponent<Asset>().PrepareForceSimulation(initialVelocity);
        if (AssetManager.isForceArrowGhostActive) {
            arrowHeadGhost.transform.position = arrowHeadReal.transform.position;
        }
    }

}

