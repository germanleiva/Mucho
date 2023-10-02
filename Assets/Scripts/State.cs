using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class State : TimelineUIElement
{
    public string id;
    public Action OnEnterActions { get; set; }
    public Action OnUpdateActions { get; set; }
    public Action OnExitActions { get; set; }
    public List<Transition> transitions = new();

    //public GameObject timelineElement;

    Color originalColor;
    public int StartIndex;
    public int Length;

    public GestureSequence? Gesture { get; set; }
    public AssetSequence? Asset { get; set; }

    public void OnEnter()
    {
        OnEnterActions?.Invoke();
        //Change the timeline element's image component color to green
        //if(timelineElement != null)
        {            
            originalColor = GetComponent<UnityEngine.UI.Image>().color;
            GetComponent<UnityEngine.UI.Image>().color = Color.green;
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
        //if(timelineElement != null)
        {   
            GetComponent<UnityEngine.UI.Image>().color = originalColor;
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
        GetComponent<TimelineUIElement>().SetStartX(StartIndex);
        GetComponent<TimelineUIElement>().SetWidth(Length);
    }

    public int GetStartIndex()
    {
        return StartIndex;
    }

    public void SetLength(int _Length)
    {
        Length = _Length;
    }
    public int GetCurrentLength()
    {
        return Length;
    }
}
