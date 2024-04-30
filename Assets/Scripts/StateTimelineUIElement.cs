using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class StateTimelineUIElement : TimelineUIElement
{

    public void OnClick()
    {
        DebugLogger.Instance.Log("StateTimelineUIElement clicked");
        //StateMachineUIManager.Instance.OnStateTimelineUIElementClicked(this);
    }
    

    public static GameObject CreateStateTimelineElement(GameObject prefab, RectTransform parentTransform, int startIndex, int length, int recordingLength, string _stateName)//, State previousState, State nextState)//, int _stateIndexOnTimeline)
    {

        GameObject timelineElement = Instantiate(prefab, parentTransform);
        timelineElement.SetActive(true);
        float positionX = MapIndexToTimelinePosition(parentTransform, startIndex, recordingLength);
        float sizeDeltaX = MapIndexToTimelinePosition(parentTransform, startIndex + length, recordingLength) - positionX;

        RectTransform elementRect = timelineElement.GetComponent<RectTransform>();
        elementRect.anchoredPosition = new Vector2(positionX, elementRect.anchoredPosition.y);
        elementRect.sizeDelta = new Vector2(sizeDeltaX, elementRect.sizeDelta.y);
        
        timelineElement.GetComponent<StateTimelineUIElement>().SetEvent(_stateName);
        //timelineElement.GetComponent<StateTimelineUIElement>().state = _state;
        //timelineElement.GetComponent<StateTimelineUIElement>().stateIndexOnTimeline = _stateIndexOnTimeline;

        timelineElement.GetComponent<StateTimelineUIElement>().StartIndex = startIndex;
        timelineElement.GetComponent<StateTimelineUIElement>().Length = length;
        
        return timelineElement;
    }

    public void MergeWithStateUIElement(StateTimelineUIElement stateToTheRight)
    {
        var currentExample = Recorder.Instance.currentActiveExample;
        var insertionIndex = currentExample.StatePlaceholders.IndexOf(this);


        var stateTimelineElement = StateTimelineUIElement.CreateStateTimelineElement(
                    currentExample.stateTimelineElementPrefab,
                    currentExample.statesTimelinePanel.GetComponent<RectTransform>(),
                    StartIndex,
                    Length + stateToTheRight.Length,
                    Recorder.Instance.GetSizeOfMainRecordedData(),
                    "State " + (insertionIndex +1));
        
        //TODO modify consecutive names of next states

        currentExample.StatePlaceholders.Insert(insertionIndex, stateTimelineElement.GetComponent<StateTimelineUIElement>());
        currentExample.StatePlaceholders.Remove(stateToTheRight);
        currentExample.StatePlaceholders.Remove(this);
        Destroy(stateToTheRight.gameObject);
        Destroy(this.gameObject);
    }

}
