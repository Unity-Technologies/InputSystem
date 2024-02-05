using System;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    internal static class ActiveAssetEditorHelper
    {
        public static void DrawMakeActiveGui<T>(T current, T target, string targetName, string entity, Action<T> apply)
            where T : ScriptableObject
        {
            if (current == target)
            {
                EditorGUILayout.HelpBox($"This asset contains the currently active {entity} for the Input System.", MessageType.Info);
                return;
            }

            string currentlyActiveAssetsPath = null;
            if (current != null)
                currentlyActiveAssetsPath = AssetDatabase.GetAssetPath(current);
            if (!string.IsNullOrEmpty(currentlyActiveAssetsPath))
                currentlyActiveAssetsPath = $"The currently active {entity} are stored in {currentlyActiveAssetsPath}. ";
            EditorGUILayout.HelpBox($"Note that this asset does not contain the currently active {entity} for the Input System. {currentlyActiveAssetsPath??""}Click \"Make Active\" below to make \"{targetName}\" the active one.", MessageType.Warning);
            if (GUILayout.Button($"Make active", EditorStyles.miniButton))
                apply(target);
        }
    }
}
