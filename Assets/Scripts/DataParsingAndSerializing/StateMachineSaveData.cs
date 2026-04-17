using System;
using System.Collections.Generic;

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
    public int startIndex;
    public int length;
    public string actionType;
    public string targetAssetId;

    // Optional: if you later want to reconstruct links between start/end actions
    public string associatedEndActionId;
}

public enum SequenceKind
{
    Gesture,
    Voice,
    Collision
}