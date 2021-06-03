using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class ISXProcessorAutoWorldSpaceWindow : EditorWindow
{
    private static InputAction m_action;

    [MenuItem("QA Tools/Input Test/Processor Test: Auto World Space", true, 11)]
    static bool CheckOpenTestWindow()
    {
        return false;
    }

    [MenuItem("QA Tools/Input Test/Processor Test: Auto World Space", false, 11)]
    static void OpenTestWindow()
    {
        ISXProcessorAutoWorldSpaceWindow window = GetWindow<ISXProcessorAutoWorldSpaceWindow>();
        window.Show();
    }

    void OnGUI()
    {
    }

    static ISXProcessorAutoWorldSpaceWindow()
    {
        //m_action = new InputAction(name: "AutoWorldSpaceTest", binding: "<pointer>/position", processors:"AutoWorldSpace");
    }
}
