using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.UI;

public class AssetPoseRecorder : MonoBehaviour
{   
    public static AssetPoseRecorder Instance { get; private set; }
    //public Slider playbackSlider;
    //private float recordStartTime;

    private bool isRecording = false;
    private bool isPlayingBack = false;

    //public Recordable recordable; //Should be a list of recordables

    public List <Recordable> recordableAssets = new List<Recordable>();
    
    public Recordable currentActiveRecordable;

    public Recorder mainRecorder;

    Vector3 lastLocalPosition = Vector3.zero;

    //public Recordable[] objectsToRecord;

    // Start is called before the first frame update

    public GameObject spherePrefab;
    public GameObject cubePrefab;
    public GameObject forceArrowPrefab;
    //public GameObject cylinderPrefab;
    public GameObject textAsset;
    public GameObject hmd;

    public Material defaultMaterial, transparentMaterial;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        lastLocalPosition = transform.localPosition;
    }

    public void SetRigidbodyConstraints(Recordable recordable, bool setConstraints)
    {
        if(setConstraints)
        {
            recordable.playbackObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            recordable.playbackObject.GetComponent<Rigidbody>().useGravity = false;
        }
        else
        {
            recordable.playbackObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            recordable.playbackObject.GetComponent<Rigidbody>().useGravity = true;
        }
    }

    public void ApplyForce(Recordable recordable, Vector3 force)
    {
        DebugLogger.Instance.Log("Applying force to " + recordable.playbackObject.name);
        //Reset rigidbody constraints
        SetRigidbodyConstraints(recordable, false);
        recordable.playbackObject.GetComponent<Rigidbody>().AddForce(force*0.01f, ForceMode.VelocityChange);
    }

    public void RecordForceOnAsset(Recordable recordable, Vector3 force)
    {
        
        //Reset rigidbody constraints
        //SetRigidbodyConstraints(recordable, true);
        //recordable.playbackObject.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 5, ForceMode.VelocityChange);
        if (isRecording)
        {
            DebugLogger.Instance.Log("Recorded force on " + recordable.playbackObject.name);
            //recordable.appliedForce = force;
            //recordable.Record(mainRecorder.playbackSlider.value);
        }
        else
        {
            DebugLogger.Instance.Log("Recording off, could not record force on " + recordable.playbackObject.name);
        }
    }

    public void Hide(Recordable recordable)
    {
        
        //Turn the material in recordable.playbackObject to 0.5 alpha
        recordable.playbackObject.GetComponent<MeshRenderer>().material = transparentMaterial;
        recordable.showStatus = false;
        if(isRecording)
        {
            DebugLogger.Instance.Log("Recorded hide for  " + recordable.playbackObject.name);
            recordable.InsertAssetRecordFrame(mainRecorder.playbackSlider.value,true);
            //recordable.Record(mainRecorder.playbackSlider.value);
        }
    }

    public void Show(Recordable recordable)
    {
        
        //Turn the material in recordable.playbackObject to 1 alpha
        recordable.playbackObject.GetComponent<MeshRenderer>().material = defaultMaterial;
        recordable.showStatus = true;
        if (isRecording)
        {
            DebugLogger.Instance.Log("Recorded show for " + recordable.playbackObject.name);
            recordable.InsertAssetRecordFrame(mainRecorder.playbackSlider.value,true);
            //recordable.Record(mainRecorder.playbackSlider.value);
            //Re
        }
    }

    public void StartRecording(Recordable recordable)
    {
        //currAppState = Manager.AppState.RECORD;
        //rootPlaybackArea.SetActive(false);
        DebugLogger.Instance.Log("StartRecording in " + recordable.playbackObject.name);
        currentActiveRecordable = recordable;
        if (currentActiveRecordable.recordedData == null)
        {
            currentActiveRecordable.recordedData = new List<RecordFrameData>(mainRecorder.GetSizeOfMainRecordedData());
        }
        CopyTimeStampsFromMainRecorder(currentActiveRecordable);
        //recordStartTime = mainRecorder.re
        //currentActiveRecordable.
        //currentActiveRecordable.ResetData();        
        isRecording = true;
        isPlayingBack = false;
        //recordStartTime = mainRecorder.playbackSlider.value;
    }

    private void CopyTimeStampsFromMainRecorder(Recordable recordable)
    {
        foreach(var data in mainRecorder.objectsToRecord[0].recordedData)
        {
            recordable.recordedData.Add(new RecordFrameData(recordable.playbackObject.transform.localPosition, recordable.playbackObject.transform.localRotation, true, data.timestamp));
        }
    }

    public void StopRecording(Recordable recordable)
    {
        DebugLogger.Instance.Log("StopRecording in " + recordable.playbackObject.name);
        isRecording = false;
        isPlayingBack = true;
        //mainRecorder.playbackSlider.value = 0;

    }

    public void ResetRecording(Recordable recordable)
    {
        DebugLogger.Instance.Log("ResetRecording in " + recordable.playbackObject.name);
        isRecording = false;
        isPlayingBack = false;
        recordable.ResetData();
        //mainRecorder.playbackSlider.value = 0;
    }
   
   public float distanceFromHand = 0.0001f;
   public float playbackSpeed = 0.003f;

   public void SetPlaybackSpeed(float speed)
    {
         playbackSpeed = speed;
    }

    public void SetDistanceFromHand(float distance)
    {
        distanceFromHand = distance;
    }

    private void Update()
    {
        if (isRecording)
        {
            //DebugLogger.Instance.Log("Current Position: " + currentActiveRecordable.playbackObject.transform.localPosition + " Last Recorded Position: " + lastLocalPosition);
            //Check if there is a difference between the current position and the last recorded position and if the difference is greater than 0.01 then record the current position
            //DebugLogger.Instance.Log("Distance: " + Vector3.Distance(currentActiveRecordable.playbackObject.transform.localPosition, lastLocalPosition));
            if (Vector3.Distance(currentActiveRecordable.playbackObject.transform.localPosition, lastLocalPosition) > distanceFromHand)            
            {
                
                //print into debugger.instance.log both the current position and the last recorded position
                
                DebugLogger.Instance.Log("Added new frame data for " + currentActiveRecordable.playbackObject.name + " at " + mainRecorder.playbackSlider.value);
                //recordable.Record(Time.time - recordStartTime);
                //currentActiveRecordable.Record(mainRecorder.playbackSlider.value);
                currentActiveRecordable.InsertAssetRecordFrame(mainRecorder.playbackSlider.value);
                //Increment the slider value by a small value proportional to the total recording time
                mainRecorder.playbackSlider.value += playbackSpeed;// / (mainRecorder.recordingDuration * 100);
                
            }
            lastLocalPosition = currentActiveRecordable.playbackObject.transform.localPosition;

            //recordable.Record(Time.time - recordStartTime);
        }
        else if (isPlayingBack)
        {
            //float currentTime = Time.time - recordStartTime;
            float currentTime = mainRecorder.playbackSlider.value;
            
            foreach (var recordable in recordableAssets)
            {
                //DebugLogger.Instance.Log("Playing back " + recordable.playbackObject.name + " at " + currentTime);
                // Find the two frames to interpolate between.
                RecordFrameData previousFrame = null;
                RecordFrameData nextFrame = null;
                foreach (var data in recordable.recordedData)
                {
                    if (data.timestamp <= currentTime)
                    {
                        previousFrame = data;
                    }
                    else
                    {
                        nextFrame = data;
                        break;
                    }
                }

                if (recordable.playbackObject != null)
                {
                    if (previousFrame != null && nextFrame != null)
                    {
                        // Interpolate between the two frames.
                        float t = (currentTime - previousFrame.timestamp) / (nextFrame.timestamp - previousFrame.timestamp);
                        recordable.playbackObject.transform.localPosition = Vector3.Lerp(previousFrame.rootPosition, nextFrame.rootPosition, t);
                        recordable.playbackObject.transform.localRotation = Quaternion.Lerp(previousFrame.rootRotation, nextFrame.rootRotation, t) * Quaternion.Euler(recordable.rotationCorrection);//Modify the rotation in the recorded data to add 180 degrees to the  axis
                        if (previousFrame.showStatusForThisFrame)
                        {
                            DebugLogger.Instance.Log("Playing back show for " + recordable.playbackObject.name);
                            recordable.playbackObject.GetComponent<MeshRenderer>().material = defaultMaterial;
                        }
                        else
                        {
                            DebugLogger.Instance.Log("Playing back hide for " + recordable.playbackObject.name);
                            recordable.playbackObject.GetComponent<MeshRenderer>().material = transparentMaterial;
                        } 

                        /*if(nextFrame.force != Vector3.zero)
                        {
                            DebugLogger.Instance.Log("Playing back force " + nextFrame.force + " for " + recordable.playbackObject.name);
                            ApplyForce(recordable, nextFrame.force);
                        }*/
                    }
                    else if (previousFrame != null)
                    {
                        // If there's no next frame, use the data from the previous frame.
                        recordable.playbackObject.transform.localPosition = previousFrame.rootPosition;
                        recordable.playbackObject.transform.localRotation = previousFrame.rootRotation;
                    }
                }
            }
        }
    }

    public void EnableGrabForAllAssets()
    {
        DebugLogger.Instance.Log("Enabling grab for all assets");
        foreach (var recordable in recordableAssets)
        {
            DebugLogger.Instance.Log("Enabling grab for " + recordable.playbackObject.name);
            recordable.grabCollider.enabled = true;
        }
    }

    public void DisableGrabForAllAssets()
    {
        DebugLogger.Instance.Log("Disabling grab for all assets");
        foreach (var recordable in recordableAssets)
        {
            DebugLogger.Instance.Log("Disabling grab for " + recordable.playbackObject.name);
            recordable.grabCollider.enabled = false;
        }
    }

    public void SpawnSphere()
    {
        DebugLogger.Instance.LogInVR("Spawned Sphere");
        GameObject obj = Instantiate(spherePrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
        obj.SetActive(true);
        recordableAssets.Add(obj.GetComponentInChildren<Recordable>());
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }

    public void SpawnSphere(Transform target)
    {
        DebugLogger.Instance.LogInVR("Spawned Sphere");
        GameObject obj = Instantiate(spherePrefab, target.position, Quaternion.identity);
        obj.SetActive(true);
        recordableAssets.Add(obj.GetComponentInChildren<Recordable>());
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }

    public void SpawnCube()
    {
        DebugLogger.Instance.LogInVR("Spawned Cube");
        GameObject obj = Instantiate(cubePrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
        obj.SetActive(true);
        recordableAssets.Add(obj.GetComponentInChildren<Recordable>());
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }

    public void SpawnCube(Transform target)
    {
        DebugLogger.Instance.LogInVR("Spawned Cube");
        GameObject obj = Instantiate(cubePrefab, target.position, Quaternion.identity);
        obj.SetActive(true);
        recordableAssets.Add(obj.GetComponentInChildren<Recordable>());
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }

    public void SpawnText()
    {
        DebugLogger.Instance.LogInVR("Spawned Text");
        GameObject obj = Instantiate(textAsset, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
        obj.SetActive(true);
        recordableAssets.Add(obj.GetComponentInChildren<Recordable>());
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }

    public void SpawnText(Transform target)
    {
        DebugLogger.Instance.LogInVR("Spawned Text");
        GameObject obj = Instantiate(textAsset, target.position, Quaternion.identity);
        obj.SetActive(true);
        recordableAssets.Add(obj.GetComponentInChildren<Recordable>());
        //obj.GetComponent<Rigidbody>().AddForce(hmd.transform.forward * 1000);
    }

    public void AddForceArrowToAsset(Recordable recordable)
    {
        //GameObject obj = Instantiate(cubePrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
        GameObject forceArrow = Instantiate(forceArrowPrefab, hmd.transform.position + hmd.transform.forward * 0.5f, Quaternion.identity);
        forceArrow.SetActive(true);
        ForceArrow forceArrowScript = forceArrow.GetComponent<ForceArrow>();
        forceArrowScript.asset = recordable.playbackObject.transform;
        //forceArrowScript.arrowHead should be positioned 1 unit above the arrowEnd in the y axis
        forceArrowScript.arrowHead.position = recordable.playbackObject.transform.position + new Vector3(0,0.1f,0);
        
    }
   
}
