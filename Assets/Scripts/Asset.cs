using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asset : MonoBehaviour
{
    public GameObject AssetMenu;
    
    // Start is called before the first frame update

    void Awake()
    {
        AssetMenu.SetActive(false);
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void toggleAssetMenu()
    {
        AssetMenu.SetActive(!AssetMenu.activeSelf);
    }

}
