using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;

public class CustomStateMachine : MonoBehaviour
{
    public static CustomStateMachine Instance { get; private set; }

    public StateMachineModel stateMachineModel;
    
    public TMPro.TMP_Text currentActiveStateText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PrintDetailsOfStateMachine(bool VRConsoleEnabled = false)
    {
        DebugLogger.Instance.Log("Printing details of state machine", VRConsoleEnabled);
        foreach (var state in stateMachineModel.states)
        {
            state.PrintDetailsOfState(VRConsoleEnabled);
        }
        
        DebugLogger.Instance.Log("END DETAILS", VRConsoleEnabled);

    }


    public void CreateStateGraph(GameObject stateElementPrefab, RectTransform parentTransform)
    {
        StateGraphUI.ResetStateGraph();
        
        for(int i = 2;i < parentTransform.childCount; i++)
        {
            Destroy(parentTransform.GetChild(i).gameObject);
        }
        
        foreach (var state in stateMachineModel.states)
        {
            state.stateGraphElement = StateGraphUI.CreateStateGraphElement(stateElementPrefab, parentTransform, state);
        }
        //StateGraphUI.CreateStateGraphElement(stateElementPrefab, parentTransform, initialState);
    }

    public void ProcessFrame(Frame lastFrameObject)
    {
        //DebugLogger.Instance.Log("ProcessFrame in the StateMachine");
        currentActiveStateText.text = "Current state: " + stateMachineModel.currentState.name;

        stateMachineModel.ProcessFrame(lastFrameObject);
    }
}

public class StateMachineModel
{
    public Example myExample;
    public static StateMachineModel Instance { get; set; }

    public string InitialStateId { get; private set; } // todo 160426 DTO

    private State _currentState;

    public State currentState
    {
        get => _currentState;
        set
        {
            //Notify the UI so we highlight the corresponding StateTimelineUIElement

            Recorder.Instance.HighlightStateTimelineUIElement(value._id);

            _currentState = value;
        }
    }

    public List<State> states = new();

    public StateMachineModel(Example example)
    {
        if (example == null)
        {
            throw new NotImplementedException();
        }
        myExample = example;
    }

    public void AddState(State state)
    {
        states.Add(state);
    }

    public List<Transition> transitionsTo(State state)
    {
        var allTransitions = new List<Transition>();
        foreach (var myState in states)
        {
            allTransitions.AddRange(myState.transitions);
        }
        
        return allTransitions.FindAll(x => x.to == state);
    }

    public void SetInitialState(State state)
    {
        DebugLogger.Instance.Log("Setting initial state to " + state.name,true);
        currentState = state;
        // foreach (var state in states)
        // {
        //     state.Value.ResetStateUIColor();
        // }
        InitialStateId = state._id; // todo 160426 DTO
    }
    
    // // todo 160426 DTO
    // public void ResetToInitialState()
    // {
    //     var initial = states.Find(s => s._id == InitialStateId);
    //     if (initial != null)
    //     {
    //         currentState = initial;
    //     }
    // }
    // Unchanged

    public void InvokeOnEnterActionsOfInitialState()
    {
        DebugLogger.Instance.Log("Invoking OnEnter actions of initial state " + currentState.name,true);
        currentState.OnEnter();
    }
    public void ProcessFrame(Frame lastFrameObject)
    {             
        // Debug.Log("**** Entering Process Frame");
        // Debug.Log("---------------------------");
        // Debug.Log("Analyzing frame: ");
        // Debug.Log("Frame leftHandGesture: " +  lastFrameObject.leftHandGesture);
        // Debug.Log("Frame rightHandGesture: " + lastFrameObject.rightHandGesture);
        // Debug.Log("Frame collidingObjectThisFrame_1: "  + lastFrameObject.collidingObjectThisFrame_1);
        // Debug.Log("Frame collidingObjectThisFrame_2: "  + lastFrameObject.collidingObjectThisFrame_2);
        // Debug.Log("Frame voiceCommand: " + lastFrameObject.voiceCommand);
        
        foreach (var transition in currentState.transitions)
        {
            if (transition.ShouldApply(lastFrameObject))
            {
                DebugLogger.Instance.Log("Transition applied from" + transition.from + " to " + transition.to);
                ApplyTransition(transition);
            }
            else
            {
                DebugLogger.Instance.Log("Transition NOT applied from " + transition.from + " to " + transition.to);
            }
        }

        // this.currentState.OnUpdate()
    }

