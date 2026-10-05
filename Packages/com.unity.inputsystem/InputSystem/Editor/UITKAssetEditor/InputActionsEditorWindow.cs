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
        private bool m_AutoSaveFailed;

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
            if (InputSystem.settings.IsFeatureEnabled(InputFeatureNames.kUseIMGUIEditorForAssets))
                return false;

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
            if (window.isDirty)
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

            window.isDirty = false;
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

        private static GUIContent GetEditorTitle(InputActionAsset asset)
        {
            return new GUIContent(asset.name + " (Input Actions Editor)");
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
                    {
                        // Can happen when an asset is deleted while the window is open.
                        // Delay the closure to avoid null-refs on repaint.
                        EditorApplication.delayCall += Close;
                        return;
                    }

                    m_AssetJson = InputActionsEditorWindowUtils.ToJsonWithoutName(asset);

                    if (m_AssetObjectForEditing == null)
                    {
                        workingCopy = InputActionAssetManager.CreateWorkingCopy(asset);
                        m_State = new InputActionsEditorState(m_State, new SerializedObject(workingCopy));
                        if (m_State.m_Analytics == null)
                            m_State.m_Analytics = analytics;
                        m_AssetObjectForEditing = workingCopy;
                    }
                    else
                        m_State = new InputActionsEditorState(m_State, new SerializedObject(m_AssetObjectForEditing));
                    isDirty = HasContentChanged();

                    // saveChangesMessage is not serialized, so the prompt would be blank after a domain reload.
                    UpdateWindowTitle();
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

            if (IsProjectSettingsWindowInputAsset() && InputActionsEditorSettingsProvider.IsInputActionsPageActive)
            {
                var helpBox = new HelpBox("This asset is assigned as the Project-wide Input Actions in Project Settings. Changes made here will affect input behavior across the entire project. Avoid editing this asset simultaneously in Project Settings windows.",
                    HelpBoxMessageType.Warning);
                rootVisualElement.Add(helpBox);
            }

            m_View = new InputActionsEditorView(rootVisualElement, m_StateContainer, false, () => Save(isAutoSave: false));
            m_StateContainer.Initialize(rootVisualElement.Q("action-editor"));
        }

        private bool IsProjectSettingsWindowInputAsset()
        {
            var projectWideActions = InputSystem.actions;
            if (projectWideActions == null)
                return false;
            var path = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            return path == AssetDatabase.GetAssetPath(projectWideActions);
        }

        private void OnStateChanged(InputActionsEditorState newState, UIRebuildMode editorRebuildMode)
        {
            DirtyInputActionsEditorWindow(newState);
            m_State = newState;
        }

        private void UpdateWindowTitle()
        {
            titleContent = GetEditorTitle(GetEditedAsset());
            saveChangesMessage = "Do you want to save the changes you made in:\n" +
                AssetDatabase.GUIDToAssetPath(m_AssetGUID) + "\n\nYour changes will be lost if you don't save them.";
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

            // If an auto-save did not go through (e.g. version control refused the checkout), stop relying on
            // auto-save so that closing the window prompts instead of silently dropping the changes.
            if (isAutoSave && isDirty)
            {
                m_AutoSaveFailed = true;
                UpdateUnsavedChangesState();
            }

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
            isDirty = HasContentChanged();
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
            if (InputEditorUserSettings.autoSaveInputActionAssets && isDirty)
            {
                // We'd like to avoid saving in case the focus was lost due to the drop-down window being spawned.
                // This code should be cleaned up once we migrate the InputControl stuff from ImGUI completely.
                // Since at that point it stops being a separate window that steals focus.
                // (See case ISXB-1221)
                if (!InputControlPathEditor.IsShowingDropdown && !m_View.IsControlSchemeViewActive())
                {
                    Save(isAutoSave: true);
                }
            }

            analytics.RegisterEditorFocusOut();
        }

        public override void SaveChanges()
        {
            Save(isAutoSave: false);
        }

        public override void DiscardChanges()
        {
            // Clear the dirty state so OnDestroy does not auto-save the changes the user chose to discard.
            isDirty = false;
            base.DiscardChanges();
        }

        private void OnDestroy()
        {
            // Closing the tab does not always take focus away from the window, so OnLostFocus may not have run.
            if (InputEditorUserSettings.autoSaveInputActionAssets && isDirty && m_AssetObjectForEditing != null)
                Save(isAutoSave: true);

            CleanupStateContainer();
            if (m_AssetObjectForEditing != null)
                DestroyImmediate(m_AssetObjectForEditing);

            m_View?.DestroyView();
        }

        private bool TryUpdateFromAsset()
        {
            Debug.Assert(!string.IsNullOrEmpty(m_AssetGUID), "Asset GUID is empty");
            var assetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
            if (assetPath == null)
            {
                Debug.LogWarning($"Failed to open InputActionAsset with GUID {m_AssetGUID}. The asset might have been deleted.");
                return false;
            }

            InputActionAsset workingCopy = null;
            try
            {
                var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
                workingCopy = InputActionAssetManager.CreateWorkingCopy(asset);
                m_AssetJson = InputActionsEditorWindowUtils.ToJsonWithoutName(asset);
                m_State = new InputActionsEditorState(m_State, new SerializedObject(workingCopy));
                isDirty = false;
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
        public bool isDirty
        {
            get { return m_IsDirty; }
            private set
            {
                m_IsDirty = value;
                if (!value)
                    m_AutoSaveFailed = false;
                UpdateUnsavedChangesState();
            }
        }

        private void UpdateUnsavedChangesState()
        {
            // With auto-save enabled, changes are saved on focus loss or when the window is destroyed, so there is
            // nothing to prompt for on close, unless a previous auto-save attempt failed.
            hasUnsavedChanges = m_IsDirty && (!InputEditorUserSettings.autoSaveInputActionAssets || m_AutoSaveFailed);
        }

        public void OnAssetMoved()
        {
            // When an asset is moved, we only need to update window title since content is unchanged
            UpdateWindowTitle();
        }

        public void OnAssetDeleted()
        {
            // When associated asset is deleted on disk, just close the editor, but also mark the editor
            // as not being dirty to avoid prompting the user to save changes.
            isDirty = false;
            Close();
        }

        public void OnAssetImported()
        {
            // If the editor has pending changes done by the user and the contents changes on disc, there
            // is not much we can do about it but to ignore loading the changes. If the editors asset is
            // unmodified, we can refresh the editor with the latest content from disc.
            if (isDirty)
                return;

            // If our asset has disappeared from disk, just close the window.
            var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
            if (string.IsNullOrEmpty(assetPath))
            {
                isDirty = false; // Avoid checks
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
