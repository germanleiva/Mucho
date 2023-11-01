using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TextTo3D : MonoBehaviour
{
    // Your API key from Meshy
    private string apiKey = "YOUR_API_KEY";

    // The base URL of the Text To 3D API
    private string baseUrl = "https://api.meshy.ai/v1/text-to-3d";

    // The object prompt for the Text To 3D task
    private string objectPrompt = "a monster mask";

    // The style prompt for the Text To 3D task
    private string stylePrompt = "red fangs, Samurai outfit that fused with japanese batik style";

    // The negative prompt for the Text To 3D task
    private string negativePrompt = "low quality, low resolution, low poly, ugly";

    // The enable PBR option for the Text To 3D task
    private bool enablePBR = true;

    // The art style option for the Text To 3D task
    private string artStyle = "generic";

    // The task id of the created Text To 3D task
    private string taskId;

    // The status of the created Text To 3D task
    private string taskStatus;

    // The model URL of the created Text To 3D task
    private string modelUrl;

    // The texture URLs of the created Text To 3D task
    private List<string> textureUrls;

    // Start is called before the first frame update
    void Start()
    {
        // Create a new Text To 3D task with the given prompts and options
        StartCoroutine(CreateTextTo3DTask());
    }

    // Update is called once per frame
    void Update()
    {
        // If the task id is not null, check the status of the task every second
        if (taskId != null)
        {
            StartCoroutine(CheckTextTo3DTaskStatus());
        }
        
        // If the task status is SUCCEEDED, download and display the model and textures
        if (taskStatus == "SUCCEEDED")
        {
            StartCoroutine(DownloadAndDisplayModel());
        }
        
        // If the task status is FAILED or EXPIRED, show an error message
        if (taskStatus == "FAILED" || taskStatus == "EXPIRED")
        {
            Debug.LogError("The Text To 3D task failed or expired.");
        }
    }

    // A coroutine that creates a new Text To 3D task with the given prompts and options
    IEnumerator CreateTextTo3DTask()
    {
        // Create a JSON object with the parameters for the Text To 3D task
        var json = new {
            object_prompt = objectPrompt,
            style_prompt = stylePrompt,
            negative_prompt = negativePrompt,
            enable_pbr = enablePBR,
            art_style = artStyle,
        };

        var jsonStr = JsonUtility.ToJson(json);

        // Create a POST request to the Text To 3D API endpoint with the JSON object and the API key as headers
        var request = new UnityWebRequest(baseUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonStr));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        // Send the request and wait for a response or an error
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<Response>(request.downloadHandler.text);
            taskId = response.result.id;
            Debug.Log("Created a new Text To 3D task with id: " + taskId);
        }
        else
        {
            Debug.LogError("Error creating a new Text To 3D task: " + request.error);
        }
        
    }

   [System.Serializable]
   public class Response 
   {
       public Result result;
   }

   [System.Serializable]
   public class Result 
   {
       public string id;
   }

   [System.Serializable]
   public class TaskStatusResponse 
   {
       public string status;
       public string model_url;
       public List<TextureUrls> texture_urls;
   }

   [System.Serializable]
   public class TextureUrls 
   {
       public string base_color;
       public string metallic;
       public string normal;
       public string roughness;
   }

    // A coroutine that checks the status of a Text To 3D task given a valid task id 
    IEnumerator CheckTextTo3DTaskStatus()
    {
        // Create a GET request to the Text To 3D API endpoint with the task id and the API key as headers
        var request = new UnityWebRequest(baseUrl + "/" + taskId, "GET");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        // Send the request and wait for a response or an error
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = JsonUtility.FromJson<TaskStatusResponse>(request.downloadHandler.text);
            taskStatus = response.status;

            Debug.Log("The Text To 3D task status is: " + taskStatus);

            // If the task status is SUCCEEDED, get the model URL and the texture URLs from the response
            if (taskStatus == "SUCCEEDED")
            {
                modelUrl = response.model_url;
                textureUrls = new List<string>();
                foreach (var textureUrlObject in response.texture_urls)
                {
                    textureUrls.Add(textureUrlObject.base_color);
                    textureUrls.Add(textureUrlObject.metallic);
                    textureUrls.Add(textureUrlObject.normal);
                    textureUrls.Add(textureUrlObject.roughness);
                }
            }
        }
        else
        {
            Debug.LogError("Error checking the Text To 3D task status: " + request.error);
        }
    }

    // A coroutine that downloads and displays the model and textures from the Text To 3D task
    IEnumerator DownloadAndDisplayModel()
    {
        // Create a GET request to the model URL
        var modelRequest = UnityWebRequestAssetBundle.GetAssetBundle(modelUrl);

        // Send the request and wait for a response or an error
        yield return modelRequest.SendWebRequest();

        if (modelRequest.result == UnityWebRequest.Result.Success)
        {
            // Load the asset bundle from the response and get the first asset as a GameObject
            var assetBundle = DownloadHandlerAssetBundle.GetContent(modelRequest);
            var assetName = assetBundle.GetAllAssetNames()[0];
            var modelObject = assetBundle.LoadAsset<GameObject>(assetName);

            Debug.Log("Downloaded the model from the Text To 3D task.");

            // Instantiate the model object in the scene and get its mesh renderer component
            var modelInstance = Instantiate(modelObject);
            var meshRenderer = modelInstance.GetComponent<MeshRenderer>();

            // Create a list of materials for the model object
            var materials = new List<Material>();

            // For each texture URL in the texture URLs list, create a GET request to the texture URL
            foreach (var textureUrl in textureUrls)
            {
                var textureRequest = UnityWebRequestTexture.GetTexture(textureUrl);

                // Send the request and wait for a response or an error
                yield return textureRequest.SendWebRequest();

                if (textureRequest.result == UnityWebRequest.Result.Success)
                {
                    // Load the texture from the response and create a new material with it
                    var texture = DownloadHandlerTexture.GetContent(textureRequest);
                    var material = new Material(Shader.Find("Standard"));
                    material.mainTexture = texture;

                    Debug.Log("Downloaded a texture from the Text To 3D task.");

                    // Add the material to the list of materials
                    materials.Add(material);
                }
                else
                {
                    Debug.LogError("Error downloading a texture from the Text To 3D task: " + textureRequest.error);
                }
            }

            // Assign the list of materials to the mesh renderer component of the model object
            meshRenderer.materials = materials.ToArray();
        }
        else
        {
            Debug.LogError("Error downloading the model from the Text To 3D task: " + modelRequest.error);
        }
    }
}
