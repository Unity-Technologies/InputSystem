#if UNITY_EDITOR

using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    // Input system UI dialogs centralized as utility methods.
    // In the future we may introduce possibility to mock/stub these dialogs via delegates to allow
    // automated testing of dialog options and secure that no dialogs are shown in editor testing.
    internal static class Dialog
    {
        // Represents the result of the user selecting a dialog option.
        public enum Result
        {
            // User decided that unsaved changes should be saved to the associated asset.
            Save = 0,

            // Operation was cancelled by the user.
            Cancel = 1,

            // Unsaved changes should be discarded and NOT be saved to the associated asset.
            Discard = 2
        }

        // User UI dialog windows related to InputActionAssets
        public static class InputActionAsset
        {
            // Shows a dialog prompting the user to save or discard unsaved changes.
            // May return Result.Save, Result.Cancel or Result.Discard.
            public static Result ShowSaveChanges(string path)
            {
                return (Result)EditorUtility.DisplayDialogComplex(
                    title: "Input Action Asset has been modified",
                    message: $"Do you want to save the changes you made in:\n{path}\n\nYour changes will be lost if you don't save them.",
                    ok: "Save",
                    cancel: "Cancel",
                    alt: "Don't Save");
            }

            // Shows a dialog prompting the user to discard changes or cancel the operation.
            // May return Result.Discard or Result.Cancel.
            public static Result ShowDiscardUnsavedChanges(string path)
            {
                var result = EditorUtility.DisplayDialog(
                    title: "Unsaved changes",
                    message:
                    $"You have unsaved changes for '{path}'. Do you want to discard the changes and delete the asset?",
                    ok: "Yes, Delete",
                    cancel: "No, Cancel");
                return result ? Result.Discard : Result.Cancel;
            }
        }
    }
}

#endif // UNITY_EDITOR