    private void ApplyTransition(Transition transition)
    {
        //DebugLogger.Instance.Log("Applying transition from " + transition.from + " to " + transition.to, VRConsoleEnabled: true);
        DebugLogger.Instance.Log("Transitioning from " + transition.from + " to " + transition.to);
        this.currentState.OnExit();
        this.currentState = transition.to;
        this.currentState.OnEnter();
        //onupdate()?? 
    }

    public static void CombinedStateMachine(List<Example> examples)
    {
        void addMissingActionsToState(State aStateToDelete,State aStateToStay)
        {
            foreach (var onEnterActionSequence in aStateToDelete.OnEnterActionsSequences)
            {
                if (!aStateToStay.OnEnterActionsSequences.Any(x => x.IsEquivalentSequence(onEnterActionSequence)))
                {
                    aStateToStay.OnEnterActionsSequences.Add(onEnterActionSequence);
                }
            }

            foreach (var onExitActionSequence in aStateToDelete.OnExitActionsSequences)
            {
                if (!aStateToStay.OnExitActionsSequences.Any(x => x.IsEquivalentSequence(onExitActionSequence)))
                {
                    aStateToStay.OnExitActionsSequences.Add(onExitActionSequence);
                }
            }
        }
        List<StateMachineModel> stateMachines = new ();
        foreach (var example in examples)
        {
            stateMachines.Add(CreateStateMachine(example));
        }

        var resultingStateMachine = stateMachines.First();
        stateMachines.RemoveAt(0);
        
        //We need to analyze all the CustomStateMachines to merge the duplicated states
        foreach (var stateMachineToDelete in stateMachines)
        {
            for (int i = 0; i < stateMachineToDelete.states.Count; i++)
            {
                var currentStateToDelete = stateMachineToDelete.states.ElementAt(i);
         
                //Find if the state does not exist in the resultingStateMachine
                var equivalentState = resultingStateMachine.states
                    .Find(resultingState => resultingState.IsStateEquivalentTo(currentStateToDelete));
                if (equivalentState != null)
                {
                    //this state is represented in the resultingStateMachine
                    
                    //TODO Should we add the extra actions in this state if there are any?
                    //For every onEnterActionSequence in currentStateToDelete that is not in equivalentState we need to add it to equivalentState
                    addMissingActionsToState(currentStateToDelete,equivalentState);
                    
                    //We need to change the id of the corresponding StatePlaceHolder/StateUIElementSequence to be the id of this currentStateToDelete
                    //because we need to highlight thing in example 2-3-4... even if the stateplaceholder is in example 1

                    var statePlaceholderCorrespondingToTheCurrentStateToDelete =
                        stateMachineToDelete.myExample.StatePlaceholders.Find(statePlaceHolder =>
                            statePlaceHolder.stateModelId == currentStateToDelete._id);
                    statePlaceholderCorrespondingToTheCurrentStateToDelete.stateModelId = equivalentState._id;

                } else {
                    //This state is not equal to any state in the resultingStateMachine
                    //We need to add this state to the resultingStateMachine
                    if (i == 0)
                    {
                        //This is the first state. The only option is to merge both initial states
                        if (resultingStateMachine.states.Count > 0)
                        {
                            addMissingActionsToState(currentStateToDelete, resultingStateMachine.states.First());
                        }

                    } else 
                    {
                        var previousStateToDelete = stateMachineToDelete.states.ElementAt(i - 1);
                        var equivalentPreviousState = resultingStateMachine.states
                            .Find(x => x.IsStateEquivalentTo(previousStateToDelete));

                        if (equivalentPreviousState == null)
                        {
                            throw new Exception("This should not happen");
                        }
                        
                        
                        /*
                        equivalentPreviousState;
                        previousState;
                        */

                        var equivalentCurrentState = new State(currentStateToDelete.name, resultingStateMachine, currentStateToDelete._id);
                        //Copy all onEnter/onUpdate/onExit/etc
                        equivalentCurrentState.OnEnterActionsSequences.AddRange(currentStateToDelete.OnEnterActionsSequences);
                        equivalentCurrentState.OnUpdateActionsSequences.AddRange(currentStateToDelete.OnUpdateActionsSequences);
                        equivalentCurrentState.OnExitActionsSequences.AddRange(currentStateToDelete.OnExitActionsSequences);
                        resultingStateMachine.AddState(equivalentCurrentState);
                        
                        //Could it be this a potentialTransitionToAdd is already in the resultingStateMachine?
                        //Technically no, because currentState is not on the resultingStateMachine so any transition to currentState should not be in the resultingStateMachine
                        foreach (var transitionToAdd in currentStateToDelete.transitionsToMe())
                        {
                            
                            //This transition is not in the resultingStateMachine
                            //We need to add this transition to the resultingStateMachine
                            equivalentPreviousState.AddTransitionTo(equivalentCurrentState, transitionToAdd.condition, transitionToAdd.textDescription, transitionToAdd.triggers);
                        }
                        
                    }
                }
            }
        }

        CustomStateMachine.Instance.stateMachineModel = resultingStateMachine;
        StateMachineModel.Instance = resultingStateMachine;
    }
    
