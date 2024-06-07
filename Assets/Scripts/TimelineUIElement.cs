using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class TimelineUIElement : MonoBehaviour
{

    [SerializeField]
    private Canvas rootCanvas;
    [SerializeField]
    private RectTransform rootRectTransform;
    private RectTransform rectTransform;
    float defaultY;
    [SerializeField]
    TMPro.TMP_Text eventText;

    //Store startindex and length
    public int StartIndex;
    public int Length;
    public static float MinimumLength => 75;

    public void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        defaultY = rectTransform.anchoredPosition.y;
    }
    
    /*public void SetStartAndLength(int _startIndex, int _length)
    {
        StartIndex = _startIndex;
        Length = _length;
    }*/

    public void DragElement(BaseEventData data)
    {
        DebugLogger.Instance.Log("Panel is being dragged");
        // Update the position of the UI element based on the mouse position
        //transform.position = Input.mousePosition;
        PointerEventData pointerEventData = data as PointerEventData;        
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rootCanvas.transform as RectTransform, pointerEventData.position, rootCanvas.worldCamera, out position);
        //RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRectTransform, pointerEventData.position, canvas.worldCamera, out position);
        if(IsUIElementInsideRootPanel(position))
        {
            var pos = rootCanvas.transform.TransformPoint(position);
            transform.position = pos;
        }
        else
        {
            DebugLogger.Instance.Log("UI element is outside the root panel");
        }      

    }

    public void OnDragEnd(BaseEventData data)
    {       
        DebugLogger.Instance.Log("Panel drag ended");
        /*PointerEventData pointerEventData = data as PointerEventData;        
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rootCanvas.transform as RectTransform, pointerEventData.position, rootCanvas.worldCamera, out position); 
        //if(IsUIElementInsideRootPanel(position))
        {
            var pos = rootCanvas.transform.TransformPoint(position);
            transform.position = new Vector3(pos.x, defaultY, 0);
        }*/

        Vector2 pos = rectTransform.anchoredPosition;       
        pos.y= defaultY;
        rectTransform.anchoredPosition = pos;
    }

    public void OnClick(BaseEventData data)
    {
        DebugLogger.Instance.Log("Panel clicked");
        //SetColor(Color.blue);
    }
    
    public void OnDeleteActionEvent(GameObject deleteEventPanel)
    {
        DebugLogger.Instance.Log("Delete action event on object " + this);

        int correspondingAssetInstanceID = gameObject.transform.parent.GetComponent<TimelineAssetRow>().AssetInstanceID;
        Asset correspondingAsset = Recorder.Instance.allAssets.Find(asset => asset.GetInstanceID() == correspondingAssetInstanceID);
        if (correspondingAsset == null)
        {
            Debug.LogError("Asset not found");
            return;
        }
        
        //Delete the action event from the asset action sequence list
        Recorder.Instance.DeleteAssetActionSequence(correspondingAsset,StartIndex,Length);
        
        //Refresh collisions
        //TODO refresh should run a physics simulation to recalculate the collisions
        Recorder.Instance.RefreshTimelineCollisions();

        deleteEventPanel.SetActive(false);
        Destroy(gameObject);
    }

    void Update()
    {
                    

    }

    //destroy the action timeline event
    

    public static float MapIndexToTimelinePosition(RectTransform _rectTransform, int index, int recordingLength)
    {
        float rectStartX = 0;
        float rectEndX =  _rectTransform.GetComponent<RectTransform>().rect.width;
        int indexStart = 0;
        int indexEnd = recordingLength;
        //Map index to value scaled between rectStartX and rectEndX
        float mappedValue = Map(index, indexStart, indexEnd, rectStartX, rectEndX);
        return mappedValue;
    }
    public static float Map(float x, float in_min, float in_max, float out_min, float out_max) 
    {
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }

    public static GameObject CreateTimelineElement(GameObject prefab, RectTransform parentTransform, int startIndex, int length, int recordingLength, string id,
        Asset asset = null)
    {
        GameObject timelineElement = Instantiate(prefab, parentTransform);
        timelineElement.SetActive(true);
        
        float positionX = MapIndexToTimelinePosition(parentTransform, startIndex, recordingLength);
        float sizeDeltaX = MapIndexToTimelinePosition(parentTransform, startIndex + length, recordingLength) - positionX;

        //minimum length of action timeline events
        if(AssetActionSequence.IsActionEnum(id))
        {
            if (sizeDeltaX < MinimumLength)
            {
                sizeDeltaX = MinimumLength;
            }
        }
        RectTransform elementRect = timelineElement.GetComponent<RectTransform>();
        elementRect.anchoredPosition = new Vector2(positionX, elementRect.anchoredPosition.y);
        elementRect.sizeDelta = new Vector2(sizeDeltaX, elementRect.sizeDelta.y);
        
        timelineElement.GetComponent<TimelineUIElement>().SetEvent(id);

        timelineElement.GetComponent<TimelineUIElement>().SetStartAndLength(startIndex, length);
        
        return timelineElement;
    }
    
    public void SetStartAndLength(int _startIndex, int _length)
    {
        StartIndex = _startIndex;
        Length = _length;
    }

    

    public static void SetTimeLineElementWidthAccordingToText(GameObject timelineElement)
    {
        RectTransform elementRect = timelineElement.GetComponent<RectTransform>();
        TextMeshProUGUI textComponent = timelineElement.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            string text = textComponent.text;
            // Get dimensions of the text in the TextMeshProUGUI
            Vector2 textSize = textComponent.GetPreferredValues(text);

            // Set width of recttransform based on the length of the text 
            elementRect.sizeDelta = new Vector2(textSize.x, elementRect.sizeDelta.y);
        }
    }

    public void SetEvent(string text)
    {
        eventText.text = text;
    }

    //Function to check whether a Vector2 is within the bounds of the RectTransform
    public bool IsUIElementInsideRootPanel(Vector2 point)
    {
        //return RectTransformUtility.RectangleContainsScreenPoint(rootRectTransform, point, rootCanvas.worldCamera);
        //Get y max and min of the RectTransform
        float yMax = rootRectTransform.anchoredPosition.y + rootRectTransform.sizeDelta.y / 2;
        float yMin = rootRectTransform.anchoredPosition.y - rootRectTransform.sizeDelta.y / 2;
        return point.y <= yMax && point.y >= yMin;

    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetStartX(float x)
    {
        Vector2 pos = rectTransform.anchoredPosition;       
        pos.x = x;
        rectTransform.anchoredPosition = pos;
    }

    public void SetDimensions(float x, float width)
    {
        Vector2 pos = rectTransform.anchoredPosition;       
        pos.x = x;
        rectTransform.anchoredPosition = pos;

        Vector2 size = rectTransform.sizeDelta;
        size.x = width;
        rectTransform.sizeDelta = size;
    }

    public void SetWidth(float width)
    {
        Vector2 size = rectTransform.sizeDelta;
        size.x = width;
        rectTransform.sizeDelta = size;
    }

    public void SetColor(Color color)
    {
        GetComponent<UnityEngine.UI.Image>().color = color;
    }
}
