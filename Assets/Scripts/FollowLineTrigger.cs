using UnityEngine;

public class FollowLineTrigger : MonoBehaviour
{
    // Warning: altering this enum will break the CalculateNewBestTrigger() in FollowLine.cs, which relies on the enum values to decide which trigger is the "best" one to follow.
    public enum FollowTargetType { LEFTHAND, RIGHTHAND, LEFTFOCUS, RIGHTFOCUS, GAZEFOCUS};

    public FollowTargetType followTargetType;

    new public Renderer renderer; //TODO J - Is this normal
    
    [SerializeField] private Material highlightMaterial;
    private Material defaultMaterial;

    // Start is called before the first frame update
    void Start()
    {
        SaveDefaultColorThisTrigger();
    }
    
    
    void OnCollisionEnter(Collision collision)
    {
        //DebugLogger.Instance.Log("FollowLineTrigger: OnTriggerEnter, collision with " + collision.gameObject.name);
        //Check if the parent of the other collider has the FollowLine component
        
        if(collision.gameObject.name != "FollowGuideSphere")
        {
            //DebugLogger.Instance.Log("FollowGuideSphere not found");
            return;
        }
        
        FollowLine followLine = collision.gameObject.transform.parent.parent.GetComponentInChildren<FollowLine>();

        if(followLine != null)
        {
            followLine.Register_FLT(this);
            // renderer.material = highlightMaterial;


            // var asset = followLine.asset.GetComponent<Asset>();

            // followLine.FollowLineAction = () =>
            // {
            //     asset.RecordFollow(followTargetType); 
            //     renderer.material = defaultMaterial;
            // };
        }
    }

    void OnCollisionExit(Collision collision)
    {
        //DebugLogger.Instance.Log("FollowLineTrigger: OnTriggerExit, collision with " + collision.gameObject.name);
        //Check if the parent of the other collider has the FollowLine component

        if(collision.gameObject.name != "FollowGuideSphere")
        {
            //DebugLogger.Instance.Log("FollowGuideSphere not found");
            return;
        }
        
        FollowLine followLine = collision.gameObject.transform.parent.parent.GetComponentInChildren<FollowLine>();

        if (followLine != null)
        {
            followLine.UnRegister_FLT(this);
        }
        // UnHighLightThisTrigger();
        
    }

    public void SetHighLightThisTrigger(bool doHighlight)
    {
        renderer.material = doHighlight ? highlightMaterial : defaultMaterial;
    }

    private void SaveDefaultColorThisTrigger()
    {
        defaultMaterial = renderer.material;
    }
    
    // public void UnHighLightThisTrigger()
    // {
    //     // if(followLine != null)
    //     // {
    //         //followLine.InitializeFollowLine();
    //         renderer.material = defaultMaterial;
    //         // followLine.FollowLineAction = null;
    //     // }
    // }
}
