using System;
using System.Collections.Generic;

public static class AssetActionSequenceSaveMapper
{
    public static AssetActionSequenceSaveData ToSaveData(AssetActionSequence sequence, ISceneReferenceResolver resolver)
    {
        if (sequence == null)
            return null;

        return new AssetActionSequenceSaveData
        {
            startIndex = sequence.StartIndex,
            length = sequence.Length,
            actionType = sequence.ActionType.ToString(),
            targetAssetId = resolver.GetId(sequence.TargetAsset),

            // optional placeholder for future use
            associatedEndActionId = null
        };
    }

    public static List<AssetActionSequenceSaveData> ToSaveDataList(List<AssetActionSequence> sequences, ISceneReferenceResolver resolver)
    {
        List<AssetActionSequenceSaveData> result = new();

        if (sequences == null)
            return result;

        foreach (var sequence in sequences)
        {
            AssetActionSequenceSaveData saveData = ToSaveData(sequence, resolver);
            if (saveData != null)
            {
                result.Add(saveData);
            }
        }

        return result;
    }

    public static AssetActionSequence FromSaveData(AssetActionSequenceSaveData saveData, ISceneReferenceResolver resolver)
    {
        if (saveData == null)
            return null;

        if (!Enum.TryParse(saveData.actionType, out ACTION_ENUM actionType))
        {
            DebugLogger.Instance.Log("Invalid ACTION_ENUM while loading: " + saveData.actionType);
            return null;
        }

        Asset targetAsset = resolver.ResolveAsset(saveData.targetAssetId);
        if (targetAsset == null)
        {
            DebugLogger.Instance.Log("Could not resolve target asset while loading action: " + saveData.targetAssetId);
            return null;
        }

        AssetActionSequence sequence = new AssetActionSequence
        {
            StartIndex = saveData.startIndex,
            Length = saveData.length,
            ActionType = actionType,
            TargetAsset = targetAsset
        };

        sequence.ActionDelegate = BuildActionDelegate(sequence);

        return sequence;
    }

    public static List<AssetActionSequence> FromSaveDataList(List<AssetActionSequenceSaveData> saveDataList, ISceneReferenceResolver resolver)
    {
        List<AssetActionSequence> result = new();

        if (saveDataList == null)
            return result;

        foreach (var saveData in saveDataList)
        {
            AssetActionSequence sequence = FromSaveData(saveData, resolver);
            if (sequence != null)
            {
                result.Add(sequence);
            }
        }

        return result;
    }

    private static Action BuildActionDelegate(AssetActionSequence sequence)
    {
        return () =>
        {
            DebugLogger.Instance.Log(
                "Rebuilt ActionDelegate invoked for action " +
                sequence.ActionType + " on asset " + sequence.TargetAsset.name);

            // Here you must call the real logic you already use in your project
            // for ACTION_ENUM on TargetAsset.
        };
    }
}