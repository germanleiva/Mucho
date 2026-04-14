using System;
using Oculus.Interaction.Surfaces;
using UnityEngine;

public class MeshCopy : MonoBehaviour
{
    public GameObject PotentialAssetToChange;
    [Header("Bounds Clipper Settings")]
    public Vector3 boundsClipperPosition = new Vector3(0, 0, 0);
    public Vector3 boundsClipperSize = new Vector3(1, 1, 1);
    //Disable script at start
    /*void Start()
    {
        //enabled = false;
    }
    
    void OnCollisionEnter (Collision other)
    {
        if(other.gameObject.GetComponent<Asset> () != null)
        {
            DebugLogger.Instance.Log("MeshCopy: Collision with " + other.gameObject.name);
            //Copy the mesh from this object to the other object
            MeshFilter otherMeshFilter = other.gameObject.GetComponent<MeshFilter>();
            MeshFilter thisMeshFilter = gameObject.GetComponent<MeshFilter>();

            Bounds originalBounds = thisMeshFilter.mesh.bounds;
            Bounds newBounds = otherMeshFilter.mesh.bounds;

            // Find the longest dimension of each mesh
            float longestDimensionOriginal = Mathf.Max(originalBounds.size.x, Mathf.Max(originalBounds.size.y, originalBounds.size.z));
            float longestDimensionNew = Mathf.Max(newBounds.size.x, Mathf.Max(newBounds.size.y, newBounds.size.z));

            // Calculate uniform scale factor
            float scaleFactor = longestDimensionOriginal / longestDimensionNew;

            otherMeshFilter.mesh = thisMeshFilter.mesh;

            // Apply the scale uniformly
            other.gameObject.transform.localScale *= 1/scaleFactor;
            other.gameObject.GetComponent<SphereColliderVisualizer>().visualizerSphere.transform.localScale = new Vector3(1, 1, 1);
            DebugLogger.Instance.Log("MeshCopy: Scaling " + gameObject.name + " by " + scaleFactor);


            //otherMeshFilter.mesh = thisMeshFilter.mesh;

            //Copy the material from this object to the other object
            MeshRenderer otherMeshRenderer = other.gameObject.GetComponent<MeshRenderer>();
            MeshRenderer thisMeshRenderer = gameObject.GetComponent<MeshRenderer>();
            otherMeshRenderer.material = thisMeshRenderer.material;

            if(other.gameObject.GetComponent<Asset>() != null)
            {
                other.gameObject.GetComponent<Asset>().defaultMaterial = thisMeshRenderer.material;
            }

            
            //set this object to inactive
            gameObject.SetActive(false);
        }
        
    
    }*/
    
    //Old version for basketball example only
    /* 
     public void ApplyMeshChange()
    {
        if (PotentialAssetToChange != null)
        {
            DebugLogger.Instance.Log("MeshCopy: Apply mesh change to " + PotentialAssetToChange.gameObject.name);
            //Copy the mesh from this object to the other object
            MeshFilter otherMeshFilter = PotentialAssetToChange.gameObject.GetComponent<MeshFilter>();
            MeshFilter thisMeshFilter = gameObject.GetComponent<MeshFilter>();

            Bounds originalBounds = thisMeshFilter.mesh.bounds;
            Bounds newBounds = otherMeshFilter.mesh.bounds;

            // Find the longest dimension of each mesh
            float longestDimensionOriginal = Mathf.Max(originalBounds.size.x, Mathf.Max(originalBounds.size.y, originalBounds.size.z));
            float longestDimensionNew = Mathf.Max(newBounds.size.x, Mathf.Max(newBounds.size.y, newBounds.size.z));

            // Calculate uniform scale factor
            float scaleFactor = longestDimensionOriginal / longestDimensionNew;

            otherMeshFilter.mesh = thisMeshFilter.mesh;

            // Apply the scale uniformly
            PotentialAssetToChange.gameObject.transform.localScale *= 1/scaleFactor;
            PotentialAssetToChange.gameObject.GetComponent<SphereColliderVisualizer>().visualizerSphere.transform.localScale = new Vector3(1, 1, 1);
            DebugLogger.Instance.Log("MeshCopy: Scaling " + gameObject.name + " by " + scaleFactor);


            //otherMeshFilter.mesh = thisMeshFilter.mesh;

            //Copy the material from this object to the other object
            MeshRenderer otherMeshRenderer = PotentialAssetToChange.gameObject.GetComponent<MeshRenderer>();
            MeshRenderer thisMeshRenderer = gameObject.GetComponent<MeshRenderer>();
            otherMeshRenderer.material = thisMeshRenderer.material;
            
            PotentialAssetToChange.gameObject.GetComponent<Asset>().defaultMaterial = thisMeshRenderer.material;
        }
    }*/


