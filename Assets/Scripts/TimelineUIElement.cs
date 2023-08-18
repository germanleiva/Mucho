using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class TimelineUIElement : MonoBehaviour
{

    [SerializeField]
    private Canvas rootCanvas;
    [SerializeField]
    private RectTransform rootRectTransform;
    private RectTransform rectTransform;
    float defaultY;

    public void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        defaultY = rectTransform.anchoredPosition.y;
    }
    
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

    void Update()
    {
                    

    }

    //Function to check whether a Vector2 is within the bounds of the RectTransform
    public bool IsUIElementInsideRootPanel(Vector2 point)
    {
        //return RectTransformUtility.RectangleContainsScreenPoint(rootRectTransform, point, rootCanvas.worldCamera);
        //Get y max and min of the RectTransform
        float yMax = rootRectTransform.anchoredPosition.y + rootRectTransform.sizeDelta.y / 2;
        float yMin = rootRectTransform.anchoredPosition.y - rootRectTransform.sizeDelta.y / 2;
        if (point.y > yMax || point.y < yMin)
        {
            return false;
        }
        else
        {
            return true;
        }

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

    public void test1()
    {
        SetDimensions(0, 100);
    }

    public void test2()
    {
        SetDimensions(100, 500);
    }


}
