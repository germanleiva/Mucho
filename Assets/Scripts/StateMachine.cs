using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine : MonoBehaviour
{
    public static StateMachine Instance { get; private set; }

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
        states[name] = state;
    }

    public void SetInitialState(string name)
    {
        currentState = states[name];
        currentState.OnEnter();
    }

    public void TransitionToState(State nextState)
    {
        currentState.OnExit();
        currentState = nextState;
        currentState.OnEnter();
    }

    void Update()
    {
        currentState?.OnUpdate();
    }
}

