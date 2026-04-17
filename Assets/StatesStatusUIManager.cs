using System;
using TMPro;
using UnityEngine;

public class StatesStatusUIManager : MonoBehaviour
{
    private static StatesStatusUIManager Instance { get; set; }
    
    [SerializeField] private TMP_Text currentExampleText;
    [SerializeField] private TMP_Text allExamplesText;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        if (currentExampleText == null)
        {
            throw new Exception("UI Text component 'currentExampleText' is not assigned in the inspector.");
        }
        if (allExamplesText == null)
        {
            throw new Exception("UI Text component 'allExamplesText' is not assigned in the inspector.");
        }
    }
    
    private void OnEnable()
    {
        if (Recorder.Instance != null)
        {
            Recorder.Instance.OnAnyExampleStateVersionUpdated += HandleAnyExampleStateVersionUpdated;
        }
    }

    private void OnDisable()
    {
        if (Recorder.Instance != null)
        {
            Recorder.Instance.OnAnyExampleStateVersionUpdated -= HandleAnyExampleStateVersionUpdated;
        }
    }

    private void HandleAnyExampleStateVersionUpdated(
        Example example,
        int inputVersion,
        int generatedVersion,
        bool areUpToDate)
    {
        Debug.Log(
            $"Example {example.exampleId} changed. Input={inputVersion}, Generated={generatedVersion}, UpToDate={areUpToDate}"
        );
        
        if (areUpToDate)
        {
            currentExampleText.text = "<color=\"green\">up to date";
            bool areAllUpToDate = Recorder.Instance.AreAllExamplesUpToDate();
            allExamplesText.text = areAllUpToDate
                ? "<color=\"green\">all up to date"
                : "<color=\"red\">(one or more) dirty";
        }
        else
        {
            currentExampleText.text = "<color=\"red\">dirty";
            // allExamplesText.text = "<color=\"red\">(one or more) dirty";
            allExamplesText.text = "<color=\"red\">dirty";
        }
    }
}
