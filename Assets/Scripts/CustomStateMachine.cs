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

    public StateMachineModel stateMachineModel = new();
    
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
    public static StateMachineModel Instance { get; set; }
    
    public State currentState;
    public List<State> states = new();
    
    public void AddState(string name, State state)
    {
        state.name = name;
        states.Add(state);
    }

    public void SetInitialState(State state)
    {
        DebugLogger.Instance.Log("Setting initial state to " + state.name,true);
        currentState = state;
        // foreach (var state in states)
        // {
        //     state.Value.ResetStateUIColor();
        // }        
    }

    public void InvokeOnEnterActionsOfInitialState()
    {
        DebugLogger.Instance.Log("Invoking OnEnter actions of initial state " + currentState.name,true);
        currentState.OnEnter();
    }
    public void ProcessFrame(Frame lastFrameObject)
    {                
        
        foreach (var transition in currentState.transitions)
        {
            if (transition.ShouldApply(lastFrameObject))
            {
                //DebugLogger.Instance.Log("Transition applied from" + transition.from + " to " + transition.to);
                ApplyTransition(transition);
            }
            else
            {
                //DebugLogger.Instance.Log("Transition NOT applied from " + transition.from + " to " + transition.to);
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

    public static StateMachineModel CombinedStateMachine(List<Example> examples)
    {
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
                var currentState = stateMachineToDelete.states.ElementAt(i);
         
                //Find if the state does not exist in the resultingStateMachine
                var equivalentState = resultingStateMachine.states
                    .Find(x => x.IsStateEqualTo(currentState));
                if (equivalentState != null)
                {
                    //this state is represented in the resultingStateMachine
                    
                    //TODO Should we add the extra actions in this state if there are any?

                } else {
                    //This state is not equal to any state in the resultingStateMachine
                    //We need to add this state to the resultingStateMachine
                    if (i == 0)
                    {
                        //This is the first state. The only option is to merge both initial states
                    } else 
                    {
                        var previousState = stateMachineToDelete.states.ElementAt(i - 1);
                        var equivalentPreviousState = resultingStateMachine.states
                            .Find(x => x.IsStateEqualTo(previousState));

                        if (equivalentPreviousState == null)
                        {
                            throw new Exception("This should not happen");
                        }
                        
                        
                        /*
                        equivalentPreviousState;
                        previousState;
                        */

                        var equivalentCurrentState = new State();
                        equivalentCurrentState.name = currentState.name;
                        //Copy all onEnter/onUpdate/onExit/etc
                        equivalentCurrentState.OnEnterActions = currentState.OnEnterActions;
                        equivalentCurrentState.OnUpdateActions = currentState.OnUpdateActions;
                        equivalentCurrentState.OnExitActions = currentState.OnExitActions;
                        equivalentCurrentState.OnEnterActionsStr = currentState.OnEnterActionsStr;
                        equivalentCurrentState.OnUpdateActionsStr = currentState.OnUpdateActionsStr;
                        equivalentCurrentState.OnExitActionsStr = currentState.OnExitActionsStr;
                        equivalentCurrentState.Gesture = currentState.Gesture;
                        equivalentCurrentState.Collision = currentState.Collision;
                        equivalentCurrentState.VoiceSequence = currentState.VoiceSequence;
                        resultingStateMachine.AddState(currentState.name,currentState);
                        
                        var potentialTransitionsToAdd = previousState.transitions.FindAll(x => x.to.IsStateEqualTo(currentState));
                        
                        //Could it be this a potentialTransitionToAdd is already in the resultingStateMachine?
                        //Technically no, because currentState is not on the resultingStateMachine so any transition to currentState should not be in the resultingStateMachine
                        
                        foreach (var transitionToAdd in potentialTransitionsToAdd)
                        {
                            
                            //This transition is not in the resultingStateMachine
                            //We need to add this transition to the resultingStateMachine
                            equivalentPreviousState.AddTransitionTo(equivalentCurrentState, transitionToAdd.condition, transitionToAdd.textDescription);
                        }
                        
                    }
                }
            }
        }

        CustomStateMachine.Instance.stateMachineModel = resultingStateMachine;
        StateMachineModel.Instance = resultingStateMachine;

        return resultingStateMachine;
    }
    
    public static StateMachineModel CreateStateMachine(Example example)
    {
        StateMachineModel stateMachine = new StateMachineModel();
        
        var localStatesDict = new Dictionary<StateTimelineUIElement,State>();

        foreach (var statePlaceholder in example.StatePlaceholders) {
            var newState = new State {
                name = "State " + stateMachine.states.Count
            };
            stateMachine.AddState(newState.name, newState);

            localStatesDict.Add(statePlaceholder, newState);
        }

        var gestures = example.AllGestureSequences;
        var collisions = example.CollisionModels;
        var voiceCommands = example.VoiceCommandSequences;

        List<Sequence> allPotentialTriggers = gestures.Cast<Sequence>()
                                  .Concat(collisions.Cast<Sequence>())
                                  .Concat(voiceCommands.Cast<Sequence>())
                                  .ToList();

        var allActions = example.assetsDict.Select(keyValuePair => keyValuePair.Value.assetActions).ToList(); 

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
                    currentState.AddTransitionTo(nextState, transitionConditionFunction, transitionDescription);
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
                            currentState.OnEnterActions += () => assetSequence.ActionDelegate();
                            currentState.OnEnterActionsStr += assetSequence.ActionType.ToString() + " ";
                        }
                        else
                        {
                            DebugLogger.Instance.Log("Adding action(s) " + assetSequence.ActionType + " for state " +
                                                     currentState.name + " in OnExitActions");
                            currentState.OnExitActions += () => assetSequence.ActionDelegate();
                            currentState.OnExitActionsStr += assetSequence.ActionType.ToString() + " ";
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
    public string name;
    public Action OnEnterActions { get; set; }
    public Action OnUpdateActions { get; set; }
    public Action OnExitActions { get; set; }

    public string OnEnterActionsStr = "";
    public string OnUpdateActionsStr = "";
    public string OnExitActionsStr = "";

    public List<Transition> transitions = new();

    public GameObject timelineElement;

    public GameObject stateGraphElement;

    Color originalColor = Color.white;
    public GestureSequence Gesture { get; set; }
    public AssetActionSequence Collision { get; set; }

    public VoiceSequence VoiceSequence { get; set; }

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

    public bool IsStateEqualTo(State state) 
    {
        //Check if the state's actions and transitions are equal
        
        if(OnEnterActionsStr == state.OnEnterActionsStr &&            
           OnExitActionsStr == state.OnExitActionsStr && 
           transitions.Select(t => t.textDescription).SequenceEqual(state.transitions.Select(t => t.textDescription)))
        {
            return true;
        }
        else
        {
            return false;
        }  
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

    override public string ToString()
    {
        return name;
    }

    public void AddTransitionTo(State targetState, Func<Frame, bool> condition, string textDescription = "empty description")
    {
        transitions.Add(new Transition
        {
            from = this,
            to = targetState,
            condition = condition,
            textDescription = textDescription
        });
    }

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
        if(OnEnterActions != null)
        {    
            DebugLogger.Instance.Log("OnEnter: " + OnEnterActionsStr, VRConsoleEnabled);
        } 
        else
        {
            //DebugLogger.Instance.Log("OnEnter: null", VRConsoleEnabled);
        }
        //Iterate and print OnExit actions    
        if(OnExitActions != null)
        {   
            DebugLogger.Instance.Log("OnExit: " + OnExitActionsStr, VRConsoleEnabled);      
        } 
        else
        {
            //DebugLogger.Instance.Log("OnExit: null", VRConsoleEnabled);
        }

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

    public string textDescription;

    public bool ShouldApply(Frame frame)
    {
        return condition.Invoke(frame);
    }
}

public class Frame
{
    public InputManager.Gesture leftHandGesture;
    public InputManager.Gesture rightHandGesture;

    public string voiceCommand;

    public GameObject collidingObjectThisFrame_1, collidingObjectThisFrame_2;

    public bool IsColliding(GameObject object1, GameObject object2)
    {
        // DebugLogger.Instance.Log("IsColliding?: " + object1 + " " + object2);
        return (object1 == collidingObjectThisFrame_1 && object2 == collidingObjectThisFrame_2) || (object1 == collidingObjectThisFrame_2 && object2 == collidingObjectThisFrame_1);
    }
}