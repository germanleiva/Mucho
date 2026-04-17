using System;
using System.Collections.Generic;

public static class StateMachineLoadMapper
{
    public static StateMachineModel FromSaveData(StateMachineSaveData saveData, Example example, ISceneReferenceResolver resolver)
    {
        StateMachineModel model = new StateMachineModel(example);
        Dictionary<string, State> statesById = new();

        foreach (var stateSaveData in saveData.states)
        {
            State state = new State(stateSaveData.name, model, stateSaveData.id);

            state.OnEnterActionsSequences = AssetActionSequenceSaveMapper.FromSaveDataList(stateSaveData.onEnterActions, resolver);
            state.OnUpdateActionsSequences = AssetActionSequenceSaveMapper.FromSaveDataList(stateSaveData.onUpdateActions, resolver);
            state.OnExitActionsSequences = AssetActionSequenceSaveMapper.FromSaveDataList(stateSaveData.onExitActions, resolver);

            model.AddState(state);
            statesById[state._id] = state;
        }

        foreach (var transitionSaveData in saveData.transitions)
        {
            if (!statesById.TryGetValue(transitionSaveData.fromStateId, out var fromState))
            {
                DebugLogger.Instance.Log("Missing fromStateId while loading: " + transitionSaveData.fromStateId);
                continue;
            }

            if (!statesById.TryGetValue(transitionSaveData.toStateId, out var toState))
            {
                DebugLogger.Instance.Log("Missing toStateId while loading: " + transitionSaveData.toStateId);
                continue;
            }

            List<Sequence> triggers = SequenceSaveMapper.FromSaveDataList(transitionSaveData.triggers, resolver);
            Func<Frame, bool> condition = SequenceSaveMapper.BuildConditionFromTriggers(triggers);

            fromState.AddTransitionTo(
                toState,
                condition,
                transitionSaveData.textDescription,
                triggers
            );
        }

        if (!string.IsNullOrWhiteSpace(saveData.initialStateId) &&
            statesById.TryGetValue(saveData.initialStateId, out var initialState))
        {
            model.SetInitialState(initialState);
        }
        else if (model.states.Count > 0)
        {
            model.SetInitialState(model.states[0]);
        }

        return model;
    }
}