    public static StateMachineModel CreateStateMachine(Example example)
    {
        StateMachineModel stateMachine = new StateMachineModel(example);
        
        var localStatesDict = new Dictionary<StateTimelineUIElement,State>();

        foreach (var statePlaceholder in example.StatePlaceholders)
        {
            var newState = new State("State " + stateMachine.states.Count, stateMachine);
            stateMachine.AddState(newState);

            statePlaceholder.stateModelId = newState._id;
            localStatesDict.Add(statePlaceholder, newState);
        }

        var gestures = example.AllGestureSequences;
        var collisions = example.activeCollisionModels;
        var voiceCommands = example.VoiceCommandSequences;

        List<Sequence> allPotentialTriggers = gestures.Cast<Sequence>()
                                  .Concat(collisions.Cast<Sequence>())
                                  .Concat(voiceCommands.Cast<Sequence>())
                                  .ToList();

        // var allActions = example.assetsDict.Select(keyValuePair => keyValuePair.Value.assetActions).ToList();         foreach (Asset asset in Recorder.Instance.currentActiveExample.assetsDict.Keys)
        var allActions = example.GetAllAssetActionListsReadOnly();

        State firstState = null;

        for (int i = 0; i < example.StatePlaceholders.Count; i++) {
            var currentStatePlaceholder = example.StatePlaceholders[i];
            var nexStatePlaceholder = i < example.StatePlaceholders.Count - 1 ? example.StatePlaceholders[i + 1] : null;

            var currentState = localStatesDict[currentStatePlaceholder];
            if (i == 0) {
                firstState = currentState;
            }

            if (nexStatePlaceholder != null) {
                var nextState = localStatesDict[nexStatePlaceholder];

                //Find transitions between currentStatePlaceholder to nexStatePlaceholder
                var actualTriggers = allPotentialTriggers.FindAll(x => x.CanTriggerAt(nexStatePlaceholder.StartIndex));
                if (actualTriggers.Count > 0) {
                    //Let's build the transition
                    Func<Frame, bool> transitionConditionFunction = (Frame frame) => {return true;};
                    string transitionDescription = "" + currentState.name + "->" + nextState.name + ":";
                    foreach (var trigger in actualTriggers) {
                        transitionConditionFunction = trigger.AddConditionToFunction(transitionConditionFunction);
                        transitionDescription = transitionDescription + " && " + trigger.ToString();
                    }
                    //Let's add a transition between currentState and nextState
                    currentState.AddTransitionTo(nextState, transitionConditionFunction, transitionDescription, actualTriggers);
                }
            } else {
                //currentStatePlaceholder is the last statePlaceholder
            }

            DebugLogger.Instance.Log("Adding OnEnter and OnExit actions to state " + currentState.name);

            //var stateInTimeline = currentActiveExample.StatesDict.First().Key;
            foreach (var assetSequences in allActions)
            {
                foreach (var assetSequence in assetSequences)
                {
                    if (assetSequence.StartIndex >= currentStatePlaceholder.StartIndex &&
                        assetSequence.StartIndex < currentStatePlaceholder.StartIndex + currentStatePlaceholder.Length)
                    {
                        int distanceToStateStart =
                            Math.Abs(assetSequence.StartIndex - currentStatePlaceholder.StartIndex);
                        int distanceToStateEnd = Math.Abs(currentStatePlaceholder.StartIndex +
                            currentStatePlaceholder.Length - assetSequence.StartIndex - 1);

                        if (distanceToStateStart < distanceToStateEnd)
                        {
                            DebugLogger.Instance.Log("Adding action(s) " + assetSequence.ActionType + " for state " +
                                                     currentState.name + " in OnEnterActions");
                            currentState.OnEnterActionsSequences.Add(assetSequence);
                        }
                        else
                        {
                            DebugLogger.Instance.Log("Adding action(s) " + assetSequence.ActionType + " for state " +
                                                     currentState.name + " in OnExitActions");
                            currentState.OnExitActionsSequences.Add(assetSequence);
                        }
                    }
                }
            }
        }

        stateMachine.SetInitialState(firstState);

        Recorder.Instance.PrintDetailsOfStateMachine(localStatesDict.Values.ToList());    
        return stateMachine;
    }
}

