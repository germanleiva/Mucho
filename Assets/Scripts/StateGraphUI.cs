using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateGraphUI : MonoBehaviour
{
    //public GameObject stateGraphElementPrefab;
    static float lastStatePosX = 0;
    static float lastStatePosY = 0;

    static float gap = 30;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static GameObject CreateStateGraphElement(GameObject prefab, RectTransform parentTransform, State _state)//, State previousState, State nextState)//, int _stateIndexOnTimeline)
    {
        GameObject timelineElement = Instantiate(prefab, parentTransform);
        timelineElement.SetActive(true);

        RectTransform elementRect = timelineElement.GetComponent<RectTransform>();
        
        float positionX = lastStatePosX + elementRect.sizeDelta.x + gap;
        lastStatePosX = positionX;
        
        elementRect.anchoredPosition = new Vector2(positionX, elementRect.anchoredPosition.y);   
        
        timelineElement.GetComponentInChildren<TMPro.TMP_Text>().text = _state.id;
        
        return timelineElement;
    }
}
