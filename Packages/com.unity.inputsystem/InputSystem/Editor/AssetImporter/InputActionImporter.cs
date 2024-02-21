#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using UnityEngine.InputSystem.Utilities;

////FIXME: The importer accesses icons through the asset db (which EditorGUIUtility.LoadIcon falls back on) which will
////       not yet have been imported when the project is imported from scratch; this results in errors in the log and in generic
////       icons showing up for the assets

#pragma warning disable 0649
namespace UnityEngine.InputSystem.Editor
{
    /// <summary>
    /// Interface representing an Input Action Asset editor.
    /// </summary>
    internal interface IInputActionsAssetEditor
    {
        /// <summary>
        /// A read-only string representation of the asset GUID associated with the asset being edited.
        /// </summary>
        string assetGUID { get; }

        /// <summary>
        /// Returns whether the editor has unsaved changes compared to the associated source asset.
        /// </summary>
        bool isDirty { get; }

        /// <summary>
        /// Callback triggered when the associated asset is imported.
        /// </summary>
        void OnAssetImported();

        /// <summary>
        /// Callback triggered when the associated asset is moved.
        /// </summary>
        void OnAssetMoved();

        /// <summary>
        /// Callback triggered when the associated asset is deleted.
        /// </summary>
        void OnAssetDeleted();
    }

    /// <summary>
    /// Imports an <see cref="InputActionAsset"/> from JSON.
    /// </summary>
    /// <remarks>
    /// Can generate code wrappers for the contained action sets as a convenience.
    /// Will not overwrite existing wrappers except if the generated code actually differs.
    /// </remarks>
    [ScriptedImporter(kVersion, InputActionAsset.Extension)]
    internal class InputActionImporter : ScriptedImporter
    {
        private const int kVersion = 13;

        [SerializeField] private bool m_GenerateWrapperCode;
        [SerializeField] private string m_WrapperCodePath;
        [SerializeField] private string m_WrapperClassName;
        [SerializeField] private string m_WrapperCodeNamespace;

        private static InlinedArray<Action> s_OnImportCallbacks;

        public static event Action onImport
        {
            add => s_OnImportCallbacks.Append(value);
            remove => s_OnImportCallbacks.Remove(value);
        }

        private static InputActionAsset CreateFromJson(AssetImportContext context)
        {
            ////REVIEW: need to check with version control here?
            // Read JSON file.
            string content;
            try
            {
                content = File.ReadAllText(EditorHelpers.GetPhysicalPath(context.assetPath));
            }
            catch (Exception exception)
            {
                context.LogImportError($"Could not read file '{context.assetPath}' ({exception})");
                return null;
            }

            // Create asset.
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();

            // Parse JSON and configure asset.
            try
            {
                // Attempt to parse JSON
                asset.LoadFromJson(content);

                // Make sure action map names are unique within JSON file
                var names = new HashSet<string>();
                foreach (var map in asset.actionMaps)
                {
                    if (!names.Add(map.name))
                    {
                        throw new Exception(
                            "Unable to parse {context.assetPath} due to duplicate Action Map name: '{map.name}'. Make sure Action Map names are unique within the asset and reattempt import.");
                    }
                }

                // Make sure action names are unique within each action map in JSON file
                names.Clear();
                foreach (var map in asset.actionMaps)
                {
                    foreach (var action in map.actions)
                    {
                        if (!names.Add(action.name))
                        {
                            throw new Exception(
                                $"Unable to parse {{context.assetPath}} due to duplicate Action name: '{action.name}' within Action Map '{map.name}'. Make sure Action Map names are unique within the asset and reattempt import.");
                        }
                    }

                    names.Clear();
                }

                // Force name of asset to be that on the file on disk instead of what may be serialized
                // as the 'name' property in JSON. (Unless explicitly given)
                asset.name = NameFromAssetPath(context.assetPath);

                // Add asset.
                ////REVIEW: the icons won't change if the user changes skin; not sure it makes sense to differentiate here
                context.AddObjectToAsset("<root>", asset, InputActionAssetIconLoader.LoadAssetIcon());
                context.SetMainObject(asset);

                // Make sure all the elements in the asset have GUIDs and that they are indeed unique.
                // Create sub-assets for each action to allow search and editor referencing/picking.
                SetupAsset(asset, context.AddObjectToAsset);
            }
            catch (Exception exception)
            {
                context.LogImportError($"Could not parse input actions in JSON format from '{context.assetPath}' ({exception})");
                DestroyImmediate(asset);
                asset = null;
            }

            return asset;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (ctx == null)
                throw new ArgumentNullException(nameof(ctx));

            foreach (var callback in s_OnImportCallbacks)
                callback();

            var asset = CreateFromJson(ctx);
            if (asset == null)
                return;

            if (m_GenerateWrapperCode)
                GenerateWrapperCode(ctx, asset, m_WrapperCodeNamespace, m_WrapperClassName, m_WrapperCodePath);

            // Refresh editors.
            InputActionEditorWindow.RefreshAllOnAssetReimport();
            // TODO UITK editor window is missing
        }

