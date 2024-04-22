using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

public class CustomStateMachine : MonoBehaviour
{
    public static CustomStateMachine Instance { get; private set; }

    private State currentState;
    private Dictionary<string, State> states = new Dictionary<string, State>();

    public TMPro.TMP_Text currentActiveStateText;

    private State initialState;

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

    public void AddState(string name, State state)
    {
        state.name = name;
        states.Add(name, state);
    }

    public void SetInitialState(string name)
    {
        DebugLogger.Instance.Log("Setting initial state to " + name,true);
        currentState = states[name];
        foreach (var state in states)
        {
            state.Value.ResetStateUIColor();
        }        
    }

    public void InvokeOnEnterActionsOfInitialState()
    {
        DebugLogger.Instance.Log("Invoking OnEnter actions of initial state " + currentState.name,true);
        currentState.OnEnter();
    }

    public int GetSize()
    {
        return states.Count;
    }

    public void DeleteState(string name)
    {
        states.Remove(name);
    }

    public void DeleteAllStates()
    {
        states.Clear();
    }

    public void PrintDetailsOfStateMachine(bool VRConsoleEnabled = false)
    {
        DebugLogger.Instance.Log("Printing details of state machine", VRConsoleEnabled);
        foreach (var state in states)
        {
            state.Value.PrintDetailsOfState(VRConsoleEnabled);
        }
    }


    public void CreateStateGraph(GameObject stateElementPrefab, RectTransform parentTransform)
    {
        StateGraphUI.ResetStateGraph();
        
        for(int i = 2;i < parentTransform.childCount; i++)
        {
            Destroy(parentTransform.GetChild(i).gameObject);
        }
        
        foreach (var state in states)
        {
            state.Value.stateGraphElement = StateGraphUI.CreateStateGraphElement(stateElementPrefab, parentTransform, state.Value);
        }
        //StateGraphUI.CreateStateGraphElement(stateElementPrefab, parentTransform, initialState);
    }

    public void ProcessFrame(Frame lastFrameObject)
    {                
        //DebugLogger.Instance.Log("ProcessFrame in the StateMachine");
        currentActiveStateText.text = "Current state: " + currentState.name;

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

        if(stateGraphElement != null)
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
        DebugLogger.Instance.Log("IsColliding?: " + object1 + " " + object2);
        return (object1 == collidingObjectThisFrame_1 && object2 == collidingObjectThisFrame_2) || (object1 == collidingObjectThisFrame_2 && object2 == collidingObjectThisFrame_1);
    }


}

