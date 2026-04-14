using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))] //e.g., a Button, Toggle, etc.
public class AppStateListenerUpdateSelectable : MonoBehaviour
{
    // public enum AppState { RECORDING, PLAYBACK, SIMULATING, LIVE, EDIT_BOUNDING_SPHERE } from Manager.cs
    // Create a checkbox for each app state in the inspector to control whether the selectable should be interactable in that state
    [SerializeField] private bool recording = true;
    [SerializeField] private bool playback = true;
    [SerializeField] private bool simulating = true;
    [SerializeField] private bool live = true;


    private Selectable _selectable;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
        if (_selectable == null)
            throw new Exception("UIAppStateListener: Button not found");
    }

    private void OnEnable()
    {
        if (Manager.Instance != null)
        {
            Manager.Instance.OnAppStateChanged += HandleAppStateChanged;
        }
    }

    private void OnDisable()
    {
        if (Manager.Instance != null)
        {
            Manager.Instance.OnAppStateChanged -= HandleAppStateChanged;
        }
    }

    private void HandleAppStateChanged(Manager.AppState oldState, Manager.AppState newState)
    {
        DebugLogger.Instance.Log($"[UIAppStateListener] [{this.gameObject.name}] Handling app state change from {oldState} to {newState}");
        UpdateButtonInteractableState(newState);
    }

    void UpdateButtonInteractableState(Manager.AppState state)
    {
        switch (state)
        {
            case Manager.AppState.RECORDING:
                _selectable.interactable = this.recording;
                break;
            case Manager.AppState.PLAYBACK:
                _selectable.interactable = this.playback;
                break;
            case Manager.AppState.SIMULATING:
                _selectable.interactable = this.simulating;
                break;
            case Manager.AppState.LIVE:
                _selectable.interactable = this.live;
                break;
            default:
                _selectable.interactable = true; // Default to interactable if state is unknown
                break;
        }
    }
    
    #region Context Menus for Quick Debugging purposes
    [ContextMenu("Test State/Recording")]
    private void TestRecording()
    {
        if (Manager.Instance == null)
        {
            Debug.LogWarning("Manager.Instance is null");
            return;
        }

        Manager.Instance.currAppState = Manager.AppState.RECORDING;
    }

    [ContextMenu("Test State/Playback")]
    private void TestPlayback()
    {
        if (Manager.Instance == null)
        {
            Debug.LogWarning("Manager.Instance is null");
            return;
        }

        Manager.Instance.currAppState = Manager.AppState.PLAYBACK;
    }

    [ContextMenu("Test State/Simulating")]
    private void TestSimulating()
    {
        if (Manager.Instance == null)
        {
            Debug.LogWarning("Manager.Instance is null");
            return;
        }

        Manager.Instance.currAppState = Manager.AppState.SIMULATING;
    }

    [ContextMenu("Test State/Live")]
    private void TestLive()
    {
        if (Manager.Instance == null)
        {
            Debug.LogWarning("Manager.Instance is null");
            return;
        }

        Manager.Instance.currAppState = Manager.AppState.LIVE;
    }
    #endregion
}