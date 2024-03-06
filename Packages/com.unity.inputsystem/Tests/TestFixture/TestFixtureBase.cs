using NUnit.Framework;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif

namespace UnityEngine.InputSystem
{
    // A common test fixture base which mocks/stubs that:
    // - Mock dialogs with delegates to avoid dialogs preventing test execution.
    // - Removes any test files created with AssetDatabaseUtils at the end of a test case.
    public class TestFixtureBase
    {
        [SetUp]
        public virtual void Setup()
        {
#if UNITY_EDITOR
            MockDialogs();
#endif // UNITY_EDITOR
        }

        [TearDown]
        public virtual void TearDown()
        {
#if UNITY_EDITOR
            RestoreDialogs();

            // Clean-up assets created by test
            AssetDatabaseUtils.Restore();
#endif // UNITY_EDITOR
        }

#if UNITY_EDITOR
        // Replaces all dialogs in Input System editor code
        private static void MockDialogs()
        {
            // Default mock dialogs to avoid unexpected cancellation of standard flows
            Dialog.InputActionAsset.SetSaveChanges((_) => Dialog.Result.Discard);
            Dialog.InputActionAsset.SetDiscardUnsavedChanges((_) => Dialog.Result.Discard);
            Dialog.InputActionAsset.SetCreateAndOverwriteExistingAsset((_) => Dialog.Result.Discard);
            Dialog.ControlScheme.SetDeleteControlScheme((_) => Dialog.Result.Delete);
        }

        private static void RestoreDialogs()
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
