using UnityEngine;

public class StateMachineFileIOTester : MonoBehaviour
{
    [SerializeField] private Example exampleForLoad;

    private void OnGUI()
    {
        const float buttonWidth = 160f;
        const float buttonHeight = 40f;
        const float left = 120f;
        const float top = 120f;
        const float spacing = 10f;

        if (GUI.Button(new Rect(left, top, buttonWidth, buttonHeight), "Test Save"))
        {
            if (CustomStateMachine.Instance == null)
            {
                DebugLogger.Instance.Log("Test Save failed: CustomStateMachine.Instance is null");
                return;
            }

            if (CustomStateMachine.Instance.stateMachineModel == null)
            {
                DebugLogger.Instance.Log("Test Save failed: stateMachineModel is null");
                return;
            }

            StateMachineFileIO.TestSave(CustomStateMachine.Instance.stateMachineModel);
        }

        if (GUI.Button(new Rect(left, top + buttonHeight + spacing, buttonWidth, buttonHeight), "Test Load"))
        {
            if (exampleForLoad == null)
            {
                DebugLogger.Instance.Log("Test Load failed: exampleForLoad is null");
                return;
            }

            StateMachineModel loadedModel = StateMachineFileIO.TestLoad(exampleForLoad);
            if (loadedModel != null)
            {
                CustomStateMachine.Instance.stateMachineModel = loadedModel;
                StateMachineModel.Instance = loadedModel;
                DebugLogger.Instance.Log("Test Load succeeded");
            }
        }
    }
}