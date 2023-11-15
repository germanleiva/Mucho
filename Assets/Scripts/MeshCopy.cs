using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class MeshCopy : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter (Collision other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("RecordableAsset"))
        {
            DebugLogger.Instance.Log("MeshCopy: Collision with " + other.gameObject.name);
            //Copy the mesh from this object to the other object
            MeshFilter otherMeshFilter = other.gameObject.GetComponent<MeshFilter>();
            MeshFilter thisMeshFilter = gameObject.GetComponent<MeshFilter>();
            otherMeshFilter.mesh = thisMeshFilter.mesh;

            //Copy the material from this object to the other object
            MeshRenderer otherMeshRenderer = other.gameObject.GetComponent<MeshRenderer>();
            MeshRenderer thisMeshRenderer = gameObject.GetComponent<MeshRenderer>();
            otherMeshRenderer.material = thisMeshRenderer.material;

            if(other.gameObject.GetComponent<Recordable>() != null)
            {
                other.gameObject.GetComponent<Recordable>().defaultMaterial = thisMeshRenderer.material;
            }


            //Find the bounds of the mesh and then scale the other object to match
            /*Bounds bounds = otherMeshFilter.mesh.bounds;
            Vector3 scale = other.transform.localScale;
            scale.x = bounds.size.x;
            scale.y = bounds.size.y;
            scale.z = bounds.size.z;
            other.transform.localScale = scale;*/

            
            //set this object to inactive
            gameObject.SetActive(false);
        }
        
    
    }

}
