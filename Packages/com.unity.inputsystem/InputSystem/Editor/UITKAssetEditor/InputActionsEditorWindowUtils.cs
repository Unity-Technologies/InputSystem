#if UNITY_EDITOR
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

        public enum DialogResult
        {
            // Unsaved changes should be saved to asset.
            Save = 0,

            // Operation was cancelled.
            Cancel = 1,

            // Unsaved changes should not be saved to asset.
            DontSave = 2
        }

        public static DialogResult ConfirmSaveChanges(string path)
        {
            var result = EditorUtility.DisplayDialogComplex("Input Action Asset has been modified",
                $"Do you want to save the changes you made in:\n{path}\n\nYour changes will be lost if you don't save them.", "Save", "Cancel", "Don't Save");
            return (DialogResult)result;
        }

        public static DialogResult ConfirmDeleteAssetWithUnsavedChanges(string path)
        {
            var result = EditorUtility.DisplayDialog("Unsaved changes",
                $"You have unsaved changes for '{path}'. Do you want to discard the changes and delete the asset?",
                "Yes, Delete", "No, Cancel");
            if (result)
                return DialogResult.DontSave;
            return DialogResult.Cancel;
        }
    }

    // TODO Ideally instead sits on InputActionAssetManager
    internal static class InputActionAssetEditorExtensions
    {
        public static bool IsEqualJsonContent(this InputActionAsset asset, string otherJson)
        {
            var first = asset.ToJsonContent();
            var second = otherJson; // TODO This should really be sanitized up-front when synchronizing with asset
            return first == second;
        }

        public static bool HasChanged(this InputActionAsset asset, string editTargetAssetJson)
        {
            // Checks if the asset being edited is a new asset that was never saved before.
            // If it is, there's nothing to save.
            // At the moment, an asset only has the default asset layout content on disk when it is first created.
            // So in this case we cannot go through the normal path and compare what's on disk with what has been serialized.
            if (editTargetAssetJson == InputActionAsset.kDefaultAssetLayoutJson && asset.IsEmpty())
                return false;

            var newAssetJson = asset.ToJson();
            return newAssetJson != editTargetAssetJson;
        }
    }
}
#endif
