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

    private static AssetMenu _currentlyOpenMenu;

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

        asset.OnMeshUpdated += HandleMeshUpdated;
    }

    private void OnDisable()
    {
        asset.OnMeshUpdated -= HandleMeshUpdated;
        
        if (_currentlyOpenMenu == this)
        {
            _currentlyOpenMenu = null;
        }
    }

    private void HandleMeshUpdated(Asset _) => UpdateTitle();

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
    } // TODO J - This isn't really used? The variable is class private, so it shouldn't be visible outside of this class..so what is this for?

    private void ShowAssetMenu()
    {
        // If there is another menu open, and it's not this one (we just opened it), close it
        if (_currentlyOpenMenu != null && _currentlyOpenMenu != this)
        {
            _currentlyOpenMenu.gameObject.SetActive(false);
        }
        // Set this menu as the currently open menu and show it
        _currentlyOpenMenu = this;
        
        // Effectively "show" the menu by enabling the gameobject
        gameObject.SetActive(true);
    }

    private void HideAssetMenu()
    {
        // If this is the currently open menu, clear the reference to it since we're closing it
        if (_currentlyOpenMenu == this)
        {
            _currentlyOpenMenu = null;
        }

        // Effectively "hide" the menu by disabling the gameobject
        gameObject.SetActive(false);
    }

    public void ToggleAssetMenu()
    {
        if (gameObject.activeSelf)
        {
            HideAssetMenu();
        }
        else
        {
            ShowAssetMenu();
        }
    }

    public void DeleteRootObject()
    {
        Destroy(rootObject);
    }
    
    #region Context Menu Button Callbacks
    [ContextMenu("Toggle Asset Menu")]
    private void ContextMenuToggleAssetMenu()
    {
        ToggleAssetMenu();
    }
    #endregion
}