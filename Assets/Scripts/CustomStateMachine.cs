using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CustomStateMachine : MonoBehaviour
{
    public static CustomStateMachine Instance { get; private set; }

    private State currentState;
    private Dictionary<string, State> states = new Dictionary<string, State>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddState(string name, State state)
    {
        state.id = name;
        states.Add(name, state);
    }

    public void SetInitialState(string name)
    {
        DebugLogger.Instance.Log("Setting initial state to " + name,true);
        currentState = states[name];
        // currentState.OnEnter();
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


    public void ProcessFrame(Frame lastFrameObject)
    {                
        DebugLogger.Instance.Log("ProcessFrame in the StateMachine");

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
    public string id;
    public Action OnEnterActions { get; set; }
    public Action OnUpdateActions { get; set; }
    public Action OnExitActions { get; set; }
    public List<Transition> transitions = new();

    public GameObject timelineElement;

    Color originalColor;
    private int StartIndex;
    private int Length;

    public GestureSequence? Gesture { get; set; }
    public AssetSequence? Collision { get; set; }

    public void OnEnter()
    {
        DebugLogger.Instance.Log("OnEnter: " + id);
        DebugLogger.Instance.Log("OnEnter: " + OnEnterActions);
        OnEnterActions?.Invoke();
        //Change the timeline element's image component color to green
        if(timelineElement != null)
        {            
            originalColor = timelineElement.GetComponent<UnityEngine.UI.Image>().color;
            timelineElement.GetComponent<UnityEngine.UI.Image>().color = Color.green;
        }
    }
    public void OnUpdate()
    {
        OnUpdateActions?.Invoke();
    }

    public void OnExit()
    {
        DebugLogger.Instance.Log("OnExit: " + id);
        DebugLogger.Instance.Log("OnExit: " + OnExitActions);
        OnExitActions?.Invoke();
        //Change the timeline element's image component color back to the original color
        if(timelineElement != null)
        {   
            timelineElement.GetComponent<UnityEngine.UI.Image>().color = originalColor;
        }
    }

    override public string ToString()
    {
        return id;
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

    public void CopyTransitionFromState(State sourceState)
    {
        foreach (var transition in sourceState.transitions)
        {
            transitions.Add(new Transition
            {
                from = this,
                to = transition.to,
                condition = transition.condition
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

    public void RefreshStateStartAndLength(int _StartIndex, int _Length)
    {
        StartIndex = _StartIndex;
        Length = _Length;
        timelineElement.GetComponent<TimelineUIElement>().SetStartX(StartIndex);
        timelineElement.GetComponent<TimelineUIElement>().SetWidth(Length);
    }

    /*public int GetStartIndex()
    {
        return StartIndex;
    }

    public int GetCurrentLength()
    {
        return Length;
    }*/

    public void PrintDetailsOfState(bool VRConsoleEnabled = false)
    {
        DebugLogger.Instance.Log("State: " + id, VRConsoleEnabled);
        //Iterate and print OnEnter actions
        /*if(OnEnterActions != null)
        {
            foreach (var action in OnEnterActions.GetInvocationList())
            {
                DebugLogger.Instance.Log("OnEnter: " + action.Method.Name, VRConsoleEnabled);
            }
        } 
        else
        {
            DebugLogger.Instance.Log("OnEnter: null", VRConsoleEnabled);
        }
        //Iterate and print OnExit actions    
        if(OnExitActions != null)
        {
            foreach (var action in OnExitActions.GetInvocationList())
            {
                DebugLogger.Instance.Log("OnExit: " + action.Method.Name, VRConsoleEnabled);
            }
        } 
        else
        {
            DebugLogger.Instance.Log("OnExit: null", VRConsoleEnabled);
        }*/

        foreach (var transition in transitions)
        {
            DebugLogger.Instance.Log("Transition: " + transition.from + " " + transition.to, VRConsoleEnabled);
            DebugLogger.Instance.Log("Transition text description: " + transition.textDescription, VRConsoleEnabled);
            //Iterate and print transition conditions
            if(transition.condition != null)
            {
                foreach (var action in transition.condition.GetInvocationList())
                {
                    DebugLogger.Instance.Log("Condition: " + action.Method.Name, VRConsoleEnabled);
                }
            } 
            else
            {
                DebugLogger.Instance.Log("Condition: null", VRConsoleEnabled);
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

    public GameObject collidingObjectThisFrame_1, collidingObjectThisFrame_2;

    public bool IsColliding(GameObject object1, GameObject object2)
    {
        DebugLogger.Instance.Log("IsColliding?: " + object1 + " " + object2);
        return (object1 == collidingObjectThisFrame_1 && object2 == collidingObjectThisFrame_2) || (object1 == collidingObjectThisFrame_2 && object2 == collidingObjectThisFrame_1);
    }


}