        internal static void SetupAsset(InputActionAsset asset)
        {
            SetupAsset(asset, (identifier, subAsset, icon) =>
                AssetDatabase.AddObjectToAsset(subAsset, asset));
        }

        private delegate void AddObjectToAsset(string identifier, Object subAsset, Texture2D icon);

        private static void SetupAsset(InputActionAsset asset, AddObjectToAsset addObjectToAsset)
        {
            FixMissingGuids(asset);
            CreateInputActionReferences(asset, addObjectToAsset);
        }

        private static void FixMissingGuids(InputActionAsset asset)
        {
            // Make sure all the elements in the asset have GUIDs and that they are indeed unique.
            foreach (var map in asset.actionMaps)
            {
                // Make sure action map has GUID.
                if (string.IsNullOrEmpty(map.m_Id) || asset.actionMaps.Count(x => x.m_Id == map.m_Id) > 1)
                    map.GenerateId();

                // Make sure all actions have GUIDs.
                foreach (var action in map.actions)
                {
                    var actionId = action.m_Id;
                    if (string.IsNullOrEmpty(actionId) || asset.actionMaps.Sum(m => m.actions.Count(a => a.m_Id == actionId)) > 1)
                        action.GenerateId();
                }

                // Make sure all bindings have GUIDs.
                for (var i = 0; i < map.m_Bindings.LengthSafe(); ++i)
                {
                    var bindingId = map.m_Bindings[i].m_Id;
                    if (string.IsNullOrEmpty(bindingId) || asset.actionMaps.Sum(m => m.bindings.Count(b => b.m_Id == bindingId)) > 1)
                        map.m_Bindings[i].GenerateId();
                }
            }
        }

        private static void CreateInputActionReferences(InputActionAsset asset, AddObjectToAsset addObjectToAsset)
        {
            var actionIcon = InputActionAssetIconLoader.LoadActionIcon();
            foreach (var map in asset.actionMaps)
            {
                foreach (var action in map.actions)
                {
                    var actionReference = ScriptableObject.CreateInstance<InputActionReference>();
                    actionReference.Set(action);
                    addObjectToAsset(action.m_Id, actionReference, actionIcon);

                    // Backwards-compatibility (added for 1.0.0-preview.7).
                    // We used to call AddObjectToAsset using objectName instead of action.m_Id as the object name. This fed
                    // the action name (*and* map name) into the hash generation that was used as the basis for the file ID
                    // object the InputActionReference object. Thus, if the map and/or action name changed, the file ID would
                    // change and existing references to the InputActionReference object would become invalid.
                    //
                    // What we do here is add another *hidden* InputActionReference object with the same content to the
                    // asset. This one will use the old file ID and thus preserve backwards-compatibility. We should be able
                    // to remove this for 2.0.
                    //
                    // Case: https://fogbugz.unity3d.com/f/cases/1229145/
                    var backcompatActionReference = Instantiate(actionReference);
                    backcompatActionReference.name = actionReference.name; // Get rid of the (Clone) suffix.
                    backcompatActionReference.hideFlags = HideFlags.HideInHierarchy;
                    addObjectToAsset(actionReference.name, backcompatActionReference, actionIcon);
                }
            }
        }

