#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.ShortcutManagement;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Editor
{
    internal class InputActionsEditorWindow : EditorWindow, IInputActionAssetEditor
    {
        // Register editor type via static constructor to enable asset monitoring
        static InputActionsEditorWindow()
        {
            InputActionAssetEditor.RegisterType<InputActionsEditorWindow>();
        }

        static readonly Vector2 k_MinWindowSize = new Vector2(740, 450);
        // For UI testing purpose
        internal InputActionAsset currentAssetInEditor => m_AssetObjectForEditing;
        [SerializeField] private InputActionAsset m_AssetObjectForEditing;
        [SerializeField] private InputActionsEditorState m_State;
        [SerializeField] private string m_AssetGUID;

        private string m_AssetJson;
        private bool m_IsDirty;

        private StateContainer m_StateContainer;
        private InputActionsEditorView m_View;

        private InputActionsEditorSessionAnalytic m_Analytics;

        private InputActionsEditorSessionAnalytic analytics =>
            m_Analytics ??= new InputActionsEditorSessionAnalytic(
                InputActionsEditorSessionAnalytic.Data.Kind.EditorWindow);

        // Unity 6.3 changed signature of OpenAsset, and now it accepts entity id instead of instance id.
        [OnOpenAsset]
#if UNITY_6000_3_OR_NEWER
        public static bool OpenAsset(EntityId entityId, int line)
        {
            if (!InputActionImporter.IsInputActionAssetPath(AssetDatabase.GetAssetPath(entityId)))
                return false;

            return OpenAsset(EditorUtility.EntityIdToObject(entityId));
        }

#else
        public static bool OpenAsset(int instanceId, int line)
        {
            if (!InputActionImporter.IsInputActionAssetPath(AssetDatabase.GetAssetPath(instanceId)))
                return false;

            return OpenAsset(EditorUtility.InstanceIDToObject(instanceId));
        }

#endif

        private static bool OpenAsset(Object obj)
        {
            // Grab InputActionAsset.
            // NOTE: We defer checking out an asset until we save it. This allows a user to open an .inputactions asset and look at it
            //       without forcing a checkout.
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

        private static InputActionsEditorWindow OpenWindow(InputActionAsset asset, string actionMapToSelect = null, string actionToSelect = null)
        {
            ////REVIEW: It'd be great if the window got docked by default but the public EditorWindow API doesn't allow that
            ////        to be done for windows that aren't singletons (GetWindow<T>() will only create one window and it's the
            ////        only way to get programmatic docking with the current API).
            // See if we have an existing editor window that has the asset open.
            var existingWindow = InputActionAssetEditor.FindOpenEditor<InputActionsEditorWindow>(AssetDatabase.GetAssetPath(asset));
            if (existingWindow != null)
            {
                existingWindow.Focus();
                return existingWindow;
            }

            var window = GetWindow<InputActionsEditorWindow>();
            if (window.m_IsDirty)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(window.m_AssetGUID);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    // Prompt user with a dialog
                    var result = Dialog.InputActionAsset.ShowSaveChanges(assetPath);
                    switch (result)
                    {
                        case Dialog.Result.Save:
                            window.Save(isAutoSave: false);
                            break;
                        case Dialog.Result.Cancel:
                            return window;
                        case Dialog.Result.Discard:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(result));
                    }
                }
            }

            window.m_IsDirty = false;
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
        public static InputActionsEditorWindow OpenEditor(InputActionAsset asset)
        {
            return OpenWindow(asset, null, null);
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
            if (m_State.serializedObject == null)
            {
                InputActionAsset workingCopy = null;
                try
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
                    var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);

                    if (asset == null)
                        throw new Exception($"Failed to load asset \"{assetPath}\". The file may have been deleted or moved.");

                    m_AssetJson = InputActionsEditorWindowUtils.ToJsonWithoutName(asset);

                    if (m_AssetObjectForEditing == null)
                    {
                        workingCopy = InputActionAssetManager.CreateWorkingCopy(asset);
                        if (m_State.m_Analytics == null)
                            m_State.m_Analytics = analytics;
                        m_State = new InputActionsEditorState(m_State, new SerializedObject(workingCopy));
                        m_AssetObjectForEditing = workingCopy;
                    }
                    else
                        m_State = new InputActionsEditorState(m_State, new SerializedObject(m_AssetObjectForEditing));
                    m_IsDirty = HasContentChanged();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    if (workingCopy != null)
                        DestroyImmediate(workingCopy);
                    Close();
                    return;
                }
            }

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

            if (m_State.m_Analytics == null)
                m_State.m_Analytics = m_Analytics;

            m_StateContainer = new StateContainer(m_State, m_AssetGUID);
            m_StateContainer.StateChanged += OnStateChanged;

            rootVisualElement.Clear();
            if (!rootVisualElement.styleSheets.Contains(InputActionsEditorWindowUtils.theme))
                rootVisualElement.styleSheets.Add(InputActionsEditorWindowUtils.theme);
            m_View = new InputActionsEditorView(rootVisualElement, m_StateContainer, false, () => Save(isAutoSave: false));

            m_StateContainer.Initialize(rootVisualElement.Q("action-editor"));
        }

        private void OnStateChanged(InputActionsEditorState newState, UIRebuildMode editorRebuildMode)
        {
            DirtyInputActionsEditorWindow(newState);
            m_State = newState;
        }

        private void UpdateWindowTitle()
        {
            titleContent = GetEditorTitle(GetEditedAsset(), m_IsDirty);
        }

        private InputActionAsset GetEditedAsset()
        {
            return m_State.serializedObject.targetObject as InputActionAsset;
        }

        private void Save(bool isAutoSave)
        {
            var path = AssetDatabase.GUIDToAssetPath(m_AssetGUID);

            var projectWideActions = InputSystem.actions;
            if (projectWideActions != null && path == AssetDatabase.GetAssetPath(projectWideActions))
                ProjectWideActionsAsset.Verify(GetEditedAsset());

            if (InputActionAssetManager.SaveAsset(path, GetEditedAsset().ToJson()))
                TryUpdateFromAsset();

            if (isAutoSave)
                analytics.RegisterAutoSave();
            else
                analytics.RegisterExplicitSave();
        }

        private bool HasContentChanged()
        {
            var editedAsset = GetEditedAsset();
            var editedAssetJson = InputActionsEditorWindowUtils.ToJsonWithoutName(editedAsset);
            return editedAssetJson != m_AssetJson;
        }

        private void DirtyInputActionsEditorWindow(InputActionsEditorState newState)
        {
            var isWindowDirty = HasContentChanged();

            if (m_IsDirty == isWindowDirty)
                return;

            m_IsDirty = isWindowDirty;
            UpdateWindowTitle();
        }

        private void OnEnable()
        {
            analytics.Begin();
        }

        private void OnDisable()
        {
            analytics.End();
        }

        private void OnFocus()
        {
            analytics.RegisterEditorFocusIn();
        }

        private void OnLostFocus()
        {
            if (InputEditorUserSettings.autoSaveInputActionAssets && m_IsDirty)
            {
                // We'd like to avoid saving in case the focus was lost due to the drop-down window being spawned.
                // This code should be cleaned up once we migrate the InputControl stuff from ImGUI completely.
                // Since at that point it stops being a separate window that steals focus.
                // (See case ISXB-1221)
                if (!InputControlPathEditor.IsShowingDropdown)
                    Save(isAutoSave: true);
            }

            analytics.RegisterEditorFocusOut();
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
                    Save(isAutoSave: false);
                    break;
                case Dialog.Result.Cancel:
                    // Cancel editor quit. (open new editor window with the edited asset)
                    ReshowEditorWindowWithUnsavedChanges();
                    break;
                case Dialog.Result.Discard:
                    // Don't save, quit - reload the old asset from the json to prevent the asset from being dirtied
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

            m_View?.DestroyView();
        }

        private void ReshowEditorWindowWithUnsavedChanges()
        {
            var window = CreateWindow<InputActionsEditorWindow>();

            // Move/transfer ownership of m_AssetObjectForEditing to new window
            window.m_AssetObjectForEditing = m_AssetObjectForEditing;
            m_AssetObjectForEditing = null;

            // Move/transfer ownership of m_State to new window (struct)
            window.m_State = m_State;
            m_State = new InputActionsEditorState();

            // Just copy trivial arguments
            window.m_AssetGUID = m_AssetGUID;
            window.m_AssetJson = m_AssetJson;
            window.m_IsDirty = m_IsDirty;

            // Note that view and state container will get destroyed with this window instance
            // and recreated for this window below
            window.BuildUI();
            window.Show();

            // Make sure window title is up to date
            window.UpdateWindowTitle();
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
                m_State = new InputActionsEditorState(m_State, new SerializedObject(workingCopy));
                m_IsDirty = false;
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
            window.Save(isAutoSave: false);
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
