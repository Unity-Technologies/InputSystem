// UITK TreeView is not supported in earlier versions
// Therefore the UITK version of the InputActionAsset Editor is not available on earlier Editor versions either.
#if UNITY_EDITOR && UNITY_INPUT_SYSTEM_PROJECT_WIDE_ACTIONS
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.ShortcutManagement;

namespace UnityEngine.InputSystem.Editor
{
    // TODO: Remove when UIToolkit editor is complete and set as the default editor
    [InitializeOnLoad]
    internal static class EnableUITKEditor
    {
        static EnableUITKEditor()
        {
        }
    }

    internal class InputActionsAssetEditorWindow : EditorWindow, IInputActionAssetEditor
    {
        // Register editor type via static constructor to enable asset monitoring
        static InputActionsAssetEditorWindow()
        {
            InputActionAssetEditor.RegisterType<InputActionsAssetEditorWindow>();
        }

        // TODO Consider moving state into its own struct so it can just be assigned or reset

        static readonly Vector2 k_MinWindowSize = new Vector2(650, 450);

        [SerializeField] private InputActionAsset m_AssetObjectForEditing;
        [SerializeField] private InputActionsEditorState m_State;
        [SerializeField] private string m_AssetGUID;

        private int m_AssetId;
        private string m_AssetJson;
        private bool m_IsDirty;
        private StateContainer m_StateContainer;

        static readonly Vector2 k_MinWindowSize = new Vector2(650, 450);

        [OnOpenAsset]
        public static bool OpenAsset(int instanceId, int line)
        {
            if (InputSystem.settings.IsFeatureEnabled(InputFeatureNames.kUseIMGUIEditorForAssets))
                return false;
            if (!InputActionImporter.IsInputActionAssetPath(AssetDatabase.GetAssetPath(instanceId)))
                return false;

            // Grab InputActionAsset.
            // NOTE: We defer checking out an asset until we save it. This allows a user to open an .inputactions asset and look at it
            //       without forcing a checkout.
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            var asset = obj as InputActionAsset;

            string actionMapToSelect = null;
            string actionToSelect = null;

            // Means we're dealing with an InputActionReference, e.g. when expanding the an .input action asset
            // on the Asset window and selecting an Action.
            if (asset == null)
            {
                var actionReference = obj as InputActionReference;
                if (actionReference != null && actionReference.asset != null)
                {
                    asset = actionReference.asset;
                    actionMapToSelect = actionReference.action.actionMap?.name;
                    actionToSelect = actionReference.action?.name;
                }
                else
                {
                    return false;
                }
            }

            OpenWindow(asset, actionMapToSelect, actionToSelect);
            return true;
        }

        private static InputActionsAssetEditorWindow OpenWindow(InputActionAsset asset, string actionMapToSelect = null, string actionToSelect = null)
        {
            ////REVIEW: It'd be great if the window got docked by default but the public EditorWindow API doesn't allow that
            ////        to be done for windows that aren't singletons (GetWindow<T>() will only create one window and it's the
            ////        only way to get programmatic docking with the current API).
            // See if we have an existing editor window that has the asset open.
            var instanceId = asset.GetInstanceID();
            var window = GetOrCreateWindow(instanceId, out var isAlreadyOpened);
            if (isAlreadyOpened)
            {
                window.Focus();
                return window;
            }

            window.m_IsDirty = false;
            window.m_AssetId = instanceId;
            window.minSize = k_MinWindowSize;
            window.SetAsset(asset, actionToSelect, actionMapToSelect);
            window.Show();

            return window;
        }

        /// <summary>
        /// Open the specified <paramref name="asset"/> in an editor window. Used when someone hits the "Edit Asset" button in the
        /// importer inspector.
        /// </summary>
        /// <param name="asset">The InputActionAsset to open.</param>
        /// <returns>The editor window.</returns>
        public static InputActionsAssetEditorWindow OpenEditor(InputActionAsset asset)
        {
            return OpenWindow(asset, null, null);
        }

        private static InputActionsAssetEditorWindow GetOrCreateWindow(int id, out bool isAlreadyOpened)
        {
            isAlreadyOpened = false;
            if (HasOpenInstances<InputActionsAssetEditorWindow>())
            {
                var openWindows = Resources.FindObjectsOfTypeAll(typeof(InputActionsAssetEditorWindow)) as InputActionsAssetEditorWindow[];
                var alreadyOpenWindow = openWindows?.ToList().FirstOrDefault(window => window.m_AssetId.Equals(id));
                isAlreadyOpened = alreadyOpenWindow != null;
                return isAlreadyOpened ? alreadyOpenWindow : CreateWindow<InputActionsAssetEditorWindow>();
            }
            return GetWindow<InputActionsAssetEditorWindow>();
        }

        private static GUIContent GetEditorTitle(InputActionAsset asset, bool isDirty)
        {
            var text = asset.name + " (Input Actions Editor)";
            if (isDirty)
                text = "(*) " + text;
            return new GUIContent(text);
        }

