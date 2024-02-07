#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QA.Input_Test.Editor
{
    public static class Utils
    {
        // Sets Input System project-wide actions to null which is equivalent to user assigning null
        [MenuItem("QA Tools/Show Active Input System Project-wide Actions")]
        static void ShowInputSystemProjectWideActions()
        {
            var actions = InputSystem.actions;
            if (actions == null)
            {
                Debug.Log("InputSystem.actions is currently NOT set");
                return;
            }
            Debug.Log($"InputSystem.actions is currently name: {actions.name}, assetPath: {AssetDatabase.GetAssetPath(actions)}, instanceID: {actions.GetInstanceID()}");
        }

        // Sets Input System project-wide actions to null which is equivalent to user assigning null
        [MenuItem("QA Tools/Reset Input System Project-wide Actions")]
        static void ResetInputSystemProjectWideActions()
        {
            InputSystem.actions = null;
            Debug.Log("InputSystem.actions successfully reset to null");
        }
    }
}

#endif // UNITY_EDITOR
