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

    private List<Transition> allTransitions
    {
        get
        {
            var transitions = new List<Transition>();
            foreach (var state in states.Values)
            {
                transitions.AddRange(state.transitions);
            }
            return transitions;
        }
    }

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
    }

}

public class State
{
    public string id;
    public Action OnEnterActions { get; set; }
    public Action OnUpdateActions { get; set; }
    public Action OnExitActions { get; set; }
    public List<Transition> transitions = new List<Transition>();

    public void OnEnter()
    {
        OnEnterActions?.Invoke();
    }
    public void OnUpdate()
    {
        OnUpdateActions?.Invoke();
    }

    public void OnExit()
    {
        OnExitActions?.Invoke();
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
}

public class Transition
{
    public State from;
    public State to;
    //public Func<GameObject, GameObject, bool> collisionCondition;
    public Func<Frame, bool> condition;

    public bool ShouldApply(Frame frame)
    {
        return condition.Invoke(frame);
        /*if(frame.isColliding(frame.collidingObject1, frame.collidingObject2))
        {
            return true;
        }
        return false;*/
    }
}

public class Frame
{
    public InputManager.Gesture leftHandGesture;
    public InputManager.Gesture rightHandGesture;

    public GameObject collidingObject1, collidingObject2;

    public bool isColliding(GameObject object1, GameObject object2)
    {
        return (object1 == collidingObject1 && object2 == collidingObject2) || (object1 == collidingObject2 && object2 == collidingObject1);
        //return object1 != null && object2 != null;
    }


}

