#if UNITY_EDITOR

using System;
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
            Discard = 2,

            // User decided to delete the associated resource.
            Delete = 3,
        }

        // User UI dialog windows related to InputActionAssets
        public static class InputActionAsset
        {
            #region Save Changes Dialog

            private static Func<string, Result> saveChanges = DefaultSaveChanges;

            internal static void SetSaveChanges(Func<string, Result> dialog)
            {
                saveChanges = dialog ?? DefaultSaveChanges;
            }

            private static Result DefaultSaveChanges(string path)
            {
                var id = EditorUtility.DisplayDialogComplex(
                    title: "Input Action Asset has been modified",
                    message: $"Do you want to save the changes you made in:\n{path}\n\nYour changes will be lost if you don't save them.",
                    ok: "Save",
                    cancel: "Cancel",
                    alt: "Don't Save");
                switch (id)
                {
                    case 0: return Result.Save;
                    case 1: return Result.Cancel;
                    case 2: return Result.Discard;
                    default: throw new ArgumentOutOfRangeException(nameof(id));
                }
            }

            // Shows a dialog prompting the user to save or discard unsaved changes.
            // May return Result.Save, Result.Cancel or Result.Discard.
            public static Result ShowSaveChanges(string path)
            {
                return saveChanges(path);
            }

            #endregion

            #region Discard Unsaved Changes Dialog

            private static Func<string, Result> discardUnsavedChanges = DefaultDiscardUnsavedChanges;

            internal static void SetDiscardUnsavedChanges(Func<string, Result> dialog)
            {
                discardUnsavedChanges = dialog ?? DefaultDiscardUnsavedChanges;
            }

            private static Result DefaultDiscardUnsavedChanges(string path)
            {
                var pressedOkButton = EditorUtility.DisplayDialog(
                    title: "Unsaved changes",
                    message:
                    $"You have unsaved changes for '{path}'. Do you want to discard the changes and delete the asset?",
                    ok: "Yes, Delete",
                    cancel: "No, Cancel");
                return pressedOkButton ? Result.Discard : Result.Cancel;
            }

            // Shows a dialog prompting the user to discard changes or cancel the operation.
            // May return Result.Discard or Result.Cancel.
            public static Result ShowDiscardUnsavedChanges(string path)
            {
                return discardUnsavedChanges(path);
            }

            #endregion

            #region Create and overwrite existing asset dialog

            private static Func<string, Result>
            createAndOverwriteExistingAsset = DefaultCreateAndOverwriteExistingAsset;

            internal static void SetCreateAndOverwriteExistingAsset(Func<string, Result> dialog)
            {
                createAndOverwriteExistingAsset = dialog ?? DefaultCreateAndOverwriteExistingAsset;
            }

            private static Result DefaultCreateAndOverwriteExistingAsset(string path)
            {
                var pressedOkButton = EditorUtility.DisplayDialog(
                    title: "Create Input Action Asset",
                    message: $"This will overwrite the existing asset: '{path}'. Continue and overwrite?",
                    ok: "Ok",
                    cancel: "Cancel");
                return pressedOkButton ? Result.Discard : Result.Cancel;
            }

            // Shows a dialog prompting the user whether the intention is to create an asset and overwrite the
            // currently existing asset. May return Result.Discard to overwrite or Result.Cancel to cancel.
            public static Result ShowCreateAndOverwriteExistingAsset(string path)
            {
                return createAndOverwriteExistingAsset(path);
            }

            #endregion
        }

        // User UI dialog windows related to InputControlSchemes
        public static class ControlScheme
        {
            private static Func<string, Result> deleteControlScheme = DefaultDeleteControlScheme;

            internal static void SetDeleteControlScheme(Func<string, Result> dialog)
            {
                deleteControlScheme = dialog ?? DefaultDeleteControlScheme;
            }

            private static Result DefaultDeleteControlScheme(string controlSchemeName)
            {
                // Ask for confirmation.
                var pressedOkButton = EditorUtility.DisplayDialog("Delete scheme?",
                    message: $"Do you want to delete control scheme '{controlSchemeName}'?",
                    ok: "Delete",
                    cancel: "Cancel");
                return pressedOkButton ? Result.Delete : Result.Cancel;
            }

            // Shows a dialog prompting the user to delete a control scheme or cancel the operation.
            // May return Result.Delete or Result.Cancel.
            public static Result ShowDeleteControlScheme(string controlSchemeName)
            {
                return deleteControlScheme(controlSchemeName);
            }
        }
    }
}

#endif // UNITY_EDITOR