        private static void GenerateWrapperCode(AssetImportContext ctx, InputActionAsset asset, string codeNamespace, string codeClassName, string codePath)
        {
            var maps = asset.actionMaps;
            // When using code generation, it is an error for any action map to be named the same as the asset itself.
            // https://fogbugz.unity3d.com/f/cases/1212052/
            var className = !string.IsNullOrEmpty(codeClassName) ? codeClassName : CSharpCodeHelpers.MakeTypeName(asset.name);
            if (maps.Any(x =>
                CSharpCodeHelpers.MakeTypeName(x.name) == className || CSharpCodeHelpers.MakeIdentifier(x.name) == className))
            {
                ctx.LogImportError(
                    $"{asset.name}: An action map in an .inputactions asset cannot be named the same as the asset itself if 'Generate C# Class' is used. "
                    + "You can rename the action map in the asset, rename the asset itself or assign a different C# class name in the import settings.");
                return;
            }

            var wrapperFilePath = codePath;
            if (string.IsNullOrEmpty(wrapperFilePath))
            {
                // Placed next to .inputactions file.
                var assetPath = ctx.assetPath;
                var directory = Path.GetDirectoryName(assetPath);
                var fileName = Path.GetFileNameWithoutExtension(assetPath);
                wrapperFilePath = Path.Combine(directory, fileName) + ".cs";
            }
            else if (wrapperFilePath.StartsWith("./") || wrapperFilePath.StartsWith(".\\") ||
                     wrapperFilePath.StartsWith("../") || wrapperFilePath.StartsWith("..\\"))
            {
                // User-specified file relative to location of .inputactions file.
                var assetPath = ctx.assetPath;
                var directory = Path.GetDirectoryName(assetPath);
                wrapperFilePath = Path.Combine(directory, wrapperFilePath);
            }
            else if (!wrapperFilePath.ToLower().StartsWith("assets/") &&
                     !wrapperFilePath.ToLower().StartsWith("assets\\"))
            {
                // User-specified file in Assets/ folder.
                wrapperFilePath = Path.Combine("Assets", wrapperFilePath);
            }

            var dir = Path.GetDirectoryName(wrapperFilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var options = new InputActionCodeGenerator.Options
            {
                sourceAssetPath = ctx.assetPath,
                namespaceName = codeNamespace,
                className = codeClassName,
            };

            if (InputActionCodeGenerator.GenerateWrapperCode(wrapperFilePath, asset, options))
            {
                // When we generate the wrapper code cs file during asset import, we cannot call ImportAsset on that directly because
                // script assets have to be imported before all other assets, and are not allowed to be added to the import queue during
                // asset import. So instead we register a callback to trigger a delayed asset refresh which should then pick up the
                // changed/added script, and trigger a new import.
                EditorApplication.delayCall += AssetDatabase.Refresh;
            }
        }

#if UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
        internal static IEnumerable<InputActionReference> LoadInputActionReferencesFromAsset(string assetPath)
        {
            // Get all InputActionReferences are stored at the same asset path as InputActionAsset
            // Note we exclude 'hidden' action references (which are present to support one of the pre releases)
            return AssetDatabase.LoadAllAssetsAtPath(assetPath).Where(
                o => o is InputActionReference && !((InputActionReference)o).hideFlags.HasFlag(HideFlags.HideInHierarchy))
                .Cast<InputActionReference>();
        }

        // Get all InputActionReferences from assets in the project. By default it only gets the assets in the "Assets" folder.
        internal static IEnumerable<InputActionReference> LoadInputActionReferencesFromAssetDatabase(string[] foldersPath = null, bool skipProjectWide = false)
        {
            string[] searchFolders = null;
            // If folderPath is null, search in "Assets" folder.
            if (foldersPath == null)
            {
                searchFolders = new string[] { "Assets" };
            }

            // Get all InputActionReference from assets in "Asset" folder. It does not search inside "Packages" folder.
            var inputActionReferenceGUIDs = AssetDatabase.FindAssets($"t:{typeof(InputActionReference).Name}", searchFolders);

            // To find all the InputActionReferences, the GUID of the asset containing at least one action reference is
            // used to find the asset path. This is because InputActionReferences are stored in the asset database as sub-assets of InputActionAsset.
            // Then the whole asset is loaded and all the InputActionReferences are extracted from it.
            // Also, the action references are duplicated to have backwards compatibility with the 1.0.0-preview.7. That
            // is why we look for references withouth the `HideFlags.HideInHierarchy` flag.
            var inputActionReferencesList = new List<InputActionReference>();
            foreach (var guid in inputActionReferenceGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var assetInputActionReferenceList = LoadInputActionReferencesFromAsset(assetPath).ToList();

                if (skipProjectWide && assetInputActionReferenceList.Count() > 0)
                {
                    if (assetInputActionReferenceList[0].m_Asset == InputSystem.actions)
                        continue;
                }

                inputActionReferencesList.AddRange(assetInputActionReferenceList);
            }
            return inputActionReferencesList;
        }

#endif

        // Add item to plop an .inputactions asset into the project.
        [MenuItem("Assets/Create/Input Actions")]
        public static void CreateInputAsset()
        {
            ProjectWindowUtil.CreateAssetWithContent("New Controls." + InputActionAsset.Extension,
                InputActionAsset.kDefaultAssetLayoutJson, InputActionAssetIconLoader.LoadAssetIcon());
        }

        // File extension of the associated asset
        private const string kFileExtension = "." + InputActionAsset.Extension;

        // Evaluates whether the given path is a path to an asset of the associated type based on extension.
        public static bool IsInputActionAssetPath(string path)
        {
            return path.EndsWith(kFileExtension, StringComparison.InvariantCultureIgnoreCase);
        }

        public static string NameFromAssetPath(string assetPath)
        {
            Debug.Assert(IsInputActionAssetPath(assetPath));
            return Path.GetFileNameWithoutExtension(assetPath);
        }

        private static IInputActionsAssetEditor FindEditorForAssetWithGuid<T>(string guid) where T : EditorWindow
        {
            // Currently assumes there is only one instance per type
            var result = new List<IInputActionsAssetEditor>();
            var editors = FindAllEditors<T>();
            return editors.FirstOrDefault(w => w.assetGUID == guid);
        }

        private static IInputActionsAssetEditor FindEditorForAssetPath<T>(string path) where T : EditorWindow
        {
            return FindEditorForAssetWithGuid<T>(AssetDatabase.AssetPathToGUID(path));
        }

        private static List<IInputActionsAssetEditor> FindAllEditors<T>(Predicate<IInputActionsAssetEditor> predicate = null, List<IInputActionsAssetEditor> result = null) where T : EditorWindow
        {
            return FindAllEditors(typeof(T), predicate, result);
        }
        
        private static List<IInputActionsAssetEditor> FindAllEditors(Type type, Predicate<IInputActionsAssetEditor> predicate = null, List<IInputActionsAssetEditor> result = null)
        {
            result ??= new List<IInputActionsAssetEditor>();
            var editors = Resources.FindObjectsOfTypeAll(type);
            foreach (var editor in editors)
            {
                if (editor is IInputActionsAssetEditor actionsAssetEditor && (predicate == null || predicate(actionsAssetEditor)))
                    result.Add(actionsAssetEditor);
            }
            return result;
        }

        private static List<IInputActionsAssetEditor> FindAllEditorsForAssetPath<T>(string assetPath, List<IInputActionsAssetEditor> result = null) where T : EditorWindow
        {
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            return FindAllEditors<T>((editor) => editor.assetGUID == assetGuid, result);
        }

        // Scenarios:
        // - Input Action Editor is open with an unmodified asset and ...
        //   ... user deletes an asset. Prompt user whether to delete asset or not. If deleted, close asset in editor.
        //   ... user moves an asset. Moving the file should not affect content of the file.

        // Asset modification processor designed to handle the following scenarios:
        // - When an asset is about to get deleted, evaluate if there is a pending unsaved edited copy of the asset
        //   and in this case, prompt the user that there are unsaved changes and allow the user to cancel the operation
        //   and allow to save the pending changes or confirm to delete the asset and discard the pending unsaved changes.
        // - If the asset being deleted is unmodified, no dialog prompt is displayed and the asset is deleted.
        private class InputActionAssetModificationProcessor : AssetModificationProcessor
        {
            // TODO This will yield +2 dialogs which may be seen as disruptive UX. It also adds complexity. Check how other assets handle this situation.
            [System.Diagnostics.CodeAnalysis.SuppressMessage(category: "Microsoft.Usage",
                checkId: "CA1801:ReviewUnusedParameters",
                MessageId = "options",
                Justification = "options parameter required by Unity API")]
            public static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
            {
                if (InputActionImporter.IsInputActionAssetPath(path))
                {
                    // Find GUID uniquely identifying asset at path
                    var window = FindEditorForAssetPath<InputActionsAssetEditorWindow>(path);
                    if (window != null)
                    {
                        // If there's unsaved changes, ask for confirmation to either abort or delete.
                        var forceQuit = false;
                        if (window.isDirty)
                        {
                            var result = InputActionsEditorWindowUtils.ConfirmDeleteAssetWithUnsavedChanges(path);
                            if (result == InputActionsEditorWindowUtils.DialogResult.Cancel)
                            {
                                // User canceled. Stop the deletion.
                                return AssetDeleteResult.FailedDelete;
                            }

                            forceQuit = true;
                        }

                        // Note only valid for internal editor operations
                        //window.Dismiss(forceQuit);
                        //window.Refresh();
                    }
                }

                return default;
            }

            public static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath)
            {
                if (InputActionImporter.IsInputActionAssetPath(sourcePath))
                {
                    Debug.Log("OnWillMoveAsset: " + sourcePath + " to " + destinationPath);

                    var window = FindEditorForAssetPath<InputActionsAssetEditorWindow>(sourcePath);
                    if (window != null)
                    {
                        // TODO Note that its only valid for internal editor operations
                        //window.OnMove();
                    }
                }

                return default;
            }
        }

