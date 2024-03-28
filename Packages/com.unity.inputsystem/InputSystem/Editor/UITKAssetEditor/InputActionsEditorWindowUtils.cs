#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal static class InputActionsEditorWindowUtils
    {
        /// <summary>
        /// Return a relative path to the currently active theme style sheet.
        /// </summary>
        public static StyleSheet theme => EditorGUIUtility.isProSkin
        ? AssetDatabase.LoadAssetAtPath<StyleSheet>(InputActionsEditorConstants.PackagePath + InputActionsEditorConstants.ResourcesPath + "/InputAssetEditorDark.uss")
        : AssetDatabase.LoadAssetAtPath<StyleSheet>(InputActionsEditorConstants.PackagePath + InputActionsEditorConstants.ResourcesPath + "/InputAssetEditorLight.uss");

        // Similar to InputActionAsset.WriteFileJson but excludes the name
        [Serializable]
        private struct WriteFileJsonNoName
        {
            public InputActionMap.WriteMapJson[] maps;
            public InputControlScheme.SchemeJson[] controlSchemes;
        }

        // Similar to InputActionAsset.ToJson() but converts to JSON excluding the name property and any additional JSON
        // content that may be part of the file not recognized as required data.
        public static string ToJsonWithoutName(InputActionAsset asset)
        {
            return JsonUtility.ToJson(new WriteFileJsonNoName
            {
                maps = InputActionMap.WriteFileJson.FromMaps(asset.m_ActionMaps).maps,
                controlSchemes = InputControlScheme.SchemeJson.ToJson(asset.m_ControlSchemes),
            }, prettyPrint: true);
        }
    }
}
#endif
