using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StateMachineSaveData
{
    public string initialStateId;
    public List<StateSaveData> states = new();
    public List<TransitionSaveData> transitions = new();
}

[Serializable]
public class StateSaveData
{
    public string id;
    public string name;

    public List<AssetActionSequenceSaveData> onEnterActions = new();
    public List<AssetActionSequenceSaveData> onUpdateActions = new();
    public List<AssetActionSequenceSaveData> onExitActions = new();
}

[Serializable]
public class TransitionSaveData
{
    public string fromStateId;
    public string toStateId;
    public string textDescription;
    public List<SequenceSaveData> triggers = new();
}

[Serializable]
public class SequenceSaveData
{
    public SequenceKind kind;

    public int startIndex;
    public int length;

    // GestureSequence
    public string gestureType;

    // VoiceSequence
    public string voiceCommand;

    // CollisionSequence
    public bool isActive;
    public string collidingObject1Id;
    public string collidingObject2Id;
}

[Serializable]
public class AssetActionSequenceSaveData
{
    public string id;

    public int startIndex;
    public int length;

    public string actionType;
    public string targetAssetId;

    public string associatedEndActionId;

    // For CHANGE_COLOR
    public SerializableColor color;

    // For PIN
    public SerializableVector3 pinPosition;

    // For APPLY_FORCE_START
    public SerializableVector3 initialVelocity;
}

[Serializable]
public class SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3() {}

    public SerializableVector3(Vector3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[Serializable]
public class SerializableColor
{
    public float r;
    public float g;
    public float b;
    public float a;

    public SerializableColor() {}

    public SerializableColor(Color c)
    {
        r = c.r;
        g = c.g;
        b = c.b;
        a = c.a;
    }

    public Color ToColor()
    {
        return new Color(r, g, b, a);
    }
}

public enum SequenceKind
{
    Gesture,
    Voice,
    Collision
}