        // Regarding https://issuetracker.unity3d.com/product/unity/issues/guid/ISXB-749
        //
        // When an action asset is renamed, copied, or moved in the Editor, the "Name" field in the JSON will
        // hold the old name and won't match the asset objects name in memory which is set based on the filename
        // by the scripted imported. To avoid this, this asset post-processor detects any imported or moved assets
        // with a JSON name property not matching the importer assigned name and updates the JSON name based on this.
        // This basically solves any problem related to unmodified assets.
        //
        // In addition to the above, if the asset is also open in an editor, the following applies:
        // a) If the asset is unmodified in the editor, and the associated asset is renamed, copied or moved in the
        //    Editor, the editor in-memory version of the asset is also updated to reflect content from disc.
        // b) If the asset is modified in the editor, and the associated asset is renamed, copied or moved in the
        //    Editor, the editor in-memory version of the asset is not updated from from disc. The user may then...
        //    a.1) ...discard any modifications, the asset is already valid and names are in sync.
        //    a.2) ...save modifications, the asset is saved with a JSON name derived from the asset path leaving
        //         the names in sync. To avoid running into a situation where the editor believes it sees additional
        //         changes coming from disk, it is recommended that editor implementations do not compare JSON names
        //         when determining if an asset has been modified or not.
        //
        // For clarity, the tables below indicate the callback sequences of the asset modification processor and
        // asset post-processor for various user operations done on assets.
        //
        // User operation:                Callback sequence:
        // ----------------------------------------------------------------------------------------
        // Save                           Imported(s)
        // Delete                         OnWillDelete(s), Deleted(s)
        // Copy                           Imported(s)
        // Rename                         OnWillMove(s,d), Imported(d), Moved(s,d)
        // Move (drag) / Cut+Paste        OnWillMove(s,d), Moved(s,d)
        // ------------------------------------------------------------------------------------------------------------
        //
        // User operation:                Callback/call sequence:
        // ------------------------------------------------------------------------------------------------------------
        // Save                           Imported(s)
        // Delete                         OnWillDelete(s), Deleted(s)
        // Copy                           Imported(s), Fix(s), Imported(s)
        // Rename                         OnWillMove(s,d), Imported(d), Fix(d), Moved(s,d), Imported(d)
        // Move(drag) / Cut+Paste         OnWillMove(s,d), Moved(s,d)
        // ------------------------------------------------------------------------------------------------------------
        // Note that as stated in table above, JSON name changes (called "Fix" above) will only be executed when either
        // Copying, Renaming within the editor. For all other operations the name and file name would not differ.
        //
        // External user operation:       Callback/call sequence:
        // ------------------------------------------------------------------------------------------------------------
        // Save                           Imported(s)
        // Delete                         Deleted(s)
        // Copy                           Imported(s)
        // Rename                         Imported(d), Deleted(s)
        // Move(drag) / Cut+Paste         Imported(d), Deleted(s)
        // ------------------------------------------------------------------------------------------------------------
        
