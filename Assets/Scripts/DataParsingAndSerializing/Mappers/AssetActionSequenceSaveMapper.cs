using System;
using System.Collections.Generic;
using Action = System.Action;
using UnityEngine;
public static class AssetActionSequenceSaveMapper
{
    public static AssetActionSequenceSaveData ToSaveData(AssetActionSequence sequence, ISceneReferenceResolver resolver)
    {
        if (sequence == null)
            return null;

        return new AssetActionSequenceSaveData
        {
            id = sequence.Id,
            startIndex = sequence.StartIndex,
            length = sequence.Length,
            actionType = sequence.ActionType.ToString(),
            targetAssetId = resolver.GetId(sequence.TargetAsset),
            associatedEndActionId = sequence.associatedEndAction != null ? sequence.associatedEndAction.Id : null,
            color = sequence.StoredColor.HasValue ? new SerializableColor(sequence.StoredColor.Value) : null,
            pinPosition = sequence.StoredPinPosition.HasValue ? new SerializableVector3(sequence.StoredPinPosition.Value) : null,
            initialVelocity = sequence.StoredInitialVelocity.HasValue ? new SerializableVector3(sequence.StoredInitialVelocity.Value) : null
        };
    }

    public static List<AssetActionSequenceSaveData> ToSaveDataList(List<AssetActionSequence> sequences, ISceneReferenceResolver resolver)
    {
        List<AssetActionSequenceSaveData> result = new();

        if (sequences == null)
            return result;

        foreach (var sequence in sequences)
        {
            var data = ToSaveData(sequence, resolver);
            if (data != null)
            {
                result.Add(data);
            }
        }

        return result;
    }
}

public static class AssetActionSequenceLoadMapper
{
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
            Id = saveData.id,
            StartIndex = saveData.startIndex,
            Length = saveData.length,
            ActionType = actionType,
            TargetAsset = targetAsset,
            StoredColor = saveData.color != null ? saveData.color.ToColor() : null,
            StoredPinPosition = saveData.pinPosition != null ? saveData.pinPosition.ToVector3() : null,
            StoredInitialVelocity = saveData.initialVelocity != null ? saveData.initialVelocity.ToVector3() : null
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
            var sequence = FromSaveData(saveData, resolver);
            if (sequence != null)
            {
                result.Add(sequence);
            }
        }

        // Reconnect associatedEndAction references after all sequences exist
        Dictionary<string, AssetActionSequence> byId = new();
        foreach (var seq in result)
        {
            byId[seq.Id] = seq;
        }

        for (int i = 0; i < saveDataList.Count && i < result.Count; i++)
        {
            string endId = saveDataList[i].associatedEndActionId;
            if (!string.IsNullOrWhiteSpace(endId) && byId.TryGetValue(endId, out var endAction))
            {
                result[i].associatedEndAction = endAction;
            }
        }

        return result;
    }

    private static Action BuildActionDelegate(AssetActionSequence sequence)
    {
        Asset associatedAsset = sequence.TargetAsset;

        switch (sequence.ActionType)
        {
            case ACTION_ENUM.HIDE:
                return () =>
                {
                    associatedAsset.IsVisible = false;
                };

            case ACTION_ENUM.SHOW:
                return () =>
                {
                    associatedAsset.IsVisible = true;
                };

            case ACTION_ENUM.ANIMATE:
                return () =>
                {
                    associatedAsset.StartAnimation();
                    associatedAsset._IsAnimated = true;
                };

            case ACTION_ENUM.CHANGE_COLOR:
                return () =>
                {
                    if (!sequence.StoredColor.HasValue)
                    {
                        DebugLogger.Instance.Log("CHANGE_COLOR missing StoredColor for asset " + associatedAsset.name);
                        return;
                    }

                    associatedAsset.CurrentColor = sequence.StoredColor.Value;
                };

            case ACTION_ENUM.PIN:
                return () =>
                {
                    if (!sequence.StoredPinPosition.HasValue)
                    {
                        DebugLogger.Instance.Log("PIN missing StoredPinPosition for asset " + associatedAsset.name);
                        return;
                    }

                    associatedAsset.Pin(sequence.StoredPinPosition.Value);
                };

            case ACTION_ENUM.APPLY_FORCE_START:
                return () =>
                {
                    if (!sequence.StoredInitialVelocity.HasValue)
                    {
                        DebugLogger.Instance.Log("APPLY_FORCE_START missing StoredInitialVelocity for asset " + associatedAsset.name);
                        return;
                    }

                    associatedAsset.ApplyForce(sequence.StoredInitialVelocity.Value);
                };

            case ACTION_ENUM.FOLLOW_LEFT_HAND:
            case ACTION_ENUM.FOLLOW_RIGHT_HAND:
            case ACTION_ENUM.FOLLOW_L_FOCUS:
            case ACTION_ENUM.FOLLOW_R_FOCUS:
            case ACTION_ENUM.FOLLOW_G_FOCUS:
                return BuildFollowDelegate(sequence.ActionType, associatedAsset);

            case ACTION_ENUM.FOLLOW_END:
                return () =>
                {
                    DebugLogger.Instance.Log("FOLLOW_END loaded, but no stop-follow behavior was provided.");
                };

            case ACTION_ENUM.APPLY_FORCE_END:
                return () =>
                {
                    DebugLogger.Instance.Log("APPLY_FORCE_END loaded, but no execution behavior was provided.");
                };

            case ACTION_ENUM.STOP_ANIMATE:
                return () =>
                {
                    DebugLogger.Instance.Log("STOP_ANIMATE loaded, but no stop animation behavior was provided.");
                };

            default:
                return () =>
                {
                    DebugLogger.Instance.Log("Unsupported action type during delegate reconstruction: " + sequence.ActionType);
                };
        }
    }

    private static Action BuildFollowDelegate(ACTION_ENUM actionType, Asset associatedAsset)
    {
        return () =>
        {
            bool isLive = Manager.Instance != null && Manager.Instance.currAppState == Manager.AppState.LIVE;
            GameObject target = ResolveFollowTarget(actionType, isLive);

            if (target != null)
            {
                associatedAsset.ApplyFollow(target.transform);
            }
            else
            {
                DebugLogger.Instance.Log("Follow target could not be resolved for action " + actionType);
            }
        };
    }

    private static GameObject ResolveFollowTarget(ACTION_ENUM actionType, bool isLive)
    {
        switch (actionType)
        {
            case ACTION_ENUM.FOLLOW_LEFT_HAND:
                return isLive ? InputManager.Instance.leftHandPinchObj : InputManager.Instance.playbackLeftHandPinchObj;

            case ACTION_ENUM.FOLLOW_RIGHT_HAND:
                return isLive ? InputManager.Instance.rightHandPinchObj : InputManager.Instance.playbackRightHandPinchObj;

            case ACTION_ENUM.FOLLOW_L_FOCUS:
                return isLive ? InputManager.Instance.leftFocus : InputManager.Instance.playbackLeftFocus;

            case ACTION_ENUM.FOLLOW_R_FOCUS:
                return isLive ? InputManager.Instance.rightFocus : InputManager.Instance.playbackRightFocus;

            case ACTION_ENUM.FOLLOW_G_FOCUS:
                return isLive ? InputManager.Instance.gazeFocus : InputManager.Instance.playbackGazeFocus;

            default:
                return null;
        }
    }
}