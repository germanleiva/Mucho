using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class MeshCopy : MonoBehaviour
{
    public GameObject PotentialAssetToChange;
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
    }

}
