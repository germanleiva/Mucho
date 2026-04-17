using System.IO;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;

public static class StateMachineFileIO
{
    public static void SaveToFile(StateMachineModel model, string filePath)
    {
        ISceneReferenceResolver resolver = new SceneReferenceResolver();
        StateMachineSaveData saveData = StateMachineSaveMapper.ToSaveData(model, resolver);

        string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
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
}