    public void ApplyMeshChange()
    {
        // PotentialAsset: Sphere/Cube that will be changed
        // gameObject: Premade object that will be copied
        if(PotentialAssetToChange == null)
        {
            DebugLogger.Instance.Log("MeshCopy: No potential asset to change");
            return;
        }
        DebugLogger.Instance.Log("MeshCopy: :) Potential asset to change: " + PotentialAssetToChange.gameObject.name);
        // MESH
        //Copy the mesh from this object to the other object 
        MeshFilter sphereMeshFilter = PotentialAssetToChange.gameObject.GetComponent<MeshFilter>();
        // Try to get the mesh filter from the gameobject
        MeshFilter premadeAssetMeshFilter = gameObject.GetComponent<MeshFilter>();
        sphereMeshFilter.mesh =  premadeAssetMeshFilter.mesh;
        
        // ROTATION AND SCALE
        Quaternion rotation = gameObject.transform.rotation;
        PotentialAssetToChange.gameObject.transform.rotation = rotation; 
        Vector3 sizeScaleFactor =  gameObject.transform.localScale; //TODO J - Why 2f?
        PotentialAssetToChange.gameObject.transform.localScale = sizeScaleFactor;
        // PotentialAssetToChange.GetComponentInChildren<BoundsClipper>().Size *= 2; //TODO J Added this comment - Check here
        //MATERIAL
        MeshRenderer otherMeshRenderer = PotentialAssetToChange.gameObject.GetComponent<MeshRenderer>();
        MeshRenderer thisMeshRenderer = gameObject.GetComponent<MeshRenderer>();
        otherMeshRenderer.material = thisMeshRenderer.material;
        PotentialAssetToChange.gameObject.GetComponent<Asset>().defaultMaterial = thisMeshRenderer.material;
        
        //TODO J Added
        var boundsClipper = PotentialAssetToChange.gameObject.GetComponentInChildren<BoundsClipper>(); 
        if (boundsClipper == null)
        {
            throw new Exception("Bounds Clipper shouldn't be null in the Asset");
        }
        boundsClipper.Size = this.boundsClipperSize;
        
        // COLLIDER
        SphereCollider sphereCollider = PotentialAssetToChange.gameObject.GetComponent<SphereCollider>();
        SphereCollider premadeAssetSphereCollider = gameObject.GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            float radius = 0.0f;
            Vector3 center = Vector3.zero;
            if(premadeAssetSphereCollider == null)
            {
                MeshCollider premadeAssetMeshCollider = gameObject.GetComponent<MeshCollider>();
                if (premadeAssetMeshCollider != null)
                {
                    Bounds bounds = premadeAssetMeshCollider.bounds;
                    radius = bounds.extents.magnitude;
                    center = bounds.center;
                }
            }
            else
            {
                radius = premadeAssetSphereCollider.radius;
                center = premadeAssetSphereCollider.center;
            }
            // New radius
            sphereCollider.radius = radius;
            
            // Center of the sphere collider
            sphereCollider.center = center;
        }
        
        PotentialAssetToChange.gameObject.GetComponent<SphereColliderVisualizer>().visualizerSphere.transform.localScale = new Vector3(1, 1, 1);

        
        //NAME
        PotentialAssetToChange.gameObject.name = gameObject.name.Replace("Premade", "");
        
        //Update the rotation of the asset
        Asset potentialAsset = PotentialAssetToChange.gameObject.GetComponent<Asset>();
        potentialAsset.InitialRotation = rotation;
        
        // Notify listeners after the whole mesh update is complete
        potentialAsset.NotifyMeshUpdated();
    }

}