        private void SetAsset(InputActionAsset asset, string actionToSelect = null, string actionMapToSelect = null)
        {
            var existingWorkingCopy = m_AssetObjectForEditing;

            try
            {
                // Obtain and persist GUID for the associated asset
                Debug.Assert(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out m_AssetGUID, out long _),
                    $"Failed to get asset {asset.name} GUID");

                // Attempt to update editor and internals based on associated asset
                if (!TryUpdateFromAsset())
                    return;

                // Select the action that was selected on the Asset window.
                if (actionMapToSelect != null && actionToSelect != null)
                {
                    m_State = m_State.SelectActionMap(actionMapToSelect);
                    m_State = m_State.SelectAction(actionToSelect);
                }

                BuildUI();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                if (existingWorkingCopy != null)
                    DestroyImmediate(existingWorkingCopy);
            }
        }

        private void CreateGUI() // Only domain reload
        {
            // When opening the window for the first time there will be no state or asset yet.
            // In that case, we don't do anything as SetAsset() will be called later and at that point the UI can be created.
            // Here we only recreate the UI e.g. after a domain reload.
            if (string.IsNullOrEmpty(m_AssetGUID))
                return;

            // After domain reloads the state will be in a invalid state as some of the fields
            // cannot be serialized and will become null.
            // Therefore we recreate the state here using the fields which were saved.
            if (m_State.serializedObject == null && !TryUpdateFromAsset())
                return;

            BuildUI();
        }

        private void CleanupStateContainer()
        {
            if (m_StateContainer != null)
            {
                m_StateContainer.StateChanged -= OnStateChanged;
                m_StateContainer = null;
            }
        }

        private void BuildUI()
        {
            CleanupStateContainer();

            m_StateContainer = new StateContainer(rootVisualElement, m_State);
            m_StateContainer.StateChanged += OnStateChanged;

            rootVisualElement.Clear();
            if (!rootVisualElement.styleSheets.Contains(InputActionsEditorWindowUtils.theme))
                rootVisualElement.styleSheets.Add(InputActionsEditorWindowUtils.theme);
            var view = new InputActionsEditorView(rootVisualElement, m_StateContainer, false, Save);

            m_StateContainer.Initialize();
        }

        private void OnStateChanged(InputActionsEditorState newState)
        {
            DirtyInputActionsEditorWindow(newState);
            m_State = newState;

            #if UNITY_INPUT_SYSTEM_INPUT_ACTIONS_EDITOR_AUTO_SAVE_ON_FOCUS_LOST
            // No action taken apart from setting dirty flag, auto-save triggered as part of having a dirty asset
            // and editor loosing focus instead.
            #else
            if (InputEditorUserSettings.autoSaveInputActionAssets)
                Save();
            #endif
        }

        private void UpdateWindowTitle()
        {
            titleContent = GetEditorTitle(GetEditedAsset(), m_IsDirty);
        }

        private InputActionAsset GetEditedAsset()
        {
            return m_State.serializedObject.targetObject as InputActionAsset;
        }

        private void Save()
        {
            var path = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            if (InputActionAssetManager.SaveAsset(path, GetEditedAsset().ToJson()))
                TryUpdateFromAsset();
        }

        private bool HasContentChanged()
        {
            var editedAsset = GetEditedAsset();
            var editedAssetJson = InputActionsEditorWindowUtils.ToJsonWithoutName(editedAsset);
            return editedAssetJson != m_AssetJson;
        }

        private void DirtyInputActionsEditorWindow(InputActionsEditorState newState)
        {
            #if UNITY_INPUT_SYSTEM_INPUT_ACTIONS_EDITOR_AUTO_SAVE_ON_FOCUS_LOST
            // Window is dirty is equivalent to if asset has changed
            var isWindowDirty = HasContentChanged();
            #else
            // Window is dirty is never true since every change is auto-saved
            var isWindowDirty = !InputEditorUserSettings.autoSaveInputActionAssets && HasContentChanged();
            #endif

            if (m_IsDirty == isWindowDirty)
                return;

            m_IsDirty = isWindowDirty;
            UpdateWindowTitle();
        }

        private void OnLostFocus()
        {
            // Auto-save triggers on focus-lost instead of on every change
            #if UNITY_INPUT_SYSTEM_INPUT_ACTIONS_EDITOR_AUTO_SAVE_ON_FOCUS_LOST
            if (InputEditorUserSettings.autoSaveInputActionAssets && m_IsDirty)
                Save();
            #endif
        }

