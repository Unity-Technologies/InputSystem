#if UNITY_EDITOR
using UnityEditor;

namespace UnityEngine.InputSystem.OnScreen
{
    internal static class UGUIOnScreenControlEditorUtils
    {
        public static void ShowWarningIfNotPartOfCanvasHierarchy(OnScreenControl target)
        {
            if (UGUIOnScreenControlUtils.GetCanvasRectTransform(target.transform) == null)
                UnityEditor.EditorGUILayout.HelpBox(target.GetWarningMessage(), UnityEditor.MessageType.Warning);
        }
    }
}
#endif
