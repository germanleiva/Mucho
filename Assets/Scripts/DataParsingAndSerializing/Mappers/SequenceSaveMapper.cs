using System;
using System.Collections.Generic;
using UnityEngine;

public static class SequenceSaveMapper
{
    public static SequenceSaveData ToSaveData(Sequence sequence, ISceneReferenceResolver resolver)
    {
        if (sequence is GestureSequence gesture)
        {
            return new SequenceSaveData
            {
                kind = SequenceKind.Gesture,
                startIndex = gesture.StartIndex,
                length = gesture.Length,
                gestureType = gesture.GestureType.ToString()
            };
        }

        if (sequence is VoiceSequence voice)
        {
            return new SequenceSaveData
            {
                kind = SequenceKind.Voice,
                startIndex = voice.StartIndex,
                length = voice.Length,
                voiceCommand = voice.VoiceCommand
            };
        }

        if (sequence is CollisionSequence collision)
        {
            return new SequenceSaveData
            {
                kind = SequenceKind.Collision,
                startIndex = collision.StartIndex,
                length = collision.Length,
                isActive = collision.IsActive,
                collidingObject1Id = resolver.GetId(collision.CollidingObject1),
                collidingObject2Id = resolver.GetId(collision.CollidingObject2)
            };
        }

        DebugLogger.Instance.Log("Unsupported Sequence subtype during serialization: " + sequence.GetType().Name);
        return null;
    }

    public static List<SequenceSaveData> ToSaveDataList(List<Sequence> sequences, ISceneReferenceResolver resolver)
    {
        List<SequenceSaveData> result = new();

        if (sequences == null)
            return result;

        foreach (var sequence in sequences)
        {
            SequenceSaveData saveData = ToSaveData(sequence, resolver);
            if (saveData != null)
            {
                result.Add(saveData);
            }
        }

        return result;
    }

    public static Sequence FromSaveData(SequenceSaveData saveData, ISceneReferenceResolver resolver)
    {
        switch (saveData.kind)
        {
            case SequenceKind.Gesture:
            {
                if (!Enum.TryParse(saveData.gestureType, out InputManager.Gesture gestureType))
                {
                    DebugLogger.Instance.Log("Invalid gesture type while loading: " + saveData.gestureType);
                    return null;
                }

                return new GestureSequence
                {
                    StartIndex = saveData.startIndex,
                    Length = saveData.length,
                    GestureType = gestureType
                };
            }

            case SequenceKind.Voice:
            {
                return new VoiceSequence
                {
                    StartIndex = saveData.startIndex,
                    Length = saveData.length,
                    VoiceCommand = saveData.voiceCommand
                };
            }

            case SequenceKind.Collision:
            {
                GameObject object1 = resolver.ResolveGameObject(saveData.collidingObject1Id);
                GameObject object2 = resolver.ResolveGameObject(saveData.collidingObject2Id);

                if (object1 == null || object2 == null)
                {
                    DebugLogger.Instance.Log(
                        "Could not resolve collision objects while loading. object1Id=" +
                        saveData.collidingObject1Id + ", object2Id=" + saveData.collidingObject2Id);
                    return null;
                }

                CollisionSequence collision = new CollisionSequence
                {
                    StartIndex = saveData.startIndex,
                    Length = saveData.length,
                    IsActive = saveData.isActive,
                    CollidingObject1 = object1,
                    CollidingObject2 = object2
                };

                collision.collisionDelegate = frame => frame.IsColliding(collision.CollidingObject1, collision.CollidingObject2);

                return collision;
            }

            default:
                DebugLogger.Instance.Log("Unsupported SequenceKind while loading: " + saveData.kind);
                return null;
        }
    }

    public static List<Sequence> FromSaveDataList(List<SequenceSaveData> saveDataList, ISceneReferenceResolver resolver)
    {
        List<Sequence> result = new();

        if (saveDataList == null)
            return result;

        foreach (var saveData in saveDataList)
        {
            Sequence sequence = FromSaveData(saveData, resolver);
            if (sequence != null)
            {
                result.Add(sequence);
            }
        }

        return result;
    }

    public static Func<Frame, bool> BuildConditionFromTriggers(List<Sequence> triggers)
    {
        Func<Frame, bool> condition = frame => true;

        if (triggers == null)
            return condition;

        foreach (var trigger in triggers)
        {
            condition = trigger.AddConditionToFunction(condition);
        }

        return condition;
    }
}