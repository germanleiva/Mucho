using System;
using TMPro;
using UnityEngine;

public class AssetMenu : MonoBehaviour
{
    public Transform target; // Main camera to look at...
    public Asset asset; // The asset this menu is attached to
    private Transform _lineEnd; // The asset object (basketball)
    public Transform lineStart;
    public LineRenderer lineRenderer;

    public TMP_Text menuTitleText;
    bool isGrabbed = false;

    public GameObject rootObject;

    void Awake()
    {
        //set position 60cm in front of target
        //transform.position = target.position + target.forward * 0.6f;
    }

    private void OnEnable()
    {
        #region Check if all references are set
            if (asset == null) throw new Exception("AssetMenu: Asset reference is not set.");
            if (target == null) throw new Exception("AssetMenu: Target is not set.");
            if (lineRenderer == null) throw new Exception("AssetMenu: LineRenderer reference is not set.");
            if (lineStart == null) throw new Exception("AssetMenu: LineStart is not set.");
        #endregion

        _lineEnd = asset.transform;
        UpdateTitle();

        asset.OnMeshUpdated += _ => UpdateTitle();
    }

    private void UpdateTitle()
    {
        if (menuTitleText != null)
        {
            menuTitleText.text = asset.gameObject.name;
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (!isGrabbed)
        {
            transform.LookAt(target);
            //Change the position of the transform to be 20cm above the lineEnd gameobject's highest point
            transform.position = _lineEnd.position + new Vector3(0, 0.2f, 0);
        }

        lineRenderer.SetPosition(0, lineStart.position);
        lineRenderer.SetPosition(1, _lineEnd.position);
    }

    public void MenuGrabbed(bool _isGrabbed)
    {
        isGrabbed = _isGrabbed;
    }

    public void toggleAssetMenu()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    public void hideAssetMenu()
    {
        gameObject.SetActive(false);
    }

    public void DeleteRootObject()
    {
        Destroy(rootObject);
    }
}