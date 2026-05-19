using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMergeStatesPanelHandler : MonoBehaviour
{
    [SerializeField] private Button leftButton;
    [SerializeField] private TMP_Text leftButtonLabel;
    
    [SerializeField] private Button rightButton;
    [SerializeField] private TMP_Text rightButtonLabel;

    [SerializeField] private StateTimelineUIElement associatedUIStatePlaceholder;
    private void OnEnable()
    {
        var placeholdersStateCache = Recorder.Instance.currentActiveExample.StatePlaceholders;
        var currentIndex = placeholdersStateCache.IndexOf(associatedUIStatePlaceholder);
        if (currentIndex == -1)
        {
            throw new Exception("WTF. Associated UI State Placeholder not found in current example's state placeholders. HOW?");
        }
        
        bool isFirst = currentIndex == 0 ; // Is first
        if (isFirst)
        {
            leftButton.interactable = false;
            leftButtonLabel.text = "No STATE to merge on LEFT";
        }
        else
        {
            leftButton.interactable = true;
            leftButtonLabel.text = "Merge left";
        }
        
        bool isLast = currentIndex == placeholdersStateCache.Count - 1; // Is last
        if (isLast)
        {
            rightButton.interactable = false;
            rightButtonLabel.text = "No STATE to merge on RIGHT";
        }
        else
        {
            leftButton.interactable = true;
            leftButtonLabel.text = "Merge right";
        }
    }
}