        public static void RegisterTypeForAssetNotifications<T>() where T : EditorWindow, IInputActionsAssetEditor
        {
            InputActionAssetPostprocessor.RegisterType<T>();
        }

        public static void UnregisterTypeForAssetNotifications<T>() where T : EditorWindow, IInputActionsAssetEditor
        {
            InputActionAssetPostprocessor.UnregisterType<T>();
        }
        
        private class InputActionAssetPostprocessor : AssetPostprocessor
        {
            private static bool s_DoNotifyEditorsScheduled;
            private static readonly List<Type> s_EditorTypes = new List<Type>();
            private static List<string> s_Imported = new List<string>();
            private static List<string> s_Deleted = new List<string>();
            private static List<string> s_Moved = new List<string>();

            // Registers an editor type for receiving notifications of asset modifications if the editor is open.
            internal static void RegisterType<T>() where T : EditorWindow, IInputActionsAssetEditor
            {
                if (!s_EditorTypes.Contains(typeof(T)))
                    s_EditorTypes.Add(typeof(T));
            }
            
            // Unregisters a previously registered type from receiving notifications of asset modifications if the
            // editor is open.
            internal static void UnregisterType<T>() where T : EditorWindow, IInputActionsAssetEditor
            {
                s_EditorTypes.Remove(typeof(T));
            }

