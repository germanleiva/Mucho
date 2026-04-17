using System.Collections.Generic;
using System.Linq;

public static class StateMachineSaveMapper
{
    public static StateMachineSaveData ToSaveData(StateMachineModel model, ISceneReferenceResolver resolver)
    {
        StateMachineSaveData saveData = new StateMachineSaveData
        {
            initialStateId = model.InitialStateId
        };

        foreach (var state in model.states)
        {
            StateSaveData stateSaveData = new StateSaveData
            {
                id = state._id,
                name = state.name,
                onEnterActions = AssetActionSequenceSaveMapper.ToSaveDataList(state.OnEnterActionsSequences, resolver),
                onUpdateActions = AssetActionSequenceSaveMapper.ToSaveDataList(state.OnUpdateActionsSequences, resolver),
                onExitActions = AssetActionSequenceSaveMapper.ToSaveDataList(state.OnExitActionsSequences, resolver)
            };

            saveData.states.Add(stateSaveData);

            foreach (var transition in state.transitions)
            {
                TransitionSaveData transitionSaveData = new TransitionSaveData
                {
                    fromStateId = transition.from._id,
                    toStateId = transition.to._id,
                    textDescription = transition.textDescription,
                    triggers = SequenceSaveMapper.ToSaveDataList(transition.triggers, resolver)
                };

                saveData.transitions.Add(transitionSaveData);
            }
        }

        return saveData;
    }
}