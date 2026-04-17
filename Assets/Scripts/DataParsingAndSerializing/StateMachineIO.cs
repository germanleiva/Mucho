using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class StateMachineFileIO
{
    private const string FileName = "state_machine.json";

    private static string GetStreamingAssetsStateMachinePath()
    {
        return Path.Combine(Application.streamingAssetsPath, FileName);
    }

    public static void SaveToFile(StateMachineModel model, string filePath)
    {
        if (model == null)
        {
            DebugLogger.Instance.Log("SaveToFile failed: model is null");
            return;
        }

        string directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        ISceneReferenceResolver resolver = new SceneReferenceResolver();
        StateMachineSaveData saveData = StateMachineSaveMapper.ToSaveData(model, resolver);

        string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);

        // Creates the file if it does not exist, overwrites it if it does.
        File.WriteAllText(filePath, json);

        DebugLogger.Instance.Log("Saved state machine to " + filePath);
    }

    public static StateMachineModel LoadFromFile(string filePath, Example example)
    {
        if (!File.Exists(filePath))
        {
            DebugLogger.Instance.Log("State machine file not found: " + filePath);
            return null;
        }

        string json = File.ReadAllText(filePath);
        StateMachineSaveData saveData = JsonConvert.DeserializeObject<StateMachineSaveData>(json);

        ISceneReferenceResolver resolver = new SceneReferenceResolver();
        StateMachineModel model = StateMachineLoadMapper.FromSaveData(saveData, example, resolver);

        DebugLogger.Instance.Log("Loaded state machine from " + filePath);
        return model;
    }

    public static void TestSave(StateMachineModel model)
    {
        string path = GetStreamingAssetsStateMachinePath();
        SaveToFile(model, path);
        DebugLogger.Instance.Log("TestSave completed. File written to " + path);
    }

    public static StateMachineModel TestLoad(Example example)
    {
        if (example == null)
        {
            DebugLogger.Instance.Log("TestLoad failed: example is null");
            return null;
        }

        string path = GetStreamingAssetsStateMachinePath();
        StateMachineModel loadedModel = LoadFromFile(path, example);

        if (loadedModel == null)
        {
            DebugLogger.Instance.Log("TestLoad failed: could not load model from " + path);
            return null;
        }

        DebugLogger.Instance.Log("TestLoad completed. File loaded from " + path);
        return loadedModel;
    }
}