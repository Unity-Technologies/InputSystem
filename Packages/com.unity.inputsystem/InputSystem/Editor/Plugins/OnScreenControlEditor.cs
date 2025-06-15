using UnityEditor;
using UnityEngine.InputSystem.OnScreen;

namespace UnityEditor.InputSystem.OnScreen {
    internal static class UGUIOnScreenControlEditorUtils
    {
        public static void ShowWarningIfNotPartOfCanvasHierarchy(OnScreenControl target)
        {
            if (UGUIOnScreenControlUtils.GetCanvasRectTransform(target.transform) == null)
                UnityEditor.EditorGUILayout.HelpBox(target.GetWarningMessage(), UnityEditor.MessageType.Warning);
        }
    }
}