[System.Serializable]
public class State
{
    public string _id { get; private set; }
    private string _name;
    public string name
    {
        get => _name;
    }


    public List<AssetActionSequence> OnEnterActionsSequences = new();
    public List<AssetActionSequence> OnUpdateActionsSequences = new();
    public List<AssetActionSequence> OnExitActionsSequences = new();

    public Action OnEnterActions
    {
        get
        {
            Action allOnEnterActions = null;
            foreach (var onEnterActionSequence in OnEnterActionsSequences)
            {
                allOnEnterActions += onEnterActionSequence.ActionDelegate;
            }

            return allOnEnterActions;
        }
    }

    public Action OnUpdateActions
    {
        get
        {
            Action allOUpdateActions = null;
            foreach (var onUpdateActionSequence in OnUpdateActionsSequences)
            {
                allOUpdateActions += onUpdateActionSequence.ActionDelegate;
            }

            return allOUpdateActions;
        }
    }
    public Action OnExitActions
    {
        get
        {
            Action allOnExitActions = null;
            foreach (var onExitActionSequence in OnExitActionsSequences)
            {
                allOnExitActions += onExitActionSequence.ActionDelegate;
            }

            return allOnExitActions;
        }
    }

    public string OnEnterActionsStr {
        get
        {
            return string.Join(", ", OnEnterActionsSequences.Select(x => x.ActionType.ToString()));        
        }
    }
    
    public string OnUpdateActionsStr {
        get
        {
            return string.Join(", ", OnUpdateActionsSequences.Select(x => x.ActionType.ToString()));        
        }
    }
    
    public string OnExitActionsStr {
        get
        {
            return string.Join(", ", OnExitActionsSequences.Select(x => x.ActionType.ToString()));        
        }
    }

    public List<Transition> transitions = new();

    [NonSerialized] public GameObject timelineElement;
    [NonSerialized] public GameObject stateGraphElement;

    Color originalColor = Color.white;
    
    private StateMachineModel _stateMachine;

    public State(String name, StateMachineModel stateMachine, String id = null)
    {
        _id = id == null ? System.Guid.NewGuid().ToString() : id;
        _name = name;
        _stateMachine = stateMachine;
    }

    public void ResetStateUIColor()
    {
        //timelineElement.GetComponent<UnityEngine.UI.Image>().color = originalColor;
        stateGraphElement.GetComponent<UnityEngine.UI.Image>().color = originalColor;
    }

    public void OnEnter()
    {
        DebugLogger.Instance.Log("OnEnter: " + name + ", OnEnterActions: " + String.Join(", ", OnEnterActionsStr));

        OnEnterActions?.Invoke();

        if (stateGraphElement != null)
        {
            //originalColor = stateGraphElement.GetComponent<UnityEngine.UI.Image>().color;
            stateGraphElement.GetComponent<UnityEngine.UI.Image>().color = Color.green;
        }
    }

    public void OnUpdate()
    {
        //OnUpdateActions?.Invoke();
    }

    public bool IsStateEquivalentTo(State anotherStateInAnotherStateMachine)
    {
        //this is the state in the resultingStateMachine
        
        foreach (var transitionInTheOtherSm in anotherStateInAnotherStateMachine.transitionsToMe())
        {

            if (!transitionsToMe().Any(t => t.HasSameTriggers(transitionInTheOtherSm)))
            {
                return false;
            }
        }

        return true;
    }

