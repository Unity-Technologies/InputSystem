#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.InputSystem.Editor
{
    // We want an empty editor in the inspector. Editing happens in a dedicated window.
    [CustomEditor(typeof(InputActionAsset))]
    internal class InputActionAssetEditor : UnityEditor.Editor
    {
        protected override void OnHeaderGUI()
        {
        }

        public override void OnInspectorGUI()
        {
        }

        #region Support abstract editor registration

        private static readonly List<Type> s_EditorTypes = new List<Type>();

        // Registers an asset editor type for receiving asset modification callbacks.
        public static void RegisterType<T>() where T : IInputActionAssetEditor
        {
            if (!s_EditorTypes.Contains(typeof(T)))
                s_EditorTypes.Add(typeof(T));
        }

        // Unregisters an asset editor type from receiving asset modification callbacks.
        public static void UnregisterType<T>() where T : IInputActionAssetEditor
        {
            s_EditorTypes.Remove(typeof(T));
        }

        public static T FindOpenEditor<T>(string path) where T : EditorWindow
        {
            var openEditors = FindAllEditorsForPath(path);
            foreach (var openEditor in openEditors)
            {
                if (openEditor.GetType() == typeof(T))
                    return (T)openEditor;
            }
            return null;
        }

        // Finds all asset editors associated with the asset given by path.
        public static IInputActionAssetEditor[] FindAllEditorsForPath(string path)
        {
            var guid = AssetDatabase.AssetPathToGUID(path);
            return guid != null ? FindAllEditors((editor) => editor.assetGUID == guid) :
                Array.Empty<IInputActionAssetEditor>();
        }

        // Finds all asset editors fulfilling the given predicate.
        public static IInputActionAssetEditor[] FindAllEditors(Predicate<IInputActionAssetEditor> predicate = null)
        {
            List<IInputActionAssetEditor> editors = null;
            foreach (var type in s_EditorTypes)
                editors = FindAllEditors(type, predicate, editors);
            return editors != null ? editors.ToArray() : Array.Empty<IInputActionAssetEditor>();
        }

        private static List<IInputActionAssetEditor> FindAllEditors(Type type,
            Predicate<IInputActionAssetEditor> predicate = null,
            List<IInputActionAssetEditor> result = null)
        {
            if (result == null)
                result = new List<IInputActionAssetEditor>();
            var editors = Resources.FindObjectsOfTypeAll(type);
            foreach (var editor in editors)
            {
                if (editor is IInputActionAssetEditor actionsAssetEditor && (predicate == null || predicate(actionsAssetEditor)))
                    result.Add(actionsAssetEditor);
            }
            return result;
        }

        #endregion

        #region Asset modification processor to intercept Unity editor move or delete operations
        // Asset modification processor designed to handle the following scenarios:
        // - When an asset is about to get deleted, evaluate if there is a pending unsaved edited copy of the asset
        //   open in any associated editor and in this case, prompt the user that there are unsaved changes and allow
        //   the user to cancel the operation and allow to save the pending changes or confirm to delete the asset and
        //   discard the pending unsaved changes (via OnAssetDeleted() notification).
        // - If the asset being deleted is not open in any editors or any open copies are not modified, no dialog
        //   prompt is displayed and the asset is deleted.
        // - When an asset is about to get moved, notify any editors having the asset open about the move.
        //
        // See comments further down in this class for expected callback sequences.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Intantiated through reflection by Unity")]
        private class InputActionAssetModificationProcessor : UnityEditor.AssetModificationProcessor
        {
            public static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
            {
                if (InputActionImporter.IsInputActionAssetPath(path))
                {
                    // Find any open editors associated to the asset and if any of them holds unsaved changes
                    // allow the user to discard unsaved changes or cancel deletion.
                    var editorWithAssetOpen = InputActionAssetEditor.FindAllEditorsForPath(path);
                    foreach (var editor in editorWithAssetOpen)
                    {
                        if (editor.isDirty)
                        {
                            var result = Dialog.InputActionAsset.ShowDiscardUnsavedChanges(path);
                            if (result == Dialog.Result.Cancel)
                                return AssetDeleteResult.FailedDelete;
                            break;
                        }
                    }

                    // Notify all associated editors that asset will be deleted
                    foreach (var editor in editorWithAssetOpen)
                        editor.OnAssetDeleted();
                }

                return default;
            }

            public static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
            {
                if (InputActionImporter.IsInputActionAssetPath(sourcePath))
                {
                    var editorWithAssetOpen = InputActionAssetEditor.FindAllEditorsForPath(sourcePath);
                    foreach (var editor in editorWithAssetOpen)
                        editor.OnAssetMoved();
                }

                return default;
            }
        }

        #endregion

        #region Asset post processor to react to internal or external asset import, move, delete events.
        // Processor detecting any Unity editor internal or external (file system) changes to an asset and notifies
        // any associated asset editors about those changes via callbacks.
        //
        // Note that any editor classes interested in receiving notifications need to be registered.
        //
        // For clarity, the tables below indicate the callback sequences of the asset modification processor and
        // asset post-processor for various user operations done on assets.
        //
        // s = source file
        // d = destination file
        // * = operation may be aborted by user
        //
        // User operation:                Callback sequence:
        // ----------------------------------------------------------------------------------------
        // Write (Save)                   Imported(s)
        // Delete                         OnWillDelete(s), Deleted(s)*
        // Copy                           Imported(s)
        // Rename                         OnWillMove(s,d), Imported(d), Moved(s,d)
        // Move (drag) / Cut+Paste        OnWillMove(s,d), Moved(s,d)
        // ------------------------------------------------------------------------------------------------------------
        //
        // External user operation:       Callback/call sequence:
        // ------------------------------------------------------------------------------------------------------------
        // Save                           Imported(s)
        // Delete                         Deleted(s)
        // Copy                           Imported(s)
        // Rename                         Imported(d), Deleted(s)
        // Move(drag) / Cut+Paste         Imported(d), Deleted(s)
        // ------------------------------------------------------------------------------------------------------------
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Intantiated through reflection by Unity")]
        private class InputActionAssetPostprocessor : AssetPostprocessor
        {
            private static bool s_DoNotifyEditorsScheduled;
            private static List<string> s_Imported = new List<string>();
            private static List<string> s_Deleted = new List<string>();
            private static List<string> s_Moved = new List<string>();

            private static void Notify(IReadOnlyCollection<string> assets,
                IReadOnlyCollection<IInputActionAssetEditor> editors, Action<IInputActionAssetEditor> callback)
            {
                foreach (var asset in assets)
                {
                    var assetGuid = AssetDatabase.AssetPathToGUID(asset);
                    foreach (var editor in editors)
                    {
                        if (editor.assetGUID != assetGuid)
                            continue;

                        try
                        {
                            callback(editor);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }

            private static void NotifyEditors()
            {
                try
                {
                    // When the asset is modified outside of the editor and the importer settings are
                    // visible in the Inspector the asset references in the importer inspector need to be
                    // force rebuild (otherwise we gets lots of exceptions).
                    ActiveEditorTracker.sharedTracker.ForceRebuild();

                    // Unconditionally find all existing editors regardless of associated asset
                    var editors = InputActionAssetEditor.FindAllEditors();

                    // Abort if there are no available candidate editors
                    if (editors == null || editors.Length == 0)
                        return;

                    // Notify editors about asset changes
                    Notify(s_Imported, editors, (editor) => editor.OnAssetImported());
                    Notify(s_Deleted, editors, (editor) => editor.OnAssetDeleted());
                    Notify(s_Moved, editors, (editor) => editor.OnAssetMoved());
                }
                finally
                {
                    s_Imported.Clear();
                    s_Deleted.Clear();
                    s_Moved.Clear();

                    s_DoNotifyEditorsScheduled = false;
                }
            }

            private static void Process(string[] assets, ICollection<string> target)
            {
                foreach (var asset in assets)
                {
                    // Ignore any assets with non matching extensions
                    if (!InputActionImporter.IsInputActionAssetPath(asset))
                        continue;

                    // Register asset in target collection for delay invocation
                    target.Add(asset);

                    // If a notification execution has already been scheduled do nothing apart from registration.
                    // We do this with delayed execution to avoid excessive updates interfering with ADB.
                    if (!s_DoNotifyEditorsScheduled)
                    {
                        EditorApplication.delayCall += NotifyEditors;
                        s_DoNotifyEditorsScheduled = true;
                    }
                }
            }

            // Note: Callback prior to Unity 2021.2 did not provide a boolean indicating domain relaod.
#if UNITY_2021_2_OR_NEWER
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
#else
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                string[] movedAssets, string[] movedFromAssetPaths)
#endif
            {
                Process(importedAssets, s_Imported);
                Process(deletedAssets, s_Deleted);
                Process(movedAssets, s_Moved);
            }
        }

        #endregion
    }
}
#endif // UNITY_EDITOR
