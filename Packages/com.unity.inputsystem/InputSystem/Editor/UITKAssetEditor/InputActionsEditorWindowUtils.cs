#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputActionsEditorWindowUtils
    {
        /// <summary>
        /// Return a relative path to the currently active theme style sheet.
        /// </summary>
        public static StyleSheet theme => EditorGUIUtility.isProSkin
        ? AssetDatabase.LoadAssetAtPath<StyleSheet>(InputActionsEditorConstants.PackagePath + InputActionsEditorConstants.ResourcesPath + "/InputAssetEditorDark.uss")
        : AssetDatabase.LoadAssetAtPath<StyleSheet>(InputActionsEditorConstants.PackagePath + InputActionsEditorConstants.ResourcesPath + "/InputAssetEditorLight.uss");

        public enum ConfirmSaveChangesDialogResult
        {
            Save = 0,
            Cancel = 1,
            DontSave = 2
        }

        public static ConfirmSaveChangesDialogResult ConfirmSaveChanges(string path)
        {
            var result = EditorUtility.DisplayDialogComplex("Input Action Asset has been modified",
                $"Do you want to save the changes you made in:\n{path}\n\nYour changes will be lost if you don't save them.", "Save", "Cancel", "Don't Save");
            return (ConfirmSaveChangesDialogResult)result;
        }
    }
}
#endif