    public List<Transition> transitionsToMe()
    {
        return _stateMachine.transitionsTo(this);
    }

    public void OnExit()
    {
        DebugLogger.Instance.Log("OnExit: " + name + ", OnExitActions: " + String.Join(", ", OnExitActionsStr));
        
        OnExitActions?.Invoke();

        if(stateGraphElement != null)
        {   
            stateGraphElement.GetComponent<UnityEngine.UI.Image>().color = originalColor;
        }
    }

    public override string ToString()
    {
        return name;
    }

    public void AddTransitionTo(State targetState, Func<Frame, bool> condition, string textDescription = "empty description", List<Sequence> triggers = null)
    {
        transitions.Add(new Transition
        {
            from = this,
            to = targetState,
            condition = condition,
            textDescription = textDescription,
            triggers = triggers
        });
    }
    
    // todo 160426 DTO
    // public void AddTransitionTo(
    //     State targetState,
    //     Func<Frame, bool> condition,
    //     string textDescription = "empty description",
    //     List<Sequence> triggers = null,
    //     ConditionData serializedCondition = null)
    // {
    //     transitions.Add(new Transition
    //     {
    //         from = this,
    //         to = targetState,
    //         condition = condition,
    //         serializedCondition = serializedCondition,
    //         textDescription = textDescription,
    //         triggers = triggers ?? new List<Sequence>()
    //     });
    // }
    
    public void ClearTransitions()
    {
        transitions.Clear();
    }

    public void CopyTransitionFromState(State sourceState)
    {
        //DebugLogger.Instance.Log("Copying transition from state " + sourceState.id + " to state " + id);
        foreach (var transition in sourceState.transitions)
        {
            DebugLogger.Instance.Log("Copying transition from state " + sourceState.name + " to state " + name + " with text description " + transition.textDescription);
            transitions.Add(new Transition
            {   
                from = this,
                to = transition.to,
                condition = transition.condition,
                textDescription = transition.textDescription
            });
        }
    }

    public void ModifyTransitionTo(State newState)
    {
        foreach (var transition in transitions)
        {
            transition.to = newState;
        }
    }

    public void PrintDetailsOfState(bool VRConsoleEnabled = false)
    {
        DebugLogger.Instance.Log("State: " + name, VRConsoleEnabled);
        //Iterate and print OnEnter actions
        DebugLogger.Instance.Log("OnEnter: " + OnEnterActionsStr, VRConsoleEnabled);
        DebugLogger.Instance.Log("OnExit: " + OnExitActionsStr, VRConsoleEnabled);      

        foreach (var transition in transitions)
        {
            //DebugLogger.Instance.Log("Transition: " + transition.from + " " + transition.to, VRConsoleEnabled);
            DebugLogger.Instance.Log("Transition text description: " + transition.textDescription, VRConsoleEnabled);
            //Iterate and print transition conditions
            if(transition.condition != null)
            {
                /*foreach (var action in transition.condition.GetInvocationList())
                {
                    DebugLogger.Instance.Log("Condition: " + action.Method.Name, VRConsoleEnabled);
                }*/
            } 
            else
            {
                //DebugLogger.Instance.Log("Condition: null", VRConsoleEnabled);
            }
        }
    }
}

public class Transition
{
    public State from;
    public State to;

    public Func<Frame, bool> condition;

    // public ConditionData serializedCondition; // Serialization purposes
    
    public string textDescription;
    
    public List<Sequence> triggers;

    public bool ShouldApply(Frame frame)
    {
        return condition.Invoke(frame);
    }

    public bool HasSameTriggers(Transition anotherTransition)
    {
        return anotherTransition.triggers.All( anotherTrigger => triggers.Any( trigger => trigger.IsEquivalentSequence(anotherTrigger)));
    }
}

public class Frame
{
    public InputManager.Gesture leftHandGesture;
    public InputManager.Gesture rightHandGesture;

    public string voiceCommand;

    public Dictionary<GameObject,HashSet<GameObject>> collisions;

    public bool IsColliding(GameObject object1, GameObject object2)
    {
        // DebugLogger.Instance.Log("IsColliding?: " + object1 + " " + object2);
        return (collisions.TryGetValue(object1, out var list1) && list1.Contains(object2)) ||
               (collisions.TryGetValue(object2, out var list2) && list2.Contains(object1));

    }
}