        private void HandleOnDestroy()
        {
            // Do we have unsaved changes that we need to ask the user to save or discard?
            if (!m_IsDirty)
                return;

            // Get target asset path from GUID, if this fails file no longer exists and we need to abort.
            var assetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            if (string.IsNullOrEmpty(assetPath))
                return;

            // Prompt user with a dialog
            var result = Dialog.InputActionAsset.ShowSaveChanges(assetPath);
            switch (result)
            {
                case Dialog.Result.Save:
                    Save();
                    break;
                case Dialog.Result.Cancel:
                    // Cancel editor quit. (open new editor window with the edited asset)
                    ReshowEditorWindowWithUnsavedChanges();
                    break;
                case Dialog.Result.Discard:
                    // Don't save, quit - reload the old asset from the json to prevent the asset from being dirtied
                    // TODO I doubt this is needed since we were operating on a copy?!
                    //AssetDatabase.ImportAsset(assetPath);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }
        }

        private void OnDestroy()
        {
            HandleOnDestroy();

            // Clean-up
            CleanupStateContainer();
            if (m_AssetObjectForEditing != null)
                DestroyImmediate(m_AssetObjectForEditing);
        }

        private void ReshowEditorWindowWithUnsavedChanges()
        {
            var window = CreateWindow<InputActionsAssetEditorWindow>();
            CopyOldStatsToNewWindow(window);
            window.BuildUI();
            window.Show();
        }

        private void CopyOldStatsToNewWindow(InputActionsAssetEditorWindow window)
        {
            // TODO This cannot be fully correct?
            window.m_AssetId = m_AssetId;
            window.m_State = m_State;
            window.m_AssetJson = m_AssetJson;
            window.m_IsDirty = true;
        }

        private bool TryUpdateFromAsset()
        {
            Debug.Assert(!string.IsNullOrEmpty(m_AssetGUID), "Asset GUID is empty");
            var assetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            if (assetPath == null)
            {
                Debug.LogWarning(
                    $"Failed to open InputActionAsset with GUID {m_AssetGUID}. The asset might have been deleted.");
                return false;
            }

            InputActionAsset workingCopy = null;
            try
            {
                var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
                workingCopy = InputActionAssetManager.CreateWorkingCopy(asset);
                m_AssetJson = InputActionsEditorWindowUtils.ToJsonWithoutName(asset);
                m_IsDirty = false;
                m_State = new InputActionsEditorState(m_State, new SerializedObject(workingCopy));
            }
            catch (Exception e)
            {
                if (workingCopy != null)
                    DestroyImmediate(workingCopy);
                Debug.LogException(e);
                Close();
                return false;
            }

            m_AssetObjectForEditing = workingCopy;
            UpdateWindowTitle();

            return true;
        }

        #region IInputActionEditorWindow

        public string assetGUID => m_AssetGUID;
        public bool isDirty => m_IsDirty;

        public void OnAssetMoved()
        {
            // When an asset is moved, we only need to update window title since content is unchanged
            UpdateWindowTitle();
        }

        public void OnAssetDeleted()
        {
            // When associated asset is deleted on disk, just close the editor, but also mark the editor
            // as not being dirty to avoid prompting the user to save changes.
            m_IsDirty = false;
            Close();
        }

        public void OnAssetImported()
        {
            // If the editor has pending changes done by the user and the contents changes on disc, there
            // is not much we can do about it but to ignore loading the changes. If the editors asset is
            // unmodified, we can refresh the editor with the latest content from disc.
            if (m_IsDirty)
                return;

            // If our asset has disappeared from disk, just close the window.
            var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
            if (string.IsNullOrEmpty(assetPath))
            {
                m_IsDirty = false; // Avoid checks
                Close();
                return;
            }

            SetAsset(AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath));
        }

        #endregion

        #region Shortcuts
        [Shortcut("Input Action Editor/Save", typeof(InputActionsEditorWindow), KeyCode.S, ShortcutModifiers.Action)]
        private static void SaveShortcut(ShortcutArguments arguments)
        {
            var window = (InputActionsEditorWindow)arguments.context;
            window.Save();
        }

        [Shortcut("Input Action Editor/Add Action Map", typeof(InputActionsEditorWindow), KeyCode.M, ShortcutModifiers.Alt)]
        private static void AddActionMapShortcut(ShortcutArguments arguments)
        {
            var window = (InputActionsEditorWindow)arguments.context;
            window.m_StateContainer.Dispatch(Commands.AddActionMap());
        }

        [Shortcut("Input Action Editor/Add Action", typeof(InputActionsEditorWindow), KeyCode.A, ShortcutModifiers.Alt)]
        private static void AddActionShortcut(ShortcutArguments arguments)
        {
            var window = (InputActionsEditorWindow)arguments.context;
            window.m_StateContainer.Dispatch(Commands.AddAction());
        }

        [Shortcut("Input Action Editor/Add Binding", typeof(InputActionsEditorWindow), KeyCode.B, ShortcutModifiers.Alt)]
        private static void AddBindingShortcut(ShortcutArguments arguments)
        {
            var window = (InputActionsEditorWindow)arguments.context;
            window.m_StateContainer.Dispatch(Commands.AddBinding());
        }
        #endregion
    }
}

#endif
