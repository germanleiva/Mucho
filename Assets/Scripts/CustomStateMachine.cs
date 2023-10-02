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
        foreach (var transition in currentState.transitions)
        {
            if (transition.ShouldApply(lastFrameObject))
            {
                DebugLogger.Instance.Log("Transition applied " + transition.from + " " + transition.to);
                ApplyTransition(transition);
            }
            else
            {
                //DebugLogger.Instance.Log("Transition NOT applied " + transition.from + " " + transition.to);
            }
        }

        // this.currentState.OnUpdate()
    }

    private void ApplyTransition(Transition transition)
    {
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
    public AssetSequence? Asset { get; set; }

    public void OnEnter()
    {
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

    public void AddTransitionTo(State targetState, Func<Frame, bool> condition)
    {
        transitions.Add(new Transition
        {
            from = this,
            to = targetState,
            condition = condition
        });
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
    
}

public class Transition
{
    public State from;
    public State to;

    public Func<Frame, bool> condition;

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
        return (object1 == collidingObjectThisFrame_1 && object2 == collidingObjectThisFrame_2) || (object1 == collidingObjectThisFrame_2 && object2 == collidingObjectThisFrame_1);
    }


}

