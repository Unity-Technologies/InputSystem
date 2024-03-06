using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;

namespace Tests.InputSystem.Editor
{
    // Replicated in this test assembly to avoid building public API picked up by PackageValidator
    internal class TestUtils
    {
#if UNITY_EDITOR
        // Replaces all dialogs in Input System editor code
        public static void MockDialogs()
        {
            // Default mock dialogs to avoid unexpected cancellation of standard flows
            Dialog.InputActionAsset.SetSaveChanges((_) => Dialog.Result.Discard);
            Dialog.InputActionAsset.SetDiscardUnsavedChanges((_) => Dialog.Result.Discard);
            Dialog.InputActionAsset.SetCreateAndOverwriteExistingAsset((_) => Dialog.Result.Discard);
            Dialog.ControlScheme.SetDeleteControlScheme((_) => Dialog.Result.Delete);
        }

        public static void RestoreDialogs()
        {
            // Re-enable dialogs.
            Dialog.InputActionAsset.SetSaveChanges(null);
            Dialog.InputActionAsset.SetDiscardUnsavedChanges(null);
            Dialog.InputActionAsset.SetCreateAndOverwriteExistingAsset(null);
            Dialog.ControlScheme.SetDeleteControlScheme(null);
        }

#endif // UNITY_EDITOR
    }
}