            private static void Notify(IReadOnlyCollection<string> assets, 
                IReadOnlyCollection<IInputActionsAssetEditor> editors, Action<IInputActionsAssetEditor> callback)
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

            private static void DoNotifyEditors()
            {
                // When the asset is modified outside of the editor and the importer settings are visible in the
                // Inspector the asset references in the importer inspector need to be force rebuild
                // (otherwise we gets lots of exceptions).
                ActiveEditorTracker.sharedTracker.ForceRebuild();

                // Unconditionally find all editors from registered types regardless of associated asset
                List<IInputActionsAssetEditor> editors = null;
                foreach (var type in s_EditorTypes)
                    editors = FindAllEditors(type, null, editors);
                
                // Abort if there are no available candidate editors 
                if (editors == null)
                    return;

                // Notify editors
                Notify(s_Imported, editors, (editor) => editor.OnAssetImported());
                Notify(s_Deleted, editors, (editor) => editor.OnAssetDeleted());
                Notify(s_Moved, editors, (editor) => editor.OnAssetMoved());
            }

            private static void Process(string[] assets, ICollection<string> target)
            {
                foreach (var asset in assets)
                {
                    if (!IsInputActionAssetPath(asset))
                        continue;
                    target.Add(asset);
                    
                    // If a notification execution has already been scheduled do nothing.
                    // We do this with delayed execution to avoid excessive updates interfering with ADB.
                    if (!s_DoNotifyEditorsScheduled)
                    {
                        EditorApplication.delayCall += () =>
                        {
                            try
                            {
                                DoNotifyEditors();
                            }
                            finally
                            {
                                target.Clear();
                                s_DoNotifyEditorsScheduled = false;
                            }
                        };
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
        
        // This processor was added to adress this issue:
        // https://issuetracker.unity3d.com/product/unity/issues/guid/ISXB-749
        //
        // When an action asset is renamed, copied, or moved in the Editor, the "Name" field in the JSON will
        // hold the old name and won't match the asset objects name in memory which is set based on the filename
        // by the scripted imported. To avoid this, this asset post-processor detects any imported or moved assets
        // with a JSON name property not matching the importer assigned name and updates the JSON name based on this.
        // This basically solves any problem related to unmodified assets.
        //
        // Note that JSON names have no relevance for editor workflows and are basically ignored by the importer.
        // Note that JSON names may be the only way to identify assets loaded from non-file sources or via 
        // UnityEngine.Resources in run-time.
        //
        // In addition to the above, if the asset is also open in an editor, the following applies:
        // a) If the asset is unmodified in the editor, and the associated asset is renamed, copied or moved in the
        //    Editor, the editor in-memory version of the asset is also updated to reflect content from disc.
        // b) If the asset is modified in the editor, and the associated asset is renamed, copied or moved in the
        //    Editor, the editor in-memory version of the asset is not updated from from disc. The user may then...
        //    a.1) ...discard any modifications, the asset is already valid and names are in sync.
        //    a.2) ...save modifications, the asset is saved with a JSON name derived from the asset path leaving
        //         the names in sync. To avoid running into a situation where the editor believes it sees additional
        //         changes coming from disk, it is recommended that editor implementations do not compare JSON names
        //         when determining if an asset has been modified or not.
        //
        // For clarity, the tables below indicate the callback sequences of the asset modification processor and
        // asset post-processor for various user operations done on assets.
        //
        // User operation:                Callback sequence:
        // ----------------------------------------------------------------------------------------
        // Save                           Imported(s)
        // Delete                         OnWillDelete(s), Deleted(s)
        // Copy                           Imported(s)
        // Rename                         OnWillMove(s,d), Imported(d), Moved(s,d)
        // Move (drag) / Cut+Paste        OnWillMove(s,d), Moved(s,d)
        // ------------------------------------------------------------------------------------------------------------
        //
        // User operation:                Callback/call sequence:
        // ------------------------------------------------------------------------------------------------------------
        // Save                           Imported(s)
        // Delete                         OnWillDelete(s), Deleted(s)
        // Copy                           Imported(s), Fix(s), Imported(s)
        // Rename                         OnWillMove(s,d), Imported(d), Fix(d), Moved(s,d), Imported(d)
        // Move(drag) / Cut+Paste         OnWillMove(s,d), Moved(s,d)
        // ------------------------------------------------------------------------------------------------------------
        // Note that as stated in table above, JSON name changes (called "Fix" above) will only be executed when either
        // Copying, Renaming within the editor. For all other operations the name and file name would not differ.
        //
        // External user operation:       Callback/call sequence:
        // ------------------------------------------------------------------------------------------------------------
        // Save                           Imported(s)
        // Delete                         Deleted(s)
        // Copy                           Imported(s)
        // Rename                         Imported(d), Deleted(s)
        // Move(drag) / Cut+Paste         Imported(d), Deleted(s)
        // ------------------------------------------------------------------------------------------------------------
        private class InputActionJsonNameModifierAssetProcessor : AssetPostprocessor
        {
            // Note: Callback prior to Unity 2021.2 did not provide a boolean indicating domain relaod.
#if UNITY_2021_2_OR_NEWER
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
#else
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
                string[] movedAssets, string[] movedFromAssetPaths)
#endif
            {
                foreach (var assetPath in importedAssets)
                {
                    if (IsInputActionAssetPath(assetPath))
                        CheckAndRenameJsonNameIfDifferent(assetPath);
                }
            }

            private static void CheckAndRenameJsonNameIfDifferent(string assetPath)
            {
                InputActionAsset asset = null;
                try
                {
                    asset = InputActionAsset.FromJson(File.ReadAllText(assetPath));
                    var desiredName = Path.GetFileNameWithoutExtension(assetPath);
                    if (asset.name == desiredName)
                        return;
                    asset.name = desiredName;
                    if (!InputActionAssetManager.WriteAsset(assetPath, asset.ToJson()))
                    {
                        Debug.LogError($"Unable to change JSON name for asset at \"{assetPath}\" since the asset-path could not be checked-out as editable in the underlying version-control system.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                finally
                {
                    if (asset != null)
                        DestroyImmediate(asset);
                }
            }
        }
    }
}

#endif // UNITY_EDITOR
