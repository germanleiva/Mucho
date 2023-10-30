using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowLine : MonoBehaviour
{
    public Transform asset;
    public Transform lineHead;

    Transform targetTransform;

    bool isInitialized = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(isInitialized)
        {
            PositionAndScaleLineBody();
        }
    }

    public void InitializeLine(Transform _targetTransform)
    {
        targetTransform = _targetTransform;
        isInitialized = true;
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        isInitialized = false;
        gameObject.SetActive(false);
    }

    private void PositionAndScaleLineBody()
    {
        lineHead.position = targetTransform.position;

        // Position the cylinder
        transform.position = Vector3.Lerp(asset.position, lineHead.position, 0.5f);

        // Scale the cylinder
        float distance = Vector3.Distance(asset.position, lineHead.position);
        transform.localScale = new Vector3(transform.localScale.x, distance / 2, transform.localScale.z);

        // Rotate the cylinder
        Vector3 direction = lineHead.position - asset.position;
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
        transform.rotation = rotation;
    }
}
